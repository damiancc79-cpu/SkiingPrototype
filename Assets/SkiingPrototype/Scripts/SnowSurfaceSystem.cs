using System.Collections.Generic;
using UnityEngine;

namespace SkiingPrototype
{
    /// <summary>
    /// A lightweight, gameplay-first deformable snow layer. Each ski pass packs a grid cell,
    /// leaving visible grooves and changing the friction/grip sampled by the skier.
    /// </summary>
    public class SnowSurfaceSystem : MonoBehaviour
    {
        [Header("Simulation")]
        [SerializeField] private float cellSize = 1.65f;
        [SerializeField] private float trackSpacing = 0.34f;
        [SerializeField] private float stampInterval = 0.11f;
        [SerializeField] private float freshSnowFriction = 1.12f;
        [SerializeField] private float packedSnowFriction = 0.72f;
        [SerializeField] private float freshSnowCarve = 1.08f;
        [SerializeField] private float packedSnowCarve = 0.74f;
        [SerializeField] private float naturalRecoveryPerSecond = 0.008f;

        [Header("Presentation")]
        [SerializeField] private int maxVisibleStamps = 360;
        [SerializeField] private float stampWidth = 0.16f;
        [SerializeField] private Color packedTrackColor = new Color(0.47f, 0.67f, 0.78f, 0.76f);

        private readonly Dictionary<Vector2Int, float> compaction = new Dictionary<Vector2Int, float>();
        private readonly Queue<GameObject> visibleStamps = new Queue<GameObject>();
        private RigidbodySkierController skier;
        private SkiInputSource input;
        private Material stampMaterial;
        private float nextStampTime;

        public float CurrentCompaction { get; private set; }
        public string SurfaceLabel => CurrentCompaction < 0.25f ? "Fresh powder" : CurrentCompaction < 0.65f ? "Tracked snow" : "Hardpack groove";

        public void Configure(RigidbodySkierController newSkier)
        {
            skier = newSkier;
            input = skier != null ? skier.GetComponent<SkiInputSource>() : null;
        }

        private void Start()
        {
            if (skier == null)
            {
                skier = FindAnyObjectByType<RigidbodySkierController>();
                input = skier != null ? skier.GetComponent<SkiInputSource>() : null;
            }

            stampMaterial = new Material(Shader.Find("Standard"));
            stampMaterial.color = packedTrackColor;
            stampMaterial.SetFloat("_Glossiness", 0.05f);
        }

        private void FixedUpdate()
        {
            if (skier == null || !skier.Ground.IsGrounded)
            {
                return;
            }

            Vector3 point = skier.Ground.Point;
            CurrentCompaction = Sample(point);
            skier.SetSnowResponse(
                Mathf.Lerp(freshSnowFriction, packedSnowFriction, CurrentCompaction),
                Mathf.Lerp(freshSnowCarve, packedSnowCarve, CurrentCompaction));

            if (skier.PlanarSpeed < 2.2f || Time.time < nextStampTime)
            {
                return;
            }

            float effort = Mathf.Clamp01(0.18f + skier.PlanarSpeed / 50f + Mathf.Abs(input != null ? input.Carve : 0f) * 0.38f + (input != null ? input.Brake : 0f) * 0.25f);
            Vector3 right = Vector3.ProjectOnPlane(skier.transform.right, skier.Ground.Normal).normalized;
            Stamp(point + right * trackSpacing, skier.Ground.Normal, effort);
            Stamp(point - right * trackSpacing, skier.Ground.Normal, effort);
            nextStampTime = Time.time + stampInterval;
        }

        private void Update()
        {
            if (compaction.Count == 0 || naturalRecoveryPerSecond <= 0f)
            {
                return;
            }

            // Recovery is deliberately slow: a line remains readable and strategically relevant within a run.
            List<Vector2Int> keys = new List<Vector2Int>(compaction.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                compaction[keys[i]] = Mathf.Max(0f, compaction[keys[i]] - naturalRecoveryPerSecond * Time.deltaTime);
            }
        }

        private void Stamp(Vector3 point, Vector3 normal, float effort)
        {
            Vector2Int key = ToCell(point);
            float existing = compaction.TryGetValue(key, out float amount) ? amount : 0f;
            compaction[key] = Mathf.Clamp01(existing + effort * 0.18f);
            CreateVisualStamp(point + normal * 0.018f, normal, effort);
        }

        private float Sample(Vector3 point)
        {
            Vector2Int key = ToCell(point);
            return compaction.TryGetValue(key, out float amount) ? amount : 0f;
        }

        private Vector2Int ToCell(Vector3 point)
        {
            return new Vector2Int(Mathf.FloorToInt(point.x / cellSize), Mathf.FloorToInt(point.z / cellSize));
        }

        private void CreateVisualStamp(Vector3 position, Vector3 normal, float effort)
        {
            GameObject stamp = GameObject.CreatePrimitive(PrimitiveType.Quad);
            stamp.name = "Packed Snow Groove";
            stamp.transform.SetParent(transform);
            stamp.transform.position = position;
            stamp.transform.rotation = Quaternion.LookRotation(normal, skier.transform.forward);
            stamp.transform.localScale = new Vector3(stampWidth + effort * 0.12f, 0.54f + effort * 0.55f, 1f);
            stamp.GetComponent<Renderer>().material = stampMaterial;
            Destroy(stamp.GetComponent<Collider>());
            visibleStamps.Enqueue(stamp);

            if (visibleStamps.Count > maxVisibleStamps)
            {
                Destroy(visibleStamps.Dequeue());
            }
        }
    }
}
