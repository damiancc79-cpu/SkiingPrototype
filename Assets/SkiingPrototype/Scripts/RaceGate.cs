using UnityEngine;

namespace SkiingPrototype
{
    public class RaceGate : MonoBehaviour
    {
        [SerializeField] private RaceGateType gateType;
        [SerializeField] private int checkpointIndex = -1;
        [SerializeField] private RaceManager raceManager;
        [SerializeField] private Renderer[] statusRenderers;
        [SerializeField] private Color inactiveColor = new Color(0.55f, 0.56f, 0.58f);
        [SerializeField] private Color nextColor = new Color(1f, 0.88f, 0.18f);
        [SerializeField] private Color passedColor = new Color(0.1f, 0.8f, 0.35f);
        [SerializeField] private Color blockedColor = new Color(1f, 0.28f, 0.16f);

        public RaceGateType GateType => gateType;
        public int CheckpointIndex => checkpointIndex;

        private void OnTriggerEnter(Collider other)
        {
            RigidbodySkierController skier = other.GetComponentInParent<RigidbodySkierController>();
            if (skier == null || raceManager == null)
            {
                return;
            }

            raceManager.HandleGateCrossed(this);
        }

        public void Configure(RaceManager manager, RaceGateType type, int index, Renderer[] renderers)
        {
            raceManager = manager;
            gateType = type;
            checkpointIndex = index;
            statusRenderers = renderers;
        }

        public void SetVisualState(bool isNext, bool isPassed, bool isBlocked)
        {
            Color color = inactiveColor;
            if (isPassed)
            {
                color = passedColor;
            }
            else if (isBlocked)
            {
                color = blockedColor;
            }
            else if (isNext)
            {
                color = nextColor;
            }

            if (statusRenderers == null)
            {
                return;
            }

            for (int i = 0; i < statusRenderers.Length; i++)
            {
                if (statusRenderers[i] != null)
                {
                    statusRenderers[i].material.color = color;
                }
            }
        }
    }
}
