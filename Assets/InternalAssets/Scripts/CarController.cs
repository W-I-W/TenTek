using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Маршрут")]
    public Transform[] waypoints;    // Точки маршрута
    public float speed = 5f;         // Скорость движения
    public float turnSpeed = 5f;     // Скорость поворота
    public float stopDistance = 0.2f; // Расстояние для смены точки

    private int currentWaypointIndex = 0;

    void Update()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypointIndex];

        // Направление к следующей точке
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0; // чтобы не дергалось по вертикали

        // Движение
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        // Поворот
        if (direction.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // Проверка достижения точки
        if (Vector3.Distance(transform.position, target.position) < stopDistance)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
                currentWaypointIndex = 0; // зацикливаем маршрут
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
