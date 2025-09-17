using System.Net.NetworkInformation;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class StartVideoController : MonoBehaviour
{
    [SerializeField] private VideoPlayer m_Player;
    [SerializeField] private UnityEvent m_OnEnd;
    [SerializeField] private UnityEvent m_OnStart;


    private void Start()
    {
        m_OnStart?.Invoke();
    }

    private void OnEnable()
    {
        m_Player.loopPointReached += OnEndVideo;
    }

    private void OnDisable()
    {
        m_Player.loopPointReached -= OnEndVideo;
    }

    private void OnEndVideo(VideoPlayer value)
    {
        m_OnEnd?.Invoke();
    }
}
