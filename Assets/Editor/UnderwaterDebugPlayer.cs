#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace UnderwaterOptimization
{
    public static class UnderwaterDebugPlayer
    {
        private const string RootName = "DebugPlayer";
        private const string CameraName = "DebugCamera";
        private const string FlashlightName = "Underwater_Flashlight";

        [MenuItem("Tools/Underwater/7. Add Debug Player (桌面调试)", false, 7)]
        public static void AddDebugPlayerMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Underwater", "请先退出 Play 模式再运行。", "好");
                return;
            }

            string report = AddDebugPlayer();
            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Tools/Underwater/8. Remove Debug Player (恢复相机)", false, 8)]
        public static void RemoveDebugPlayerMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Underwater", "请先退出 Play 模式再运行。", "好");
                return;
            }

            RemoveDebugPlayer();
            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static string AddDebugPlayer()
        {
            RemoveDebugPlayerInternal();

            GameObject mainCamera = GameObject.Find("Main Camera");
            if (mainCamera != null)
            {
                Camera cam = mainCamera.GetComponent<Camera>();
                if (cam != null) cam.enabled = false;

                Transform oldFlashlight = mainCamera.transform.Find(FlashlightName);
                if (oldFlashlight != null) oldFlashlight.gameObject.SetActive(false);
            }

            GameObject previewCamera = GameObject.Find("Preview_Inside");
            if (previewCamera != null)
            {
                Camera cam = previewCamera.GetComponent<Camera>();
                if (cam != null) cam.enabled = false;
            }

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Debug Player");
            root.tag = "Player";
            root.transform.position = new Vector3(25f, 0.65f, 40f);
            root.transform.rotation = Quaternion.identity;

            UnityEngine.CharacterController controller = root.AddComponent<UnityEngine.CharacterController>();
            controller.height = 1.75f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.875f, 0f);
            controller.slopeLimit = 45f;
            controller.stepOffset = 0.35f;
            controller.skinWidth = 0.04f;
            controller.minMoveDistance = 0f;

            GameObject cameraGO = new GameObject(CameraName);
            cameraGO.transform.SetParent(root.transform, false);
            cameraGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cameraGO.tag = "MainCamera";

            Camera camera = cameraGO.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.008f, 0.03f, 0.04f);
            camera.fieldOfView = 65f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 300f;
            camera.depth = 20f;
            camera.allowHDR = true;
            camera.allowMSAA = true;

            UniversalAdditionalCameraData data = cameraGO.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.renderShadows = true;
            data.requiresDepthTexture = true;

            GameObject flashlight = new GameObject(FlashlightName);
            flashlight.transform.SetParent(cameraGO.transform, false);
            flashlight.transform.localPosition = new Vector3(0.22f, -0.14f, 0.25f);

            Light light = flashlight.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.94f, 0.82f);
            light.intensity = 2200f;
            light.range = 34f;
            light.spotAngle = 42f;
            light.innerSpotAngle = 22f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.9f;
            flashlight.AddComponent<UnderwaterFlashlight>();

            DebugPlayerMotor motor = root.AddComponent<DebugPlayerMotor>();
            motor.cameraTransform = cameraGO.transform;

            return "[Underwater] 调试角色已生成：DebugPlayer @ (25, 0.65, 40)，Tag=Player。\n" +
                   "进 Play 后：WASD 走动、Shift 跑、空格跳、鼠标转视角、F 开手电。\n" +
                   "Main Camera 和 Preview_Inside 已暂时关闭；不用了点菜单 8 恢复。";
        }

        public static void RemoveDebugPlayer()
        {
            RemoveDebugPlayerInternal();

            GameObject mainCamera = GameObject.Find("Main Camera");
            if (mainCamera != null)
            {
                Camera cam = mainCamera.GetComponent<Camera>();
                if (cam != null) cam.enabled = true;

                Transform oldFlashlight = mainCamera.transform.Find(FlashlightName);
                if (oldFlashlight != null) oldFlashlight.gameObject.SetActive(true);
            }

            GameObject previewCamera = GameObject.Find("Preview_Inside");
            if (previewCamera != null)
            {
                Camera cam = previewCamera.GetComponent<Camera>();
                if (cam != null) cam.enabled = true;
            }
        }

        private static void RemoveDebugPlayerInternal()
        {
            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);
        }
    }
}
#endif