using System.Collections.Generic;
using UnityEngine;

public class WaypointWalker : MonoBehaviour
{
    public enum PathMode { Loop, PingPong, Once }

    [Header("Path")]
    public List<Transform> Waypoints = new List<Transform>();
    public bool startAtNearest = true;
    public PathMode mode = PathMode.Loop;

    [Header("Movement")]
    public float moveSpeed = 2.0f;
    public float rotationSpeed = 720f;     // град/сек
    public float arriveDistance = 0.2f;    // рассто€ние Ђдостиг точкиї
    public float defaultWaitSeconds = 0f;  // пауза на каждой точке (можно 0)

    [Header("Animator (optional)")]
    public Animator animator;              // можно не задавать Ч найдЄтс€ сам
    public string animatorSpeedFloat = "Speed"; // оставь пустым если не нужен
    public string animatorMovingBool = "";      // оставь пустым если не нужен

    private int _index = -1;   // текуща€ точка
    private int _dir = 1;      // дл€ PingPong
    private float _waitTimer = 0f;
    private bool _isWaiting = false;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        if (Waypoints == null || Waypoints.Count == 0)
        {
            Debug.LogWarning($"{name}: WaypointWalker Ч нет точек пути");
            enabled = false;
            return;
        }

        _index = startAtNearest ? GetNearestIndex(transform.position) : 0;
        FaceTo(Waypoints[_index].position);
    }

    void Update()
    {
        if (_isWaiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _isWaiting = false;
                AdvanceIndex();
            }
            UpdateAnimator(0f, false);
            return;
        }

        Vector3 target = Waypoints[_index].position;
        Vector3 to = target - transform.position;
        to.y = 0f;

        float dist = to.magnitude;
        if (dist <= arriveDistance)
        {
            BeginWaitAtPoint(_index);
            return;
        }

        // поворот
        if (to.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, rotationSpeed * Time.deltaTime);
        }

        // шаг
        float step = moveSpeed * Time.deltaTime;
        Vector3 move = Vector3.ClampMagnitude(to, step);
        transform.position += move;

        UpdateAnimator(moveSpeed, true);
    }

    private void BeginWaitAtPoint(int pointIndex)
    {
        float wait = defaultWaitSeconds;
        // ≈сли хочешь разные паузы на точках Ч повесь на точку компонент WaypointWait и задай врем€
        var wp = Waypoints[pointIndex];
        if (wp)
        {
            var waitComp = wp.GetComponent<WaypointWait>();
            if (waitComp) wait = waitComp.waitSeconds;
        }

        if (wait > 0f)
        {
            _isWaiting = true;
            _waitTimer = wait;
            UpdateAnimator(0f, false);
        }
        else
        {
            AdvanceIndex();
        }
    }

    private void AdvanceIndex()
    {
        if (Waypoints.Count <= 1) return;

        if (mode == PathMode.Loop)
        {
            _index = (_index + 1) % Waypoints.Count;
        }
        else if (mode == PathMode.PingPong)
        {
            _index += _dir;
            if (_index >= Waypoints.Count)
            {
                _index = Waypoints.Count - 2;
                _dir = -1;
            }
            else if (_index < 0)
            {
                _index = 1;
                _dir = 1;
            }
        }
        else // Once
        {
            _index = Mathf.Min(_index + 1, Waypoints.Count - 1);
        }
    }

    private int GetNearestIndex(Vector3 from)
    {
        int best = 0;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < Waypoints.Count; i++)
        {
            if (!Waypoints[i]) continue;
            float d = (Waypoints[i].position - from).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = i; }
        }
        return best;
    }

    private void FaceTo(Vector3 worldPos)
    {
        Vector3 to = worldPos - transform.position; to.y = 0f;
        if (to.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
    }

    private void UpdateAnimator(float speedMagnitude, bool moving)
    {
        if (!animator) return;
        if (!string.IsNullOrEmpty(animatorSpeedFloat))
            animator.SetFloat(animatorSpeedFloat, moving ? speedMagnitude : 0f);
        if (!string.IsNullOrEmpty(animatorMovingBool))
            animator.SetBool(animatorMovingBool, moving);
    }

    void OnDrawGizmosSelected()
    {
        if (Waypoints == null || Waypoints.Count == 0) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < Waypoints.Count; i++)
        {
            var t = Waypoints[i];
            if (!t) continue;
            Gizmos.DrawSphere(t.position, 0.12f);
            int next = i + 1;
            if (mode == PathMode.Loop) next %= Waypoints.Count;
            if (next < Waypoints.Count && Waypoints[next])
                Gizmos.DrawLine(t.position, Waypoints[next].position);
        }
        Gizmos.color = new Color(1, 0.5f, 0, 0.35f);
        Gizmos.DrawWireSphere(transform.position, arriveDistance);
    }
}

/// <summary>Ќеоб€зательный компонент: поставь на любую точку, чтобы задать индивидуальную паузу.</summary>
public class WaypointWait : MonoBehaviour
{
    public float waitSeconds = 0f;
}
