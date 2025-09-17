// Designed by KINEMATION, 2025.

using System;
using KINEMATION.KAnimationCore.Runtime.Rig;

using System.Collections.Generic;

using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Experimental.Animations;
using UnityEngine.Playables;

namespace KINEMATION.MagicBlend.Runtime
{
    public struct MagicBlendingData
    {
        public MagicBlendAsset blendAsset;
        public AnimationClip overlayPose;
        public float overlaySpeed;
        public float blendTime;
        public bool useLinear;

        public static MagicBlendingData Empty = new MagicBlendingData()
        {
            overlayPose = null,
            blendTime = -1f,
            useLinear = false,
        };
    }
    
    [HelpURL("https://kinemation.gitbook.io/magic-blend-documentation/")]
    public class MagicBlending : MonoBehaviour
    {
        public PlayableGraph playableGraph;
        public MagicBlendAsset BlendAsset => blendAsset;
        
        [Tooltip("This asset controls the blending weights.")]
        [SerializeField] private MagicBlendAsset blendAsset;
        
        [Tooltip("Will update weights every frame.")]
        [SerializeField] private bool forceUpdateWeights = true;
        [Tooltip("Will process the Overlay pose. Keep it on most of the time.")]
        [SerializeField] private bool alwaysAnimatePoses = true;

        private const ushort PlayableSortingPriority = 900;
        
        private Animator _animator;
        private KRigComponent _rigComponent;

        private AnimationLayerMixerPlayable _playableMixer;
        private NativeArray<BlendStreamAtom> _atoms;
        
        private PoseJob _poseJob;
        private OverlayJob _overlayJob;
        private LayeringJob _layeringJob;

        private AnimationScriptPlayable _poseJobPlayable;
        private AnimationScriptPlayable _overlayJobPlayable;
        private AnimationScriptPlayable _layeringJobPlayable;

        private bool _isInitialized;
        private float _blendPlayback = 1f;
        private float _blendTime = 0f;
        private AnimationCurve _blendCurve;
        
        private MagicBlendingData _blendData;
        private MagicBlendingData _desiredBlendData;
        private MagicBlendAsset _previousBlendAsset;
        private List<int> _blendedIndexes = new List<int>();

        private Dictionary<string, int> _hierarchyMap;
        private RuntimeAnimatorController _cachedController;
        private AnimationPlayableOutput _magicBlendOutput;
        
        private bool _forceBlendOut;
        private bool _wasAnimatorActive;

        public void UpdateMagicBlendAsset(MagicBlendAsset newAsset)
        {
            UpdateMagicBlendAsset(newAsset, MagicBlendingData.Empty);
        }

        public void UpdateMagicBlendAsset(MagicBlendAsset newAsset, MagicBlendingData blendingData, 
            bool ignoreBlending = false)
        {
            if (newAsset == null || !_isInitialized)
            {
                return;
            }

            _desiredBlendData = blendingData;
            _desiredBlendData.blendAsset = newAsset;

            bool useBlending = blendingData.blendTime > 0f || newAsset.blendTime > 0f;
            if (useBlending && !ignoreBlending)
            {
                _layeringJob.cachePose = true;
                _layeringJobPlayable.SetJobData(_layeringJob);
                return;
            }
            
            _layeringJob.blendWeight = 1f;
            _layeringJobPlayable.SetJobData(_layeringJob);

            SetNewAsset();

            if (!alwaysAnimatePoses)
            {
                _poseJob.readPose = true;
                _overlayJob.cachePose = true;

                _poseJobPlayable.SetJobData(_poseJob);
                _overlayJobPlayable.SetJobData(_overlayJob);
            }
        }
        
        /// <summary>
        /// Sets a new blending asset.
        /// </summary>
        /// <param name="newAsset">Blending asset.</param>
        /// <param name="overlayOverride">Override clip.</param>>
        /// <param name="useBlending">Whether we need blending.</param>
        /// <param name="blendTime">Blending time in seconds.</param>
        /// <param name="useCurve">Whether we need curve or linear transition.</param>
        [Obsolete("Use the alternative override!")]
        public void UpdateMagicBlendAsset(MagicBlendAsset newAsset, bool useBlending = true, 
            float blendTime = -1f, bool useCurve = true, AnimationClip overlayOverride = null)
        {
            UpdateMagicBlendAsset(newAsset, new MagicBlendingData()
            {
                blendTime = blendTime,
                useLinear = !useCurve,
                overlayPose = overlayOverride
            });
        }

