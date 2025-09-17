using UnityEngine;

/// Простая прямоугольная стена-барьер.
/// Чем ближе к плоскости стены со "внутренней" стороны, тем сильнее толкает внутрь карты.
/// Работает даже при detectCollisions=false (no-clip), т.к. сам читает позицию и правит скорость.
/// Повесь на растянутый Cube с BoxCollider (толщина маленькая). Дублируй 4 раза по периметру.
[RequireComponent(typeof(BoxCollider))]
[DefaultExecutionOrder(200)] // выполняемся после контроллера, чтобы перезаписать скорость
public class RepellingBarrier : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Rigidbody духа (тот же, что у контроллера)")]
    public Rigidbody targetRb;

    [Header("Side Settings")]
    [Tooltip("Какая сторона считается ВНУТРЕННЕЙ (в сторону карты)? Если true — внутренняя по -Z (back), если false — по +Z (forward).")]
    public bool insideIsNegativeZ = true;

    [Header("Effect")]
    [Tooltip("Толщина зоны отталкивания со стороны карты (м)")]
    public float effectThickness = 4f;

    [Tooltip("Сила ускорения у самой плоскости (м/с^2)")]
    public float strength = 22f;

    [Tooltip("Множитель силы, если дух залетел ЗА стену (снаружи), чтобы резко вернуть внутрь")]
    public float outsideMultiplier = 2.5f;

    [Tooltip("Кривая затухания по расстоянию: 1/расстояние^power")]
    public float falloffPower = 2f;

    [Tooltip("Лимит результирующей скорости вдоль направления внутрь (м/с)")]
    public float maxBoostSpeed = 10f;

    [Header("Gizmos")]
    public Color gizmoColor = new Color(0.2f, 0.7f, 1f, 0.15f);

    BoxCollider box;

    void Awake()
    {
        box = GetComponent<BoxCollider>();
        box.isTrigger = false; // не нужен, логика не через триггеры
        if (targetRb == null)
        {
            // Попробуем найти автоматически
            var ghost = FindObjectOfType<GhostFirstPersonController>();
            if (ghost) targetRb = ghost.GetComponent<Rigidbody>();
        }
    }

    void FixedUpdate()
    {
        if (targetRb == null) return;

        // Локальные координаты точки RB относительно BoxCollider (учитываем смещение center)
        Vector3 local = transform.InverseTransformPoint(targetRb.position) - box.center;

        // Размеры коллайдера (половинки)
        Vector3 half = box.size * 0.5f;

        // Проверяем, что мы в пределах длины/высоты прямоугольной стены (по X и Y локальным)
        if (Mathf.Abs(local.x) > half.x || Mathf.Abs(local.y) > half.y)
            return; // за пределами "пролёта" стены — не трогаем

        // Z — перпендикуляр к плоскости стены
        float z = local.z;

        // Нормаль "внутрь карты"
        Vector3 inward = insideIsNegativeZ ? -transform.forward : transform.forward;

        float accel = 0f;

        if (insideIsNegativeZ)
        {
            // ВНУТРИ = z <= 0, СНАРУЖИ = z > 0
            if (z <= 0f) // внутри: чем ближе к плоскости (z≈0), тем сильнее толкаем внутрь (минус Z)
            {
                float depth = -z; // 0 у плоскости, растёт вглубь
                float t = Mathf.Clamp01(1f - depth / effectThickness); // 1 у плоскости → 0 на краю зоны
                accel = strength * Mathf.Pow(t, falloffPower);
            }
            else // снаружи — резко возвращаем внутрь
            {
                accel = strength * outsideMultiplier;
            }
        }
        else
        {
            // ВНУТРИ = z >= 0, СНАРУЖИ = z < 0 (если внутренняя сторона по +Z)
            if (z >= 0f)
            {
                float depth = z;
                float t = Mathf.Clamp01(1f - depth / effectThickness);
                accel = strength * Mathf.Pow(t, falloffPower);
            }
            else
            {
                accel = strength * outsideMultiplier;
            }
        }

        if (accel > 0f)
        {
            // Добавляем скорость внутрь карты, с лимитом по модулю вдоль inward
            Vector3 v = targetRb.linearVelocity;
            float along = Vector3.Dot(v, inward);
            float add = accel * Time.fixedDeltaTime;

            // Если уже разогнались внутрь быстрее лимита — не бустим
            if (along < maxBoostSpeed)
            {
                float space = maxBoostSpeed - along;
                // Сколько можно добавить, чтобы не превысить лимит
                float addClamped = Mathf.Min(add, space);
                v += inward * addClamped;
                targetRb.linearVelocity = v;
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Визуализируем прямоугольник стены и "зону влияния" со стороны карты
        var bx = GetComponent<BoxCollider>();
        if (!bx) return;

        // Основная плоскость (толстым прозрачным)
        Gizmos.color = gizmoColor;
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Vector3 faceSize = new Vector3(bx.size.x, bx.size.y, 0.02f);
        Vector3 facePos  = new Vector3(bx.center.x, bx.center.y, insideIsNegativeZ ? -bx.size.z * 0.5f : bx.size.z * 0.5f);
        Gizmos.DrawCube(facePos, faceSize);

        // Зона влияния по нормали внутрь
        Vector3 zonePos = facePos + new Vector3(0, 0, insideIsNegativeZ ? -effectThickness * 0.5f : effectThickness * 0.5f);
        Vector3 zoneSize = new Vector3(bx.size.x, bx.size.y, effectThickness);
        Gizmos.DrawWireCube(zonePos, zoneSize);

        Gizmos.matrix = old;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + (insideIsNegativeZ ? -transform.forward : transform.forward) * 2f);
    }
#endif
}
