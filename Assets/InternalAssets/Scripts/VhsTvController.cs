using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// ТВ: автоприём кассеты (Tag = "Cassette") + управление монитором и девушкой.
/// Возврат кассеты — туда, где она была в МОМЕНТ НАЧАЛА захвата (если на ней есть ReturnHome),
/// иначе — туда, где была в момент вставки.
public class VhsTvController : MonoBehaviour
{
    [Header("Cassette (жёсткий фильтр)")]
    public string cassetteTag = "Cassette";
    public Transform slotPoint;
    public float catchRadius = 0.7f;
    public LayerMask detectionMask = ~0;

    [Header("Авто-режим")]
    public float autoCheckInterval = 0.05f;
    public float cooldownAfterFinish = 1.0f;

    [Header("Монитор/экран")]
    public GameObject monitorRoot;                 // куб-экран (делаем active/inactive)
    public TvImageSlideshow monitorSlideshow;      // опционально

    [Header("Girl — выбери один режим")]
    public GameObject existingGirl;                // Inactive в сцене
    public GameObject girlPrefab;
    public Transform girlSpawnPoint;

    [Header("Timing")]
    public float spawnDelay = 30f;
    public float despawnAfter = 120f;

    [Header("Cassette handling на время сценки")]
    public bool makeCassetteKinematic = true;
    public bool disableCassetteColliders = true;
    public bool hideCassetteRenderers = true;

    [Header("Debug")]
    public bool debugLogs = false;
    public Color gizmoColor = new Color(0.2f, 0.7f, 1f, 0.25f);

    // runtime
    bool busy;
    float nextAutoTime;
    GameObject spawnedGirl;

    Rigidbody cassetteRb;
    Transform cassetteOriginalParent;
    Vector3 cassetteOriginalPos;
    Quaternion cassetteOriginalRot;
    bool cassetteOriginalKinematic;
    bool cassetteOriginalGravity;
    readonly List<(Collider col, bool enabled)> cassetteCols = new();
    readonly List<(Renderer ren, bool enabled)> cassetteRens = new();

    void Awake()
    {
        if (!monitorSlideshow && monitorRoot)
            monitorSlideshow = monitorRoot.GetComponent<TvImageSlideshow>() ?? monitorRoot.GetComponentInChildren<TvImageSlideshow>(true);
    }

    void Update()
    {
        if (busy || slotPoint == null) return;
        if (Time.time < nextAutoTime) return;

        TryAutoAccept();
        nextAutoTime = Time.time + autoCheckInterval;
    }

    void TryAutoAccept()
    {
        Rigidbody rb = FindCassetteNearSlot(out int hits, out int passed);
        if (debugLogs) Debug.Log($"[VHS] auto-check: hits={hits}, cassetteMatches={passed}, rb={(rb ? rb.name : "null")}");

        if (rb == null) return;

        AcceptAndHideCassette(rb);

        if (monitorRoot && !monitorRoot.activeSelf) monitorRoot.SetActive(true);
        if (monitorSlideshow) monitorSlideshow.StartShow();

        StartCoroutine(SceneRoutine());
    }

    Rigidbody FindCassetteNearSlot(out int totalHits, out int passedTag)
    {
        totalHits = 0; passedTag = 0;
        Collider[] hits = Physics.OverlapSphere(slotPoint.position, catchRadius, detectionMask, QueryTriggerInteraction.Collide);
        totalHits = hits.Length;

        float best = float.MaxValue;
        Rigidbody bestRb = null;

        foreach (var col in hits)
        {
            var rb = col.attachedRigidbody;
            if (rb == null) continue;

            if (!IsCassette(col, rb.transform)) continue;
            passedTag++;

            float d = Vector3.SqrMagnitude(col.ClosestPoint(slotPoint.position) - slotPoint.position);
            if (d < best) { best = d; bestRb = rb; }
        }
        return bestRb;
    }

    bool IsCassette(Collider hitCol, Transform rbOwner)
    {
        if (string.IsNullOrEmpty(cassetteTag)) return false;
        if (hitCol.CompareTag(cassetteTag)) return true;
        for (Transform p = hitCol.transform.parent; p != null && p != rbOwner.parent; p = p.parent)
            if (p.CompareTag(cassetteTag)) return true;
        return false;
    }

    void AcceptAndHideCassette(Rigidbody rb)
    {
        busy = true;

        cassetteRb = rb;
        cassetteOriginalParent = rb.transform.parent;
        cassetteOriginalPos = rb.transform.position;   // fallback: где была в момент вставки
        cassetteOriginalRot = rb.transform.rotation;

        cassetteOriginalKinematic = rb.isKinematic;
        cassetteOriginalGravity = rb.useGravity;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (makeCassetteKinematic) rb.isKinematic = true;
        rb.useGravity = false;

        cassetteCols.Clear();
        if (disableCassetteColliders)
            foreach (var c in rb.GetComponentsInChildren<Collider>(true)) { cassetteCols.Add((c, c.enabled)); c.enabled = false; }

        cassetteRens.Clear();
        if (hideCassetteRenderers)
            foreach (var r in rb.GetComponentsInChildren<Renderer>(true)) { cassetteRens.Add((r, r.enabled)); r.enabled = false; }

        if (debugLogs) Debug.Log($"[VHS] Cassette accepted: {rb.name}");
    }

    IEnumerator SceneRoutine()
    {
        yield return new WaitForSeconds(spawnDelay);

        if (existingGirl != null) existingGirl.SetActive(true);
        else if (girlPrefab && girlSpawnPoint) spawnedGirl = Instantiate(girlPrefab, girlSpawnPoint.position, girlSpawnPoint.rotation);

        if (despawnAfter > 0f) yield return new WaitForSeconds(despawnAfter);

        if (existingGirl != null) existingGirl.SetActive(false);
        else if (spawnedGirl) { Destroy(spawnedGirl); spawnedGirl = null; }

        if (monitorSlideshow) monitorSlideshow.StopShow(true);
        if (monitorRoot && monitorRoot.activeSelf) monitorRoot.SetActive(false);

        ReturnCassette();
        nextAutoTime = Time.time + cooldownAfterFinish;
        busy = false;

        if (debugLogs) Debug.Log("[VHS] Scene finished");
    }

    void ReturnCassette()
    {
        if (cassetteRb == null) return;

        // Пытаемся вернуть по ReturnHome (где лежала перед захватом)
        Vector3 retPos = cassetteOriginalPos;
        Quaternion retRot = cassetteOriginalRot;
        var home = cassetteRb.GetComponent<ReturnHome>();
        if (home != null && home.TryGetReturnPose(out var p, out var r))
        {
            retPos = p; retRot = r;
        }

        cassetteRb.transform.SetParent(cassetteOriginalParent, true);
        cassetteRb.transform.SetPositionAndRotation(retPos, retRot);

        cassetteRb.isKinematic = cassetteOriginalKinematic;
        cassetteRb.useGravity = cassetteOriginalGravity;
        cassetteRb.linearVelocity = Vector3.zero;
        cassetteRb.angularVelocity = Vector3.zero;

        foreach (var (col, wasEnabled) in cassetteCols) if (col) col.enabled = wasEnabled;
        foreach (var (ren, wasEnabled) in cassetteRens) if (ren) ren.enabled = wasEnabled;

        cassetteCols.Clear();
        cassetteRens.Clear();
        cassetteRb = null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!slotPoint) return;
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(slotPoint.position, catchRadius);
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.9f);
        Gizmos.DrawWireSphere(slotPoint.position, catchRadius);
    }
#endif
}
