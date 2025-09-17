
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;

public class SoundContainer : MonoBehaviour
{

    [SerializeField] private AudioSource m_Source;
    [SerializeField] private List<AudioResource> m_Sources;

    public void Play(int index)
    {
        m_Source.resource = m_Sources[index];
        m_Source.Play();
    }
}
