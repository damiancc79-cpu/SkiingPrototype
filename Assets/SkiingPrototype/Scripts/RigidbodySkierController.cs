using System;
using UnityEngine;

namespace SkiingPrototype
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SkiInputSource))]
    [RequireComponent(typeof(SlopeProbe))]
    [RequireComponent(typeof(SkierCrashHandler))]
    public class RigidbodySkierController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform resetPoint;

        [Header("Slope Movement")]
        [SerializeField] private float downhillAcceleration = 32f;
        [SerializeField] private float minSlopeAngleForAcceleration = 3f;
        [SerializeField] private float fullAccelerationSlopeAngle = 38f;
        [SerializeField] private float maxSpeed = 36f;
        [SerializeField] private float groundStickForce = 18f;

        [Header("Carving")]
        [SerializeField] private float carveAcceleration = 28f;
        [SerializeField] private float carveVelocityRedirect = 3.5f;
        [SerializeField] private float rotationSharpness = 10f;

        [Header("Braking And Friction")]
        [SerializeField] private float brakingAcceleration = 34f;
        [SerializeField] private float groundFriction = 2.2f;
        [SerializeField] private float lateralSkidFriction = 3f;
        [SerializeField] private float groundDrag = 0.12f;
        [SerializeField] private float airDrag = 0.04f;

        [Header("Air Control")]
        [SerializeField] private float airControlAcceleration = 8f;
        [SerializeField] private float airRotationSharpness = 3.5f;

        [Header("Dynamic Snow")]
        [SerializeField] private float snowFrictionMultiplier = 1f;
        [SerializeField] private float snowCarveMultiplier = 1f;

        [Header("Reset")]
        [SerializeField] private bool handleResetInput = true;
        [SerializeField] private Vector3 fallbackStartPosition = new Vector3(0f, 18f, 0f);
        [SerializeField] private Vector3 fallbackStartEuler = new Vector3(18f, 180f, 0f);

        private Rigidbody body;
        private SkiInputSource input;
        private SlopeProbe slopeProbe;
        private SkierCrashHandler crashHandler;
        private bool wasGrounded;
        private float airborneStartYVelocity;
        private float landingStateUntil;

        public SlopeHit Ground { get; private set; }
        public SkierMovementState MovementState { get; private set; }
        public float Speed => body.linearVelocity.magnitude;
        public float PlanarSpeed { get; private set; }
        public Rigidbody PhysicsBody => body;
        public event Action ResetInputPressed;
        public event Action<float, SlopeHit> Landed;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            input = GetComponent<SkiInputSource>();
            slopeProbe = GetComponent<SlopeProbe>();
            crashHandler = GetComponent<SkierCrashHandler>();

            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void Start()
        {
            if (resetPoint == null)
            {
                GameObject resetObject = GameObject.FindWithTag("Respawn");
                if (resetObject != null)
                {
                    resetPoint = resetObject.transform;
                }
            }
        }

        private void Update()
        {
            if (input.ResetPressed)
            {
                ResetInputPressed?.Invoke();
                if (handleResetInput)
                {
                    ResetToStart();
                }
            }
        }

        private void FixedUpdate()
        {
            Ground = slopeProbe.Probe();
            body.linearDamping = Ground.IsGrounded ? groundDrag : airDrag;

            if (crashHandler.IsCrashed)
            {
                MovementState = SkierMovementState.Crashed;
                wasGrounded = Ground.IsGrounded;
                return;
            }

            // The controller owns normal ski orientation via MoveRotation. Leaving collision
            // angular velocity active makes the capsule tumble/spin after a landing; crashes
            // return above, so their intentional torque remains intact.
            body.angularVelocity = Vector3.zero;

            if (Ground.IsGrounded)
            {
                TickGrounded();
            }
            else
            {
                TickAirborne();
            }

            wasGrounded = Ground.IsGrounded;
            LimitPlanarSpeed();
        }

        private void TickGrounded()
        {
            Vector3 normal = Ground.Normal;
            Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, normal).normalized;
            if (downhill.sqrMagnitude < 0.001f)
            {
                downhill = Vector3.ProjectOnPlane(transform.forward, normal).normalized;
            }

            Vector3 planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, normal);
            PlanarSpeed = planarVelocity.magnitude;

            if (!wasGrounded)
            {
                float downwardSpeed = Mathf.Max(0f, -airborneStartYVelocity);
                Landed?.Invoke(downwardSpeed, Ground);
                crashHandler.EvaluateLanding(Ground, downwardSpeed);
                landingStateUntil = Time.time + 0.35f;
            }

            float slopeFactor = Mathf.InverseLerp(minSlopeAngleForAcceleration, fullAccelerationSlopeAngle, Ground.Angle);
            body.AddForce(downhill * downhillAcceleration * slopeFactor, ForceMode.Acceleration);
            body.AddForce(-normal * groundStickForce, ForceMode.Acceleration);

            Vector3 lateral = Vector3.Cross(normal, downhill).normalized;
            body.AddForce(lateral * input.Carve * carveAcceleration * snowCarveMultiplier, ForceMode.Acceleration);

            if (planarVelocity.sqrMagnitude > 0.05f)
            {
                Vector3 targetVelocityDirection = Vector3.Slerp(
                    planarVelocity.normalized,
                    (planarVelocity.normalized + lateral * input.Carve * 0.45f).normalized,
                    carveVelocityRedirect * Time.fixedDeltaTime);

                Vector3 redirected = targetVelocityDirection * planarVelocity.magnitude;
                body.linearVelocity += (redirected - planarVelocity) * Mathf.Clamp01(carveVelocityRedirect * Time.fixedDeltaTime);

                body.AddForce(-planarVelocity.normalized * groundFriction * snowFrictionMultiplier, ForceMode.Acceleration);
                body.AddForce(-lateral * Vector3.Dot(planarVelocity, lateral) * lateralSkidFriction * snowFrictionMultiplier, ForceMode.Acceleration);

                if (input.Brake > 0.01f)
                {
                    body.AddForce(-planarVelocity.normalized * brakingAcceleration * input.Brake, ForceMode.Acceleration);
                }
            }

            RotateTowardVelocity(normal, planarVelocity, rotationSharpness);

            if (Time.time < landingStateUntil)
            {
                MovementState = SkierMovementState.Landing;
            }
            else if (input.Brake > 0.15f)
            {
                MovementState = SkierMovementState.Braking;
            }
            else if (Mathf.Abs(input.Carve) > 0.1f)
            {
                MovementState = SkierMovementState.Carving;
            }
            else
            {
                MovementState = SkierMovementState.Grounded;
            }
        }

        private void TickAirborne()
        {
            if (wasGrounded)
            {
                airborneStartYVelocity = body.linearVelocity.y;
            }

            Vector3 airForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 airRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            body.AddForce((airRight * input.Carve + airForward * (1f - input.Brake)) * airControlAcceleration, ForceMode.Acceleration);

            RotateTowardVelocity(Vector3.up, Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up), airRotationSharpness);
            PlanarSpeed = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude;
            MovementState = SkierMovementState.Airborne;
        }

        private void RotateTowardVelocity(Vector3 up, Vector3 planarVelocity, float sharpness)
        {
            Vector3 forward = planarVelocity.sqrMagnitude > 0.2f
                ? planarVelocity.normalized
                : Vector3.ProjectOnPlane(transform.forward, up).normalized;

            if (forward.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion target = Quaternion.LookRotation(forward, up);
            Quaternion next = Quaternion.Slerp(body.rotation, target, 1f - Mathf.Exp(-sharpness * Time.fixedDeltaTime));
            body.MoveRotation(next);
        }

        private void LimitPlanarSpeed()
        {
            Vector3 vertical = Vector3.Project(body.linearVelocity, Ground.IsGrounded ? Ground.Normal : Vector3.up);
            Vector3 planar = body.linearVelocity - vertical;
            if (planar.magnitude > maxSpeed)
            {
                body.linearVelocity = planar.normalized * maxSpeed + vertical;
            }
        }

        public void ResetToStart()
        {
            Transform start = resetPoint;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            crashHandler.ClearCrash();

            if (start != null)
            {
                body.position = start.position;
                body.rotation = start.rotation;
            }
            else
            {
                body.position = fallbackStartPosition;
                body.rotation = Quaternion.Euler(fallbackStartEuler);
            }

            MovementState = SkierMovementState.Grounded;
            landingStateUntil = 0f;
            wasGrounded = false;
        }

        public void SetResetPoint(Transform newResetPoint)
        {
            resetPoint = newResetPoint;
        }

        public void SetHandleResetInput(bool shouldHandleResetInput)
        {
            handleResetInput = shouldHandleResetInput;
        }

        /// <summary>Called by the snow system. Packed tracks are fast but shed lateral grip.</summary>
        public void SetSnowResponse(float frictionMultiplier, float carveMultiplier)
        {
            snowFrictionMultiplier = Mathf.Clamp(frictionMultiplier, 0.55f, 1.5f);
            snowCarveMultiplier = Mathf.Clamp(carveMultiplier, 0.55f, 1.35f);
        }
    }
}
