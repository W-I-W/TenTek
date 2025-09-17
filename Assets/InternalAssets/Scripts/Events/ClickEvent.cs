using UnityEngine;
using UnityEngine.Events;

public class ClickEvent : MonoBehaviour, IClickable
{

    [SerializeField] private UnityEvent m_OnClick;
    public void OnClick()
    {
        m_OnClick?.Invoke();
    }
}
