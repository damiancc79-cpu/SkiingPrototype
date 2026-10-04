using UnityEngine;

namespace SkiingPrototype
{
    public struct SlopeHit
    {
        public bool IsGrounded;
        public Vector3 Point;
        public Vector3 Normal;
        public float Angle;
        public float Distance;
        public Collider Collider;
    }

    public class SlopeProbe : MonoBehaviour
    {
        [Header("Cast")]
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float castStartHeight = 0.75f;
        [SerializeField] private float castDistance = 1.35f;
        [SerializeField] private float sphereRadius = 0.34f;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Grounding")]
        [SerializeField] private float maxGroundAngle = 72f;

        public SlopeHit Current { get; private set; }

        public SlopeHit Probe()
        {
            Vector3 origin = transform.position + Vector3.up * castStartHeight;
            bool hitGround = Physics.SphereCast(
                origin,
                sphereRadius,
                Vector3.down,
                out RaycastHit hit,
                castDistance,
                groundMask,
                triggerInteraction);

            SlopeHit result = new SlopeHit
            {
                IsGrounded = false,
                Point = transform.position,
                Normal = Vector3.up,
                Angle = 0f,
                Distance = castDistance,
                Collider = null
            };

            if (hitGround)
            {
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                result = new SlopeHit
                {
                    IsGrounded = angle <= maxGroundAngle,
                    Point = hit.point,
                    Normal = hit.normal,
                    Angle = angle,
                    Distance = hit.distance,
                    Collider = hit.collider
                };
            }

            Current = result;
            return result;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Current.IsGrounded ? Color.green : Color.yellow;
            Vector3 origin = transform.position + Vector3.up * castStartHeight;
            Gizmos.DrawWireSphere(origin + Vector3.down * castDistance, sphereRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(Current.Point, Current.Point + Current.Normal);
        }
    }
}
