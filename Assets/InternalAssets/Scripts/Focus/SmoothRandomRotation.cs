using UnityEngine;

public class SmoothRandomRotation : MonoBehaviour
{
    public float rotationSpeed = 30f;      // скорость вращения (градусы в секунду)
    public float changeDirectionTime = 2f; // как часто менять направление

    private Vector3 currentAxis;   // текущая ось вращения
    private Vector3 targetAxis;    // новая ось вращения
    private float t;               // прогресс интерполяции
    private float timer;

    void Start()
    {
        // задаём случайное начальное направление
        currentAxis = Random.onUnitSphere;
        targetAxis = Random.onUnitSphere;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // меняем направление каждые N секунд
        if (timer >= changeDirectionTime)
        {
            timer = 0f;
            currentAxis = targetAxis;
            targetAxis = Random.onUnitSphere; // выбираем новое случайное направление
            t = 0f;
        }

        // плавный переход между направлениями
        t += Time.deltaTime / changeDirectionTime;
        Vector3 smoothAxis = Vector3.Slerp(currentAxis, targetAxis, t);

        // вращаем сферу
        transform.Rotate(smoothAxis * rotationSpeed * Time.deltaTime, Space.World);
    }
}
