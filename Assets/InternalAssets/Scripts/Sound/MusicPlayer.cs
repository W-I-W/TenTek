using System.Collections.Generic;

using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource m_Source;
    [SerializeField] private List<AudioClip> m_Clips;
    [SerializeField] private int m_Index = 0;

    private void Update()
    {
        OnNext();
    }

    private void OnNext()
    {
        if (!m_Source.isPlaying)
        {
            m_Index = m_Index + 1 < m_Clips.Count ? m_Index + 1 : 0;
            m_Source.clip = m_Clips[m_Index];
            m_Source.Play();
        }
    }
}
