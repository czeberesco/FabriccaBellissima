using System.IO;
using FabriccaBellissima.Factory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace FabriccaBellissima.FactoryEditor
{
    public static class FactoryDemoGenerator
    {
        private const string GeneratedFolder = "Assets/Factory/Generated";
        private const string ScenePath = "Assets/Scenes/MainGameScene.unity";
        private const string ResourcesFolder = "Assets/Resources";
        private const string ProjectContextPath = "Assets/Resources/ProjectContext.prefab";

        [MenuItem("Tools/Fabbrica Bellissima/Generate Complete Demo")]
        public static void GenerateCompleteDemo()
        {
            EnsureFolder();
            CreateProjectContext();
            CreateMaterials();
            CreatePrefabs();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainGameScene";

            GameObject systems = new GameObject("Factory Systems");
            FactoryDemoAuthoring authoring = systems.AddComponent<FactoryDemoAuthoring>();
            authoring.ResetToDefaults();
            File.WriteAllText($"{GeneratedFolder}/DefaultScenario.json", authoring.CompileScenario().ToJson(true));
            AssetDatabase.ImportAsset($"{GeneratedFolder}/DefaultScenario.json", ImportAssetOptions.ForceUpdate);
            FactorySimulationCoordinator coordinator = systems.AddComponent<FactorySimulationCoordinator>();
            FactoryPresentation presentation = systems.AddComponent<FactoryPresentation>();
            FactoryControlPanel controlPanel = systems.AddComponent<FactoryControlPanel>();
            FactorySceneInstaller installer = systems.AddComponent<FactorySceneInstaller>();
            installer.Configure(authoring, coordinator, presentation, controlPanel);
            SceneContext sceneContext = systems.AddComponent<SceneContext>();
            sceneContext.Installers = new MonoInstaller[] { installer };

            CreateEnvironment();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = systems;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Fabbrica Bellissima demo generated at {ScenePath}");
        }

        public static void GenerateFromCommandLine()
        {
            GenerateCompleteDemo();
        }

        public static void CapturePreviewFromCommandLine()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            FactoryDemoAuthoring authoring = Object.FindFirstObjectByType<FactoryDemoAuthoring>();
            FactorySimulationCoordinator coordinator = Object.FindFirstObjectByType<FactorySimulationCoordinator>();
            FactoryPresentation presentation = Object.FindFirstObjectByType<FactoryPresentation>();
            Camera camera = Camera.main;
            if (authoring == null || coordinator == null || presentation == null || camera == null)
                throw new MissingReferenceException("Generated demo scene is missing a required factory component or camera.");

            var registry = FactoryNodeFactoryRegistry.CreateDefault();
            var simulationFactory = new FactorySimulationFactory(registry,
                new FactoryTransferResolver(), new FactorySnapshotBuilder());
            coordinator.Construct(authoring, simulationFactory);
            presentation.Construct(coordinator);
            coordinator.Initialize(authoring.CompileScenario());
            presentation.SendMessage("Start", SendMessageOptions.RequireReceiver);

            const int width = 1600;
            const int height = 900;
            var renderTexture = new RenderTexture(width, height, 24);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../factory-preview.png"));
            File.WriteAllBytes(output, image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = previous;
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(image);
            Debug.Log($"Factory preview captured at {output}");
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
                AssetDatabase.CreateFolder("Assets/Factory", "Generated");
        }

        private static void CreateProjectContext()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");

            GameObject root = new GameObject("ProjectContext");
            ProjectContext context = root.AddComponent<ProjectContext>();
            FactoryProjectInstaller installer = root.AddComponent<FactoryProjectInstaller>();
            context.Installers = new MonoInstaller[] { installer };
            PrefabUtility.SaveAsPrefabAsset(root, ProjectContextPath);
            Object.DestroyImmediate(root);
        }

        private static void CreateMaterials()
        {
            CreateOrUpdateMaterial("Machine.mat", new Color(0.18f, 0.38f, 0.42f), 0.35f);
            CreateOrUpdateMaterial("Conveyor.mat", new Color(0.08f, 0.1f, 0.12f), 0.15f);
            CreateOrUpdateMaterial("Floor.mat", new Color(0.12f, 0.14f, 0.16f), 0f);
        }

        private static void CreatePrefabs()
        {
            GameObject machine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            machine.name = "MachinePrototype";
            machine.transform.localScale = new Vector3(2.2f, 1.4f, 2.2f);
            machine.GetComponent<Renderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>($"{GeneratedFolder}/Machine.mat");
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beacon.name = "ActivityBeacon";
            beacon.transform.SetParent(machine.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            beacon.transform.localScale = new Vector3(0.2f, 0.15f, 0.2f);
            PrefabUtility.SaveAsPrefabAsset(machine, $"{GeneratedFolder}/MachinePrototype.prefab");
            Object.DestroyImmediate(machine);

            GameObject belt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            belt.name = "ConveyorPrototype";
            belt.transform.localScale = new Vector3(0.9f, 0.18f, 4f);
            belt.GetComponent<Renderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>($"{GeneratedFolder}/Conveyor.mat");
            PrefabUtility.SaveAsPrefabAsset(belt, $"{GeneratedFolder}/ConveyorPrototype.prefab");
            Object.DestroyImmediate(belt);
        }

        private static void CreateEnvironment()
        {
            Camera camera = new GameObject("Factory Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(3f, 20f, -17f);
            camera.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = 11f;
            camera.rect = new Rect(0.3f, 0f, 0.7f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.055f, 0.075f);
            camera.gameObject.tag = "MainCamera";

            Light light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Factory Floor";
            floor.transform.position = new Vector3(3f, -0.25f, 1f);
            floor.transform.localScale = new Vector3(3.5f, 1f, 3f);
            floor.GetComponent<Renderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>($"{GeneratedFolder}/Floor.mat");
        }

        private static void CreateOrUpdateMaterial(string fileName, Color color, float metallic)
        {
            string path = $"{GeneratedFolder}/{fileName}";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(material);
        }
    }
}
