using UnityEngine;

namespace SkiingPrototype
{
    [RequireComponent(typeof(RigidbodySkierController))]
    [RequireComponent(typeof(SkiInputSource))]
    [RequireComponent(typeof(SkierCrashHandler))]
    public class SkierFeedbackController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform skiVisual;
        [SerializeField] private SkiFollowCamera followCamera;
        [SerializeField] private RaceManager raceManager;

        [Header("Snow Spray")]
        [SerializeField] private bool createDefaultParticles = true;
        [SerializeField] private ParticleSystem continuousSpray;
        [SerializeField] private ParticleSystem landingBurst;
        [SerializeField] private ParticleSystem crashBurst;
        [SerializeField] private float minSprayRate = 8f;
        [SerializeField] private float maxSprayRate = 90f;
        [SerializeField] private float speedForMaxSpray = 32f;
        [SerializeField] private float carveSprayBoost = 34f;
        [SerializeField] private float brakeSprayBoost = 60f;
        [SerializeField] private float airborneSprayFadeSpeed = 8f;
        [SerializeField] private int softLandingParticles = 16;
        [SerializeField] private int normalLandingParticles = 34;
        [SerializeField] private int hardLandingParticles = 70;
        [SerializeField] private int crashParticles = 130;

        [Header("Ski Trails")]
        [SerializeField] private bool createDefaultTrails = true;
        [SerializeField] private TrailRenderer leftTrail;
        [SerializeField] private TrailRenderer rightTrail;
        [SerializeField] private float baseTrailWidth = 0.08f;
        [SerializeField] private float carvingTrailWidth = 0.2f;
        [SerializeField] private float trailLifetime = 1.8f;
        [Range(0f, 1f)]
        [SerializeField] private float trailOpacity = 0.55f;
        [SerializeField] private float trailEmissionMinSpeed = 2.5f;

        [Header("Visual Lean")]
        [SerializeField] private float maxLeanAngle = 24f;
        [SerializeField] private float leanSpeedForMax = 26f;
        [SerializeField] private float leanSharpness = 9f;
        [SerializeField] private float skiYawAngle = 10f;
        [SerializeField] private float landingCompression = 0.18f;
        [SerializeField] private float compressionRecoverySharpness = 12f;

        [Header("Landing Feedback")]
        [SerializeField] private float softLandingSpeed = 5f;
        [SerializeField] private float hardLandingSpeed = 13f;
        [SerializeField] private float cameraLandingImpulse = 0.22f;

        [Header("Audio Placeholders")]
        [SerializeField] private bool createPlaceholderAudio = true;
        [SerializeField] private AudioSource snowLoopSource;
        [SerializeField] private AudioSource skidLoopSource;
        [SerializeField] private AudioSource oneShotSource;
        [SerializeField] private float snowLoopVolume = 0.28f;
        [SerializeField] private float skidVolume = 0.22f;
        [SerializeField] private float landingVolume = 0.42f;
        [SerializeField] private float crashVolume = 0.7f;
        [SerializeField] private float checkpointVolume = 0.36f;

        private RigidbodySkierController skier;
        private SkiInputSource input;
        private SkierCrashHandler crashHandler;
        private ParticleSystem.EmissionModule sprayEmission;
        private ParticleSystem.MainModule sprayMain;
        private AudioClip snowLoopClip;
        private AudioClip skidClip;
        private AudioClip landingClip;
        private AudioClip crashClip;
        private AudioClip checkpointClip;
        private float currentSprayRate;
        private float compression;
        private Material trailMaterial;

        private void Awake()
        {
            skier = GetComponent<RigidbodySkierController>();
            input = GetComponent<SkiInputSource>();
            crashHandler = GetComponent<SkierCrashHandler>();

            if (createDefaultParticles)
            {
                EnsureParticleSystems();
            }

            if (createDefaultTrails)
            {
                EnsureTrails();
            }

            if (createPlaceholderAudio)
            {
                EnsureAudio();
            }

            if (continuousSpray != null)
            {
                sprayEmission = continuousSpray.emission;
                sprayMain = continuousSpray.main;
            }
        }

        private void OnEnable()
        {
            skier.Landed += HandleLanding;
            crashHandler.CrashImpact += HandleCrashImpact;

            if (raceManager != null)
            {
                raceManager.GatePassed += HandleGatePassed;
                raceManager.GateRejected += HandleGateRejected;
                raceManager.RaceReset += HandleRaceReset;
            }
        }

        private void OnDisable()
        {
            skier.Landed -= HandleLanding;
            crashHandler.CrashImpact -= HandleCrashImpact;

            if (raceManager != null)
            {
                raceManager.GatePassed -= HandleGatePassed;
                raceManager.GateRejected -= HandleGateRejected;
                raceManager.RaceReset -= HandleRaceReset;
            }
        }

