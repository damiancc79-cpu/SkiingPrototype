using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkiingPrototype
{
    public class RaceManager : MonoBehaviour
    {
        private const string BestTimeKey = "SkiingPrototype.BestTime";

        [Header("References")]
        [SerializeField] private RigidbodySkierController skier;
        [SerializeField] private SkierCrashHandler crashHandler;
        [SerializeField] private SkiFollowCamera followCamera;
        [SerializeField] private Transform resetPoint;

        [Header("Course")]
        [SerializeField] private RaceGate startGate;
        [SerializeField] private RaceGate finishGate;
        [SerializeField] private List<RaceGate> checkpoints = new List<RaceGate>();

        [Header("Timer")]
        [SerializeField] private bool persistBestTime = true;

        public RaceRunState RunState { get; private set; } = RaceRunState.WaitingToStart;
        public int NextCheckpointIndex { get; private set; }
        public int PassedCheckpointCount => NextCheckpointIndex;
        public int TotalCheckpointCount => checkpoints.Count;
        public float CurrentTime { get; private set; }
        public float FinalTime { get; private set; }
        public float BestTime { get; private set; }
        public bool HasBestTime => BestTime > 0f;
        public bool NewBestThisRun { get; private set; }
        public string StatusMessage { get; private set; } = "Cross the start gate";
        public string NextGateLabel { get; private set; } = "Start";

        public event Action RaceReset;
        public event Action<RaceGate> GatePassed;
        public event Action<RaceGate> GateRejected;

        private void Awake()
        {
            LoadBestTime();
        }

        private void Start()
        {
            if (skier == null)
            {
                skier = FindAnyObjectByType<RigidbodySkierController>();
            }

            if (skier != null)
            {
                skier.SetHandleResetInput(false);
                skier.ResetInputPressed += RestartRun;
            }

            if (crashHandler == null && skier != null)
            {
                crashHandler = skier.GetComponent<SkierCrashHandler>();
            }

            if (crashHandler != null)
            {
                crashHandler.Crashed += HandleCrash;
            }

            ResetRunState();
        }

        private void OnDestroy()
        {
            if (skier != null)
            {
                skier.ResetInputPressed -= RestartRun;
            }

            if (crashHandler != null)
            {
                crashHandler.Crashed -= HandleCrash;
            }
        }

        private void Update()
        {
            if (RunState == RaceRunState.Racing)
            {
                CurrentTime += Time.deltaTime;
            }
        }

        public void Configure(
            RigidbodySkierController newSkier,
            SkierCrashHandler newCrashHandler,
            SkiFollowCamera newFollowCamera,
            Transform newResetPoint,
            RaceGate newStartGate,
            RaceGate newFinishGate,
            List<RaceGate> newCheckpoints)
        {
            skier = newSkier;
            crashHandler = newCrashHandler;
            followCamera = newFollowCamera;
            resetPoint = newResetPoint;
            startGate = newStartGate;
            finishGate = newFinishGate;
            checkpoints = newCheckpoints;
        }

        public void HandleGateCrossed(RaceGate gate)
        {
            if (RunState == RaceRunState.Resetting || RunState == RaceRunState.Finished)
            {
                return;
            }

            if (gate.GateType == RaceGateType.Start)
            {
                if (RunState == RaceRunState.WaitingToStart)
                {
                    StartRace();
                    GatePassed?.Invoke(gate);
                }

                return;
            }

            if (RunState != RaceRunState.Racing)
            {
                StatusMessage = "Cross the start gate first";
                GateRejected?.Invoke(gate);
                return;
            }

            if (gate.GateType == RaceGateType.Checkpoint)
            {
                HandleCheckpoint(gate);
                return;
            }

            if (gate.GateType == RaceGateType.Finish)
            {
                HandleFinish();
            }
        }

        public void RestartRun()
        {
            RunState = RaceRunState.Resetting;
            CurrentTime = 0f;
            FinalTime = 0f;
            NewBestThisRun = false;

            if (skier != null)
            {
                skier.ResetToStart();
            }

            if (followCamera != null)
            {
                followCamera.SnapToTarget();
            }

            ResetRunState();
            RaceReset?.Invoke();
        }

        private void StartRace()
        {
            RunState = RaceRunState.Racing;
            CurrentTime = 0f;
            FinalTime = 0f;
            NewBestThisRun = false;
            StatusMessage = "Checkpoint 1 required";
            UpdateNextGateLabel();
            RefreshGateVisuals();
        }

        private void HandleCheckpoint(RaceGate gate)
        {
            if (gate.CheckpointIndex == NextCheckpointIndex)
            {
                NextCheckpointIndex++;
                StatusMessage = NextCheckpointIndex >= checkpoints.Count
                    ? "All checkpoints clear. Finish gate open."
                    : $"Checkpoint {NextCheckpointIndex + 1} required";
                UpdateNextGateLabel();
                RefreshGateVisuals();
                GatePassed?.Invoke(gate);
                return;
            }

            if (gate.CheckpointIndex < NextCheckpointIndex)
            {
                StatusMessage = $"Checkpoint {gate.CheckpointIndex + 1} already passed";
            }
            else
            {
                StatusMessage = $"Skipped checkpoint. Next required: {NextGateLabel}";
            }

            RefreshGateVisuals(gate);
            GateRejected?.Invoke(gate);
        }

        private void HandleFinish()
        {
            if (NextCheckpointIndex < checkpoints.Count)
            {
                StatusMessage = $"Finish locked. Next required: {NextGateLabel}";
                RefreshGateVisuals(finishGate);
                GateRejected?.Invoke(finishGate);
                return;
            }

            RunState = RaceRunState.Finished;
            FinalTime = CurrentTime;
            StatusMessage = "Finished";

            if (!HasBestTime || FinalTime < BestTime)
            {
                BestTime = FinalTime;
                NewBestThisRun = true;
                if (persistBestTime)
                {
                    PlayerPrefs.SetFloat(BestTimeKey, BestTime);
                    PlayerPrefs.Save();
                }
            }

            RefreshGateVisuals();
            GatePassed?.Invoke(finishGate);
        }

        private void HandleCrash(string reason)
        {
            if (RunState == RaceRunState.Finished || RunState == RaceRunState.Resetting)
            {
                return;
            }

            RunState = RaceRunState.Crashed;
            StatusMessage = string.IsNullOrEmpty(reason) ? "Crashed. Press R to restart." : $"{reason}. Press R to restart.";
            RefreshGateVisuals();
        }

        private void ResetRunState()
        {
            RunState = RaceRunState.WaitingToStart;
            NextCheckpointIndex = 0;
            CurrentTime = 0f;
            FinalTime = 0f;
            NewBestThisRun = false;
            StatusMessage = "Cross the start gate";
            UpdateNextGateLabel();
            RefreshGateVisuals();
        }

        private void UpdateNextGateLabel()
        {
            if (RunState == RaceRunState.WaitingToStart)
            {
                NextGateLabel = "Start";
            }
            else if (NextCheckpointIndex < checkpoints.Count)
            {
                NextGateLabel = $"Checkpoint {NextCheckpointIndex + 1}";
            }
            else
            {
                NextGateLabel = "Finish";
            }
        }

        private void RefreshGateVisuals(RaceGate blockedGate = null)
        {
            if (startGate != null)
            {
                startGate.SetVisualState(RunState == RaceRunState.WaitingToStart, RunState != RaceRunState.WaitingToStart, blockedGate == startGate);
            }

            for (int i = 0; i < checkpoints.Count; i++)
            {
                RaceGate checkpoint = checkpoints[i];
                if (checkpoint == null)
                {
                    continue;
                }

                checkpoint.SetVisualState(
                    RunState == RaceRunState.Racing && i == NextCheckpointIndex,
                    i < NextCheckpointIndex,
                    blockedGate == checkpoint);
            }

            if (finishGate != null)
            {
                finishGate.SetVisualState(
                    RunState == RaceRunState.Racing && NextCheckpointIndex >= checkpoints.Count,
                    RunState == RaceRunState.Finished,
                    blockedGate == finishGate);
            }
        }

        private void LoadBestTime()
        {
            BestTime = persistBestTime ? PlayerPrefs.GetFloat(BestTimeKey, 0f) : 0f;
        }

        public static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remainingSeconds = seconds - minutes * 60f;
            return $"{minutes:00}:{remainingSeconds:00.00}";
        }
    }
}
