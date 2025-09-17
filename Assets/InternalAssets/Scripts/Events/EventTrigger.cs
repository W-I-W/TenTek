using BoingKit;

using Sisus.Init;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Windows;


namespace Bow
{

    public class EventTrigger : MonoBehaviour, IClickable
    {
        [SerializeField] private InputSystem m_Input;
        [SerializeField] private Sprite m_Sprite;
        [SerializeField] private MeshRenderer m_Mesh;
        [SerializeField] private PhotoController m_Photo;

        private const string TexPropName = "_BaseMap";


        public Sprite sprite => m_Sprite;

        private void Start()
        {
            Texture texture = m_Sprite.texture;
            m_Mesh.material.SetTexture(TexPropName, texture);
        }

        //private void OnEnable()
        //{
        //    m_Input.onTake += OnTake;
        //}

        //private void OnDisable()
        //{
        //    m_Input.onTake -= OnTake;
        //}

        //private void OnTriggerExit2D(Collider2D collision)
        //{
        //    bool isPlayer = collision.TryGetComponent(out Inventory inventory);
        //    if (!isPlayer) return;
        //    //inventory.isLock = false;
        //    m_IsPlayer = false;
        //    OnClickExit?.Invoke();
        //}


        //public void OnClick()
        //{
        //    bool isPlayer = collision.TryGetComponent(out Inventory inventory);
        //    if (!isPlayer) return;
        //    //inventory.isLock = m_IsLockInventory;
        //    m_IsPlayer = true;
        //    m_IsClick = false;
        //}

        public void OnClick()
        {
            //if (m_IsPlayer)
            //{
            //if (m_IsClick && m_IsClose)
            //{
            //    OnClickExit?.Invoke();
            //    m_IsClick = false;
            //    return;
            //}
            m_Photo.Play(m_Sprite);
            gameObject.SetActive(false);

            //OnClickEnter?.Invoke();
            //m_IsClick = true;
            //}
        }
    }
}