        private void Update()
        {
            UpdateSpray();
            UpdateTrails();
            UpdateLean();
            UpdateAudio();
        }

        public void Configure(Transform newVisualRoot, Transform newSkiVisual, SkiFollowCamera newFollowCamera, RaceManager newRaceManager)
        {
            if (newVisualRoot != null)
            {
                visualRoot = newVisualRoot;
            }

            if (newSkiVisual != null)
            {
                skiVisual = newSkiVisual;
            }

            if (newFollowCamera != null)
            {
                followCamera = newFollowCamera;
            }

            if (raceManager != null && isActiveAndEnabled)
            {
                raceManager.GatePassed -= HandleGatePassed;
                raceManager.GateRejected -= HandleGateRejected;
                raceManager.RaceReset -= HandleRaceReset;
            }

            if (newRaceManager != null)
            {
                raceManager = newRaceManager;
            }

            if (raceManager != null && isActiveAndEnabled)
            {
                raceManager.GatePassed += HandleGatePassed;
                raceManager.GateRejected += HandleGateRejected;
                raceManager.RaceReset += HandleRaceReset;
            }
        }

        private void UpdateSpray()
        {
            if (continuousSpray == null)
            {
                return;
            }

            float speedT = Mathf.InverseLerp(0f, speedForMaxSpray, skier.Speed);
            float skidT = Mathf.Max(Mathf.Abs(input.Carve), input.Brake);
            float targetRate = skier.Ground.IsGrounded && !crashHandler.IsCrashed
                ? Mathf.Lerp(minSprayRate, maxSprayRate, speedT) + Mathf.Abs(input.Carve) * carveSprayBoost + input.Brake * brakeSprayBoost
                : 0f;

            currentSprayRate = Mathf.Lerp(currentSprayRate, targetRate, 1f - Mathf.Exp(-airborneSprayFadeSpeed * Time.deltaTime));
            sprayEmission.rateOverTime = currentSprayRate;
            sprayMain.startSpeed = Mathf.Lerp(2f, 8f, Mathf.Clamp01(speedT + skidT * 0.45f));
            sprayMain.startSize = Mathf.Lerp(0.08f, 0.22f, Mathf.Clamp01(speedT + skidT));

            if (currentSprayRate > 1f && !continuousSpray.isPlaying)
            {
                continuousSpray.Play();
            }
            else if (currentSprayRate <= 1f && continuousSpray.isPlaying)
            {
                continuousSpray.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void UpdateTrails()
        {
            float intensity = Mathf.Clamp01(Mathf.InverseLerp(trailEmissionMinSpeed, speedForMaxSpray, skier.Speed) + Mathf.Max(Mathf.Abs(input.Carve), input.Brake) * 0.7f);
            bool emitting = skier.Ground.IsGrounded && !crashHandler.IsCrashed && skier.Speed >= trailEmissionMinSpeed;
            float width = Mathf.Lerp(baseTrailWidth, carvingTrailWidth, intensity);

            ApplyTrail(leftTrail, emitting, width, intensity);
            ApplyTrail(rightTrail, emitting, width, intensity);
        }

        private void ApplyTrail(TrailRenderer trail, bool emitting, float width, float intensity)
        {
            if (trail == null)
            {
                return;
            }

            trail.emitting = emitting;
            trail.time = trailLifetime;
            trail.startWidth = width;
            trail.endWidth = width * 0.25f;
            Color start = new Color(1f, 1f, 1f, trailOpacity * Mathf.Lerp(0.45f, 1f, intensity));
            Color end = new Color(1f, 1f, 1f, 0f);
            trail.startColor = start;
            trail.endColor = end;
        }

        private void UpdateLean()
        {
            if (visualRoot == null)
            {
                return;
            }

            float speedT = Mathf.InverseLerp(3f, leanSpeedForMax, skier.PlanarSpeed);
            float targetLean = -input.Carve * maxLeanAngle * speedT;
            compression = Mathf.Lerp(compression, 0f, 1f - Mathf.Exp(-compressionRecoverySharpness * Time.deltaTime));

            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetLean);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, targetRotation, 1f - Mathf.Exp(-leanSharpness * Time.deltaTime));
            visualRoot.localScale = new Vector3(1f + compression * 0.08f, 1f - compression, 1f + compression * 0.05f);

            if (skiVisual != null)
            {
                float skiYaw = input.Carve * skiYawAngle * speedT;
                skiVisual.localRotation = Quaternion.Slerp(
                    skiVisual.localRotation,
                    Quaternion.Euler(0f, skiYaw, 0f),
                    1f - Mathf.Exp(-leanSharpness * Time.deltaTime));
            }

            if (followCamera != null)
            {
                followCamera.SetTurnFeedback(input.Carve * speedT, crashHandler.IsCrashed);
            }
        }