        public void StopMagicBlending()
        {
            _forceBlendOut = true;
            _blendPlayback = 0f;
        }

        public void SetOverlayTime(float newTime)
        {
            var overlayPlayable = _overlayJobPlayable.GetInput(0);
            if (!overlayPlayable.IsValid()) return;
            overlayPlayable.SetTime(newTime);
        }

        public float GetOverlayTime(bool isNormalized = true)
        {
            var overlayPlayable = _overlayJobPlayable.GetInput(0);
            if (!overlayPlayable.IsValid() || !blendAsset.isAnimation)
            {
                return 0f;
            }

            float length = (float) overlayPlayable.GetDuration();
            if (Mathf.Approximately(length, 0f))
            {
                return 0f;
            }

            float time = (float) overlayPlayable.GetTime();
            return isNormalized ? Mathf.Clamp01(time / length) : time;
        }

        protected virtual void SetProcessJobs(bool isActive)
        {
            _poseJobPlayable.SetProcessInputs(isActive);
            _overlayJobPlayable.SetProcessInputs(isActive);
        }

        protected virtual void SetNewAsset()
        {
            _blendData = _desiredBlendData;

            if (blendAsset != null && blendAsset.blendOutType == MagicBlendOutType.DoNotBlendOut)
            {
                _previousBlendAsset = blendAsset;
            }
            
            blendAsset = _blendData.blendAsset;
            if (_previousBlendAsset == null && blendAsset.blendOutType == MagicBlendOutType.DoNotBlendOut)
            {
                _previousBlendAsset = blendAsset;
            }

            if (_blendData.overlayPose == null) _blendData.overlayPose = blendAsset.overlayPose;
            _blendCurve = _blendData.useLinear ? null : blendAsset.blendCurve;
            _blendTime = _blendData.blendTime > 0f ? _blendData.blendTime : blendAsset.blendTime;
            
            MagicBlendLibrary.ConnectPose(_poseJobPlayable, playableGraph, blendAsset.basePose);

            float overlaySpeed = Mathf.Approximately(_blendData.overlaySpeed, 0f)
                ? blendAsset.overlaySpeed
                : _blendData.overlaySpeed;
            
            float speed = blendAsset.isAnimation ? overlaySpeed : 0f;
            if (blendAsset.HasOverrides())
            {
                MagicBlendLibrary.ConnectOverlays(_overlayJobPlayable, playableGraph, _blendData.overlayPose, 
                    blendAsset.overrideOverlays, speed);
            }
            else
            {
                MagicBlendLibrary.ConnectPose(_overlayJobPlayable, playableGraph, _blendData.overlayPose, speed);
            }
            
            // Reset all weights.
            for (int i = 0; i < _hierarchyMap.Count; i++)
            {
                var atom = _atoms[i];
                atom.baseWeight = atom.additiveWeight = atom.localWeight = 0f;
                _atoms[i] = atom;
            }
            
            // Add indexes which will be blended.
            _blendedIndexes.Clear();
            foreach (var blend in blendAsset.layeredBlends)
            {
                foreach (var element in blend.layer.elementChain)
                {
                    _hierarchyMap.TryGetValue(element.name, out int index);
                    _blendedIndexes.Add(index);
                }
            }
            
            // Active the jobs processing.
            SetProcessJobs(true);
            _forceBlendOut = false;
            
            // Update weights.
            UpdateBlendWeights();
        }

        protected virtual void BuildMagicMixer()
        {
            if (!_playableMixer.IsValid())
            {
                _playableMixer = AnimationLayerMixerPlayable.Create(playableGraph, 3);
                InitializeJobs();
                _playableMixer.ConnectInput(0, _poseJobPlayable, 0, 1f);
                _playableMixer.ConnectInput(1, _overlayJobPlayable, 0, 1f);
                _playableMixer.ConnectInput(2, _layeringJobPlayable, 0, 1f);
            }
            
            _magicBlendOutput.SetSourcePlayable(_playableMixer);
            _magicBlendOutput.SetSortingOrder(PlayableSortingPriority);

            int num = playableGraph.GetOutputCount();
            int animatorPlayableIndex = 0;

            for (int i = 0; i < num; i++)
            {
                var sourcePlayable = playableGraph.GetOutput(i).GetSourcePlayable();
                if (sourcePlayable.GetPlayableType() != typeof(AnimatorControllerPlayable))
                {
                    continue;
                }
                
                animatorPlayableIndex = i;
            }

            var animatorOutput = playableGraph.GetOutput(animatorPlayableIndex);
            var animatorPlayable = animatorOutput.GetSourcePlayable();
            
            if (_layeringJobPlayable.IsValid())
            {
                _layeringJobPlayable.DisconnectInput(0);
            }
            
            _layeringJobPlayable.ConnectInput(0, animatorPlayable, 0, 1f);
            
            if (blendAsset != null)
            {
                UpdateMagicBlendAsset(blendAsset, new MagicBlendingData()
                {
                    blendTime = -1f,
                });
            }
        }
        
