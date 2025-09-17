using DG.Tweening;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class PhotoController : MonoBehaviour
{
    [SerializeField] private Image m_Image;
    [SerializeField] private RectTransform m_Panel;
    [SerializeField] private float m_Duration = 1;
    [SerializeField] private UnityEvent<Sprite> m_OnPlay;

    private Tween m_Tween;
    //private GameObject m_Target;

    public UnityAction<string> onItem { get; set; }

    public static PhotoController instance { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    public void Play(Sprite sprite)
    {
        //m_Target = meshObject;
        onItem?.Invoke(sprite.name);
        m_Panel.anchoredPosition = new Vector3(0, 5000, 0);
        m_Panel.gameObject.SetActive(true);
        m_Tween = m_Panel.DOLocalMoveY(0, m_Duration);
        m_Image.sprite = sprite;
        m_OnPlay?.Invoke(sprite);
    }

    public void Stop()
    {
        if (!m_Panel.gameObject.activeSelf) return;
        DOTween.Kill(m_Tween);
        m_Tween = m_Panel.DOLocalMoveY(5000, m_Duration)
            .OnComplete(() =>
            {
                m_Panel.gameObject.SetActive(true);
                //m_Target.gameObject.SetActivePhoto(false);
            });
    }

    private void OnDestroy()
    {
        DOTween.Kill(m_Tween);
    }
}
