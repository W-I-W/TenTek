using UnityEngine;
using UnityEngine.Video;

public class ItemPhoto : Item
{
    [Space]
    [SerializeField] private Sprite m_Sprite;
    [SerializeField] private SpriteRenderer m_SR;

    public Sprite sprite { get => m_Sprite; }

    public override void OnDrag()
    {
        base.OnDrag();
        m_SR.enabled = true;
    }

    public override void OnDrop()
    {
        base.OnDrop();
        m_SR.enabled = false;
    }
}
