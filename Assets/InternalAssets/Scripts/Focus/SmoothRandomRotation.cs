using UnityEngine;

public class SmoothRandomRotation : MonoBehaviour
{
    public float rotationSpeed = 30f;      // �������� �������� (������� � �������)
    public float changeDirectionTime = 2f; // ��� ����� ������ �����������

    private Vector3 currentAxis;   // ������� ��� ��������
    private Vector3 targetAxis;    // ����� ��� ��������
    private float t;               // �������� ������������
    private float timer;

    void Start()
    {
        // ����� ��������� ��������� �����������
        currentAxis = Random.onUnitSphere;
        targetAxis = Random.onUnitSphere;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // ������ ����������� ������ N ������
        if (timer >= changeDirectionTime)
        {
            timer = 0f;
            currentAxis = targetAxis;
            targetAxis = Random.onUnitSphere; // �������� ����� ��������� �����������
            t = 0f;
        }

        // ������� ������� ����� �������������
        t += Time.deltaTime / changeDirectionTime;
        Vector3 smoothAxis = Vector3.Slerp(currentAxis, targetAxis, t);

        // ������� �����
        transform.Rotate(smoothAxis * rotationSpeed * Time.deltaTime, Space.World);
    }
}
