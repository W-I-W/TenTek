using UnityEngine;
using UnityEngine.Events;

public class InputKeyEvent : MonoBehaviour
{
    [SerializeField] private KeyCode m_Key;
    [SerializeField] private UnityEvent<bool> m_Event;

    private bool m_IsPressed = false;

    private void Update()
    {
        if (Input.GetKeyDown(m_Key))
        {
            m_IsPressed = !m_IsPressed;
            m_Event?.Invoke(m_IsPressed);
        }
    }
}