        protected virtual void InitializeMagicBlending()
        {
            playableGraph = _animator.playableGraph;
            _atoms = MagicBlendLibrary.SetupBlendAtoms(_animator, _rigComponent);
            
            _magicBlendOutput = AnimationPlayableOutput.Create(playableGraph, "MagicBlendOutput", _animator);
            _isInitialized = true;
            BuildMagicMixer();
            
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            playableGraph.Play();
        }
        
        private void InitializeJobs()
        {
            var rootSceneHandle = _animator.BindSceneTransform(_animator.transform);
            
            _poseJob = new PoseJob()
            {
                atoms = _atoms,
                root = rootSceneHandle,
                alwaysAnimate = alwaysAnimatePoses,
                readPose = false
            };
            _poseJobPlayable = AnimationScriptPlayable.Create(playableGraph, _poseJob, 1);

            _overlayJob = new OverlayJob()
            {
                atoms = _atoms,
                root = rootSceneHandle,
                alwaysAnimate = alwaysAnimatePoses,
                cachePose = false
            };
            _overlayJobPlayable = AnimationScriptPlayable.Create(playableGraph, _overlayJob, 1);
            
            _layeringJob = new LayeringJob()
            {
                atoms = _atoms,
                root = rootSceneHandle,
                blendWeight = 1f,
                cachePose = false,
            };
            _layeringJobPlayable = AnimationScriptPlayable.Create(playableGraph, _layeringJob, 1);
        }

        private void OnEnable()
        {
            if(_isInitialized) BuildMagicMixer();
        }

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _cachedController = _animator.runtimeAnimatorController;
            _wasAnimatorActive = _animator.isActiveAndEnabled;
            
            _rigComponent = GetComponentInChildren<KRigComponent>();
            _hierarchyMap = new Dictionary<string, int>();

            var hierarchy = _rigComponent.GetRigTransforms();
            for (int i = 0; i < hierarchy.Length; i++)
            {
                _hierarchyMap.Add(hierarchy[i].name, i);
            }
            
            InitializeMagicBlending();

#if UNITY_EDITOR
            _cachedBlendAsset = blendAsset;
#endif
        }

        protected virtual void UpdateBlendWeights(float globalWeight = 1f)
        {
            int index = 0;

            foreach (var blend in blendAsset.layeredBlends)
            {
                foreach (var unused in blend.layer.elementChain)
                {
                    int realIndex = _blendedIndexes[index];
                    
                    var atom = _atoms[realIndex];
                    atom.baseWeight = blend.baseWeight * blendAsset.globalWeight * globalWeight;
                    atom.additiveWeight = blend.additiveWeight * blendAsset.globalWeight * globalWeight;
                    atom.localWeight = blend.localWeight * blendAsset.globalWeight * globalWeight;
                    _atoms[realIndex] = atom;
                    
                    index++;
                }
            }

            var overlayPlayable = _overlayJobPlayable.GetInput(0);
            int count = overlayPlayable.GetInputCount();
            if (count == 0) return;

            for (int i = 1; i < count; i++)
            {
                overlayPlayable.SetInputWeight(i, blendAsset.overrideOverlays[i - 1].weight);
            }
        }

