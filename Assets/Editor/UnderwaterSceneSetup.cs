#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace UnderwaterOptimization
{
    public static class UnderwaterSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/Craked.unity";
        private const string ProfileFolder = "Assets/Settings";
        private const string ProfilePath = "Assets/Settings/UnderwaterAtmosphereProfile.asset";

        //  亮度总表：想整体变暗 / 变亮，只改这一段，然后点菜单 4 立刻生效。
        private const float Exposure = -1.0f;

        private static readonly Color HoleColor = new Color(1f, 0.93f, 0.82f);
        private const float HoleIntensity = 6000f;
        private const float HoleRange = 48f;
        private const float HoleSpotAngle = 58f;
        private const float HoleInnerSpotAngle = 26f;

        private static readonly Color FillColor = new Color(0.30f, 0.50f, 0.66f);
        private const float FillIntensity = 50f;
        private const float FillRange = 42f;

        private static readonly Color WarmColor = new Color(1f, 0.75f, 0.5f);
        private const float WarmIntensity = 140f;
        private const float WarmRange = 13f;

        private static readonly Color AmbientColor = new Color(0.020f, 0.030f, 0.040f); // 环境光往下压，暗部更沉
        private const float FogDensity = 0.045f;           // 保留（指数模式备用），当前用线性雾
        private const float FogStart = 3.5f;               // 线性雾：3.5 米外开始起雾
        private const float FogEnd = 20f;                  // 线性雾：20 米处完全融入雾
        private static readonly Color FogColor = new Color(0.05f, 0.14f, 0.17f); // 雾色偏青

        private const float BloomIntensity = 0.30f;
        private const float VignetteIntensity = 0.28f;

        private static readonly Vector3 FillPosition = new Vector3(25f, 12f, 51.6f);
        private static readonly Vector3 WarmPosition = new Vector3(20f, 9f, 44f);

        [MenuItem("Tools/Underwater/1. Setup Atmosphere", false, 1)]
        public static void SetupAtmosphere()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Underwater", "请先退出 Play 模式再运行。", "好");
                return;
            }

            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            // 让重复运行也保持干净：先清掉上一次生成的物体
            DestroyIfExists("Underwater_GlobalVolume");
            DestroyIfExists("Underwater_HoleLight");
            DestroyIfExists("Underwater_FillLight");
            DestroyIfExists("Underwater_WarmLight");
            DestroyIfExists("Preview_Inside");
            DestroyIfExists("Underwater_LightShaft");
            DestroyIfExists("Underwater_Dust");
            DestroyIfExists("Underwater_Flashlight");

            VolumeProfile profile = CreateAtmosphereProfile();
            SetupGlobalVolume(profile);
            SetupMainCamera();
            SetupRenderSettings();
            SetupHoleLight();
            SetupFillLights();
            SetupPreviewCamera();
            FocusSceneView();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        [MenuItem("Tools/Underwater/2. Toggle Preview Camera", false, 2)]
        public static void TogglePreviewCamera()
        {
            GameObject go = GameObject.Find("Preview_Inside");
            if (go == null)
            {
                Debug.LogWarning("[Underwater] 没找到 Preview_Inside，请先运行 1. Setup Atmosphere。");
                return;
            }

            Camera cam = go.GetComponent<Camera>();
            if (cam == null)
            {
                Debug.LogWarning("[Underwater] Preview_Inside 上没有 Camera 组件。");
                return;
            }

            cam.enabled = !cam.enabled;
        }

        [MenuItem("Tools/Underwater/3. Toggle Atmosphere (A/B)", false, 3)]
        public static void ToggleAtmosphere()
        {
            GameObject volumeGO = GameObject.Find("Underwater_GlobalVolume");
            GameObject lightGO = GameObject.Find("Underwater_HoleLight");
            if (volumeGO == null || lightGO == null)
            {
                Debug.LogWarning("[Underwater] 没找到氛围物体，请先运行 1. Setup Atmosphere。");
                return;
            }

            Volume volume = volumeGO.GetComponent<Volume>();
            Light holeLight = lightGO.GetComponent<Light>();
            bool enable = volume != null && !volume.enabled;

            if (volume != null)
                volume.enabled = enable;
            if (holeLight != null)
                holeLight.enabled = enable;

            GameObject fillGO = GameObject.Find("Underwater_FillLight");
            if (fillGO != null)
            {
                Light fillLight = fillGO.GetComponent<Light>();
                if (fillLight != null)
                    fillLight.enabled = enable;
            }

            GameObject warmGO = GameObject.Find("Underwater_WarmLight");
            if (warmGO != null)
            {
                Light warmLight = warmGO.GetComponent<Light>();
                if (warmLight != null)
                    warmLight.enabled = enable;
            }

            GameObject shaftGO = GameObject.Find("Underwater_LightShaft");
            if (shaftGO != null)
            {
                MeshRenderer shaftRenderer = shaftGO.GetComponent<MeshRenderer>();
                if (shaftRenderer != null)
                    shaftRenderer.enabled = enable;
            }

            GameObject dustGO = GameObject.Find("Underwater_Dust");
            if (dustGO != null)
            {
                ParticleSystemRenderer dustRenderer = dustGO.GetComponent<ParticleSystemRenderer>();
                if (dustRenderer != null)
                    dustRenderer.enabled = enable;
            }

            SetCameraAtmosphere(Camera.main, enable);

            GameObject previewGO = GameObject.Find("Preview_Inside");
            if (previewGO != null)
            {
                Camera previewCam = previewGO.GetComponent<Camera>();
                SetCameraAtmosphere(previewCam, enable);
            }

            RenderSettings.fog = enable;
            if (enable)
            {
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = FogColor;
                RenderSettings.fogStartDistance = FogStart;
                RenderSettings.fogEndDistance = FogEnd;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = AmbientColor;
            }
            else
            {
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientIntensity = 1f;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("Tools/Underwater/4. Re-apply Lighting (不重建)", false, 4)]
        public static void ReapplyLighting()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Underwater", "请先退出 Play 模式再运行。", "好");
                return;
            }

            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            if (GameObject.Find("Underwater_HoleLight") == null)
            {
                Debug.LogWarning("[Underwater] 场景里还没有氛围灯光，请先运行 1. Setup Atmosphere。");
                return;
            }

            SetupRenderSettings();
            SetupHoleLight();
            SetupFillLights();

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                Debug.LogWarning("[Underwater] 找不到 " + ProfilePath + "，请先运行 1. Setup Atmosphere。");
                return;
            }

            ApplyProfileLook(profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetCameraAtmosphere(Camera cam, bool enable)
        {
            if (cam == null)
                return;

            cam.clearFlags = enable ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
            cam.backgroundColor = enable ? new Color(0.008f, 0.03f, 0.04f) : Color.black;

            UniversalAdditionalCameraData data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data != null)
                data.renderPostProcessing = enable;
        }

        private static VolumeProfile CreateAtmosphereProfile()
        {
            if (!AssetDatabase.IsValidFolder(ProfileFolder))
                AssetDatabase.CreateFolder("Assets", "Settings");

            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath) != null)
                AssetDatabase.DeleteAsset(ProfilePath);

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "UnderwaterAtmosphereProfile";
            AssetDatabase.CreateAsset(profile, ProfilePath);

            Tonemapping tonemapping = AddOverride<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);

            Bloom bloom = AddOverride<Bloom>(profile);
            bloom.threshold.Override(1.0f);
            bloom.intensity.Override(BloomIntensity);
            bloom.scatter.Override(0.7f);
            bloom.tint.Override(new Color(1f, 0.95f, 0.9f));
            bloom.highQualityFiltering.Override(true);

            Vignette vignette = AddOverride<Vignette>(profile);
            vignette.intensity.Override(VignetteIntensity);
            vignette.smoothness.Override(0.45f);

            ChromaticAberration chromaticAberration = AddOverride<ChromaticAberration>(profile);
            chromaticAberration.intensity.Override(0.05f);

            ColorAdjustments colorAdjustments = AddOverride<ColorAdjustments>(profile);
            colorAdjustments.postExposure.Override(Exposure);
            colorAdjustments.contrast.Override(10f);
            colorAdjustments.saturation.Override(-10f);
            colorAdjustments.colorFilter.Override(new Color(0.85f, 0.98f, 1.05f));

            LiftGammaGain liftGammaGain = AddOverride<LiftGammaGain>(profile);
            liftGammaGain.lift.Override(new Vector4(0.95f, 0.99f, 1.02f, 0f));
            liftGammaGain.gamma.Override(new Vector4(1f, 1f, 1f, 0f));
            liftGammaGain.gain.Override(new Vector4(1.05f, 1f, 0.95f, 0f));

            WhiteBalance whiteBalance = AddOverride<WhiteBalance>(profile);
            whiteBalance.temperature.Override(-15f);
            whiteBalance.tint.Override(5f);

            FilmGrain filmGrain = AddOverride<FilmGrain>(profile);
            filmGrain.type.Override(FilmGrainLookup.Thin1);
            filmGrain.intensity.Override(0.08f);
            filmGrain.response.Override(0.8f);

            foreach (VolumeComponent component in profile.components)
                EditorUtility.SetDirty(component);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void ApplyProfileLook(VolumeProfile profile)
        {
            ColorAdjustments colorAdjustments;
            if (profile.TryGet(out colorAdjustments))
            {
                colorAdjustments.postExposure.Override(Exposure);
                colorAdjustments.colorFilter.Override(new Color(0.85f, 0.98f, 1.05f));
            }

            Bloom bloom;
            if (profile.TryGet(out bloom))
                bloom.intensity.Override(BloomIntensity);

            Vignette vignette;
            if (profile.TryGet(out vignette))
                vignette.intensity.Override(VignetteIntensity);

            LiftGammaGain liftGammaGain;
            if (profile.TryGet(out liftGammaGain))
                liftGammaGain.lift.Override(new Vector4(0.95f, 0.99f, 1.02f, 0f));

            foreach (VolumeComponent component in profile.components)
                EditorUtility.SetDirty(component);
        }

        private static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.Add<T>(true);
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        private static void SetupGlobalVolume(VolumeProfile profile)
        {
            GameObject go = new GameObject("Underwater_GlobalVolume");
            Undo.RegisterCreatedObjectUndo(go, "Create Underwater Volume");

            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
        }

        private static void SetupMainCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Camera[] cameras = Object.FindObjectsOfType<Camera>();
                if (cameras.Length > 0)
                    cam = cameras[0];
            }

            if (cam == null)
            {
                Debug.LogWarning("[Underwater] 场景里没有相机，跳过相机设置。");
                return;
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.008f, 0.03f, 0.04f);

            UniversalAdditionalCameraData data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null)
                data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();

            data.renderPostProcessing = true;
            data.renderShadows = true;
            data.requiresDepthTexture = true;
            data.requiresColorTexture = true;
        }

        private static void SetupRenderSettings()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = FogStart;
            RenderSettings.fogEndDistance = FogEnd;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = AmbientColor;
        }

        private static void SetupHoleLight()
        {
            GameObject go = GameObject.Find("Underwater_HoleLight");
            if (go == null)
            {
                GameObject hole = GameObject.Find("ceiling hole");
                Vector3 position = hole != null ? hole.transform.position : new Vector3(24.6f, 39.1f, 51.7f);

                go = new GameObject("Underwater_HoleLight");
                Undo.RegisterCreatedObjectUndo(go, "Create Hole Light");
                // 放在天花板下方，避免灯被天花板挡住
                go.transform.position = position + Vector3.down * 0.6f;
                go.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                go.AddComponent<Light>();
            }

            Light light = go.GetComponent<Light>();
            if (light == null)
                light = go.AddComponent<Light>();

            light.type = LightType.Spot;
            light.color = HoleColor;
            light.intensity = HoleIntensity;
            light.range = HoleRange;
            light.spotAngle = HoleSpotAngle;
            light.innerSpotAngle = HoleInnerSpotAngle;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.9f;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.4f;
        }

        private static void SetupFillLights()
        {
            CreatePointLight("Underwater_FillLight", FillPosition, FillColor, FillIntensity, FillRange);
            CreatePointLight("Underwater_WarmLight", WarmPosition, WarmColor, WarmIntensity, WarmRange);
        }

        private static void CreatePointLight(string objectName, Vector3 position, Color color, float intensity, float range)
        {
            GameObject go = GameObject.Find(objectName);
            if (go == null)
            {
                go = new GameObject(objectName);
                Undo.RegisterCreatedObjectUndo(go, "Create " + objectName);
                go.transform.position = position;
                go.AddComponent<Light>();
            }

            Light light = go.GetComponent<Light>();
            if (light == null)
                light = go.AddComponent<Light>();

            go.transform.position = position;
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static void SetupPreviewCamera()
        {
            GameObject go = new GameObject("Preview_Inside");
            Undo.RegisterCreatedObjectUndo(go, "Create Preview Camera");
            go.transform.position = new Vector3(12f, 14f, 44f);
            go.transform.LookAt(new Vector3(25f, 8f, 51f));

            Camera cam = go.AddComponent<Camera>();
            cam.enabled = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.008f, 0.03f, 0.04f);
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.depth = 10f;
            cam.allowHDR = true;
            cam.allowMSAA = true;

            UniversalAdditionalCameraData data = go.GetComponent<UniversalAdditionalCameraData>();
            if (data == null)
                data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.renderShadows = true;
            data.requiresDepthTexture = true;
            data.requiresColorTexture = true;
        }

        private static void FocusSceneView()
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null)
                return;

            view.LookAt(new Vector3(25f, 8f, 51f), Quaternion.Euler(10f, 0f, 0f), 25f);
            view.Repaint();
        }

        private static void DestroyIfExists(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);
        }
    }
}
#endif