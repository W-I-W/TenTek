using System.Collections.Generic;

using BoingKit;

using UnityEngine;
using UnityEngine.Events;

public class ReceiverVideoAll : MonoBehaviour
{
    [SerializeField] private LayerMask m_Mask;
    //[SerializeField] protected List<Item> m_ItemFocus;
    [SerializeField] protected UnityEvent<Item> m_OnItemEnter;
    [SerializeField] protected UnityEvent<Item> m_OnItemExit;
    [SerializeField] private UnityEvent m_OnCharacterExit;


    private void OnTriggerEnter(Collider other)
    {
        bool isItem = other.TryGetComponent(out ItemVideo item);
        if (!isItem) return;
        if (!item.enabled) return;
        m_OnItemEnter?.Invoke(item);
        item.gameObject.SetActive(false);
    }
}
