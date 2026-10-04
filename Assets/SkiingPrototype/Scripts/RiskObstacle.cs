using UnityEngine;

namespace SkiingPrototype
{
    /// <summary>Marks scenery that contributes to a proximity/near-miss risk calculation.</summary>
    public class RiskObstacle : MonoBehaviour
    {
        [SerializeField] private float dangerRadius = 4.5f;
        public float DangerRadius => dangerRadius;
    }
}
