using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Радио: пульсирует в ожидании, по E запускает плейлист,
/// через spawnDelay активируется девушка (или спавним префаб),
/// через despawnAfter девушка исчезает, музыка останавливается, пульс возвращается.
///
/// Требования:
/// - На объекте должен быть Collider (для твоего GhostInteractor).
/// - GhostInteractor должен звать IInteractable.Interact() при E.
///
/// Подключи на радио этот скрипт, добавь AudioSource (скрипт сам создаст, если нет).
public class RadioController : MonoBehaviour, IInteractable
{
    // ========= Взаимодействие =========
    [Header("Interaction")]
    [SerializeField] private string hintText = "Включить радио";
    public string Hint => isRunning ? "" : hintText;

    // ========= Визуальный пульс =========
    [Header("Idle Pulse (attention)")]
    [Tooltip("Что пульсировать (обычно сам корпус радио или его пустышка). Если пусто — возьмём transform.")]
    public Transform pulseTarget;
    [Tooltip("Амплитуда пульса (масштаб)")]
    public float pulseAmplitude = 0.06f;
    [Tooltip("Частота пульса, Гц")]
    public float pulseFrequency = 2.0f;
    [Tooltip("Включать пульс, когда радио ждёт запуска")]
    public bool pulseWhenIdle = true;

    // ========= Аудио / плейлист =========
    [Header("Playlist")]
    [Tooltip("Треки, которые проигрываем по порядку/вперемешку")]
    public AudioClip[] clips;
    [Tooltip("Перемешать порядок при старте")]
    public bool shuffleOnStart = false;
    [Tooltip("Повторять плейлист по кругу, пока идёт сценка")]
    public bool loopPlaylist = true;
    [Range(0f, 1f)] public float volume = 0.85f;
    [Tooltip("3D звук: 0 — 2D, 1 — 3D")]
    [Range(0f, 1f)] public float spatialBlend = 1f;
    [Tooltip("Минимальная дистанция громкости")]
    public float minDistance = 3f;
    [Tooltip("Максимальная дистанция слышимости")]
    public float maxDistance = 20f;

    // ========= Девушка =========
    [Header("Girl – выбери один режим")]
    [Tooltip("Заранее положенная (Inactive) девушка")]
    public GameObject existingGirl;
    [Tooltip("Либо префаб и точка спавна")]
    public GameObject girlPrefab;
    public Transform girlSpawnPoint;

    [Header("Timing")]
    [Tooltip("Через сколько после старта радио появится девушка, сек")]
    public float spawnDelay = 30f;
    [Tooltip("Через сколько после появления исчезнет девушка, сек")]
    public float despawnAfter = 120f;

    [Header("Finish")]
    [Tooltip("Остановить музыку, когда сценка закончится")]
    public bool stopMusicOnFinish = true;

    [Header("Debug")]
    public bool debugLogs = false;

    // ======== runtime ========
    AudioSource a;
    bool isRunning;                 // идёт сценка
    bool pulseEnabled = true;
    Vector3 pulseBaseScale;
    Coroutine playlistCo;
    GameObject spawnedGirl;

    void Awake()
    {
        if (!pulseTarget) pulseTarget = transform;
        pulseBaseScale = pulseTarget.localScale;

        a = GetComponent<AudioSource>();
        if (!a) a = gameObject.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.loop = false;
        a.volume = volume;
        a.spatialBlend = spatialBlend;
        a.minDistance = minDistance;
        a.maxDistance = maxDistance;
        a.rolloffMode = AudioRolloffMode.Custom; // юнити сам построит кривую
    }

    void Update()
    {
        // Пульс в ожидании
        if (!isRunning && pulseWhenIdle && pulseEnabled && pulseTarget)
        {
            float k = 1f + Mathf.Sin(Time.time * Mathf.PI * 2f * pulseFrequency) * pulseAmplitude;
            pulseTarget.localScale = pulseBaseScale * k;
        }
    }

    // ========= IInteractable =========
    public void Interact()
    {
        if (isRunning) return;
        if (clips == null || clips.Length == 0)
        {
            if (debugLogs) Debug.LogWarning("[Radio] Нет клипов в плейлисте.");
            return;
        }

        // <<< ЕДИНСТВЕННОЕ ИЗМЕНЕНИЕ: защёлкиваем флаг до запуска корутин
        isRunning = true;

        StartSequence();
    }

    // ========= основная последовательность =========
    void StartSequence()
    {
        // выключаем пульс
        pulseEnabled = false;
        if (pulseTarget) pulseTarget.localScale = pulseBaseScale;

        // стартуем музыку
        if (playlistCo != null) StopCoroutine(playlistCo);
        playlistCo = StartCoroutine(PlaylistRoutine());

        // запустить тайминг появления/исчезновения девушки
        StartCoroutine(SceneRoutine());
        if (debugLogs) Debug.Log("[Radio] Sequence started.");
    }

    IEnumerator SceneRoutine()
    {
        // появление
        if (spawnDelay > 0f) yield return new WaitForSeconds(spawnDelay);

        if (existingGirl != null)
        {
            existingGirl.SetActive(true);
        }
        else if (girlPrefab && girlSpawnPoint)
        {
            spawnedGirl = Instantiate(girlPrefab, girlSpawnPoint.position, girlSpawnPoint.rotation);
        }

        // исчезновение
        if (despawnAfter > 0f) yield return new WaitForSeconds(despawnAfter);

        if (existingGirl != null)
        {
            existingGirl.SetActive(false);
        }
        else if (spawnedGirl)
        {
            Destroy(spawnedGirl);
            spawnedGirl = null;
        }

        // финиш
        if (stopMusicOnFinish) StopPlaylist();
        isRunning = false;          // ← только здесь снова разрешим E
        pulseEnabled = true;        // снова пульсируем
        if (debugLogs) Debug.Log("[Radio] Sequence finished.");
    }

    // ========= плейлист =========
    IEnumerator PlaylistRoutine()
    {
        // подготовим порядок
        List<int> order = new List<int>(clips.Length);
        for (int i = 0; i < clips.Length; i++) order.Add(i);
        if (shuffleOnStart) Shuffle(order);

        int idx = 0;
        while (isRunning)
        {
            var clip = clips[order[idx]];
            if (clip != null)
            {
                a.Stop();
                a.clip = clip;
                a.volume = volume;
                a.Play();

                // ждём окончания трека, но не дольше, чем его длина + небольшой запас
                float t = Mathf.Max(0.1f, clip.length + 0.1f);
                float elapsed = 0f;
                while (isRunning && a.isPlaying && elapsed < t)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            idx++;
            if (idx >= order.Count)
            {
                if (!loopPlaylist) break;
                idx = 0;
                if (shuffleOnStart) Shuffle(order); // перемешивать каждый круг
            }
        }
        // если сценка ещё идёт, а лупа нет — просто молча ждём конца сценки
        playlistCo = null;
    }

    void StopPlaylist()
    {
        if (playlistCo != null) { StopCoroutine(playlistCo); playlistCo = null; }
        if (a) a.Stop();
    }

    void Shuffle(List<int> arr)
    {
        for (int i = arr.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }

    // На всякий случай — стопнем всё при выключении объекта
    void OnDisable()
    {
        StopPlaylist();
        if (pulseTarget) pulseTarget.localScale = pulseBaseScale;
        isRunning = false;
        pulseEnabled = pulseWhenIdle;
    }
}