        protected virtual void Update()
        {
            if (_blendData.blendAsset == null || blendAsset == null) return;
            
            var activeAnimator = _animator.runtimeAnimatorController;

            if (_cachedController != activeAnimator || _wasAnimatorActive != _animator.isActiveAndEnabled)
            {
                BuildMagicMixer();
            }
            
            _cachedController = activeAnimator;
            _wasAnimatorActive = _animator.isActiveAndEnabled;

            if (blendAsset.isAnimation)
            {
                int count = _overlayJobPlayable.GetInput(0).GetInputCount();
                
                var overlayPlayable = count == 0 ? _overlayJobPlayable.GetInput(0) 
                    : _overlayJobPlayable.GetInput(0).GetInput(0);
                
                if (overlayPlayable.GetTime() >= _blendData.overlayPose.length)
                {
                    if (blendAsset.blendOutType > MagicBlendOutType.DoNotBlendOut)
                    {
                        if ((blendAsset.blendOutType == MagicBlendOutType.AutoBlendOut 
                            || _previousBlendAsset == null))
                        {
                            if(!_forceBlendOut) StopMagicBlending();
                        }
                        else
                        {
                            UpdateMagicBlendAsset(_previousBlendAsset);
                        }
                    }
                    else if(_blendData.overlayPose.isLooping)
                    {
                        overlayPlayable.SetTime(0f);
                    }
                }

                if (count > 1)
                {
                    for (int i = 1; i < count; i++)
                    {
                        var overrideOverlay = _overlayJobPlayable.GetInput(0).GetInput(i);
                        var overrideClip = blendAsset.overrideOverlays[i - 1].overlay;

                        if (!overrideClip.isLooping || overrideOverlay.GetTime() < overrideClip.length 
                                                    || blendAsset.blendOutType > MagicBlendOutType.DoNotBlendOut)
                        {
                            continue;
                        }
                        
                        overrideOverlay.SetTime(0f);
                    }
                }
            }

            float globalWeight = 1f;
            if (_forceBlendOut)
            {
                _blendPlayback = Mathf.Clamp(_blendPlayback + Time.deltaTime, 0f, _blendTime);
                float blendOutWeight = _blendPlayback / _blendTime;
                globalWeight = 1f - (_blendCurve?.Evaluate(blendOutWeight) ?? blendOutWeight);
            }
            
            if (forceUpdateWeights || _forceBlendOut)
            {
                UpdateBlendWeights(globalWeight);
            }

            if (Mathf.Approximately(globalWeight, 0f))
            {
                SetProcessJobs(false);
                blendAsset = null;
#if UNITY_EDITOR
                _cachedBlendAsset = null;
#endif
            }
            
            if (_forceBlendOut || Mathf.Approximately(_blendPlayback, _blendTime)) return;
            
            _blendPlayback = Mathf.Clamp(_blendPlayback + Time.deltaTime, 0f, _blendTime);
            float normalizedWeight = _blendTime > 0f ? _blendPlayback / _blendTime : 1f;

            _layeringJob.blendWeight = _blendCurve?.Evaluate(normalizedWeight) ?? normalizedWeight;
            _layeringJobPlayable.SetJobData(_layeringJob);
        }

        protected virtual void LateUpdate()
        {
            if (!alwaysAnimatePoses && _poseJob.readPose)
            {
                _poseJob.readPose = false;
                _overlayJob.cachePose = false;
            
                _poseJobPlayable.SetJobData(_poseJob);
                _overlayJobPlayable.SetJobData(_overlayJob);
            }
            
            if (_layeringJob.cachePose)
            {
                SetNewAsset();
                
                _blendPlayback = 0f;
                
                _layeringJob.cachePose = false;
                _layeringJob.blendWeight = 0f;
                _layeringJobPlayable.SetJobData(_layeringJob);
                
                if (!alwaysAnimatePoses)
                {
                    _poseJob.readPose = true;
                    _overlayJob.cachePose = true;
            
                    _poseJobPlayable.SetJobData(_poseJob);
                    _overlayJobPlayable.SetJobData(_overlayJob);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            if (playableGraph.IsValid() && playableGraph.IsPlaying())
            {
                playableGraph.Stop();
            }

            if (_atoms.IsCreated)
            {
                _atoms.Dispose();
            }
        }

        public void SetMagicBlendAsset(MagicBlendAsset newAsset)
        {
            blendAsset = newAsset;
        }
        
#if UNITY_EDITOR
        private MagicBlendAsset _cachedBlendAsset;
        
        private void OnValidate()
        {
            if (!_isInitialized)
            {
                return;
            }

            _poseJob.alwaysAnimate = alwaysAnimatePoses;
            _overlayJob.alwaysAnimate = alwaysAnimatePoses;
            
            _poseJobPlayable.SetJobData(_poseJob);
            _overlayJobPlayable.SetJobData(_overlayJob);

            if (_cachedBlendAsset == blendAsset)
            {
                return;
            }
            
            UpdateMagicBlendAsset(blendAsset);
            (_cachedBlendAsset, blendAsset) = (blendAsset, _cachedBlendAsset);
        }
#endif
    }
}