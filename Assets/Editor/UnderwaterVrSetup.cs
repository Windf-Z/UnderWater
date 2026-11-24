#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.PackageManager.UI;

namespace UnderwaterOptimization
{
    public static class UnderwaterVrSetup
    {
        private const string RootName = "VR_Rig";
        private const string SimulatorName = "XR Device Simulator";

        [MenuItem("Tools/Underwater/11. Setup VR Rig (XR Origin + Simulator)", false, 11)]
        public static void SetupVrMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Underwater", "请先退出 Play 模式再运行。", "好");
                return;
            }

            string report = SetupVr();

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

        }

        public static string SetupVr()
        {
            string importReport = ImportSamples();

            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
                Object.DestroyImmediate(existing);

            GameObject simulator = GameObject.Find(SimulatorName);
            if (simulator != null)
                Object.DestroyImmediate(simulator);

            GameObject originPrefab = FindPrefab("XR Origin (XR Rig)");
            if (originPrefab == null)
                return "[Underwater] 找不到 XR Origin 预fab，先看 Package Manager 里 XRI 示例是否导入成功。";

            GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(originPrefab);
            rig.name = RootName;
            rig.transform.position = new Vector3(25f, 1.2f, 40f);
            rig.transform.rotation = Quaternion.identity;

            // 关掉桌面角色和相机，避免抢画面
            GameObject debugPlayer = GameObject.Find("DebugPlayer");
            if (debugPlayer != null)
                debugPlayer.SetActive(false);

            Camera[] cameras = Object.FindObjectsOfType<Camera>();
            foreach (Camera cam in cameras)
            {
                if (cam.transform.IsChildOf(rig.transform))
                    continue;
                cam.enabled = false;
            }

            // 只保留 VR 相机上的 AudioListener，消掉 "2 audio listeners" 警告
            AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>();
            foreach (AudioListener listener in listeners)
            {
                if (!listener.transform.IsChildOf(rig.transform))
                    listener.enabled = false;
            }

            GameObject locoPrefab = FindPrefab("XR Device Simulator");
            if (locoPrefab != null)
            {
                GameObject sim = (GameObject)PrefabUtility.InstantiatePrefab(locoPrefab);
                sim.name = SimulatorName;

                // 关掉模拟器自带的手模型，别挡住我们自己的手柄
                Renderer[] simRenderers = sim.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in simRenderers)
                {
                    if (renderer.gameObject.name.ToLower().Contains("hand"))
                        renderer.enabled = false;
                }
            }

            if (Object.FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() == null)
            {
                GameObject manager = new GameObject("XR Interaction Manager");
                manager.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            }

            XRInputBridge bridge = rig.GetComponent<XRInputBridge>();
            if (bridge == null)
                bridge = rig.AddComponent<XRInputBridge>();

            return importReport + "\n[Underwater] VR 角色已就位：WASD 移动（模拟器）、鼠标看、右键传送、手柄射线交互。";
        }

        [MenuItem("Tools/Underwater/12. Toggle Desktop / VR", false, 12)]
        public static void ToggleDesktopVr()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Underwater", "请先退出 Play 模式再运行。", "好");
                return;
            }

            GameObject player = FindRoot("DebugPlayer");
            GameObject rig = FindRoot("VR_Rig");
            GameObject simulator = FindRoot("XR Device Simulator");

            bool rigActive = rig != null && rig.activeSelf;
            bool toVr = !rigActive;

            if (player != null)
                player.SetActive(!toVr);
            if (rig != null)
                rig.SetActive(toVr);
            if (simulator != null)
                simulator.SetActive(toVr);

            Camera desktopCamera = player != null ? player.GetComponentInChildren<Camera>(true) : null;
            Camera vrCamera = rig != null ? rig.GetComponentInChildren<Camera>(true) : null;

            if (desktopCamera != null)
                desktopCamera.enabled = !toVr;
            if (vrCamera != null)
                vrCamera.enabled = toVr;

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        }

        private static GameObject FindRoot(string name)
        {
            foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (go.name == name)
                    return go;
            }

            return null;
        }

        private static string ImportSamples()
        {
            System.Collections.Generic.IEnumerable<Sample> samples = Sample.FindByPackage("com.unity.xr.interaction.toolkit", null);
            if (samples == null)
                return "[Underwater] 没读到 XRI 示例列表。";

            int imported = 0;
            foreach (Sample sample in samples)
            {
                if (sample.displayName.Contains("XR Device Simulator") || sample.displayName.Contains("Starter Assets"))
                {
                    if (sample.Import(Sample.ImportOptions.OverridePreviousImports))
                        imported++;
                }
            }

            AssetDatabase.Refresh();
            return "[Underwater] XRI 示例导入：" + imported + " 个。";
        }

        private static GameObject FindPrefab(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Samples") && path.EndsWith("/" + name + ".prefab"))
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            return null;
        }
    }
}
#endif