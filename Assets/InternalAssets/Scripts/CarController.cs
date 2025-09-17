using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("�������")]
    public Transform[] waypoints;    // ����� ��������
    public float speed = 5f;         // �������� ��������
    public float turnSpeed = 5f;     // �������� ��������
    public float stopDistance = 0.2f; // ���������� ��� ����� �����

    private int currentWaypointIndex = 0;

    void Update()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypointIndex];

        // ����������� � ��������� �����
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0; // ����� �� ��������� �� ���������

        // ��������
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        // �������
        if (direction.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // �������� ���������� �����
        if (Vector3.Distance(transform.position, target.position) < stopDistance)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
                currentWaypointIndex = 0; // ����������� �������
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }
}
