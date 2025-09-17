using UnityEngine;

[DisallowMultipleComponent]
public class ReturnHome : MonoBehaviour
{
    [Tooltip("���� ������ � ���������� � ����� ����� (�������/�������).")]
    public Transform explicitAnchor;

    [Tooltip("���� ���� explicitAnchor � ������������ ��� ��� ��������.")]
    public bool useExplicitAnchorIfSet = true;

    // ��������� ���� (��� fallback)
    private Vector3 initialPos;
    private Quaternion initialRot;

    // ���� � ������ ������ ������� �����������
    private bool hasPickupPose;
    private Vector3 pickupPos;
    private Quaternion pickupRot;

    void Awake()
    {
        initialPos = transform.position;
        initialRot = transform.rotation;
    }

    /// ������� � ������ ������ ������� (Telekinesis).
    public void SavePickupPose()
    {
        pickupPos = transform.position;
        pickupRot = transform.rotation;
        hasPickupPose = true;
    }

    /// �������� ������� ���� ��������.
    public bool TryGetReturnPose(out Vector3 pos, out Quaternion rot)
    {
        if (useExplicitAnchorIfSet && explicitAnchor != null)
        {
            pos = explicitAnchor.position;
            rot = explicitAnchor.rotation;
            return true;
        }
        if (hasPickupPose)
        {
            pos = pickupPos;
            rot = pickupRot; return true;
        }
        pos = initialPos;
        rot = initialRot; return true;
    }

#if UNITY_EDITOR
    [ContextMenu("Create/Update Explicit Anchor Here")]
    void BakeAnchorHere()
    {
        if (!explicitAnchor)
        {
            var go = new GameObject(name + "_HomeAnchor");
            go.transform.SetParent(transform.parent, true);
            explicitAnchor = go.transform;
        }
        explicitAnchor.SetPositionAndRotation(transform.position, transform.rotation);
    }
#endif
}
