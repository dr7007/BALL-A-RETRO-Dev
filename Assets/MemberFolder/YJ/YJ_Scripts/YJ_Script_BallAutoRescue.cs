using System;
using PSH;
using UnityEngine;

// Opt-in regions attached to fixed obstacles, not a global idle-ball timer.
[DisallowMultipleComponent]
[RequireComponent(typeof(YJ_Script_BallController), typeof(Rigidbody), typeof(SphereCollider))]
public sealed class YJ_Script_BallAutoRescue : MonoBehaviour
{
    [Serializable]
    public sealed class RescueZone
    {
        public string label;
        public Transform anchor;
        public Bounds localBounds;
        public Vector3 localPushDirection;
        public Vector3 localRecoveryPosition;

        public bool Contains(Vector3 position) => anchor != null && anchor.gameObject.activeInHierarchy
            && localBounds.Contains(anchor.InverseTransformPoint(position));
    }

    [SerializeField] private RescueZone[] zones = Array.Empty<RescueZone>();
    [SerializeField] private KHS_Script_PlungerController plunger;
    [Tooltip("Seconds of confinement before one gentle push.")]
    [Min(0.1f)] [SerializeField] private float stuckSeconds = 3f;
    [Tooltip("Additional seconds of confinement before checking the recovery position.")]
    [Min(0.1f)] [SerializeField] private float recoverySeconds = 2f;
    [Tooltip("World-space movement needed to count as progress (ball diameter is 0.25).")]
    [Min(0.01f)] [SerializeField] private float movementRadius = 0.18f;
    [Min(0.01f)] [SerializeField] private float pushSpeed = 0.8f;
    [Min(0f)] [SerializeField] private float clearance = 0.01f;

    private YJ_Script_BallController ball;
    private Rigidbody body;
    private SphereCollider sphere;
    private RescueZone trackedZone;
    private Vector3 progressPosition;
    private float confinedTime;
    private bool pushed;
    private bool monitorOpen;
    private bool roundUIOpen;
    private readonly Collider[] overlaps = new Collider[32];

    private void Awake()
    {
        ball = GetComponent<YJ_Script_BallController>();
        body = GetComponent<Rigidbody>();
        sphere = GetComponent<SphereCollider>();
    }

    private void OnEnable()
    {
        monitorOpen = roundUIOpen = false;
        ResetTracking();
        KHS_Script_CameraManager.MonitorEvt += OnMonitor;
        KHS_Script_UIImgFunc.RoundUIEvt += OnRoundUI;
        KHS_Script_ResetController.OnReset += ResetTracking;
    }

    private void OnDisable()
    {
        KHS_Script_CameraManager.MonitorEvt -= OnMonitor;
        KHS_Script_UIImgFunc.RoundUIEvt -= OnRoundUI;
        KHS_Script_ResetController.OnReset -= ResetTracking;
        ResetTracking();
    }

    private void OnMonitor(bool open) { monitorOpen = open; ResetTracking(); }
    private void OnRoundUI(bool open) { roundUIOpen = open; ResetTracking(); }

    private bool CanObserve => Time.timeScale > 0f && ball != null && ball.CanAutoRescue
        && sphere.enabled && !sphere.isTrigger && body.detectCollisions
        && KHS_Script_ScoreManager.Instance != null && KHS_Script_ScoreManager.Instance.IsPlaying
        && plunger != null && !plunger.isBallReady && !plunger.tutorialForceLock
        && !monitorOpen && !roundUIOpen && !PSH_Script_SettingController.IsMenuOpen;

    private void Update()
    {
        // FixedUpdate does not run at timeScale zero: discard pre-pause progress here.
        if (!CanObserve) ResetTracking();
    }

