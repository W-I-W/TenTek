using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;
using UnityEngine.Video;

public class Gallery : MonoBehaviour
{
    [SerializeField] private Cell m_Prefab;
    [SerializeField] private InputSystem m_Input;
    [SerializeField] private Transform m_Parent;
    [SerializeField] private GameObject m_PanelGallery;
    [SerializeField] private VideoPlayer m_VideoPlayer;
    [SerializeField] private RawImage m_RawVideoImage;
    [SerializeField] private Image m_Photo;

    private HashSet<GalleryData> m_Data;

    private bool isOpenGallery { get; set; } = false;



    private void Awake()
    {
        m_Data = new HashSet<GalleryData>();
        OnDisableUpdate();
    }

    private void OnEnable()
    {
        m_Input.onOpenGallery += OnOpenGallery;
    }

    private void OnDisable()
    {
        m_Input.onOpenGallery -= OnOpenGallery;
    }

    private void OnOpenGallery()
    {
        isOpenGallery = !isOpenGallery;
        m_PanelGallery.gameObject.SetActive(isOpenGallery);
    }

    public void Add(Sprite sprite)
    {
        GalleryData data = new GalleryData();
        data.Sprite = sprite;
        if (m_Data.Add(data))
        {
            Cell cell = Instantiate(m_Prefab, m_Parent);
            cell.SetSprite(sprite);
            cell.UnlockSprite();
            cell.gallery = this;
        }
    }

    public void Add(ItemVideo item)
    {
        GalleryData data = new GalleryData();
        data.Item = item;
        if (m_Data.Add(data))
        {
            Cell cell = Instantiate(m_Prefab, m_Parent);
            cell.SetItem(item);
            cell.UnlockItem(item);
            cell.gallery = this;
        }
    }

    public void Play(Sprite sprite)
    {
        m_Photo.sprite = sprite;
        m_Photo.gameObject.SetActive(true);
        m_RawVideoImage.gameObject.SetActive(false);
    }

    public void Play(VideoClip clip)
    {
        Debug.Log(clip);
        m_VideoPlayer.clip = clip;
        m_VideoPlayer.Prepare();
        m_VideoPlayer.Play();
        m_Photo.gameObject.SetActive(false);
        m_RawVideoImage.gameObject.SetActive(true);
    }

    public void OnDisableUpdate()
    {
        m_RawVideoImage.gameObject.SetActive(false);
        m_Photo.gameObject.SetActive(false);
    }
}


[Serializable]
public struct GalleryData
{
    public Sprite Sprite;
    public ItemVideo Item;
}

