using DG.Tweening;

using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.Events;

public class Item : MonoBehaviour
{
    [SerializeField] private Transform m_Parent;
    [SerializeField] private Focus m_Focus;
    [SerializeField] private Rigidbody m_Body;
    [SerializeField] private Collider m_Collider;
    [SerializeField] private UnityEvent m_OnDrag;
    [SerializeField] private UnityEvent m_OnDrop;

    private Tween m_TweenDrop;


    private void OnDestroy()
    {
        DOTween.Kill(m_TweenDrop);
    }

    public void OnDeactivate()
    {
        m_Parent.gameObject.SetActive(false);
    }

    public virtual void OnReset()
    {
        transform.parent = m_Parent;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        m_Body.isKinematic = false;
        m_Body.useGravity = true;
        m_Body.linearVelocity = Vector3.zero;
        m_Focus.gameObject.SetActive(true);
        m_Collider.enabled = true;
    }

    public virtual void OnDrop()
    {
        m_Body.isKinematic = false;
        m_Body.useGravity = true;
        transform.parent = null;
        m_Collider.enabled = true;
        m_TweenDrop = DOVirtual.DelayedCall(0.4f, () =>
        {
            OnReset();
            m_OnDrop?.Invoke();
        });
    }

    public virtual void OnDrag()
    {
        m_Focus.gameObject.SetActive(false);
        m_Body.isKinematic = true;
        m_Body.useGravity = false;
        m_Collider.enabled = false;
        m_OnDrag?.Invoke();
    }
}
