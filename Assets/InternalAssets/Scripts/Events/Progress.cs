using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class Progress : MonoBehaviour
{
    [SerializeField] private List<string> m_Items;
    [SerializeField] private VideoController m_Video;
    [SerializeField] private PhotoController m_Photo;

    [SerializeField] private UnityEvent<float> m_OnUpdateProgress;
    [SerializeField] private UnityEvent<string> m_OnObjectsMax;
    [SerializeField] private UnityEvent<string> m_OnCurrentObject;

    private HashSet<string> m_CurrentObjects;


    private void OnEnable()
    {
        m_CurrentObjects = new HashSet<string>();
        m_Video.onItem += UpdateProgress;
        m_Photo.onItem += UpdateProgress;
        m_OnObjectsMax?.Invoke(m_Items.Count.ToString());
        m_OnCurrentObject?.Invoke(m_CurrentObjects.Count.ToString());
        m_OnUpdateProgress?.Invoke((float)m_CurrentObjects.Count / m_Items.Count);
    }

    private void OnDisable()
    {
        m_Video.onItem -= UpdateProgress;
        m_Photo.onItem -= UpdateProgress;
    }

    public void UpdateProgress(string id)
    {
        m_CurrentObjects.Add(id);
        m_OnUpdateProgress?.Invoke((float)m_CurrentObjects.Count / m_Items.Count);
        m_OnCurrentObject?.Invoke(m_CurrentObjects.Count.ToString());
    }

    public void Load(Sprite sprite)
    {
        UpdateProgress(sprite.name);
    }

    public void Load(ItemVideo item)
    {
        UpdateProgress(item.clip.name);
    }
}
