using System;
using System.Collections.Generic;

using UnityEngine;

public class SelectorCharacter : MonoBehaviour
{
    [SerializeField] private Animator m_Animator;
    [SerializeField] private Transform m_Genetal;
    [Range(0, 6)]
    [SerializeField] private int m_Index = 0;
    [SerializeField] private List<AvatarCharacter> m_Character;

    private void OnEnable()
    {
        m_Character.ForEach(_ => _.Character.SetActive(false));
        m_Animator.avatar = m_Character[m_Index].Avatar;
        m_Genetal.parent = m_Character[m_Index].Genital.transform;
        m_Genetal.localPosition = Vector3.zero;
        m_Genetal.localRotation = Quaternion.identity;
        m_Character[m_Index].Character.SetActive(true);
    }
}
[Serializable]
public struct AvatarCharacter
{
    public GameObject Character;
    public Avatar Avatar;
    public Transform Genital;
}
