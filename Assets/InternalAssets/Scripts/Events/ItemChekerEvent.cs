using UnityEngine;
using UnityEngine.Events;

public class ItemChekerEvent : MonoBehaviour
{
    [SerializeField] private Item m_Item;
    [SerializeField] private UnityEvent m_OnCheck;


    public virtual void OnCheck(Item item)
    {
        if(m_Item==item)
        {
            m_OnCheck?.Invoke();
            Debug.Log($"Item {item.name} checked.");
        }
    }
}
