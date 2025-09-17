using UnityEngine;

public class MonoCursor : MonoBehaviour
{
    private void OnEnable()
    {
        CursorData.Add(gameObject);
        CursorData.OnActivate(gameObject);
    }

    private void OnDisable()
    {
        CursorData.OnDeactivate(gameObject);
    }
}
