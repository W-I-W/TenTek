using UnityEngine;
using UnityEngine.Events;

public class MonoEvent : MonoBehaviour
{
    [SerializeField] private UnityEvent m_OnEnable;
    [SerializeField] private UnityEvent m_OnDisable;


    private void OnEnable()
    {
        m_OnEnable?.Invoke();
    }

    private void OnDisable()
    {
        m_OnDisable?.Invoke();
    }
}
