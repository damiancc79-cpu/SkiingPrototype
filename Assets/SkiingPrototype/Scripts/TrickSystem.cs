using System;
using UnityEngine;

namespace SkiingPrototype
{
    /// <summary>
    /// Arcade aerial spins with a deliberately strict landing window. The physics body remains
    /// readable, while the rider animation carries the trick and its completion state.
    /// </summary>
    public class TrickSystem : MonoBehaviour
    {
        [SerializeField] private float spinDuration = 0.52f;
        [SerializeField] private float grabBonus = 0.45f;
        [SerializeField] private float landingGrace = 0.1f;
        [SerializeField] private float spinBaseValue = 340f;

        private RigidbodySkierController skier;
        private SkiInputSource input;
        private SkierCrashHandler crashHandler;
        private Transform visualPivot;
        private int completedSpins;
        private int spinDirection;
        private float spinProgress;
        private bool spinActive;
        private bool grabUsed;
        private float lastAirborneTime;

        public bool IsTricking => spinActive;
        public string ActiveTrickLabel => spinActive ? (spinDirection > 0 ? "Right 360" : "Left 360") : string.Empty;
        public event Action<float, string> TrickLanded;
        public event Action<string> TrickFailed;

        public void Configure(RigidbodySkierController newSkier, Transform newVisualPivot)
        {
            if (isActiveAndEnabled && skier != null)
            {
                skier.Landed -= HandleLanding;
            }
            skier = newSkier;
            input = skier != null ? skier.GetComponent<SkiInputSource>() : null;
            crashHandler = skier != null ? skier.GetComponent<SkierCrashHandler>() : null;
            visualPivot = newVisualPivot;
            if (isActiveAndEnabled && skier != null)
            {
                skier.Landed += HandleLanding;
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
        }

        private void OnEnable()
        {
            if (skier != null)
            {
                skier.Landed += HandleLanding;
            }
        }

        private void OnDisable()
        {
            if (skier != null)
            {
                skier.Landed -= HandleLanding;
            }
        }

        private void Update()
        {
            if (skier == null || input == null || crashHandler == null || crashHandler.IsCrashed)
            {
                return;
            }

            if (!skier.Ground.IsGrounded)
            {
                lastAirborneTime = Time.time;
                if (!spinActive && input.SpinLeftPressed)
                {
                    StartSpin(-1);
                }
                else if (!spinActive && input.SpinRightPressed)
                {
                    StartSpin(1);
                }

                if (spinActive)
                {
                    spinProgress += Time.deltaTime / spinDuration;
                    if (input.GrabHeld)
                    {
                        grabUsed = true;
                    }

                    if (visualPivot != null)
                    {
                        visualPivot.localRotation = Quaternion.Euler(0f, spinDirection * 360f * Mathf.Clamp01(spinProgress), 0f);
                    }

                    if (spinProgress >= 1f)
                    {
                        spinActive = false;
                        completedSpins++;
                        if (visualPivot != null)
                        {
                            visualPivot.localRotation = Quaternion.identity;
                        }
                    }
                }
            }
        }

        private void StartSpin(int direction)
        {
            spinDirection = direction;
            spinProgress = 0f;
            spinActive = true;
            grabUsed = false;
        }

        private void HandleLanding(float downwardSpeed, SlopeHit slope)
        {
            bool hadRecentAir = Time.time - lastAirborneTime <= landingGrace + 0.25f;
            if (!hadRecentAir)
            {
                return;
            }

            if (spinActive)
            {
                string reason = "Unfinished trick";
                TrickFailed?.Invoke(reason);
                crashHandler.Crash(reason, downwardSpeed + 8f, slope.Point);
            }
            else if (completedSpins > 0)
            {
                float value = completedSpins * spinBaseValue * (grabUsed ? 1f + grabBonus : 1f);
                string label = completedSpins > 1 ? $"{completedSpins}x 360" : grabUsed ? "360 Grab" : "Clean 360";
                TrickLanded?.Invoke(value, label);
            }

            completedSpins = 0;
            spinActive = false;
            grabUsed = false;
            if (visualPivot != null)
            {
                visualPivot.localRotation = Quaternion.identity;
            }
        }
    }
}
