using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Events;

public class Load : MonoBehaviour
{
    [SerializeField] private Save m_Save;
    [SerializeField] private UnityEvent<Sprite> m_OnSprite;
    [SerializeField] private UnityEvent<ItemVideo> m_OnItem;

    private void Start()
    {
        m_Save.indexsPhoto = ES3.Load("photo_indexs", new HashSet<int>());
        m_Save.indexsVideo = ES3.Load("video_indexs", new HashSet<int>());
        Debug.Log(m_Save.indexsPhoto.Count);
        for (int i = 0; i < m_Save.indexsPhoto.Count; i++)
        {
            int index = m_Save.indexsPhoto.ElementAt(i);
            m_OnSprite?.Invoke(m_Save[index]);
            m_Save.SetActivePhoto(index, false);
        }

        for (int i = 0; i < m_Save.indexsVideo.Count; i++)
        {
            int index = m_Save.indexsVideo.ElementAt(i);
            ItemVideo item = m_Save.GetItem(index);
            m_OnItem?.Invoke(item);
            item.gameObject.SetActive(false);
        }
        //OpenAll();
    }

    private void OpenAll()
    {
        Debug.Log(m_Save.count+ m_Save.countVideo);
        for (int i = 0; i < m_Save.count; i++)
        {
            m_OnSprite?.Invoke(m_Save[i]);
            m_Save.SetActivePhoto(i, true);
        }
        for (int i = 0; i < m_Save.countVideo; i++)
        {
            ItemVideo item = m_Save.GetItem(i);
            m_OnItem?.Invoke(item);
            item.gameObject.SetActive(true);
        }
    }
}
