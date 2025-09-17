using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

public class TimedGirlActivator : MonoBehaviour
{
    [Header("Target (������ ���� �������)")]
    public GameObject existingGirl;   // Inactive � �����
    public GameObject girlPrefab;     // ��� ������
    public Transform girlSpawnPoint;

    [Header("Timing")]
    [Tooltip("�������� ����� ������ ��������, ���.")]
    public float startDelay = 30f;
    [Tooltip("��� ����� ������� ����� �������, ���.")]
    public float activeDuration = 120f;

    [Header("Auto/Loop")]
    public bool startOnEnable = true;
    public bool loop = false;
    [Tooltip("����� ����� ���������, ��� (����� loop = true).")]
    public float loopInterval = 3f;

    [Header("Cooldown")]
    [Tooltip("������� ����� ���������� ������, � ������� �������� ����� ����� ��������, ���.")]
    public float cooldownAfterFinish = 0f;

    [Header("External Blockers")]
    [Tooltip("���� ���� �� ���� �� ���� ����������� ������ \"��������\", ������ ����� ������������.")]
    public List<BlockerEntry> externalBlockers = new List<BlockerEntry>();
    [System.Serializable]
    public class BlockerEntry
    {
        [Tooltip("��������� (������), ������� ����� ���� �����.")]
        public MonoBehaviour component;
        [Tooltip("��� �������� ����/��������, ������������ ��������� (����� = ����-�����: isRunning/IsRunning/isBusy/� )")]
        public string boolMemberName = "";
        [Tooltip("������������� ��������. ��������, ���� ��������� ����� IsIdle � ������� invert = true.")]
        public bool invert = false;
    }

    [Header("Control")]
    [Tooltip("������������ ��������� ������, ���� ���� ���.")]
    public bool ignoreIfRunning = true;
    [Tooltip("�� ���������, ���� ��� �� existingGirl ��� ������� � �����.")]
    public bool dontStartIfGirlActive = true;

    [Header("Events (�����������)")]
    public UnityEvent onStarted;
    public UnityEvent onActivated;
    public UnityEvent onDeactivated;
    public UnityEvent onFinished;

    [Header("Debug")]
    public bool debugLogs = false;

    // runtime
    Coroutine routine;
    bool isRunning;
    GameObject spawnedInstance;
    float nextAllowedStartTime = 0f;

    void OnEnable()
    {
        if (startOnEnable) StartSequence();
    }

