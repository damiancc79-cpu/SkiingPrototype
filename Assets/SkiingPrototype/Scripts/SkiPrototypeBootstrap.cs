using System.Collections.Generic;
using UnityEngine;

namespace SkiingPrototype
{
    public class SkiPrototypeBootstrap : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private bool buildOnAwake = true;
        [SerializeField] private Vector3 startPosition = new Vector3(0f, 241.5f, -6f);
        [SerializeField] private Vector3 startEuler = new Vector3(18f, 180f, 0f);

        [Header("Course")]
        [SerializeField] private float courseLength = 1300f;
        [SerializeField] private float courseWidth = 90f;
        [SerializeField] private float courseDrop = 240f;

        [Header("Skier")]
        [SerializeField] private float skierMass = 78f;

        private Material snowMaterial;
        private Material obstacleMaterial;
        private Material skierMaterial;
        private Material gateMaterial;

        private void Awake()
        {
            if (buildOnAwake)
            {
                BuildPrototype();
            }
        }

        public void BuildPrototype()
        {
            Physics.gravity = new Vector3(0f, -24f, 0f);
            CreateMaterials();
            CreateLighting();

            GameObject courseRoot = new GameObject("Course");
            GameObject raceRoot = new GameObject("Race Systems");
            Transform resetPoint = CreateResetPoint();
            resetPoint.SetParent(courseRoot.transform);

            RaceManager raceManager = raceRoot.AddComponent<RaceManager>();
            CreateCourse(courseRoot.transform, raceManager, out RaceGate startGate, out RaceGate finishGate, out List<RaceGate> checkpoints);
            RigidbodySkierController skier = CreateSkier(resetPoint, out Transform trickPivot);
            SkiFollowCamera followCamera = CreateCamera(skier);
            raceManager.Configure(
                skier,
                skier.GetComponent<SkierCrashHandler>(),
                followCamera,
                resetPoint,
                startGate,
                finishGate,
                checkpoints);
            SkierFeedbackController feedback = skier.GetComponent<SkierFeedbackController>();
            if (feedback != null)
            {
                feedback.Configure(null, null, followCamera, raceManager);
            }
            CreateDynamicSystems(courseRoot.transform, raceRoot.transform, skier, raceManager, followCamera, trickPivot);
            CreateDebugOverlay(skier);
            CreateRaceHud(raceManager, skier, raceRoot.transform);
        }

        private void CreateDynamicSystems(Transform courseRoot, Transform raceRoot, RigidbodySkierController skier, RaceManager raceManager, SkiFollowCamera followCamera, Transform trickPivot)
        {
            SnowSurfaceSystem snow = courseRoot.gameObject.AddComponent<SnowSurfaceSystem>();
            snow.Configure(skier);

            WeatherDirector weather = raceRoot.gameObject.AddComponent<WeatherDirector>();
            weather.Configure(skier, followCamera.GetComponent<Camera>());

            TrickSystem tricks = skier.gameObject.AddComponent<TrickSystem>();
            tricks.Configure(skier, trickPivot);

            RiskRewardSystem risk = raceRoot.gameObject.AddComponent<RiskRewardSystem>();
            risk.Configure(skier, raceManager, tricks);
        }

        private void CreateMaterials()
        {
            snowMaterial = NewMaterial("Graybox Snow", new Color(0.72f, 0.76f, 0.78f));
            obstacleMaterial = NewMaterial("Graybox Obstacles", new Color(0.44f, 0.47f, 0.49f));
            skierMaterial = NewMaterial("Skier Marker", new Color(0.1f, 0.34f, 0.95f));
            gateMaterial = NewMaterial("Gate Status", new Color(1f, 0.88f, 0.18f));
        }

