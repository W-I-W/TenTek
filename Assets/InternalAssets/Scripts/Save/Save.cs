
using System.Collections.Generic;

using Bow;

using UnityEngine;


public class Save : MonoBehaviour
{
    [SerializeField] private List<EventTrigger> m_DataPhoto;
    [SerializeField] private List<ItemVideo> m_DataVideo;

    private HashSet<int> m_Indexs = new HashSet<int>();
    private HashSet<int> m_IndexsVideo = new HashSet<int>();

    public int count => m_DataPhoto.Count;

    public int countVideo => m_DataVideo.Count;

    public HashSet<int> indexsPhoto
    {
        get => m_Indexs;
        set => m_Indexs = value;
    }

    public HashSet<int> indexsVideo
    {
        get => m_IndexsVideo;
        set => m_IndexsVideo = value;
    }

    public Sprite this[int index] => m_DataPhoto[index > count - 1 ? count - 1 : index < 0 ? 0 : index].sprite;


    public ItemVideo GetItem(int index)
    {
        index = index < 0 ? 0 : index > countVideo - 1 ? countVideo - 1 : index;
        return m_DataVideo[index];
    }

    public void SavePhoto(Sprite sprite)
    {
        for (int i = 0; i < m_DataPhoto.Count; i++)
        {
            if (m_DataPhoto[i].sprite == sprite)
            {
                m_Indexs.Add(i);
                ES3.Save("photo_indexs", m_Indexs);
                return;
            }
        }
    }

    public void SaveVideo(ItemVideo item)
    {
        for (int i = 0; i < countVideo; i++)
        {
            if (m_DataVideo[i] == item)
            {
                m_IndexsVideo.Add(i);
                ES3.Save("video_indexs", m_IndexsVideo);
                return;
            }
        }
    }

    public void SetActivePhoto(int index, bool isValue)
    {
        m_DataPhoto[index].gameObject.SetActive(isValue);
    }

    public void SetActiveVideo(int index, bool isValue)
    {
        m_DataVideo[index].gameObject.SetActive(isValue);
    }
}

