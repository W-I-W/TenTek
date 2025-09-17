using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class TelekinesisGrabber : MonoBehaviour
{
    [Header("Refs")] public Camera cam;

    [Header("Filter")]
    public LayerMask telekinesisMask = ~0;
    public float maxGrabMass = 200f;
    public bool allowKinematic = false;

    [Header("Grab Settings")]
    public float maxGrabDistance = 10f;
    public float holdDistance = 3f;
    public float minHoldDistance = 1.2f;
    public float maxHoldDistance = 8f;

    [Header("Motion Feel")]
    public float followStrength = 140f;
    public float damping = 18f;
    public float maxForce = 800f;
    public float rotateStrength = 5f;
    public float throwForce = 12f;
    public float grabbedDrag = 8f;
    public float grabbedAngularDrag = 8f;

    [Header("Beam")]
    public bool showBeam = true;
    public float beamWidth = 0.03f;
    public Color beamColor = new Color(0.55f, 0.85f, 1f, 0.9f);

    Rigidbody grabbed;
    float originalDrag, originalAngDrag;
    bool wasGravity;
    float targetYaw;
    [SerializeField] private LineRenderer beam;
    Material beamMat;

    void Awake()
    {
        if (!cam) cam = GetComponentInChildren<Camera>();
        if (showBeam)
        {
            beamMat = new Material(Shader.Find("Sprites/Default")); beamMat.color = beamColor;
        }
    }
    void OnDestroy()
    {
        if (beamMat) { if (Application.isPlaying) Destroy(beamMat); else DestroyImmediate(beamMat); }
    }

    void Update()
    {
        if (Mouse.current == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
            holdDistance = Mathf.Clamp(holdDistance + Mathf.Sign(scroll) * 0.3f, minHoldDistance, maxHoldDistance);

        if (Mouse.current.leftButton.wasPressedThisFrame) TryBeginGrab();
        if (Mouse.current.leftButton.wasReleasedThisFrame) EndGrab(false);

        if (grabbed && Mouse.current.rightButton.wasPressedThisFrame) EndGrab(true);

        if (grabbed && Keyboard.current != null)
        {
            if (Keyboard.current.qKey.isPressed) targetYaw -= rotateStrength;
            if (Keyboard.current.eKey.isPressed) targetYaw += rotateStrength;
        }
        else targetYaw = 0f;

        if (beam)
        {
            if (grabbed)
            {
                beam.enabled = true;
                beam.SetPosition(0, cam.transform.position);
                beam.SetPosition(1, grabbed.worldCenterOfMass);
            }
            else beam.enabled = false;
        }
    }

    void FixedUpdate()
    {
        if (!grabbed) return;

        if (Vector3.Distance(cam.transform.position, grabbed.worldCenterOfMass) > maxGrabDistance * 1.3f)
        { EndGrab(false); return; }

        Vector3 targetPos = cam.transform.position + cam.transform.forward * holdDistance;
        Quaternion look = Quaternion.LookRotation(cam.transform.forward, Vector3.up) * Quaternion.Euler(0, targetYaw, 0);

        if (grabbed.isKinematic)
        {
            grabbed.MovePosition(Vector3.Lerp(grabbed.position, targetPos, 0.35f));
            grabbed.MoveRotation(Quaternion.Slerp(grabbed.rotation, look, 0.35f));
            return;
        }

        Vector3 toTarget = targetPos - grabbed.worldCenterOfMass;
        Vector3 desiredVel = toTarget * (followStrength * Time.fixedDeltaTime);
        Vector3 force = (desiredVel - grabbed.linearVelocity) * damping;
        if (force.magnitude > maxForce) force = force.normalized * maxForce;
        grabbed.AddForce(force, ForceMode.Acceleration);

        Quaternion delta = look * Quaternion.Inverse(grabbed.rotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;
        Vector3 angVel = axis * (angle * Mathf.Deg2Rad / Time.fixedDeltaTime);
        grabbed.angularVelocity = Vector3.Lerp(grabbed.angularVelocity, angVel, 0.08f);
    }

    void TryBeginGrab()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (!Physics.Raycast(ray, out var hit, maxGrabDistance, telekinesisMask, QueryTriggerInteraction.Ignore))
            return;

        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb == null) return;

        if (rb.isKinematic && !allowKinematic) return;
        if (rb.mass > maxGrabMass) return;

        // <<< НОВОЕ: запомним «домашнюю» позу в момент начала захвата
        var home = rb.GetComponent<ReturnHome>();
        if (home) home.SavePickupPose();

        grabbed = rb;

        wasGravity = grabbed.useGravity;
        originalDrag = grabbed.linearDamping;
        originalAngDrag = grabbed.angularDamping;

        if (!grabbed.isKinematic)
        {
            grabbed.useGravity = false;
            grabbed.linearDamping = grabbedDrag;
            grabbed.angularDamping = grabbedAngularDrag;
        }

        holdDistance = Mathf.Clamp(Vector3.Distance(cam.transform.position, hit.point), minHoldDistance, maxHoldDistance);
    }

    void EndGrab(bool withThrow)
    {
        if (!grabbed) return;

        if (withThrow && !grabbed.isKinematic)
            grabbed.AddForce(cam.transform.forward * throwForce, ForceMode.VelocityChange);

        if (!grabbed.isKinematic)
        {
            grabbed.useGravity = wasGravity;
            grabbed.linearDamping = originalDrag;
            grabbed.angularDamping = originalAngDrag;
        }

        grabbed = null; targetYaw = 0f;
        if (beam) beam.enabled = false;
    }
}
