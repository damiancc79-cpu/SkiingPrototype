using UnityEngine;

namespace SkiingPrototype
{
    public class RaceHud : MonoBehaviour
    {
        [SerializeField] private RaceManager raceManager;
        [SerializeField] private RigidbodySkierController skier;
        [SerializeField] private Vector2 hudPosition = new Vector2(16f, 166f);
        [SerializeField] private Vector2 hudSize = new Vector2(390f, 305f);
        [SerializeField] private Vector2 resultsSize = new Vector2(360f, 148f);

        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private GUIStyle largeStyle;
        private GUIStyle crashStyle;
        private SnowSurfaceSystem snow;
        private WeatherDirector weather;
        private RiskRewardSystem risk;
        private TrickSystem tricks;

        private void Start()
        {
            if (raceManager == null)
            {
                raceManager = FindAnyObjectByType<RaceManager>();
            }

            if (skier == null)
            {
                skier = FindAnyObjectByType<RigidbodySkierController>();
            }

            snow = FindAnyObjectByType<SnowSurfaceSystem>();
            weather = FindAnyObjectByType<WeatherDirector>();
            risk = FindAnyObjectByType<RiskRewardSystem>();
            tricks = FindAnyObjectByType<TrickSystem>();
        }

        public void Configure(RaceManager newRaceManager, RigidbodySkierController newSkier)
        {
            raceManager = newRaceManager;
            skier = newSkier;
        }

        private void OnGUI()
        {
            if (raceManager == null || skier == null)
            {
                return;
            }

            EnsureStyles();
            DrawHud();

            if (raceManager.RunState == RaceRunState.Finished)
            {
                DrawResults();
            }
        }

        private void DrawHud()
        {
            GUI.Box(new Rect(hudPosition, hudSize), GUIContent.none, panelStyle);
            GUILayout.BeginArea(new Rect(hudPosition.x + 12f, hudPosition.y + 10f, hudSize.x - 24f, hudSize.y - 20f));
            GUILayout.Label($"Time: {RaceManager.FormatTime(raceManager.CurrentTime)}", largeStyle);
            GUILayout.Label($"Speed: {skier.Speed:0.0} m/s", labelStyle);
            GUILayout.Label($"Checkpoints: {raceManager.PassedCheckpointCount}/{raceManager.TotalCheckpointCount}", labelStyle);
            GUILayout.Label($"State: {raceManager.RunState}", labelStyle);
            GUILayout.Label($"Best: {(raceManager.HasBestTime ? RaceManager.FormatTime(raceManager.BestTime) : "--:--.--")}", labelStyle);
            GUILayout.Label($"Next: {raceManager.NextGateLabel}", labelStyle);
            if (snow != null)
            {
                GUILayout.Label($"Snow: {snow.SurfaceLabel} ({snow.CurrentCompaction * 100f:0}% packed)", labelStyle);
            }
            if (weather != null)
            {
                GUILayout.Label($"Weather: {weather.Label}", labelStyle);
            }
            if (risk != null)
            {
                GUILayout.Label($"Flow: {risk.Flow:0}/100  x{risk.Multiplier:0.00}  Score: {risk.BankedScore + risk.UnbankedScore}", labelStyle);
                GUILayout.Label(risk.IsFocusing ? "FOCUS BOOST — draining Flow" : risk.LastEvent, labelStyle);
            }
            if (tricks != null && tricks.IsTricking)
            {
                GUILayout.Label(tricks.ActiveTrickLabel, crashStyle);
            }
            if (raceManager.RunState == RaceRunState.Crashed)
            {
                GUILayout.Label("CRASHED", crashStyle);
            }
            GUILayout.Label(raceManager.StatusMessage, labelStyle);
            GUILayout.Label("Press R to restart", labelStyle);
            GUILayout.EndArea();
        }

        private void DrawResults()
        {
            Rect panel = new Rect(
                (Screen.width - resultsSize.x) * 0.5f,
                64f,
                resultsSize.x,
                resultsSize.y);

            GUI.Box(panel, GUIContent.none, panelStyle);
            GUILayout.BeginArea(new Rect(panel.x + 16f, panel.y + 14f, panel.width - 32f, panel.height - 28f));
            GUILayout.Label("Finished", largeStyle);
            GUILayout.Label($"Final Time: {RaceManager.FormatTime(raceManager.FinalTime)}", labelStyle);
            GUILayout.Label($"Best Time: {(raceManager.HasBestTime ? RaceManager.FormatTime(raceManager.BestTime) : "--:--.--")}", labelStyle);
            GUILayout.Label(raceManager.NewBestThisRun ? "New best time" : "Press R to try again", labelStyle);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.64f));
            texture.Apply();

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = texture;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            largeStyle = new GUIStyle(labelStyle)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };

            crashStyle = new GUIStyle(largeStyle)
            {
                normal = { textColor = new Color(1f, 0.35f, 0.25f) }
            };
        }
    }
}
