//#define DEBUG_ENABLED

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Search;
using UnityEngine.Serialization;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus.Init.Internal
{
	/// <summary>
	/// Specifies the different types of cross-scene references that <see cref="CrossSceneReference"/> value providers support.
	/// </summary>
	internal enum CrossSceneReferenceType
	{
		/// <summary>
		/// <see langword="null"/> or a direct reference.
		/// </summary>
		None = 0,

		/// <summary>
		/// An component or GameObject residing in a different scene.
		/// </summary>
		Scene = 1,

		/// <summary>
		/// An component or GameObject residing in a prefab instance that will only be instantiated at runtime.
		/// </summary>
		Prefab = 2
	}
	
	/// <summary>
	/// Returns a reference to a component or game object that can located in a different scene than the client.
	/// <para>
	/// Can be used to retrieve an Init argument at runtime.
	/// </para>
	/// </summary>
	[Init(Enabled = false)]
	public sealed class CrossSceneReference : ScriptableObject<GameObject, Object>, IValueByTypeProvider, IValueByTypeProviderAsync
		#if UNITY_EDITOR
		, INullGuard
		, ISerializationCallbackReceiver
		#endif
	{
		#pragma warning disable CS0414
		/// <summary>
		/// The target instance that this reference points to, available.
		/// </summary>
		/// <remarks>
		/// Null unless the target scene is open, or target prefab is being edited in Prefab Mode.
		/// </remarks>
		[SerializeField, SearchContext("", SearchViewFlags.TableView | SearchViewFlags.Borderless)]
		internal Object target;

		[SerializeField] internal Id guid;
		[SerializeField, FormerlySerializedAs("isCrossScene")] internal CrossSceneReferenceType referenceType;
		[SerializeField] internal string targetName;
		[SerializeField, FormerlySerializedAs("sceneName")] internal string sceneOrAssetName;
		[SerializeField] internal string sceneOrAssetGuid;
		[SerializeField] internal Texture icon;
		#pragma warning restore CS0414

		public string CrossSceneId => guid.ToString();
		[Obsolete("Use `CrossSceneId` instead. This property will be removed in a future version.")]
		public string Guid => CrossSceneId;

		public Object Value => GetTarget(Context.MainThread);
		
		#if UNITY_EDITOR
		NullGuardResult INullGuard.EvaluateNullGuard([AllowNull] Component client)
		{
			if(guid == Id.Empty)
			{
				return NullGuardResult.InvalidValueProviderState;
			}
		
			var sceneOrAssetPath = AssetDatabase.GUIDToAssetPath(sceneOrAssetGuid);
			var asset = sceneOrAssetPath is { Length: > 0 } ? AssetDatabase.LoadAssetAtPath<Object>(sceneOrAssetPath) : null;

			// Check if the scene or prefab asset no longer exists in the project.
			if(!asset)
			{
				return NullGuardResult.ValueProviderValueMissing;
			}

			// Check if the IdTag that is supposed to register the target at runtime is loaded into memory and has registered itself.
			foreach(var idTag in IdTag.AllTags)
			{
				if(idTag.guid == guid)
				{
					return idTag.target ? NullGuardResult.Passed : NullGuardResult.ValueProviderValueMissing;
				}
			}

			// If the target should reside inside a prefab asset, load the prefab and examine its contents manually.
			// This is probably not necessary in most cases, but it's possible that the IdTag has not had time to
			// register itself yet, if it's OnValidate method was only executed very recently.
			if(referenceType is CrossSceneReferenceType.Prefab)
			{
				if(asset is not GameObject prefab)
				{
					return NullGuardResult.InvalidValueProviderState;
				}

				foreach(var idTag in prefab.GetComponentsInChildren<IdTag>(includeInactive: true))
				{
					if(idTag.guid == guid)
					{
						return NullGuardResult.Passed;
					}
				}

				return NullGuardResult.ValueProviderValueMissing;
			}

			// If the scene containing the target is not included in Build Settings,
			// then the target can never become available at runtime in builds.
			var buildIndex = SceneUtility.GetBuildIndexByScenePath(sceneOrAssetPath);
			if(buildIndex is -1)
			{
				return NullGuardResult.ValueProviderValueMissing;
			}

			for(int i = 0, count = SceneManager.sceneCount; i < count; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if(scene.buildIndex == buildIndex && scene.isLoaded)
				{
					return NullGuardResult.ValueProviderValueMissing;
				}
			}

			return NullGuardResult.Passed;
		}
		#endif

		public Object GetTarget(Context context = Context.MainThread)
		{
			#if !UNITY_EDITOR
			return IdTag.GetInstance(guid);
			#else
			var result = IdTag.GetInstance(guid);
			if(result)
			{
				return result;
			}

			// When the target is requested in Edit Mode, can return the source source prefab as well instead
			// of the runtime instance (which of course won't exist yet).
			if(!context.IsEditMode())
			{
				return null;
			}

			if(guid == Id.Empty)
			{
				return null;
			}

			// Check if the IdTag that is supposed to register the target at runtime is loaded into memory and has registered itself.
			foreach(var idTag in IdTag.AllTags)
			{
				if(idTag.guid == guid)
				{
					return idTag.target;
				}
			}

			// If the target should reside inside a prefab asset, load the prefab and examine its contents manually.
			// This is probably not necessary in most cases, but it's possible that the IdTag has not had time to
			// register itself yet, if it's OnValidate method was only executed very recently.
			if(referenceType is CrossSceneReferenceType.Prefab)
			{
				var prefabPath = AssetDatabase.GUIDToAssetPath(sceneOrAssetGuid);
				if(prefabPath is not { Length: > 0 } || AssetDatabase.LoadAssetAtPath<Object>(prefabPath) is not GameObject prefab)
				{
					return null;
				}

				foreach(var idTag in prefab.GetComponentsInChildren<IdTag>(includeInactive: true))
				{
					if(idTag.guid == guid)
					{
						return idTag.target;
					}
				}
			}

			return null;
			#endif
		}

		protected override void Init([MaybeNull] GameObject clientGameObject, [AllowNull] Object reference)
		{
			guid = Id.Empty;
			referenceType = CrossSceneReferenceType.None;

			#if DEBUG || INIT_ARGS_SAFE_MODE
			target = reference;
			targetName = "";
			icon = null;
			#endif

			#if UNITY_EDITOR
			sceneOrAssetName = "";
			sceneOrAssetGuid = "";
			#endif

			if(!reference)
			{
				return;
			}

			var targetGameObject = GetGameObject(reference);
			if(!targetGameObject)
			{
				return;
			}

			#if UNITY_EDITOR
			icon = EditorGUIUtility.ObjectContent(null, target.GetType()).image;
			#endif

			#if DEBUG || INIT_ARGS_SAFE_MODE
			targetName = reference.name;
			if(reference is not GameObject)
			{
				targetName += " (" + reference.GetType().Name + ")";
			}
			#endif

			var targetScene = targetGameObject.scene;
			var idTag = IdTag.GetOrCreate(reference);
			guid = idTag.guid;
			if(targetScene.IsValid())
			{
				referenceType = clientGameObject && clientGameObject.scene == targetGameObject.scene
					? CrossSceneReferenceType.None
					: CrossSceneReferenceType.Scene;
			}
			else
			{
				referenceType = clientGameObject && !clientGameObject.scene.IsValid() && clientGameObject.transform.root == targetGameObject.transform.root
					? CrossSceneReferenceType.None
					: CrossSceneReferenceType.Prefab;
			}

			#if UNITY_EDITOR
			var sceneOrAssetPath = AssetDatabase.GetAssetOrScenePath(targetGameObject);
			sceneOrAssetGuid = sceneOrAssetPath is { Length: > 0 }  ? AssetDatabase.AssetPathToGUID(sceneOrAssetPath) : "";
			var sceneOrAsset = sceneOrAssetPath is { Length: > 0 } ? AssetDatabase.LoadAssetAtPath<Object>(sceneOrAssetPath) : null;
			sceneOrAssetName = referenceType is CrossSceneReferenceType.Scene ? targetGameObject.scene.name : sceneOrAsset ? sceneOrAsset.name : "";
			#endif

			static GameObject GetGameObject(object target) => target is Component component && component ? component.gameObject : target as GameObject;
		}

		public bool TryGetFor<TValue>(Component client, out TValue value)
		{
			#if UNITY_EDITOR
			target = IdTag.GetInstance(guid);

			if(!target)
			{
				value = default;
				return false;
			}

			return Find.In(target, out value);
			#else
			var target = IdTag.GetInstance(guid);
			if(target == null)
			{
				value = default;
				return false;
			}

			return Find.In(target, out value);
			#endif
		}

		public
		#if UNITY_2023_1_OR_NEWER
		Awaitable<TValue>
		#else
		System.Threading.Tasks.Task<TValue>
		#endif
		GetForAsync<TValue>(Component client)
		{
			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log($"CrossSceneReference.GetForAsync<{typeof(TValue).Name}>({client?.GetType().Name}) called with guid {guid}...", client);
			#endif

			target = IdTag.GetInstance(guid);

			if(target)
			{
				var result = Find.In<TValue>(target);
				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log($"CrossSceneReference.GetForAsync<{typeof(TValue).Name}>({client?.GetType().Name}) sync result: {target.name}\nguid: {guid}", client);
				#endif

				#if UNITY_2023_1_OR_NEWER
				return AwaitableUtility
				#else
				return System.Threading.Tasks.Task
				#endif
					.FromResult(result);
			}

			var completionSource = new
				#if UNITY_2023_1_OR_NEWER
				AwaitableCompletionSource<TValue>();
				#else
				System.Threading.Tasks.TaskCompletionSource<TValue>();
				#endif

			IdTag.AddLoadedListener(guid, loaded =>
			{
				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log($"CrossSceneReference.GetForAsync<{typeof(TValue).Name}>({client?.GetType().Name}) async result: {loaded.name}\nGuid: {guid}", loaded);
				#endif
				completionSource.SetResult(Find.In<TValue>(loaded));
			});
			
			return completionSource
				#if UNITY_2023_1_OR_NEWER
				.Awaitable;
				#else
				.Task;
				#endif
		}

		internal static bool Detect(Object owner, Object reference)
		{
			var referenceScene = GetScene(reference);
			return referenceScene.IsValid() && GetScene(owner) != referenceScene;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static Scene GetScene(Object target) => target is Component component && component ? component.gameObject.scene : target is GameObject gameObject && gameObject ? gameObject.scene : default;
		}

		public bool HasValueFor<TValue>(Component client) => IdTag.GetInstance(guid);

		#if UNITY_EDITOR
		public bool CanProvideValue<TValue>(Component client)
		{
			if(guid == Id.Empty)
			{
				return false;
			}

			// Check if the type TValue is something that even could be attached to a GameObject.
			if(!Find.typesToFindableTypes.ContainsKey(typeof(TValue)) && typeof(TValue) != typeof(GameObject))
			{
				return false;
			}

			// Check if the IdTag that is supposed to register the target at runtime is loaded into memory and has registered itself.
			foreach(var idTag in IdTag.AllTags)
			{
				if(idTag.guid == guid)
				{
					return idTag.target;
				}
			}

			// Check if the scene or prefab asset no longer exists in the project.
			var sceneOrAssetPath = AssetDatabase.GUIDToAssetPath(sceneOrAssetGuid);
			var asset = sceneOrAssetPath is { Length: > 0 } ? AssetDatabase.LoadAssetAtPath<Object>(sceneOrAssetPath) : null;
			if(!asset)
			{
				return false;
			}

			// If the target should reside inside a prefab asset, load the prefab and examine its contents manually.
			// This is probably not necessary in most cases, but it's possible that the IdTag has not had time to
			// register itself yet, if it's OnValidate method was only executed very recently.
			if(referenceType is CrossSceneReferenceType.Prefab)
			{
				if(asset is not GameObject prefab)
				{
					return false;
				}

				foreach(var idTag in prefab.GetComponentsInChildren<IdTag>(includeInactive: true))
				{
					if(idTag.guid == guid)
					{
						return idTag.target;
					}
				}

				return false;
			}

			// If the scene containing the target is not included in Build Settings,
			// then the target can never become available at runtime in builds.
			var buildIndex = SceneUtility.GetBuildIndexByScenePath(sceneOrAssetPath);
			if(buildIndex is -1)
			{
				return false;
			}

			for(int i = 0, count = SceneManager.sceneCount; i < count; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if(scene.buildIndex == buildIndex && scene.isLoaded)
				{
					return false;
				}
			}

			return true;
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize() { }
		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			if(referenceType is CrossSceneReferenceType.Scene) target = null; // Avoid warnings from Unity's serialization system about serialized cross-scene references
		}
		#endif
	}
}