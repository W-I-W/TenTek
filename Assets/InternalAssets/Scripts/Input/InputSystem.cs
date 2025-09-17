using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputSystem : MonoBehaviour
{
    public Vector2 move { get; private set; }

    public bool isMove { get; private set; }

    public UnityAction onTake { get; set; }

    public UnityAction onDo { get; set; }

    public UnityAction onHi { get; set; }

    public UnityAction onOpenGallery { get; set; }


    //private void OnMove(InputValue value)
    //{
    //    move = value.Get<Vector2>();
    //    isMove = move.sqrMagnitude > 0.1f;
    //}

    //private void OnTake(InputValue value)
    //{
    //    onTake?.Invoke();
    //}

    //private void OnDo(InputValue value)
    //{
    //    onDo?.Invoke();
    //}

    //private void OnHi()
    //{
    //    onHi?.Invoke();
    //}

    private void OnGallery(InputValue value)
    {
        onOpenGallery?.Invoke();
    }
}
