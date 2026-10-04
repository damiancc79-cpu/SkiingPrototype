using UnityEngine;

namespace SkiingPrototype
{
    public enum WeatherCondition { Bluebird, Snowfall, Whiteout, Crosswind }

    /// <summary>Weather is a live course modifier, not a cosmetic toggle.</summary>
    public class WeatherDirector : MonoBehaviour
    {
        [SerializeField] private WeatherCondition startingCondition = WeatherCondition.Snowfall;
        [SerializeField] private float windAcceleration = 4.6f;
        [SerializeField] private float changeInterval = 65f;
        [SerializeField] private bool cycleAutomatically = true;

        private RigidbodySkierController skier;
        private SkiInputSource input;
        private Camera mainCamera;
        private ParticleSystem snow;
        private float nextChangeTime;

        public WeatherCondition Current { get; private set; }
        public string Label => Current == WeatherCondition.Bluebird ? "Bluebird / firm" : Current == WeatherCondition.Snowfall ? "Snowfall / soft" : Current == WeatherCondition.Whiteout ? "Whiteout / low visibility" : "Crosswind / unstable";
        public float Visibility { get; private set; } = 1f;
        public Vector3 Wind { get; private set; }

        public void Configure(RigidbodySkierController newSkier, Camera newCamera)
        {
            skier = newSkier;
            input = skier != null ? skier.GetComponent<SkiInputSource>() : null;
            mainCamera = newCamera;
        }

        private void Start()
        {
            if (skier == null)
            {
                skier = FindAnyObjectByType<RigidbodySkierController>();
                input = skier != null ? skier.GetComponent<SkiInputSource>() : null;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            Current = startingCondition;
            nextChangeTime = Time.time + changeInterval;
            EnsureSnowParticles();
            ApplyPresentation();
        }

        private void Update()
        {
            if (input != null && input.WeatherCyclePressed)
            {
                Current = (WeatherCondition)(((int)Current + 1) % 4);
                nextChangeTime = Time.time + changeInterval;
                ApplyPresentation();
            }
            else if (cycleAutomatically && Time.time >= nextChangeTime)
            {
                Current = (WeatherCondition)(((int)Current + 1) % 4);
                nextChangeTime = Time.time + changeInterval;
                ApplyPresentation();
            }

            if (snow != null && mainCamera != null)
            {
                snow.transform.position = mainCamera.transform.position + mainCamera.transform.forward * 8f + Vector3.up * 6f;
            }
        }

        private void FixedUpdate()
        {
            if (skier == null || skier.PhysicsBody == null)
            {
                return;
            }

            float gust = Mathf.PerlinNoise(Time.time * 0.18f, 7.1f) * 2f - 1f;
            Wind = Current == WeatherCondition.Crosswind
                ? new Vector3(1f, 0f, gust * 0.2f).normalized * windAcceleration
                : Current == WeatherCondition.Whiteout
                    ? new Vector3(gust * 0.65f, 0f, 0.32f).normalized * windAcceleration * 0.44f
                    : Vector3.zero;

            if (Wind.sqrMagnitude > 0f)
            {
                float airMultiplier = skier.Ground.IsGrounded ? 0.22f : 1f;
                skier.PhysicsBody.AddForce(Wind * airMultiplier, ForceMode.Acceleration);
            }
        }

        private void ApplyPresentation()
        {
            switch (Current)
            {
                case WeatherCondition.Bluebird:
                    Visibility = 1f;
                    RenderSettings.fog = false;
                    break;
                case WeatherCondition.Snowfall:
                    Visibility = 0.78f;
                    RenderSettings.fog = true;
                    RenderSettings.fogColor = new Color(0.72f, 0.77f, 0.81f);
                    RenderSettings.fogDensity = 0.0022f;
                    break;
                case WeatherCondition.Whiteout:
                    Visibility = 0.42f;
                    RenderSettings.fog = true;
                    RenderSettings.fogColor = new Color(0.86f, 0.89f, 0.91f);
                    RenderSettings.fogDensity = 0.0075f;
                    break;
                default:
                    Visibility = 0.85f;
                    RenderSettings.fog = true;
                    RenderSettings.fogColor = new Color(0.64f, 0.7f, 0.75f);
                    RenderSettings.fogDensity = 0.0034f;
                    break;
            }

            if (snow != null)
            {
                ParticleSystem.EmissionModule emission = snow.emission;
                emission.rateOverTime = Current == WeatherCondition.Bluebird ? 0f : Current == WeatherCondition.Whiteout ? 620f : 250f;
            }
        }

        private void EnsureSnowParticles()
        {
            GameObject snowObject = new GameObject("Weather Snow");
            snowObject.transform.SetParent(transform);
            snow = snowObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = snow.main;
            main.loop = true;
            main.startLifetime = 3.5f;
            main.startSpeed = 8f;
            main.startSize = 0.055f;
            main.maxParticles = 1500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.ShapeModule shape = snow.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 1f, 20f);
            ParticleSystem.EmissionModule emission = snow.emission;
            emission.rateOverTime = 250f;
        }
    }
}
