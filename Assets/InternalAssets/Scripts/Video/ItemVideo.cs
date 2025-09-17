using UnityEngine;
using UnityEngine.Video;

public class ItemVideo : Item
{
    [Space]
    [SerializeField] private VideoClip m_Clip;
    //[SerializeField] private SpriteRenderer m_Target;

    public VideoClip clip { get => m_Clip; }

    public override void OnDrag()
    {
        base.OnDrag();
        //m_Target.enabled = true;
    }

    public override void OnDrop()
    {
        base.OnDrop();
        //m_Target.enabled = false;
    }
}
