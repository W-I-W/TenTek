using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class GhostFirstPersonController : MonoBehaviour
{
    [Header("Camera")]
    public Camera cam;
    [Range(0.1f, 2f)] public float lookSensitivity = 0.75f;
    [Range(0.01f, 1f)] public float lookSmoothing = 0.92f;
    public float pitchClamp = 85f;
    public float rollAmount = 8f;
    public float rollLerp = 10f;

    [Header("Movement (m/s)")]
    public float cruiseSpeed = 4.5f;
    public float sprintSpeed = 10f;
    public float accel = 12f;
    public float decel = 14f;
    public float verticalSpeed = 4f;
    [Range(0f, 3f)] public float airDrag = 1.2f;

    [Header("Hover")]
    public float hoverAmplitude = 0.05f;
    public float hoverFrequency = 1.2f;

    [Header("Ghost Size & Collisions")]
    public float ghostScale = 0.35f;
    public bool startNoClip = false;

    [Header("Quality of Life")]
    public bool lockCursor = true;

    // ==== Interaction (E) ====
    [Header("Interaction (E)")]
    [Tooltip("Макс. дистанция взаимодействия")]
    public float interactDistance = 3.0f;
    [Tooltip("Радиус сферкаста для попадания по объекту")]
    public float interactSphereRadius = 0.35f;
    [Tooltip("Какие слои считаем кликабельными")]
    public LayerMask interactMask = ~0;
    public QueryTriggerInteraction interactTriggers = QueryTriggerInteraction.Collide;
    public bool showInteractGizmos = false;
    public bool showInteractLogs = false;

    // --- runtime
    Rigidbody rb;

    // Input Actions
    InputAction moveAction, lookAction, ascendAction, descendAction, sprintAction, noclipToggleAction, interactAction;

    // state
    float yaw, pitch;
    Vector2 lookSmoothed;
    float hoverPhase;
    bool noClip;

    // interaction state
    IInteractable currentTarget;
    RaycastHit lastHit;

    void OnEnable()
    {
        SetupInput();
        if (lockCursor) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }

    void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        ascendAction?.Disable();
        descendAction?.Disable();
        sprintAction?.Disable();
        noclipToggleAction?.Disable();
        interactAction?.Disable();
        if (lockCursor) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // размер духа
        transform.localScale = Vector3.one * ghostScale;

        noClip = startNoClip;
        ApplyNoClip(noClip);

        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>();
            if (cam == null)
            {
                Debug.LogWarning("GhostFirstPersonController: не найдена Camera — создам автоматически.");
                GameObject c = new GameObject("GhostCamera");
                c.transform.SetParent(transform, false);
                c.AddComponent<AudioListener>();
                cam = c.AddComponent<Camera>();
                cam.nearClipPlane = 0.02f;
                cam.transform.localPosition = new Vector3(0, 0.0f, 0f);
            }
        }
        cam.nearClipPlane = 0.02f;
    }

    void SetupInput()
    {
        var map = new InputActionMap("Ghost");

        moveAction = map.AddAction("Move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        lookAction = map.AddAction("Look");
        lookAction.AddBinding("<Mouse>/delta");
        lookAction.AddBinding("<Gamepad>/rightStick");

        ascendAction = map.AddAction("Ascend", binding: "<Keyboard>/space");
        descendAction = map.AddAction("Descend", binding: "<Keyboard>/leftCtrl");
        sprintAction = map.AddAction("Sprint", binding: "<Keyboard>/leftShift");

        noclipToggleAction = map.AddAction("NoClipToggle", binding: "<Keyboard>/n");
        noclipToggleAction.performed += _ => ToggleNoClip();

        // >>> Interact (E)
        interactAction = map.AddAction("Interact", binding: "<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonSouth"); // A / Cross

        map.Enable();
    }

    void Update()
    {
        if (CursorData.isCursorVisible) return;
        // ===== Mouse look =====
        Vector2 look = lookAction.ReadValue<Vector2>() * lookSensitivity;
        lookSmoothed = Vector2.Lerp(lookSmoothed, look, 1f - lookSmoothing);

        yaw += lookSmoothed.x;
        pitch -= lookSmoothed.y;
        pitch = Mathf.Clamp(pitch, -pitchClamp, pitchClamp);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cam.transform.localRotation = Quaternion.Euler(pitch, 0f, ComputeRoll());

        // hover
        hoverPhase += Time.deltaTime * hoverFrequency * Mathf.PI * 2f;
        float hoverOffset = Mathf.Sin(hoverPhase) * hoverAmplitude;
        Vector3 camLocal = cam.transform.localPosition;
        camLocal.y = hoverOffset;
        cam.transform.localPosition = camLocal;

        // ===== Interaction scan & press =====
        ScanFront();

        if (interactAction != null && interactAction.triggered)
        {
            if (currentTarget != null)
            {
                if (showInteractLogs) Debug.Log($"[Ghost] Interact E -> {lastHit.collider.name}");
                currentTarget.Interact();
            }
            else if (showInteractLogs)
            {
                Debug.Log("[Ghost] E pressed, but no IInteractable under crosshair.");
            }
        }
    }

    float ComputeRoll()
    {
        Vector2 mv = moveAction.ReadValue<Vector2>();
        float targetRoll = -mv.x * rollAmount;
        return Mathf.LerpAngle(cam.transform.localEulerAngles.z, targetRoll, Time.deltaTime * rollLerp);
    }

    void FixedUpdate()
    {
        if (CursorData.isCursorVisible)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }
        Vector2 mv = moveAction.ReadValue<Vector2>();
        bool sprint = sprintAction.IsPressed();
        bool ascend = ascendAction.IsPressed();
        bool descend = descendAction.IsPressed();

        float targetSpeed = sprint ? sprintSpeed : cruiseSpeed;

        Vector3 wishDir = (transform.forward * mv.y + transform.right * mv.x).normalized;
        Vector3 desired = wishDir * targetSpeed;

        float v = 0f;
        if (ascend) v += verticalSpeed;
        if (descend) v -= verticalSpeed;
        desired += Vector3.up * v;

        Vector3 vel = rb.linearVelocity; // оставляю как у тебя

        Vector3 horizVel = new Vector3(vel.x, 0f, vel.z);
        Vector3 horizDesired = new Vector3(desired.x, 0f, desired.z);

        float usedAccel = (horizDesired.sqrMagnitude > 0.01f) ? accel : decel;
        horizVel = Vector3.MoveTowards(horizVel, horizDesired, usedAccel * Time.fixedDeltaTime);

        float newY = Mathf.MoveTowards(vel.y, desired.y, (accel + 6f) * Time.fixedDeltaTime);

        Vector3 newVel = new Vector3(horizVel.x, newY, horizVel.z);
        newVel = Vector3.Lerp(newVel, Vector3.zero, Time.fixedDeltaTime * airDrag);

        rb.linearVelocity = newVel; // оставляю как у тебя
    }

    // ==== Interaction helpers ====
    void ScanFront()
    {
        currentTarget = null;
        if (!cam) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        // Сначала SphereCast, чтобы легче попадать
        if (Physics.SphereCast(ray, interactSphereRadius, out lastHit, interactDistance, interactMask, interactTriggers))
        {
            currentTarget = GetInteractableFromCollider(lastHit.collider);
            if (currentTarget == null && showInteractLogs)
                Debug.Log($"[Ghost] Hit {lastHit.collider.name}, IInteractable not found on object/parents.");
            return;
        }

        // Фолбэк — обычный Raycast
        if (Physics.Raycast(ray, out lastHit, interactDistance, interactMask, interactTriggers))
        {
            currentTarget = GetInteractableFromCollider(lastHit.collider);
        }
    }

    IInteractable GetInteractableFromCollider(Collider c)
    {
        if (!c) return null;
        return c.GetComponentInParent<IInteractable>();
    }

    void ToggleNoClip()
    {
        noClip = !noClip;
        ApplyNoClip(noClip);
    }

    void ApplyNoClip(bool enabled)
    {
        rb.detectCollisions = !enabled;
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = !enabled;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!showInteractGizmos || !cam) return;
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.3f);
        var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * interactDistance);
        Gizmos.DrawWireSphere(ray.origin + ray.direction * interactDistance, interactSphereRadius);
    }
#endif
}
