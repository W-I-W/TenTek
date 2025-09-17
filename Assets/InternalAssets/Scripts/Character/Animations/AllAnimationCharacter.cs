using System;
using System.Collections.Generic;

using UnityEngine;

public class AllAnimationCharacter : MonoBehaviour
{
    
    [SerializeField] private Animator m_AnimatorFemale;
    [SerializeField] private Animator m_AnimatorMale;
    [Range(0,29)]
    [SerializeField] private int m_AnimIndex = 0;

    [SerializeField] private List<AnimationGroup> m_Animations;


    private void OnEnable()
    {
        //m_OverrideController = new AnimatorOverrideController(m_AnimatorFemale.runtimeAnimatorController);
        //m_OverrideController["Female"] = m_Animations[m_AnimIndex].Female;
        m_AnimatorFemale.runtimeAnimatorController = m_Animations[m_AnimIndex].Female;
        m_AnimatorMale.runtimeAnimatorController = m_Animations[m_AnimIndex].Male;
    }
}

[Serializable]
public struct AnimationGroup
{
    public RuntimeAnimatorController Female;
    public RuntimeAnimatorController Male;
}