        private void UpdateAudio()
        {
            if (snowLoopSource == null)
            {
                return;
            }

            float speedT = Mathf.InverseLerp(0f, speedForMaxSpray, skier.Speed);
            float skidT = skier.Ground.IsGrounded ? Mathf.Max(Mathf.Abs(input.Carve), input.Brake) : 0f;
            float targetVolume = skier.Ground.IsGrounded && !crashHandler.IsCrashed
                ? Mathf.Clamp01(speedT) * snowLoopVolume
                : 0f;

            snowLoopSource.volume = Mathf.Lerp(snowLoopSource.volume, targetVolume, 1f - Mathf.Exp(-7f * Time.deltaTime));
            snowLoopSource.pitch = Mathf.Lerp(0.85f, 1.45f, Mathf.Clamp01(speedT + skidT * 0.3f));

            if (skidLoopSource != null)
            {
                float skidVolumeTarget = skier.Ground.IsGrounded && !crashHandler.IsCrashed ? skidT * skidVolume : 0f;
                skidLoopSource.volume = Mathf.Lerp(skidLoopSource.volume, skidVolumeTarget, 1f - Mathf.Exp(-9f * Time.deltaTime));
                skidLoopSource.pitch = Mathf.Lerp(0.75f, 1.25f, Mathf.Clamp01(speedT + skidT));
            }
        }

        private void HandleLanding(float downwardSpeed, SlopeHit slope)
        {
            float impactT = Mathf.InverseLerp(softLandingSpeed, hardLandingSpeed, downwardSpeed);
            int count = downwardSpeed < softLandingSpeed
                ? softLandingParticles
                : downwardSpeed < hardLandingSpeed ? normalLandingParticles : hardLandingParticles;

            EmitBurst(landingBurst, slope.Point + slope.Normal * 0.15f, count);
            compression = Mathf.Max(compression, landingCompression * Mathf.Lerp(0.35f, 1f, impactT));

            if (followCamera != null)
            {
                followCamera.AddLandingImpact(cameraLandingImpulse * Mathf.Lerp(0.35f, 1f, impactT));
            }

            PlayOneShot(landingClip, landingVolume * Mathf.Lerp(0.35f, 1f, impactT), Mathf.Lerp(0.85f, 1.15f, impactT));
        }

        private void HandleCrashImpact(string reason, float impactStrength, Vector3 impactPoint)
        {
            float impactT = Mathf.InverseLerp(8f, 24f, impactStrength);
            EmitBurst(crashBurst, impactPoint, Mathf.RoundToInt(crashParticles * Mathf.Lerp(0.55f, 1.25f, impactT)));

            if (followCamera != null)
            {
                followCamera.AddCrashImpact(Mathf.Lerp(0.65f, 1.1f, impactT));
            }

            PlayOneShot(crashClip, crashVolume * Mathf.Lerp(0.7f, 1.15f, impactT), Mathf.Lerp(0.8f, 1.05f, impactT));
        }

        private void HandleGatePassed(RaceGate gate)
        {
            PlayOneShot(checkpointClip, checkpointVolume, gate != null && gate.GateType == RaceGateType.Finish ? 1.35f : 1f);
        }

        private void HandleGateRejected(RaceGate gate)
        {
            PlayOneShot(checkpointClip, checkpointVolume * 0.55f, 0.65f);
        }

        private void HandleRaceReset()
        {
            compression = 0f;
            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.identity;
                visualRoot.localScale = Vector3.one;
            }

            if (leftTrail != null)
            {
                leftTrail.Clear();
            }

