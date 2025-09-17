using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TvImageSlideshow : MonoBehaviour
{
    [Header("Screen (mesh)")]
    public Renderer screenRenderer;
    [Tooltip("��� ����������� �����. ����� = ���� (_BaseMap ��� _MainTex)")]
    public string texturePropertyName = "";

    [Header("Images")]
    public Texture[] images;            // Texture2D / RenderTexture
    public Sprite[] spriteImages;      // ����� ������ � �������
    public Texture idleTexture;

    [Header("Playback")]
    public bool playOnAwake = false;
    public float intervalSeconds = 2.0f;
    public bool shuffleOnStart = false;
    public bool shuffleEachLoop = false;
    public bool loop = true;

    public enum FitMode { Contain, Cover, Stretch }

    [Header("Fitting")]
    [Tooltip("��������� ��������� ������� ��������")]
    public bool preserveAspect = true;
    public FitMode fit = FitMode.Contain;

    public enum ScreenPlane { XY, XZ, YZ }
    [Tooltip("��������� ��������� ������ ���� (��� ���� �����)")]
    public ScreenPlane screenPlane = ScreenPlane.XY;

    [Header("Screen Aspect Override")]
    [Tooltip("������������ ������� ���� � ������������ �������� ������ ������")]
    public bool useCustomScreenAspect = true;
    [Tooltip("����������� ������ ������ (������/������). ��� �� ������ 16:9 = 1.7778")]
    public float customScreenAspect = 16f / 9f;

    [Tooltip("���� UV, ���� �������� ����������/����������")]
    public bool flipX = false;
    public bool flipY = false;

    [Header("Debug")]
    public bool debugLogs = false;

    // runtime
    int texPropId = -1;
    string texPropName = "_MainTex";
    Texture originalTexture;
    Material matInstance;
    Coroutine loopCo;
    int index;
    readonly List<Texture> playlist = new List<Texture>();

    void Awake()
    {
        if (!screenRenderer)
        {
            screenRenderer = GetComponentInChildren<Renderer>();
            if (!screenRenderer) { Debug.LogWarning("[Slideshow] ScreenRenderer �� ��������."); return; }
        }

        texPropName = ResolveTexturePropertyName();
        texPropId = Shader.PropertyToID(texPropName);
        matInstance = screenRenderer.material;
        originalTexture = matInstance.GetTexture(texPropId);

        BuildPlaylist();

        if (idleTexture) ApplyTexture(idleTexture, false);
        if (playOnAwake) StartShow();
    }

    string ResolveTexturePropertyName()
    {
        if (!string.IsNullOrWhiteSpace(texturePropertyName)) return texturePropertyName;
        var mat = screenRenderer ? screenRenderer.sharedMaterial : null;
        if (mat != null && mat.HasProperty("_BaseMap")) return "_BaseMap";
        return "_MainTex";
    }

    void BuildPlaylist()
    {
        playlist.Clear();
        if (images != null) foreach (var t in images) if (t) playlist.Add(t);
        if (spriteImages != null) foreach (var s in spriteImages) if (s && s.texture) playlist.Add(s.texture);
    }

    public void StartShow()
    {
        if (playlist.Count == 0) { Debug.LogWarning("[Slideshow] ��� ��������."); return; }
        StopShow(false);
        if (shuffleOnStart) Shuffle(playlist);
        index = 0;
        loopCo = StartCoroutine(Loop());
        if (debugLogs) Debug.Log("[Slideshow] Start");
    }

    public void StopShow(bool showIdle = true)
    {
        if (loopCo != null) { StopCoroutine(loopCo); loopCo = null; }
        ApplyTexture(showIdle ? (idleTexture ? idleTexture : originalTexture) : originalTexture, false);
        if (debugLogs) Debug.Log("[Slideshow] Stop");
    }

    IEnumerator Loop()
    {
        while (true)
        {
            var tex = playlist[index];
            ApplyTexture(tex, true);

            yield return new WaitForSeconds(Mathf.Max(0.01f, intervalSeconds));

            index++;
            if (index >= playlist.Count)
            {
                if (!loop) { loopCo = null; yield break; }
                index = 0;
                if (shuffleEachLoop) Shuffle(playlist);
            }
        }
    }

    void ApplyTexture(Texture tex, bool applyFit)
    {
        if (!matInstance || texPropId == -1) return;
        matInstance.SetTexture(texPropId, tex);

        if (applyFit && (preserveAspect || flipX || flipY))
            ApplyFitAndFlip(tex);
        else
        {
            matInstance.SetTextureScale(texPropName, new Vector2(flipX ? -1f : 1f, flipY ? -1f : 1f));
            matInstance.SetTextureOffset(texPropName, new Vector2(flipX ? 1f : 0f, flipY ? 1f : 0f));
        }
    }

    void ApplyFitAndFlip(Texture tex)
    {
        if (!tex) return;

        // --- ������ ������ ---
        float screenAspect;
        if (useCustomScreenAspect)
        {
            screenAspect = Mathf.Max(0.0001f, customScreenAspect);
        }
        else
        {
            var ls = screenRenderer.transform.lossyScale;
            float w, h;
            switch (screenPlane)
            {
                case ScreenPlane.XZ: w = Mathf.Abs(ls.x); h = Mathf.Abs(ls.z); break;
                case ScreenPlane.YZ: w = Mathf.Abs(ls.z); h = Mathf.Abs(ls.y); break;
                default: w = Mathf.Abs(ls.x); h = Mathf.Abs(ls.y); break; // XY
            }
            screenAspect = Mathf.Max(0.0001f, w / Mathf.Max(0.0001f, h));
        }

        // --- ������ �������� ---
        float texAspect = 1f;
        if (tex is Texture2D t2) texAspect = Mathf.Max(0.0001f, (float)t2.width / Mathf.Max(1, t2.height));
        else if (tex is RenderTexture rt) texAspect = Mathf.Max(0.0001f, (float)rt.width / Mathf.Max(1, rt.height));

        Vector2 tiling = Vector2.one;
        Vector2 offset = Vector2.zero;

        if (!preserveAspect || fit == FitMode.Stretch)
        {
            tiling = Vector2.one; offset = Vector2.zero;
        }
        else if (fit == FitMode.Contain)
        {
            bool screenWider = screenAspect >= texAspect;
            if (screenWider)
            {   // ��������� �� ������, �����/������ ������
                float tileX = texAspect / screenAspect;
                tiling = new Vector2(tileX, 1f);
                offset = new Vector2((1f - tileX) * 0.5f, 0f);
            }
            else
            {   // ��������� �� ������, ������/����� ������
                float tileY = screenAspect / texAspect;
                tiling = new Vector2(1f, tileY);
                offset = new Vector2(0f, (1f - tileY) * 0.5f);
            }
        }
        else // Cover � ������� ��, ������� ������
        {
            bool screenWider = screenAspect >= texAspect;
            if (screenWider)
            {   // ��������� ������, ������� �� ������
                float tileY = screenAspect / texAspect;
                tiling = new Vector2(1f, tileY);
                offset = new Vector2(0f, (1f - tileY) * 0.5f);
            }
            else
            {   // ��������� ������, ������� �� ������
                float tileX = texAspect / screenAspect;
                tiling = new Vector2(tileX, 1f);
                offset = new Vector2((1f - tileX) * 0.5f, 0f);
            }
        }

        // �����
        if (flipX) { tiling.x *= -1f; offset.x = 1f - offset.x; }
        if (flipY) { tiling.y *= -1f; offset.y = 1f - offset.y; }

        matInstance.SetTextureScale(texPropName, tiling);
        matInstance.SetTextureOffset(texPropName, offset);
    }

    void Shuffle<T>(IList<T> arr)
    {
        for (int i = arr.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }

    void OnDestroy()
    {
        if (matInstance && texPropId != -1)
            matInstance.SetTexture(texPropId, originalTexture);
    }
}