    /// ��������� ���� ������� (����� ����� �� ������� �������).
    public void StartSequence()
    {
        // ��� ���?
        if (ignoreIfRunning && isRunning)
        {
            if (debugLogs) Debug.Log("[TimedGirl] already running");
            return;
        }

        // �������
        if (Time.time < nextAllowedStartTime)
        {
            if (debugLogs) Debug.Log($"[TimedGirl] on cooldown: {(nextAllowedStartTime - Time.time):F1}s left");
            return;
        }

        // �� �� ������� ��� �������?
        if (dontStartIfGirlActive && existingGirl != null && existingGirl.activeInHierarchy)
        {
            if (debugLogs) Debug.Log("[TimedGirl] blocked: existingGirl already active");
            return;
        }

        // ������� ������� ������?
        if (IsAnyExternalBlockerActive(out string who))
        {
            if (debugLogs) Debug.Log($"[TimedGirl] blocked by external blocker: {who}");
            return;
        }

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run());
    }

    /// ���������� ����� ������. ����������� ������������/��������� ������� �������.
    public void StopNow(bool cleanup = true)
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        if (cleanup) DeactivateGirl();
        isRunning = false;
        onFinished?.Invoke();

        // �������
        if (cooldownAfterFinish > 0f)
            nextAllowedStartTime = Time.time + cooldownAfterFinish;

        if (debugLogs) Debug.Log("[TimedGirl] STOP");
    }

    IEnumerator Run()
    {
        isRunning = true;
        onStarted?.Invoke();
        if (debugLogs) Debug.Log("[TimedGirl] started");

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        ActivateGirl();
        onActivated?.Invoke();

        if (activeDuration > 0f)
            yield return new WaitForSeconds(activeDuration);

        DeactivateGirl();
        onDeactivated?.Invoke();

        onFinished?.Invoke();
        isRunning = false;

        if (cooldownAfterFinish > 0f)
            nextAllowedStartTime = Time.time + cooldownAfterFinish;

        if (loop)
        {
            float wait = Mathf.Max(loopInterval, cooldownAfterFinish);
            if (wait > 0f) yield return new WaitForSeconds(wait);
            routine = StartCoroutine(Run());
        }
        else
        {
            routine = null;
        }

        if (debugLogs) Debug.Log("[TimedGirl] finished");
    }

    void ActivateGirl()
    {
        if (existingGirl != null)
        {
            existingGirl.SetActive(true);
            if (debugLogs) Debug.Log("[TimedGirl] existingGirl -> SetActive(true)");
        }
        else if (girlPrefab != null && girlSpawnPoint != null)
        {
            spawnedInstance = Instantiate(girlPrefab, girlSpawnPoint.position, girlSpawnPoint.rotation);
            if (debugLogs) Debug.Log("[TimedGirl] prefab spawned");
        }
        else
        {
            Debug.LogWarning("[TimedGirl] �� ������ existingGirl �/��� prefab+spawnPoint � ������ ������������.");
        }
    }

    void DeactivateGirl()
    {
        if (existingGirl != null)
        {
            existingGirl.SetActive(false);
            if (debugLogs) Debug.Log("[TimedGirl] existingGirl -> SetActive(false)");
        }
        if (spawnedInstance != null)
        {
            Destroy(spawnedInstance);
            spawnedInstance = null;
            if (debugLogs) Debug.Log("[TimedGirl] spawned prefab destroyed");
        }
    }

    // --- External blockers check ---
    bool IsAnyExternalBlockerActive(out string who)
    {
        who = null;
        if (externalBlockers == null) return false;

        foreach (var b in externalBlockers)
        {
            if (!b?.component) continue;

            bool value;
            if (TryReadBoolMember(b.component, b.boolMemberName, out value))
            {
                if (b.invert) value = !value;
                if (value)
                {
                    who = $"{b.component.GetType().Name} ({b.component.name})";
                    return true;
                }
            }
            else
            {
                // ���� ������ �� ����� � �������, ��� ������� ���; ��� ����������� ����� �������
                if (debugLogs)
                    Debug.LogWarning($"[TimedGirl] Blocker has no readable bool member on {b.component.GetType().Name}. " +
                                     $"Set 'Bool Member Name' (��������: isRunning, IsRunning, isBusy).");
            }
        }
        return false;
    }

    bool TryReadBoolMember(MonoBehaviour comp, string memberName, out bool value)
    {
        value = false;
        var t = comp.GetType();

        // ������ ��� �� ���������, ���� ���� �� ������
        string[] defaults = new[]
        {
            "isRunning","IsRunning","running","Running",
            "isBusy","IsBusy","busy","Busy",
            "isActive","IsActive","active","Active"
        };

        // 1) ���� ��� ������ ���� � �������� ������ ���
        if (!string.IsNullOrWhiteSpace(memberName))
        {
            if (TryGetBoolViaReflection(t, comp, memberName, out value))
                return true;
        }

        // 2) ����-����� �� ���������� ������
        foreach (var n in defaults)
        {
            if (TryGetBoolViaReflection(t, comp, n, out value))
                return true;
        }
        return false;
    }

    bool TryGetBoolViaReflection(System.Type t, object instance, string name, out bool val)
    {
        val = false;
        // property?
        var prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null && prop.PropertyType == typeof(bool) && prop.CanRead)
        {
            val = (bool)prop.GetValue(instance);
            return true;
        }
        // field?
        var field = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null && field.FieldType == typeof(bool))
        {
            val = (bool)field.GetValue(instance);
            return true;
        }
        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (girlSpawnPoint)
        {
            Gizmos.color = new Color(1f, 0.5f, 0.8f, 0.5f);
            Gizmos.DrawWireSphere(girlSpawnPoint.position, 0.25f);
        }
    }

    [ContextMenu("Test/Start Sequence")]
    void _CtxStart() => StartSequence();

    [ContextMenu("Test/Stop Now")]
    void _CtxStop() => StopNow(true);
#endif
}
