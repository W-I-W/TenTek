using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
    [SerializeField] private Image m_Image;
    [SerializeField] private Sprite m_SpriteUnlockVideo;
    [SerializeField] private GameObject m_IconVideoPlay;
    [SerializeField] private Button m_CellButton;


    private Item m_Item;
    private Sprite m_ItemPhoto = null;

    private bool m_IsUnLock = false;

    public Gallery gallery { get; set; }


    public void SetItem(Item item)
    {
        m_Item = item;
        m_CellButton.interactable = false;
    }

    public void SetSprite(Sprite sprite)
    {
        m_ItemPhoto = sprite;
    }

    public bool UnlockItem(Item item)
    {
        if (m_Item != item) return false;

        if (m_Item is ItemVideo)
        {
            ItemVideo itemVideo = (ItemVideo)m_Item;
            m_IconVideoPlay.SetActive(true);
            m_Image.sprite = m_SpriteUnlockVideo;
            m_IsUnLock = true;
            m_CellButton.interactable = true;
            return true;
        }
        return false;
    }

    public bool UnlockItem()
    {
        if (m_Item == null) return false;

        ItemVideo itemVideo = (ItemVideo)m_Item;
        m_IconVideoPlay.SetActive(true);
        m_Image.sprite = m_SpriteUnlockVideo;
        m_IsUnLock = true;
        m_CellButton.interactable = true;
        return true;

    }

    public bool UnlockSprite(Sprite sprite)
    {
        if (m_ItemPhoto != sprite) return false;

        m_Image.sprite = sprite;
        m_CellButton.interactable = true;
        m_IsUnLock = true;
        return true;
    }

    public bool UnlockSprite()
    {
        if (m_ItemPhoto == null) return false;

        m_Image.sprite = m_ItemPhoto;
        m_CellButton.interactable = true;
        m_IsUnLock = true;
        return true;
    }

    public void Play()
    {
        if (!m_IsUnLock) return;

        if (m_Item != null)
        {
            ItemVideo item = (ItemVideo)m_Item;
            gallery.Play(item.clip);
            return;
        }

        if (m_ItemPhoto != null)
        {
            gallery.Play(m_ItemPhoto);
        }
    }
}