    private void FixedUpdate()
    {
        if (!CanObserve) { ResetTracking(); return; }

        Vector3 position = body.position;
        RescueZone zone = null;
        foreach (RescueZone candidate in zones)
        {
            if (candidate != null && candidate.Contains(position)) { zone = candidate; break; }
        }
        if (zone == null) { ResetTracking(); return; }

        // Compare displacement, not velocity: a wedged ball can jitter at nonzero speed.
        if (zone != trackedZone || (position - progressPosition).sqrMagnitude > movementRadius * movementRadius)
        {
            trackedZone = zone;
            progressPosition = position;
            confinedTime = 0f;
            pushed = false;
            return;
        }

        confinedTime += Time.fixedDeltaTime;
        if (confinedTime < (pushed ? recoverySeconds : stuckSeconds)) return;
        Vector3 direction = Vector3.ProjectOnPlane(zone.anchor.TransformDirection(zone.localPushDirection), Vector3.up).normalized;
        if (direction.sqrMagnitude < 0.5f) { ResetTracking(); return; }

        confinedTime = 0f;
        if (!pushed)
        {
            pushed = true;
            body.AddForce(direction * pushSpeed, ForceMode.VelocityChange);
            body.WakeUp();
            return;
        }

        Vector3 destination = zone.anchor.TransformPoint(zone.localRecoveryPosition);
        if (!IsRecoveryPositionClear(destination)) return;

        // Do not call ball-out/reset: score, credits, flipper uses and camera state stay intact.
        body.position = destination;
        body.linearVelocity = direction * pushSpeed;
        body.angularVelocity = Vector3.zero;
        body.WakeUp();
        Debug.Log($"[BallAutoRescue] Recovered ball from {zone.label}.", this);
        ResetTracking();
    }

    private bool IsRecoveryPositionClear(Vector3 destination)
    {
        Vector3 scale = sphere.transform.lossyScale;
        float radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        // A recovery region must never transfer the ball between floors.
        if (Mathf.Abs(destination.y - body.position.y) > radius * 0.5f) return false;
        Vector3 center = destination + sphere.transform.TransformVector(sphere.center);
        // This table locks the ball above its floor (center Y 0.25, radius 0.125).
        if (!Physics.Raycast(center, Vector3.down, out RaycastHit support, radius * 3f,
            Physics.AllLayers, QueryTriggerInteraction.Ignore)
            || support.collider.attachedRigidbody == body || support.normal.y < 0.8f
            || support.distance < radius - 0.005f) return false;
        int count = Physics.OverlapSphereNonAlloc(center, radius + clearance, overlaps,
            Physics.AllLayers, QueryTriggerInteraction.Collide);
        if (count == overlaps.Length) return false;
        for (int i = 0; i < count; i++)
        {
            Collider other = overlaps[i];
            if (other.attachedRigidbody == body) continue;
            // Never recover directly into a portal, scoring switch or other trigger.
            if (other.isTrigger) return false;
            if (other != support.collider) return false;
            // A floor mesh may also contain walls; do not exempt its penetrating faces.
            if (Physics.ComputePenetration(sphere, destination, body.rotation,
                other, other.transform.position, other.transform.rotation, out _, out float depth)
                && depth > 0.002f) return false;
        }
        return true;
    }

    private void ResetTracking()
    {
        trackedZone = null;
        confinedTime = 0f;
        pushed = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (zones == null) return;
        Matrix4x4 previous = Gizmos.matrix;
        foreach (RescueZone zone in zones)
        {
            if (zone == null || zone.anchor == null) continue;
            Gizmos.matrix = zone.anchor.localToWorldMatrix;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(zone.localBounds.center, zone.localBounds.size);
            Gizmos.matrix = Matrix4x4.identity;
            Vector3 center = zone.anchor.TransformPoint(zone.localBounds.center);
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(center, zone.anchor.TransformDirection(zone.localPushDirection).normalized * 0.5f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(zone.anchor.TransformPoint(zone.localRecoveryPosition), 0.125f);
        }
        Gizmos.matrix = previous;
    }
}
