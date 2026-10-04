using System.Collections.Generic;
using UnityEngine;

namespace SkiingPrototype
{
    /// <summary>
    /// Makes dangerous speed, proximity and tricks a player choice. Score is only banked at gates;
    /// crashing discards the active line, while Focus turns earned flow into a controlled speed push.
    /// </summary>
    public class RiskRewardSystem : MonoBehaviour
    {
        [SerializeField] private float speedThreshold = 18f;
        [SerializeField] private float maxSpeedForFlow = 36f;
        [SerializeField] private float nearMissDistance = 5.5f;
        [SerializeField] private float focusAcceleration = 9f;
        [SerializeField] private float focusDrain = 22f;
        [SerializeField] private float flowDecay = 4f;

        private readonly List<RiskObstacle> obstacles = new List<RiskObstacle>();
        private RigidbodySkierController skier;
        private SkiInputSource input;
        private SkierCrashHandler crashHandler;
        private RaceManager raceManager;
        private TrickSystem tricks;
        private float nearMissCooldown;

        public float Flow { get; private set; }
        public float Multiplier { get; private set; } = 1f;
        public int BankedScore { get; private set; }
        public int UnbankedScore { get; private set; }
        public string LastEvent { get; private set; } = "Find speed, then choose your line";
        public bool IsFocusing { get; private set; }

        public void Configure(RigidbodySkierController newSkier, RaceManager newRaceManager, TrickSystem newTricks)
        {
            if (isActiveAndEnabled)
            {
                Unsubscribe();
            }
            skier = newSkier;
            input = skier != null ? skier.GetComponent<SkiInputSource>() : null;
            crashHandler = skier != null ? skier.GetComponent<SkierCrashHandler>() : null;
            raceManager = newRaceManager;
            tricks = newTricks;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void Start()
        {
            if (skier == null)
            {
                skier = FindAnyObjectByType<RigidbodySkierController>();
                input = skier != null ? skier.GetComponent<SkiInputSource>() : null;
                crashHandler = skier != null ? skier.GetComponent<SkierCrashHandler>() : null;
            }

            if (raceManager == null)
            {
                raceManager = FindAnyObjectByType<RaceManager>();
            }

            obstacles.AddRange(FindObjectsByType<RiskObstacle>());
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (skier == null || crashHandler == null || crashHandler.IsCrashed || raceManager == null || raceManager.RunState != RaceRunState.Racing)
            {
                IsFocusing = false;
                return;
            }

            float speedT = Mathf.InverseLerp(speedThreshold, maxSpeedForFlow, skier.Speed);
            float carveT = Mathf.Abs(input != null ? input.Carve : 0f);
            float airT = skier.Ground.IsGrounded ? 0f : 1f;
            float riskGain = speedT * (5f + carveT * 8f + airT * 4f);
            Flow = Mathf.Clamp(Flow + (riskGain - flowDecay * (1f - speedT)) * Time.deltaTime, 0f, 100f);

            CheckNearMiss();
            IsFocusing = input != null && input.FocusHeld && Flow >= 8f;
            if (IsFocusing)
            {
                Flow = Mathf.Max(0f, Flow - focusDrain * Time.deltaTime);
            }

            Multiplier = 1f + Mathf.Floor(Flow / 20f) * 0.25f;
            UnbankedScore += Mathf.RoundToInt(riskGain * Multiplier * Time.deltaTime * 2f);
        }

        private void FixedUpdate()
        {
            if (IsFocusing && skier != null && skier.PhysicsBody != null)
            {
                Vector3 direction = skier.Ground.IsGrounded
                    ? Vector3.ProjectOnPlane(skier.transform.forward, skier.Ground.Normal).normalized
                    : Vector3.ProjectOnPlane(skier.transform.forward, Vector3.up).normalized;
                skier.PhysicsBody.AddForce(direction * focusAcceleration, ForceMode.Acceleration);
            }
        }

        private void Subscribe()
        {
            if (raceManager != null)
            {
                raceManager.GatePassed += HandleGatePassed;
                raceManager.RaceReset += HandleReset;
            }
            if (crashHandler != null)
            {
                crashHandler.Crashed += HandleCrash;
            }
            if (tricks != null)
            {
                tricks.TrickLanded += HandleTrick;
                tricks.TrickFailed += HandleTrickFailed;
            }
        }

        private void Unsubscribe()
        {
            if (raceManager != null)
            {
                raceManager.GatePassed -= HandleGatePassed;
                raceManager.RaceReset -= HandleReset;
            }
            if (crashHandler != null)
            {
                crashHandler.Crashed -= HandleCrash;
            }
            if (tricks != null)
            {
                tricks.TrickLanded -= HandleTrick;
                tricks.TrickFailed -= HandleTrickFailed;
            }
        }

        private void CheckNearMiss()
        {
            if (Time.time < nearMissCooldown)
            {
                return;
            }

            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                RiskObstacle obstacle = obstacles[i];
                if (obstacle == null)
                {
                    obstacles.RemoveAt(i);
                    continue;
                }
                float distance = Vector3.Distance(skier.transform.position, obstacle.transform.position);
                if (distance < Mathf.Min(nearMissDistance, obstacle.DangerRadius) && skier.Speed > speedThreshold)
                {
                    int points = Mathf.RoundToInt(80f * Multiplier);
                    UnbankedScore += points;
                    Flow = Mathf.Clamp(Flow + 12f, 0f, 100f);
                    LastEvent = $"Near miss +{points}";
                    nearMissCooldown = Time.time + 0.9f;
                    break;
                }
            }
        }

        private void HandleTrick(float baseValue, string label)
        {
            int points = Mathf.RoundToInt(baseValue * Multiplier);
            UnbankedScore += points;
            Flow = Mathf.Clamp(Flow + 20f, 0f, 100f);
            LastEvent = $"{label} +{points}";
        }

        private void HandleTrickFailed(string reason)
        {
            LastEvent = reason;
        }

        private void HandleGatePassed(RaceGate gate)
        {
            if (UnbankedScore > 0)
            {
                BankedScore += UnbankedScore;
                LastEvent = $"Line banked +{UnbankedScore}";
                UnbankedScore = 0;
            }
        }

        private void HandleCrash(string reason)
        {
            UnbankedScore = 0;
            Flow = 0f;
            LastEvent = "Line lost — crash";
        }

        private void HandleReset()
        {
            Flow = 0f;
            UnbankedScore = 0;
            LastEvent = "New line";
        }
    }
}