            if (rightTrail != null)
            {
                rightTrail.Clear();
            }
        }

        private void EmitBurst(ParticleSystem particleSystem, Vector3 position, int count)
        {
            if (particleSystem == null || count <= 0)
            {
                return;
            }

            particleSystem.transform.position = position;
            particleSystem.Emit(count);
        }

        private void PlayOneShot(AudioClip clip, float volume, float pitch)
        {
            if (oneShotSource == null || clip == null || volume <= 0f)
            {
                return;
            }

            oneShotSource.pitch = pitch;
            oneShotSource.PlayOneShot(clip, volume);
        }

        private void EnsureParticleSystems()
        {
            if (continuousSpray == null)
            {
                continuousSpray = CreateParticleSystem("Continuous Snow Spray", new Vector3(0f, -0.7f, -0.65f), false, 90);
            }

            if (landingBurst == null)
            {
                landingBurst = CreateParticleSystem("Landing Snow Burst", Vector3.zero, true, 120);
            }

            if (crashBurst == null)
            {
                crashBurst = CreateParticleSystem("Crash Snow Burst", Vector3.zero, true, 180);
            }
        }

        private ParticleSystem CreateParticleSystem(string systemName, Vector3 localPosition, bool worldSpace, int maxParticles)
        {
            GameObject particleObject = new GameObject(systemName);
            particleObject.transform.SetParent(transform);
            if (worldSpace)
            {
                particleObject.transform.position = transform.position;
            }
            else
            {
                particleObject.transform.localPosition = localPosition;
            }
            particleObject.transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);

            ParticleSystem particleSystem = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = !worldSpace;
            main.playOnAwake = false;
            main.maxParticles = maxParticles;
            main.simulationSpace = worldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.startLifetime = worldSpace ? 0.75f : 0.55f;
            main.startSpeed = worldSpace ? 7f : 4f;
            main.startSize = worldSpace ? 0.2f : 0.12f;
            main.startColor = new Color(1f, 1f, 1f, 0.72f);

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = !worldSpace;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = worldSpace ? 36f : 24f;
            shape.radius = worldSpace ? 1.4f : 0.55f;

            ParticleSystem.VelocityOverLifetimeModule velocity = particleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.y = worldSpace ? 1.2f : 0.2f;
            velocity.z = worldSpace ? -0.8f : -2.2f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.72f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;

            ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            return particleSystem;
        }

        private void EnsureTrails()
        {
            trailMaterial = new Material(Shader.Find("Sprites/Default"));
            if (leftTrail == null)
            {
                leftTrail = CreateTrail("Left Ski Trail", new Vector3(-0.22f, -1.02f, -0.25f));
            }

            if (rightTrail == null)
            {
                rightTrail = CreateTrail("Right Ski Trail", new Vector3(0.22f, -1.02f, -0.25f));
            }
        }

        private TrailRenderer CreateTrail(string trailName, Vector3 localPosition)
        {
            GameObject trailObject = new GameObject(trailName);
            trailObject.transform.SetParent(transform);
            trailObject.transform.localPosition = localPosition;
            trailObject.transform.localRotation = Quaternion.identity;

            TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
            trail.material = trailMaterial;
            trail.time = trailLifetime;
            trail.minVertexDistance = 0.18f;
            trail.numCornerVertices = 2;
            trail.numCapVertices = 1;
            trail.textureMode = LineTextureMode.Stretch;
            trail.alignment = LineAlignment.View;
            trail.emitting = false;
            return trail;
        }

        private void EnsureAudio()
        {
            if (snowLoopSource == null)
            {
                snowLoopSource = gameObject.AddComponent<AudioSource>();
                snowLoopSource.loop = true;
                snowLoopSource.playOnAwake = true;
                snowLoopSource.spatialBlend = 0.55f;
                snowLoopSource.volume = 0f;
            }

            if (oneShotSource == null)
            {
                oneShotSource = gameObject.AddComponent<AudioSource>();
                oneShotSource.loop = false;
                oneShotSource.playOnAwake = false;
                oneShotSource.spatialBlend = 0.45f;
            }

            if (skidLoopSource == null)
            {
                skidLoopSource = gameObject.AddComponent<AudioSource>();
                skidLoopSource.loop = true;
                skidLoopSource.playOnAwake = true;
                skidLoopSource.spatialBlend = 0.55f;
                skidLoopSource.volume = 0f;
            }

            snowLoopClip = CreateNoiseClip("Placeholder Snow Loop", 0.45f, 0.22f);
            skidClip = CreateNoiseClip("Placeholder Skid", 0.18f, 0.5f);
            landingClip = CreateToneClip("Placeholder Landing", 90f, 0.16f);
            crashClip = CreateNoiseClip("Placeholder Crash", 0.4f, 0.8f);
            checkpointClip = CreateToneClip("Placeholder Checkpoint", 680f, 0.12f);

            snowLoopSource.clip = snowLoopClip;
            snowLoopSource.Play();
            skidLoopSource.clip = skidClip;
            skidLoopSource.Play();
        }

        private AudioClip CreateToneClip(string clipName, float frequency, float duration)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - i / (float)sampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.4f;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateNoiseClip(string clipName, float duration, float gain)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            float previous = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float noise = Random.Range(-1f, 1f);
                previous = Mathf.Lerp(previous, noise, 0.18f);
                float envelope = Mathf.Clamp01(1f - i / (float)sampleCount * 0.35f);
                samples[i] = previous * gain * envelope;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
