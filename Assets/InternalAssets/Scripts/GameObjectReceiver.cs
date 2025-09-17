using UnityEngine;

public class GameObjectReceiver : MonoBehaviour
{
    [SerializeField] private GameObject m_Object;

    public void ReceiverObject()
    {
        m_Object.SetActive(!m_Object.activeSelf);
    }
}
