#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnderwaterOptimization
{
    public static class UnderwaterDetails
    {
        private const string SettingsFolder = "Assets/Settings";
        private const string ShaftName = "Underwater_LightShaft";
        private const string DustName = "Underwater_Dust";
        private const string FlashlightName = "Underwater_Flashlight";

        [MenuItem("Tools/Underwater/5. Add Atmosphere Details", false, 5)]
        public static void AddDetailsMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Underwater", "请先退出 Play 模式再运行。", "好");
                return;
            }

            BuildAll();

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        public static void BuildAll()
        {
            GameObject holeLightGO = GameObject.Find("Underwater_HoleLight");
            if (holeLightGO == null)
            {
                Debug.LogWarning("[Underwater] 没找到 Underwater_HoleLight，请先运行 1. Setup Atmosphere。");
                return;
            }

            Light holeLight = holeLightGO.GetComponent<Light>();
            BuildLightShaft(holeLightGO.transform.position);
            BuildDust(holeLightGO.transform.position);
            AddFlicker(holeLightGO);
            AddFlashlight();
        }

        private static void BuildLightShaft(Vector3 apex)
        {
            DestroyIfExists(ShaftName);

            // 灯在船体上方（y≈38），但玩家能看到的“光柱”是从舱内天花板（y≈18）漏下来的那一段。
            float ceilingY = 18.2f;
            float floorY = 4.5f;
            float length = Mathf.Max(4f, ceilingY - floorY);
            float bottomRadius = 3.2f;

            Mesh mesh = BuildConeMesh(length, 0.35f, bottomRadius, 40, 16);
            mesh.name = "LightShaftMesh";

            string meshPath = SettingsFolder + "/LightShaftMesh.asset";
            Mesh oldMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (oldMesh != null)
                AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);
            mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

            Material mat = GetOrCreateMaterial("Mat_LightShaft", "Underwater/LightShaft");
            if (mat == null)
                return;
            mat.SetColor("_Color", new Color(1f, 0.92f, 0.8f, 1f));
            mat.SetFloat("_Intensity", 0.95f);
            mat.SetFloat("_NoiseScale", 1.4f);
            mat.SetFloat("_EdgeBoost", 0.45f);
            mat.SetFloat("_NoiseSpeed", 0.12f);

            GameObject go = new GameObject(ShaftName);
            Undo.RegisterCreatedObjectUndo(go, "Create Light Shaft");
            go.transform.position = new Vector3(apex.x, ceilingY, apex.z);
            go.transform.rotation = Quaternion.identity;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static void BuildDust(Vector3 apex)
        {
            DestroyIfExists(DustName);

            Material mat = GetOrCreateMaterial("Mat_Dust", "Underwater/Particle");
            if (mat == null)
                return;
            mat.SetColor("_Color", new Color(0.75f, 0.90f, 1f, 1f));
            mat.SetFloat("_Intensity", 0.75f);
            mat.SetFloat("_Softness", 1.6f);

            GameObject go = new GameObject(DustName);
            Undo.RegisterCreatedObjectUndo(go, "Create Dust");
            go.transform.position = new Vector3(apex.x, 13.2f, apex.z);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 10f;
            main.startLifetime = 26f;
            main.startSpeed = 0.06f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.85f, 1f, 0.45f));
            main.maxParticles = 320;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.004f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 10f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(9f, 8f, 9f);

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 0.12f;
            noise.scrollSpeed = 0.04f;
            noise.damping = true;

            ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = mat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.shadowCastingMode = ShadowCastingMode.Off;
            psr.receiveShadows = false;
        }

        private static void AddFlicker(GameObject holeLightGO)
        {
            if (holeLightGO.GetComponent<UnderwaterLightFlicker>() == null)
                holeLightGO.AddComponent<UnderwaterLightFlicker>();
        }

        private static void AddFlashlight()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[Underwater] 没找到 Main Camera，跳过手电筒。");
                return;
            }

            DestroyIfExists(FlashlightName);

            GameObject go = new GameObject(FlashlightName);
            Undo.RegisterCreatedObjectUndo(go, "Create Flashlight");
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0.22f, -0.14f, 0.25f);
            go.transform.localRotation = Quaternion.identity;

            Light light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.94f, 0.82f);
            light.intensity = 2200f;
            light.range = 34f;
            light.spotAngle = 42f;
            light.innerSpotAngle = 22f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.9f;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.4f;

            go.AddComponent<UnderwaterFlashlight>();
        }

        private static Material GetOrCreateMaterial(string name, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("[Underwater] 找不到 Shader：" + shaderName + "（确认 Assets/Shader 下的 .shader 已编译）");
                return null;
            }

            string path = SettingsFolder + "/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Mesh BuildConeMesh(float length, float topRadius, float bottomRadius, int sides = 32, int rings = 14)
        {
            int vertexCount = (rings + 1) * sides;
            Vector3[] vertices = new Vector3[vertexCount];
            Color[] colors = new Color[vertexCount];
            int[] triangles = new int[rings * sides * 6];

            for (int r = 0; r <= rings; r++)
            {
                float t = (float)r / rings;
                float y = -t * length;
                float radius = Mathf.Lerp(topRadius, bottomRadius, t);
                float alpha = Mathf.Lerp(0.6f, 0.16f, Mathf.Pow(t, 0.5f));

                for (int s = 0; s < sides; s++)
                {
                    float angle = (float)s / sides * Mathf.PI * 2f;
                    int i = r * sides + s;
                    vertices[i] = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
                    colors[i] = new Color(1f, 1f, 1f, alpha);
                }
            }

            int k = 0;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int s2 = (s + 1) % sides;
                    int a0 = r * sides + s;
                    int a1 = r * sides + s2;
                    int b0 = (r + 1) * sides + s;
                    int b1 = (r + 1) * sides + s2;

                    triangles[k++] = a0; triangles[k++] = b0; triangles[k++] = b1;
                    triangles[k++] = a0; triangles[k++] = b1; triangles[k++] = a1;
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
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