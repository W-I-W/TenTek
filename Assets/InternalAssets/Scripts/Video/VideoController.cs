using DG.Tweening;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class VideoController : MonoBehaviour
{
    [SerializeField] private VideoPlayer m_Video;
    [SerializeField] private RectTransform m_Panel;
    [SerializeField] private float m_Duration = 1;
    [SerializeField] private UnityEvent<ItemVideo> m_OnItemVideoPlay;

    private Tween m_Tween;

    public UnityAction<string> onItem { get; set; }



    public void Play(Item item)
    {
        if (!(item is ItemVideo)) return;
        m_Panel.anchoredPosition = new Vector3(0, 5000, 0);
        m_Panel.gameObject.SetActive(true);
        m_Tween = m_Panel.DOLocalMoveY(-50, m_Duration);
        ItemVideo itemVideo = (ItemVideo)item;
        onItem?.Invoke(itemVideo.clip.name);
        m_Video.clip = itemVideo.clip;
        m_Video.Play();
        m_OnItemVideoPlay?.Invoke(itemVideo);
        item.gameObject.SetActive(false);
    }

    public void Stop()
    {
        DOTween.Kill(m_Tween);
        m_Tween = m_Panel.DOLocalMoveY(5000, m_Duration)
            .OnComplete(() =>
            {
                m_Video.Stop();
                m_Video.Prepare();
                m_Video.clip = null;
                m_Panel.gameObject.SetActive(true);
            });
    }

    private void OnDestroy()
    {
        DOTween.Kill(m_Tween);
    }
}
