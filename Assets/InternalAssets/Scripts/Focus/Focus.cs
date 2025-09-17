
using DG.Tweening;

using UnityEngine;
using UnityEngine.Events;

public class Focus : MonoBehaviour
{
    [SerializeField] private GameObject m_Object;
    [SerializeField] private float m_Duration = 1;
    [SerializeField] private float m_DurationRotation = 1;
    [SerializeField] private Ease m_Ease;
    [SerializeField] private Ease m_EaseRotation;

    [SerializeField] private UnityEvent m_OnEnable;
    [SerializeField] private UnityEvent m_OnDisable;

    private Tween m_TweenColor;
    private Tween m_TweenRotation;

    private void Start()
    {
        m_Object.SetActive(false);
    }



    private void OnTriggerEnter(Collider other)
    {
        bool isPlayer = other.TryGetComponent(out Inventory palyer);
        if (!isPlayer) return;
        m_OnEnable?.Invoke();
        m_Object.SetActive(true);
        Kill();
    }

    private void OnTriggerExit(Collider other)
    {
        bool isPlayer = other.TryGetComponent(out Inventory palyer);
        if (!isPlayer) return;
        m_OnDisable?.Invoke();
        m_Object.SetActive(false);
    }

    private void OnDestroy()
    {
        Kill();
    }

    private void Kill()
    {
        if (m_Object != null)
            DOTween.Kill(m_Object);
        if (m_TweenColor != null)
            DOTween.Kill(m_TweenColor);
        if (m_TweenRotation != null)
            DOTween.Kill(m_TweenRotation);
    }
}
