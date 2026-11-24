#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnderwaterOptimization
{
    public static class UnderwaterGreybox
    {
        private const string SettingsFolder = "Assets/Settings";
        private const string ControllerName = "Flow_Controller";

        private static readonly Vector3 CrackPoint = new Vector3(24.64f, 0.62f, 51.7f);

        [MenuItem("Tools/Underwater/9. Build Greybox Flow (关卡流程)", false, 9)]
        public static void BuildMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Underwater", "请先退出 Play 模式再运行。", "好");
                return;
            }

            Build();

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        [MenuItem("Tools/Underwater/10. Remove Greybox Flow", false, 10)]
        public static void RemoveMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Underwater", "请先退出 Play 模式再运行。", "好");
                return;
            }

            Remove();

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

        }

        public static void Build()
        {
            Remove();

            Material waterMat = GetOrCreateMaterial("Mat_FlowWater", "Underwater/WaterSurface");
            if (waterMat != null)
            {
                waterMat.SetColor("_DeepColor", new Color(0.012f, 0.045f, 0.060f, 1f));
                waterMat.SetColor("_ShallowColor", new Color(0.06f, 0.20f, 0.22f, 1f));
                waterMat.SetColor("_HighlightColor", new Color(0.40f, 0.62f, 0.62f, 1f));
                waterMat.SetFloat("_Alpha", 0.90f);
                waterMat.SetFloat("_WaveHeight", 0.32f);
                waterMat.SetFloat("_WaveSpeed", 1.25f);
                waterMat.SetFloat("_Fresnel", 1.0f);
                waterMat.SetFloat("_Sparkle", 1.0f);
                EditorUtility.SetDirty(waterMat);
            }

            Material deckMat = GetOrCreateMaterial("Mat_FlowDeck", "Universal Render Pipeline/Lit");
            if (deckMat != null)
            {
                deckMat.SetColor("_BaseColor", new Color(0.16f, 0.13f, 0.11f));
                deckMat.SetFloat("_Smoothness", 0.15f);
                EditorUtility.SetDirty(deckMat);
            }

            // 指路标记用 Unlit + HDR 颜色，比 Lit 的 Emission 稳，Bloom 会自动吃到
            Material markerMat = GetOrCreateMaterial("Mat_FlowMarker", "Universal Render Pipeline/Unlit");
            if (markerMat != null)
            {
                markerMat.SetColor("_BaseColor", new Color(0.35f, 2.8f, 3.2f, 1f));
                EditorUtility.SetDirty(markerMat);
            }

            Material exitMat = GetOrCreateMaterial("Mat_FlowExit", "Universal Render Pipeline/Unlit");
            if (exitMat != null)
            {
                exitMat.SetColor("_BaseColor", new Color(1.8f, 0.42f, 0.06f, 1f));
                EditorUtility.SetDirty(exitMat);
            }

            // 水：带顶点起伏的平面，初始藏在地板下面，触发后往上涨
            GameObject water = CreateWater("Flow_Water", new Vector3(25f, 0.35f, 52f), 56f, 88f, waterMat);

            // 舱顶逃生口：一块检修平台 + 上方的锚点（终点在船顶附近）
            float ceilingY = 15.2f;
            CreateBox("Flow_CeilingLedge", new Vector3(30f, ceilingY, 52f), new Vector3(3.2f, 0.4f, 3.2f), deckMat);

            // 绳索锚点：舱顶的橙色光球（有碰撞，能被射线打中）
            GameObject anchorGO = new GameObject("Flow_RopeAnchor");
            Undo.RegisterCreatedObjectUndo(anchorGO, "Create Rope Anchor");
            anchorGO.name = "Flow_RopeAnchor";
            anchorGO.transform.position = new Vector3(32.6f, ceilingY + 0.6f, 52f);

            GameObject anchorBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            anchorBall.name = "Flow_RopeAnchorBall";
            Undo.RegisterCreatedObjectUndo(anchorBall, "Create Rope Anchor Ball");
            anchorBall.transform.SetParent(anchorGO.transform, false);
            anchorBall.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
            Renderer anchorRenderer = anchorBall.GetComponent<Renderer>();
            if (anchorRenderer != null && exitMat != null)
                anchorRenderer.sharedMaterial = exitMat;
            if (anchorRenderer != null)
                anchorRenderer.shadowCastingMode = ShadowCastingMode.Off;

            GameObject landing = new GameObject("Flow_RopeLanding");
            Undo.RegisterCreatedObjectUndo(landing, "Create Rope Landing");
            landing.transform.position = new Vector3(31.0f, ceilingY + 0.55f, 52f);

            // 给锚点加一盏小灯，暗舱里远处也能看见
            Light anchorLight = anchorGO.AddComponent<Light>();
            anchorLight.type = LightType.Point;
            anchorLight.color = new Color(1f, 0.55f, 0.2f);
            anchorLight.intensity = 260f;
            anchorLight.range = 22f;
            anchorLight.shadows = LightShadows.None;

            RopeAnchor ropeAnchor = anchorGO.AddComponent<RopeAnchor>();
            ropeAnchor.landingPoint = landing.transform;

            // 绳索枪：从原来的模型位置挪到地面，方便捡
            SetupRopeGun();

            // 手电别太爆：近距离扫到白模会糊成一片
            GameObject flashlight = GameObject.Find("Underwater_Flashlight");
            if (flashlight != null)
            {
                Light flashlightLight = flashlight.GetComponent<Light>();
                if (flashlightLight != null)
                {
                    flashlightLight.intensity = 700f;
                    flashlightLight.range = 30f;
                }
            }

            // 求救电话：打完电话才开始进水
            SetupSosPhone(deckMat);
            SetupPowerGrid(deckMat);
            SetupKey(exitMat);
            SetupQuest(deckMat, exitMat);

            // 把电话和电网面板接起来（电话必须通电才能打）
            SosPhone sosPhone = Object.FindObjectOfType<SosPhone>();
            PowerGridPanel gridPanel = Object.FindObjectOfType<PowerGridPanel>();
            if (sosPhone != null && gridPanel != null)
                sosPhone.powerGrid = gridPanel;

            // 控制器
            GameObject controllerGO = new GameObject(ControllerName);
            Undo.RegisterCreatedObjectUndo(controllerGO, "Create Flow Controller");

            UnderwaterFlow flow = controllerGO.AddComponent<UnderwaterFlow>();
            flow.water = water != null ? water.transform : null;
            flow.exitZone = null;

            GameObject holeLightGO = GameObject.Find("Underwater_HoleLight");
            if (holeLightGO != null)
                flow.holeLight = holeLightGO.GetComponent<Light>();

            // 顺手把氛围细节（光柱 + 尘埃）也带上；呼吸交给 UnderwaterFlow 控制，这里不重复挂
            UnderwaterDetails.BuildAll();
            if (holeLightGO != null)
            {
                UnderwaterLightFlicker flicker = holeLightGO.GetComponent<UnderwaterLightFlicker>();
                if (flicker != null)
                    Object.DestroyImmediate(flicker);
            }

            // 求救电话拿到 controller 引用，打完才进水
            SosPhone phone = Object.FindObjectOfType<SosPhone>();
            if (phone != null)
                phone.flow = flow;

            AssetDatabase.SaveAssets();
        }

        private static void SetupPowerGrid(Material panelMaterial)
        {
            string[] oldNames =
            {
                "Flow_PowerPanel", "Flow_PowerTerminal0", "Flow_PowerTerminal1", "Flow_PowerTerminal2",
                "Flow_PowerTerminal3", "Flow_PowerTerminal4", "Flow_PowerTerminal5"
            };
            foreach (string n in oldNames)
            {
                GameObject old = GameObject.Find(n);
                if (old != null)
                    Undo.DestroyObjectImmediate(old);
            }

            GameObject panelGO = CreateBox("Flow_PowerPanel",
                new Vector3(19.6f, 1.85f, 47.4f), new Vector3(0.14f, 1.9f, 2.8f), panelMaterial);

            PowerGridPanel panel = panelGO.AddComponent<PowerGridPanel>();

            Material terminalMat = GetOrCreateMaterial("Mat_PowerTerminal", "Universal Render Pipeline/Unlit");
            if (terminalMat != null)
            {
                terminalMat.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(terminalMat);
            }

            int[] colors = { 0, 1, 2, 2, 0, 1 };
            float[] heights = { 1.42f, 1.98f, 2.54f };

            for (int i = 0; i < 6; i++)
            {
                int column = i / 3;
                int row = i % 3;
                float z = 47.4f + (column == 0 ? -0.75f : 0.75f);

                GameObject terminalGO = CreateBox("Flow_PowerTerminal" + i,
                    new Vector3(19.75f, heights[row], z), Vector3.one * 0.3f, terminalMat);

                PowerTerminal terminal = terminalGO.AddComponent<PowerTerminal>();
                terminal.panel = panel;
                terminal.colorIndex = colors[i];
                panel.terminals.Add(terminal);
            }

            // 箱体外壳：把接线板、电话包成"一台设备"
            CreateBox("Flow_CabinetBack", new Vector3(19.36f, 1.85f, 47.4f), new Vector3(0.1f, 2.1f, 2.95f), panelMaterial);
            CreateBox("Flow_CabinetTop", new Vector3(19.8f, 2.93f, 47.4f), new Vector3(0.95f, 0.1f, 2.95f), panelMaterial);
            CreateBox("Flow_CabinetBottom", new Vector3(19.8f, 0.77f, 47.4f), new Vector3(0.95f, 0.1f, 2.95f), panelMaterial);
            CreateBox("Flow_CabinetSideL", new Vector3(19.8f, 1.85f, 45.95f), new Vector3(0.95f, 2.1f, 0.1f), panelMaterial);
            CreateBox("Flow_CabinetSideR", new Vector3(19.8f, 1.85f, 48.85f), new Vector3(0.95f, 2.1f, 0.1f), panelMaterial);

            // 呼救箱的门：挡在接线板和电话前面，用钥匙才能拉开
            // 用 CreateMarker：不带碰撞体，开门后不会挡射线
            GameObject door = CreateMarker("Flow_BoxDoor",
                new Vector3(19.98f, 1.85f, 47.4f), new Vector3(0.08f, 2.05f, 2.9f), panelMaterial);

            RescueBox box = door.AddComponent<RescueBox>();
            box.door = door.transform;
            box.panel = panel;
        }

        private static void SetupQuest(Material deckMaterial, Material glowMaterial)
        {
            string[] names = { "Flow_BrokenFlashlight", "Flow_Battery", "Flow_Quest" };
            foreach (string n in names)
            {
                GameObject old = GameObject.Find(n);
                if (old != null)
                    Undo.DestroyObjectImmediate(old);
            }

            GameObject flashlight = CreateBox("Flow_BrokenFlashlight",
                new Vector3(24.4f, 1.05f, 44.4f), new Vector3(0.12f, 0.12f, 0.34f), deckMaterial);
            flashlight.AddComponent<QuestItem>().type = QuestItemType.BrokenFlashlight;

            GameObject battery = CreateBox("Flow_Battery",
                new Vector3(21.4f, 0.95f, 37.6f), new Vector3(0.1f, 0.16f, 0.1f), glowMaterial);
            battery.AddComponent<QuestItem>().type = QuestItemType.Battery;

            GameObject questGO = new GameObject("Flow_Quest");
            Undo.RegisterCreatedObjectUndo(questGO, "Create Quest");

            FlashlightQuest quest = questGO.AddComponent<FlashlightQuest>();
            quest.flow = Object.FindObjectOfType<UnderwaterFlow>();
            quest.keyObject = GameObject.Find("Flow_Key");
        }

        private static void SetupKey(Material keyMaterial)
        {
            GameObject old = GameObject.Find("Flow_Key");
            if (old != null)
                Undo.DestroyObjectImmediate(old);

            GameObject key = CreateBox("Flow_Key",
                new Vector3(24.3f, 1.15f, 42.6f), new Vector3(0.1f, 0.06f, 0.34f), keyMaterial);

            Light light = key.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.85f, 0.45f);
            light.intensity = 20f;
            light.range = 4f;
            light.shadows = LightShadows.None;

            key.AddComponent<KeyPickup>();
        }

        private static void SetupSosPhone(Material deckMaterial)
        {
            GameObject existing = GameObject.Find("Flow_Phone");
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);

            GameObject oldDesk = GameObject.Find("Flow_PhoneDesk");
            if (oldDesk != null)
                Undo.DestroyObjectImmediate(oldDesk);

            // 电话直接装在呼救箱顶上，和接线板是一台设备
            GameObject phoneGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(phoneGO, "Create Phone");
            phoneGO.name = "Flow_Phone";
            phoneGO.transform.position = new Vector3(19.72f, 1.06f, 47.4f);
            phoneGO.transform.localScale = new Vector3(0.42f, 0.26f, 0.3f);
            phoneGO.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            Renderer phoneRenderer = phoneGO.GetComponent<Renderer>();

            // 电话用发光材质 + 一盏小灯，暗舱里也能一眼看到
            Material phoneMat = AssetDatabase.LoadAssetAtPath<Material>(SettingsFolder + "/Mat_FlowExit.mat");
            if (phoneRenderer != null)
                phoneRenderer.sharedMaterial = phoneMat != null ? phoneMat : deckMaterial;

            Light phoneLight = phoneGO.AddComponent<Light>();
            phoneLight.type = LightType.Point;
            phoneLight.color = new Color(1f, 0.6f, 0.25f);
            phoneLight.intensity = 30f;
            phoneLight.range = 5f;
            phoneLight.shadows = LightShadows.None;

            phoneGO.AddComponent<SosPhone>();
        }

        private static void SetupRopeGun()
        {
            GameObject gun = GameObject.Find("Rope gun simple");
            if (gun == null)
            {
                Debug.LogWarning("[Underwater] 没找到 Rope gun simple，跳过绳索枪。");
                return;
            }

            Undo.RecordObject(gun.transform, "Move Rope Gun");
            gun.transform.position = new Vector3(24.8f, 1.6f, 46.5f);
            gun.transform.rotation = Quaternion.identity;

            // 清掉脚本重导后留下的 Missing Script
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gun);

            RopeGun ropeGun = gun.GetComponent<RopeGun>();
            if (ropeGun == null)
                ropeGun = gun.AddComponent<RopeGun>();

            ropeGun.pickupRange = 6f;
        }

        public static void Remove()
        {
            string[] names =
            {
                ControllerName, "Flow_Water", "Flow_Platform", "Flow_Ramp", "Flow_Phone", "Flow_PhoneDesk", "Flow_CeilingLedge", "Flow_PowerPanel",
                "Flow_PowerTerminal0", "Flow_PowerTerminal1", "Flow_PowerTerminal2",
                "Flow_PowerTerminal3", "Flow_PowerTerminal4", "Flow_PowerTerminal5",
                "Flow_DoorPostL", "Flow_DoorPostR", "Flow_DoorLintel", "Flow_DoorGlow",
                "Flow_MarkerCrack", "Flow_MarkerExit", "Flow_CrackZone", "Flow_ExitZone",
                "Flow_RopeAnchor", "Flow_RopeLanding", "Flow_HUD", "Flow_Key", "Flow_BoxDoor",
                "Flow_BrokenFlashlight", "Flow_Battery", "Flow_Quest",
                "Flow_CabinetBack", "Flow_CabinetTop", "Flow_CabinetBottom", "Flow_CabinetSideL", "Flow_CabinetSideR"
            };

            foreach (string name in names)
            {
                GameObject go = GameObject.Find(name);
                while (go != null)
                {
                    Undo.DestroyObjectImmediate(go);
                    go = GameObject.Find(name);
                }
            }
        }

        private static Mesh GetOrCreateRingMesh()
        {
            string path = SettingsFolder + "/RopeRingMesh.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
                return existing;

            Mesh mesh = BuildTorusMesh(1.05f, 0.18f, 48, 14);
            mesh.name = "RopeRingMesh";
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }

        private static Mesh BuildTorusMesh(float radius, float tube, int segments, int tubeSegments)
        {
            Vector3[] vertices = new Vector3[segments * tubeSegments];
            int[] triangles = new int[segments * tubeSegments * 6];

            for (int i = 0; i < segments; i++)
            {
                float u = (float)i / segments * Mathf.PI * 2f;
                for (int j = 0; j < tubeSegments; j++)
                {
                    float v = (float)j / tubeSegments * Mathf.PI * 2f;
                    float r = radius + tube * Mathf.Cos(v);
                    vertices[i * tubeSegments + j] = new Vector3(r * Mathf.Cos(u), tube * Mathf.Sin(v), r * Mathf.Sin(u));
                }
            }

            int k = 0;
            for (int i = 0; i < segments; i++)
            {
                int i2 = (i + 1) % segments;
                for (int j = 0; j < tubeSegments; j++)
                {
                    int j2 = (j + 1) % tubeSegments;
                    int a = i * tubeSegments + j;
                    int b = i2 * tubeSegments + j;
                    int c = i * tubeSegments + j2;
                    int d = i2 * tubeSegments + j2;

                    triangles[k++] = a; triangles[k++] = b; triangles[k++] = d;
                    triangles[k++] = a; triangles[k++] = d; triangles[k++] = c;
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject CreateWater(string name, Vector3 position, float width, float depth, Material material)
        {
            string meshPath = SettingsFolder + "/WaterSurfaceMesh.asset";
            Mesh oldMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (oldMesh != null)
                AssetDatabase.DeleteAsset(meshPath);

            Mesh mesh = BuildWaterMesh(width, depth, 112, 176);
            mesh.name = "WaterSurfaceMesh";
            AssetDatabase.CreateAsset(mesh, meshPath);
            AssetDatabase.SaveAssets();
            mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.position = position;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return go;
        }

        private static Mesh BuildWaterMesh(float width, float depth, int segX, int segZ)
        {
            int vx = segX + 1;
            int vz = segZ + 1;
            Vector3[] vertices = new Vector3[vx * vz];
            Vector3[] normals = new Vector3[vx * vz];
            Vector2[] uvs = new Vector2[vx * vz];
            int[] triangles = new int[segX * segZ * 6];

            for (int z = 0; z < vz; z++)
            {
                for (int x = 0; x < vx; x++)
                {
                    int i = z * vx + x;
                    vertices[i] = new Vector3(((float)x / segX - 0.5f) * width, 0f, ((float)z / segZ - 0.5f) * depth);
                    normals[i] = Vector3.up;
                    uvs[i] = new Vector2((float)x / segX, (float)z / segZ);
                }
            }

            int k = 0;
            for (int z = 0; z < segZ; z++)
            {
                for (int x = 0; x < segX; x++)
                {
                    int a = z * vx + x;
                    int b = a + 1;
                    int c = a + vx;
                    int d = c + 1;

                    triangles[k++] = a; triangles[k++] = c; triangles[k++] = b;
                    triangles[k++] = b; triangles[k++] = c; triangles[k++] = d;
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            return go;
        }

        private static GameObject CreateMarker(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = CreateBox(name, position, scale, material);
            if (go == null)
                return null;

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);

            return go;
        }

        private static GameObject CreateZone(string name, Vector3 position, Vector3 size)
        {
            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.position = position;

            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;

            return go;
        }

        private static Material GetOrCreateMaterial(string name, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("[Underwater] 找不到 Shader：" + shaderName);
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
    }
}
#endif