        private Material NewMaterial(string materialName, Color color)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.name = materialName;
            material.color = color;
            return material;
        }

        private void CreateLighting()
        {
            if (FindAnyObjectByType<Light>() == null)
            {
                GameObject lightObject = new GameObject("Directional Light");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            }

            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);
        }

        private Transform CreateResetPoint()
        {
            GameObject reset = new GameObject("Start Reset Point");
            reset.tag = "Respawn";
            reset.transform.position = startPosition;
            reset.transform.rotation = Quaternion.Euler(startEuler);
            return reset.transform;
        }

        private void CreateCourse(Transform courseParent, RaceManager raceManager, out RaceGate startGate, out RaceGate finishGate, out List<RaceGate> checkpoints)
        {
            checkpoints = new List<RaceGate>();

            GameObject slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slope.name = "Main Graybox Slope";
            slope.transform.SetParent(courseParent);
            slope.transform.position = new Vector3(0f, courseDrop * 0.5f, -courseLength * 0.5f);
            slope.transform.rotation = Quaternion.Euler(Mathf.Atan2(courseDrop, courseLength) * Mathf.Rad2Deg, 0f, 0f);
            slope.transform.localScale = new Vector3(courseWidth, 1f, courseLength);
            slope.GetComponent<Renderer>().material = snowMaterial;

            CreateSideWall(courseParent, -courseWidth * 0.58f);
            CreateSideWall(courseParent, courseWidth * 0.58f);

            Transform gatesParent = new GameObject("Gates").transform;
            gatesParent.SetParent(courseParent);

            startGate = CreateGate(gatesParent, raceManager, "Start Gate", RaceGateType.Start, -1, new Vector3(0f, CourseSurfaceY(-30f), -30f), 28f);
            checkpoints.Add(CreateGate(gatesParent, raceManager, "Checkpoint 1 - Opening Fall Line", RaceGateType.Checkpoint, 0, new Vector3(-6f, CourseSurfaceY(-165f), -165f), 30f));
            checkpoints.Add(CreateGate(gatesParent, raceManager, "Checkpoint 2 - Wide Left Right Choice", RaceGateType.Checkpoint, 1, new Vector3(0f, CourseSurfaceY(-330f), -330f), 52f));
            checkpoints.Add(CreateGate(gatesParent, raceManager, "Checkpoint 3 - Jump Exit", RaceGateType.Checkpoint, 2, new Vector3(10f, CourseSurfaceY(-515f), -515f), 34f));
            checkpoints.Add(CreateGate(gatesParent, raceManager, "Checkpoint 4 - Technical Left", RaceGateType.Checkpoint, 3, new Vector3(-18f, CourseSurfaceY(-675f), -675f), 20f));
            checkpoints.Add(CreateGate(gatesParent, raceManager, "Checkpoint 5 - Technical Right", RaceGateType.Checkpoint, 4, new Vector3(18f, CourseSurfaceY(-790f), -790f), 20f));
            checkpoints.Add(CreateGate(gatesParent, raceManager, "Checkpoint 6 - Lower Wide Choice", RaceGateType.Checkpoint, 5, new Vector3(0f, CourseSurfaceY(-980f), -980f), 56f));
            finishGate = CreateGate(gatesParent, raceManager, "Finish Gate", RaceGateType.Finish, -1, new Vector3(0f, CourseSurfaceY(-1235f), -1235f), 44f);

            Transform featuresParent = new GameObject("Jumps And Bumps").transform;
            featuresParent.SetParent(courseParent);
            CreateBump(featuresParent, new Vector3(-12f, CourseSurfaceY(-230f) + 0.2f, -230f), new Vector3(12f, 2.2f, 8f));
            CreateBump(featuresParent, new Vector3(18f, CourseSurfaceY(-430f) + 0.2f, -430f), new Vector3(14f, 2.6f, 9f));
            CreateJump(featuresParent, new Vector3(6f, CourseSurfaceY(-475f) + 0.4f, -475f), 14f, 14f, 3.8f);
            CreateBump(featuresParent, new Vector3(-20f, CourseSurfaceY(-890f) + 0.2f, -890f), new Vector3(18f, 2f, 10f));

            Transform obstaclesParent = new GameObject("Primitive Obstacles").transform;
            obstaclesParent.SetParent(courseParent);
            CreateRouteChoiceIsland(obstaclesParent, -330f);
            CreateTechnicalPoles(obstaclesParent);
            CreateRouteChoiceIsland(obstaclesParent, -980f);
            CreateEdgeObstacles(obstaclesParent);

            CreateFinishCatch(courseParent);
        }

        private void CreateSideWall(Transform parent, float x)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = x < 0f ? "Left Boundary" : "Right Boundary";
            wall.transform.SetParent(parent);
            wall.transform.position = new Vector3(x, courseDrop * 0.42f, -courseLength * 0.52f);
            wall.transform.rotation = Quaternion.Euler(Mathf.Atan2(courseDrop, courseLength) * Mathf.Rad2Deg, 0f, 0f);
            wall.transform.localScale = new Vector3(1f, 7f, courseLength);
            wall.GetComponent<Renderer>().material = obstacleMaterial;
        }

        private void CreateBump(Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject bump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bump.name = "Rounded Bump";
            bump.transform.SetParent(parent);
            bump.transform.position = position;
            bump.transform.localScale = scale;
            bump.GetComponent<Renderer>().material = snowMaterial;
        }

        private void CreateJump(Transform parent, Vector3 position, float width, float length, float height)
        {
            GameObject jump = new GameObject("Simple Jump Wedge");
            jump.transform.SetParent(parent);
            MeshFilter filter = jump.AddComponent<MeshFilter>();
            MeshRenderer renderer = jump.AddComponent<MeshRenderer>();
            MeshCollider collider = jump.AddComponent<MeshCollider>();

            float halfWidth = width * 0.5f;
            float halfLength = length * 0.5f;
            Vector3[] vertices =
            {
                new Vector3(-halfWidth, 0f, -halfLength),
                new Vector3(halfWidth, 0f, -halfLength),
                new Vector3(-halfWidth, 0f, halfLength),
                new Vector3(halfWidth, 0f, halfLength),
                new Vector3(-halfWidth, height, halfLength),
                new Vector3(halfWidth, height, halfLength)
            };

            int[] triangles =
            {
                0, 2, 1, 1, 2, 3,
                2, 4, 3, 3, 4, 5,
                0, 4, 2, 0, 1, 5,
                0, 5, 4, 1, 3, 5
            };

            Mesh mesh = new Mesh
            {
                name = "Jump Wedge Mesh",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            filter.sharedMesh = mesh;
            collider.sharedMesh = mesh;
            renderer.material = snowMaterial;
            jump.transform.position = position;
            jump.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
        }

        private void CreateFinishCatch(Transform parent)
        {
            GameObject catchPlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            catchPlane.name = "Flat Runout";
            catchPlane.transform.SetParent(parent);
            catchPlane.transform.position = new Vector3(0f, -2.8f, -courseLength - 60f);
            catchPlane.transform.localScale = new Vector3(courseWidth, 1f, 120f);
            catchPlane.GetComponent<Renderer>().material = snowMaterial;
        }

        private RaceGate CreateGate(Transform parent, RaceManager raceManager, string gateName, RaceGateType gateType, int checkpointIndex, Vector3 position, float width)
        {
            GameObject gateRoot = new GameObject(gateName);
            gateRoot.transform.SetParent(parent);
            gateRoot.transform.position = position;

            Renderer left = CreateGatePart(gateRoot.transform, "Left Pole", new Vector3(-width * 0.5f, 2.2f, 0f), new Vector3(0.55f, 4.4f, 0.55f));
            Renderer right = CreateGatePart(gateRoot.transform, "Right Pole", new Vector3(width * 0.5f, 2.2f, 0f), new Vector3(0.55f, 4.4f, 0.55f));
            Renderer top = CreateGatePart(gateRoot.transform, "Top Bar", new Vector3(0f, 4.35f, 0f), new Vector3(width, 0.34f, 0.34f));

            BoxCollider trigger = gateRoot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(width, 5.5f, 7f);
            trigger.center = new Vector3(0f, 2.5f, 0f);

            RaceGate gate = gateRoot.AddComponent<RaceGate>();
            gate.Configure(raceManager, gateType, checkpointIndex, new[] { left, right, top });
            return gate;
        }

        private Renderer CreateGatePart(Transform parent, string partName, Vector3 localPosition, Vector3 scale)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            part.transform.SetParent(parent);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = scale;
            Destroy(part.GetComponent<Collider>());

            Renderer renderer = part.GetComponent<Renderer>();
            renderer.material = new Material(gateMaterial);
            return renderer;
        }

        private void CreateRouteChoiceIsland(Transform parent, float z)
        {
            CreateRock(parent, new Vector3(0f, CourseSurfaceY(z) + 1.1f, z), new Vector3(7f, 2.3f, 7f));
            CreateTree(parent, new Vector3(-7f, CourseSurfaceY(z - 12f) + 1.6f, z - 12f));
            CreateTree(parent, new Vector3(8f, CourseSurfaceY(z + 14f) + 1.6f, z + 14f));
        }

        private void CreateTechnicalPoles(Transform parent)
        {
            float[] zs = { -600f, -642f, -705f, -748f, -820f };
            float[] xs = { 12f, -14f, 15f, -16f, 14f };
            for (int i = 0; i < zs.Length; i++)
            {
                CreatePole(parent, new Vector3(xs[i], CourseSurfaceY(zs[i]) + 2f, zs[i]), 4f);
                CreatePole(parent, new Vector3(xs[i] * -1f + Mathf.Sign(xs[i]) * 14f, CourseSurfaceY(zs[i]) + 2f, zs[i] - 10f), 3.4f);
            }
        }

        private void CreateEdgeObstacles(Transform parent)
        {
            for (int i = 0; i < 16; i++)
            {
                float z = -120f - i * 68f;
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * Random.Range(31f, 39f);

                if (i % 3 == 0)
                {
                    CreateTree(parent, new Vector3(x, CourseSurfaceY(z) + 1.6f, z));
                }
                else
                {
                    CreateRock(parent, new Vector3(x, CourseSurfaceY(z) + 0.8f, z), new Vector3(4f, 1.6f, 4f));
                }
            }
        }

        private void CreateTree(Transform parent, Vector3 position)
        {
            GameObject tree = new GameObject("Primitive Tree");
            tree.transform.SetParent(parent);
            tree.transform.position = position;
            tree.AddComponent<RiskObstacle>();

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = Vector3.zero;
            trunk.transform.localScale = new Vector3(0.7f, 2.6f, 0.7f);
            trunk.GetComponent<Renderer>().material = obstacleMaterial;

            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            crown.name = "Crown";
            crown.transform.SetParent(tree.transform);
            crown.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            crown.transform.localScale = new Vector3(2.4f, 2f, 2.4f);
            crown.GetComponent<Renderer>().material = obstacleMaterial;
        }

        private void CreateRock(Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "Primitive Rock";
            rock.transform.SetParent(parent);
            rock.transform.position = position;
            rock.transform.localScale = scale;
            rock.GetComponent<Renderer>().material = obstacleMaterial;
            rock.AddComponent<RiskObstacle>();
        }

        private void CreatePole(Transform parent, Vector3 position, float height)
        {
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Technical Pole";
            pole.transform.SetParent(parent);
            pole.transform.position = position;
            pole.transform.localScale = new Vector3(0.35f, height * 0.5f, 0.35f);
            pole.GetComponent<Renderer>().material = obstacleMaterial;
            pole.AddComponent<RiskObstacle>();
        }

        private float CourseSurfaceY(float z)
        {
            float t = Mathf.Clamp01(-z / courseLength);
            return Mathf.Lerp(courseDrop, 0f, t) + 0.7f;
        }

        private RigidbodySkierController CreateSkier(Transform resetPoint, out Transform trickPivot)
        {
            GameObject skier = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            skier.name = "Rigidbody Skier";
            skier.transform.position = resetPoint.position;
            skier.transform.rotation = resetPoint.rotation;
            skier.transform.localScale = new Vector3(0.65f, 1f, 0.65f);
            skier.GetComponent<Renderer>().material = skierMaterial;
            skier.GetComponent<Renderer>().enabled = false;

            Rigidbody body = skier.AddComponent<Rigidbody>();
            body.mass = skierMass;
            body.linearDamping = 0.08f;
            body.angularDamping = 0.25f;
            body.constraints = RigidbodyConstraints.None;

            SkiInputSource input = skier.AddComponent<SkiInputSource>();
            SlopeProbe probe = skier.AddComponent<SlopeProbe>();
            SkierCrashHandler crash = skier.AddComponent<SkierCrashHandler>();
            RigidbodySkierController controller = skier.AddComponent<RigidbodySkierController>();
            controller.SetResetPoint(resetPoint);

            _ = input;
            _ = probe;
            _ = crash;

            GameObject visualRoot = new GameObject("Procedural Skier Visuals");
            visualRoot.transform.SetParent(skier.transform);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;
            visualRoot.transform.localScale = Vector3.one;

            GameObject pivotObject = new GameObject("Trick Visual Pivot");
            pivotObject.transform.SetParent(visualRoot.transform);
            pivotObject.transform.localPosition = Vector3.zero;
            pivotObject.transform.localRotation = Quaternion.identity;
            trickPivot = pivotObject.transform;

            GameObject bodyVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyVisual.name = "Leaning Body Visual";
            bodyVisual.transform.SetParent(trickPivot);
            bodyVisual.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            bodyVisual.transform.localRotation = Quaternion.identity;
            bodyVisual.transform.localScale = new Vector3(0.82f, 0.95f, 0.82f);
            bodyVisual.GetComponent<Renderer>().material = skierMaterial;
            Destroy(bodyVisual.GetComponent<Collider>());

            GameObject skiVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            skiVisual.name = "Ski Graybox Visual";
            skiVisual.transform.SetParent(trickPivot);
            skiVisual.transform.localPosition = new Vector3(0f, -1f, 0f);
            skiVisual.transform.localRotation = Quaternion.identity;
            skiVisual.transform.localScale = new Vector3(0.22f, 0.08f, 2.15f);
            skiVisual.GetComponent<Renderer>().material = obstacleMaterial;
            Destroy(skiVisual.GetComponent<Collider>());

            SkierFeedbackController feedback = skier.AddComponent<SkierFeedbackController>();
            feedback.Configure(visualRoot.transform, skiVisual.transform, null, null);

            controller.ResetToStart();
            return controller;
        }

        private SkiFollowCamera CreateCamera(RigidbodySkierController skier)
        {
            GameObject cameraObject = new GameObject("Third Person Ski Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 2200f;

            AudioListener listener = cameraObject.AddComponent<AudioListener>();
            _ = listener;

            SkiFollowCamera follow = cameraObject.AddComponent<SkiFollowCamera>();
            follow.SetTarget(skier);
            cameraObject.transform.position = skier.transform.position + new Vector3(0f, 3f, 8f);
            cameraObject.transform.rotation = Quaternion.Euler(18f, 180f, 0f);
            follow.SnapToTarget();
            return follow;
        }

        private void CreateDebugOverlay(RigidbodySkierController skier)
        {
            GameObject overlay = new GameObject("Ski Debug Overlay");
            SkiDebugOverlay debug = overlay.AddComponent<SkiDebugOverlay>();
            _ = debug;
        }

        private void CreateRaceHud(RaceManager raceManager, RigidbodySkierController skier, Transform parent)
        {
            GameObject hudObject = new GameObject("Race HUD");
            hudObject.transform.SetParent(parent);
            RaceHud hud = hudObject.AddComponent<RaceHud>();
            hud.Configure(raceManager, skier);
        }
    }
}
