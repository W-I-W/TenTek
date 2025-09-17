using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

public class Inventory : MonoBehaviour
{
    //[SerializeField] private InputSystem m_Input;
    //[SerializeField] private Slot m_Slot;

    //[SerializeField] private UnityEvent m_OnDrag;
    //[SerializeField] private UnityEvent m_OnDrop;

    //private Item m_ItemHand;
    //private List<Item> m_ItemFocus;
    //private bool m_IsLock = false;

    //public bool isLock { get => m_IsLock; set => m_IsLock = value; }

    //private void OnEnable()
    //{
    //    m_Input.onTake += OnTake;
    //    m_ItemFocus = new List<Item>();
    //}

    //private void OnDisable()
    //{
    //    m_Input.onTake -= OnTake;
    //}

    //private void OnTriggerEnter2D(Collider2D collision)
    //{
    //    bool isItem = collision.TryGetComponent(out Item item);
    //    if (!isItem) return;
    //    if (!item.enabled) return;
    //    m_ItemFocus.Add(item);

    //}

    //private void OnTriggerExit2D(Collider2D collision)
    //{
    //    bool isItem = collision.TryGetComponent(out Item item);
    //    if (!isItem) return;
    //    if (!item.enabled) return;
    //    m_ItemFocus.Remove(item);
    //}

    //private void OnTake()
    //{
    //    if (m_IsLock) return;
    //    if (m_ItemHand == null && m_ItemFocus.Count > 0)
    //    {
    //        m_OnDrag?.Invoke();
    //        m_ItemHand = m_ItemFocus[m_ItemFocus.Count - 1];
    //        m_ItemHand.OnDrag();
    //        m_ItemHand.transform.parent = m_Slot.transform;
    //        m_ItemHand.transform.localRotation = Quaternion.identity;
    //        m_ItemHand.transform.localPosition = Vector2.zero;
    //    }
    //    else if (m_ItemHand)
    //    {
    //        m_OnDrop?.Invoke();
    //        m_ItemHand.OnDrop();
    //        m_ItemHand = null;
    //    }
    //}
}
