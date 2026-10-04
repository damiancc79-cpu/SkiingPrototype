using System;
using UnityEngine;

namespace SkiingPrototype
{
    [RequireComponent(typeof(Rigidbody))]
    public class SkierCrashHandler : MonoBehaviour
    {
        [Header("Impact Crash")]
        [SerializeField] private float crashRelativeImpactSpeed = 17f;
        [SerializeField] private float crashCooldown = 0.25f;

        [Header("Landing Crash")]
        [SerializeField] private float maxLandingSlopeAngle = 58f;
        [SerializeField] private float maxLandingUpDeviation = 50f;
        [SerializeField] private float hardLandingVerticalSpeed = 15f;

        [Header("Crash Response")]
        [SerializeField] private float crashAngularImpulse = 8f;
        [SerializeField] private float postCrashDrag = 1.8f;

        private Rigidbody body;
        private float originalDrag;
        private float lastCrashTime = -999f;

        public bool IsCrashed { get; private set; }
        public string CrashReason { get; private set; } = string.Empty;
        public event Action<string> Crashed;
        public event Action<string, float, Vector3> CrashImpact;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            originalDrag = body.linearDamping;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsCrashed || Time.time - lastCrashTime < crashCooldown)
            {
                return;
            }

            if (collision.relativeVelocity.magnitude >= crashRelativeImpactSpeed)
            {
                Vector3 impactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
                Crash("High impact", collision.relativeVelocity.magnitude, impactPoint);
            }
        }

        public void EvaluateLanding(SlopeHit slope, float downwardSpeed)
        {
            if (IsCrashed)
            {
                return;
            }

            float upDeviation = Vector3.Angle(transform.up, slope.Normal);
            bool tooSteep = slope.Angle > maxLandingSlopeAngle;
            bool misaligned = upDeviation > maxLandingUpDeviation;
            bool hardLanding = downwardSpeed > hardLandingVerticalSpeed;

            if (tooSteep || (hardLanding && misaligned))
            {
                Crash(tooSteep ? "Landed on steep slope" : "Bad landing angle", downwardSpeed, slope.Point);
            }
        }

        public void Crash(string reason)
        {
            Crash(reason, body.linearVelocity.magnitude, transform.position);
        }

        public void Crash(string reason, float impactStrength, Vector3 impactPoint)
        {
            if (IsCrashed)
            {
                return;
            }

            IsCrashed = true;
            CrashReason = reason;
            lastCrashTime = Time.time;
            body.linearDamping = postCrashDrag;
            body.AddTorque(UnityEngine.Random.onUnitSphere * crashAngularImpulse, ForceMode.VelocityChange);
            Crashed?.Invoke(reason);
            CrashImpact?.Invoke(reason, impactStrength, impactPoint);
        }

        public void ClearCrash()
        {
            IsCrashed = false;
            CrashReason = string.Empty;
            body.linearDamping = originalDrag;
            body.angularVelocity = Vector3.zero;
        }
    }
}
