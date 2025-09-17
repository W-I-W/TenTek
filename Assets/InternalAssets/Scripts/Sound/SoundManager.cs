using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private Slider m_SliderMusic;

    private void Start()
    {
        Application.targetFrameRate = 60;
    }

    private void OnEnable()
    {
        m_SliderMusic.value = ES3.Load("MusicValue", 1f);
    }

    private void OnDisable()
    {
        ES3.Save("MusicValue", m_SliderMusic.value);
    }
}
