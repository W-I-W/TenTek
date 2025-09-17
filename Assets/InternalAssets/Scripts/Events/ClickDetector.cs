using UnityEngine;

public class ClickDetector : MonoBehaviour             
{
    private Camera m_Camera;

    private void Start()
    {
        m_Camera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = m_Camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit  hit))
            {
                IClickable trigger = hit.collider.GetComponent<IClickable>();
                if (trigger != null)
                {
                    trigger.OnClick();
                }
            }
        }
    }
}
