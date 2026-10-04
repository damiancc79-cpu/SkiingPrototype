using UnityEngine;

namespace SkiingPrototype
{
    [RequireComponent(typeof(Camera))]
    public class SkiFollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private RigidbodySkierController target;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 3.2f, -7.5f);
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.2f, 1.8f);

        [Header("Feel")]
        [SerializeField] private float positionSharpness = 7f;
        [SerializeField] private float rotationSharpness = 10f;
        [SerializeField] private float minFov = 58f;
        [SerializeField] private float maxFov = 82f;
        [SerializeField] private float speedForMaxFov = 35f;
        [SerializeField] private float fovSharpness = 6f;

        [Header("Arcade Feedback")]
        [SerializeField] private float turnLagDistance = 1.25f;
        [SerializeField] private float turnLagSharpness = 5f;
        [SerializeField] private float landingDropDistance = 0.45f;
        [SerializeField] private float landingRecoverySharpness = 9f;
        [SerializeField] private Vector3 crashOffset = new Vector3(0f, 1.2f, -2.4f);
        [SerializeField] private float crashFovBonus = 6f;
        [SerializeField] private float crashRotationSharpness = 5f;

        private Camera followCamera;
        private float targetTurnLag;
        private float currentTurnLag;
        private float landingImpulse;
        private float crashImpulse;
        private bool crashFocus;

        private void Awake()
        {
            followCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            if (target == null)
            {
                target = FindAnyObjectByType<RigidbodySkierController>();
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Transform targetTransform = target.transform;
            currentTurnLag = Mathf.Lerp(currentTurnLag, targetTurnLag, 1f - Mathf.Exp(-turnLagSharpness * Time.deltaTime));
            landingImpulse = Mathf.Lerp(landingImpulse, 0f, 1f - Mathf.Exp(-landingRecoverySharpness * Time.deltaTime));
            crashImpulse = Mathf.Lerp(crashImpulse, crashFocus ? crashImpulse : 0f, 1f - Mathf.Exp(-3f * Time.deltaTime));

            Vector3 desiredPosition =
                targetTransform.position +
                targetTransform.TransformDirection(followOffset) +
                targetTransform.right * currentTurnLag +
                Vector3.down * landingImpulse * landingDropDistance +
                (crashFocus ? targetTransform.TransformDirection(crashOffset) * Mathf.Clamp01(crashImpulse) : Vector3.zero);

            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                1f - Mathf.Exp(-(crashFocus ? positionSharpness * 0.55f : positionSharpness) * Time.deltaTime));

            Vector3 lookPoint =
                targetTransform.position +
                targetTransform.TransformDirection(lookOffset);

            Quaternion desiredRotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                1f - Mathf.Exp(-(crashFocus ? crashRotationSharpness : rotationSharpness) * Time.deltaTime));

            float speedT = Mathf.InverseLerp(0f, speedForMaxFov, target.Speed);
            float desiredFov = Mathf.Lerp(minFov, maxFov, speedT) + (crashFocus ? crashFovBonus * Mathf.Clamp01(crashImpulse) : 0f);
            followCamera.fieldOfView = Mathf.Lerp(
                followCamera.fieldOfView,
                desiredFov,
                1f - Mathf.Exp(-fovSharpness * Time.deltaTime));
        }

        public void SetTarget(RigidbodySkierController newTarget)
        {
            target = newTarget;
        }

        public void SnapToTarget()
        {
            if (followCamera == null)
            {
                followCamera = GetComponent<Camera>();
            }

            if (target == null)
            {
                return;
            }

            Transform targetTransform = target.transform;
            transform.position = targetTransform.position + targetTransform.TransformDirection(followOffset);
            Vector3 lookPoint = targetTransform.position + targetTransform.TransformDirection(lookOffset);
            transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            followCamera.fieldOfView = minFov;
            targetTurnLag = 0f;
            currentTurnLag = 0f;
            landingImpulse = 0f;
            crashImpulse = 0f;
            crashFocus = false;
        }

        public void SetTurnFeedback(float turnAmount, bool isCrashed)
        {
            targetTurnLag = Mathf.Clamp(turnAmount, -1f, 1f) * -turnLagDistance;
            crashFocus = isCrashed;
        }

        public void AddLandingImpact(float strength)
        {
            landingImpulse = Mathf.Max(landingImpulse, Mathf.Clamp01(strength));
        }

        public void AddCrashImpact(float strength)
        {
            crashFocus = true;
            crashImpulse = Mathf.Max(crashImpulse, Mathf.Clamp01(strength));
        }
    }
}
