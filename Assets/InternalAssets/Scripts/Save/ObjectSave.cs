using System.Collections.Generic;

using UnityEngine;

public class ObjectSave : MonoBehaviour
{
    [SerializeField] private List<GameObject> m_Objects;

    private void OnEnable()
    {
        for (int i = 0; i < m_Objects.Count; i++)
        {
            bool isDefaultActive = m_Objects[i].gameObject.activeSelf;
            bool isActive = ES3.Load(m_Objects[i].name, isDefaultActive);
            m_Objects[i].gameObject.SetActive(isActive);
        }
    }

    public void OnSave()
    {
        for (int i = 0; i < m_Objects.Count; i++)
        {
            bool isDefaultActive = m_Objects[i].gameObject.activeSelf;
            ES3.Save(m_Objects[i].name, isDefaultActive);
        }
    }
}
