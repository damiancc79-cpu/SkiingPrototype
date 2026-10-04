using UnityEngine;

namespace SkiingPrototype
{
    public class SkiDebugOverlay : MonoBehaviour
    {
        [SerializeField] private RigidbodySkierController skier;
        [SerializeField] private SkierCrashHandler crashHandler;
        [SerializeField] private Vector2 panelPosition = new Vector2(16f, 16f);
        [SerializeField] private Vector2 panelSize = new Vector2(280f, 136f);

        private GUIStyle labelStyle;
        private GUIStyle panelStyle;

        private void Start()
        {
            if (skier == null)
            {
                skier = FindAnyObjectByType<RigidbodySkierController>();
            }

            if (crashHandler == null && skier != null)
            {
                crashHandler = skier.GetComponent<SkierCrashHandler>();
            }
        }

        private void OnGUI()
        {
            if (skier == null)
            {
                return;
            }

            EnsureStyles();

            Rect panel = new Rect(panelPosition, panelSize);
            GUI.Box(panel, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 10f, panel.width - 24f, panel.height - 20f));
            GUILayout.Label($"Speed: {skier.Speed:0.0} m/s", labelStyle);
            GUILayout.Label($"Grounded: {skier.Ground.IsGrounded}", labelStyle);
            GUILayout.Label($"Slope: {skier.Ground.Angle:0.0} deg", labelStyle);
            GUILayout.Label($"State: {skier.MovementState}", labelStyle);

            if (crashHandler != null && crashHandler.IsCrashed)
            {
                GUILayout.Label($"Crash: {crashHandler.CrashReason}", labelStyle);
                GUILayout.Label("Press R to reset", labelStyle);
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = Color.white }
            };

            panelStyle = new GUIStyle(GUI.skin.box);
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.62f));
            texture.Apply();
            panelStyle.normal.background = texture;
        }
    }
}
