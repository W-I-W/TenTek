using UnityEngine;
using UnityEngine.Events;

public class Receiver : MonoBehaviour
{
    [SerializeField] private Item m_ItemFocus;
    [SerializeField] private UnityEvent<Item> m_OnItemEnter;
    [SerializeField] private UnityEvent<Item> m_OnItemExit;

    private void OnTriggerEnter(Collider other)
    {
        bool isItem = other.TryGetComponent(out Item item);
        if (!isItem) return;
        if (m_ItemFocus == null)
        {
            m_OnItemEnter?.Invoke(item);
            item.OnReset();
            return;
        }
        if (item != m_ItemFocus) return;
        m_OnItemEnter?.Invoke(item);
        item.gameObject.SetActive(false);
        //item.OnReset();
    }

    //private void OnTriggerExit2D(Collider2D collision)
    //{
    //    bool isItem = collision.TryGetComponent(out Item item);
    //    if (!isItem) return;
    //    m_OnItemExit?.Invoke(item);
    //}
}
