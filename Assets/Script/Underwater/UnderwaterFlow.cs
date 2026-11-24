using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UnderwaterOptimization
{
    public class UnderwaterFlow : MonoBehaviour
    {
        [Header("引用")]
        public Transform water;
        public Light holeLight;
        public Collider exitZone;
        public Camera playerCamera;

        [Header("水位")]
        public float floorY = 0.62f;
        public float waterStartY = 0.35f;
        public float waterEndY = 1.85f;
        public float floodDuration = 90f;

        [Header("提示文案")]
        public string hintStart = "船体破损了，四周一片漆黑 —— 先找到手电筒和电池";
        public string hintFlood = "电话打出去了！水正在漫上来 —— 用绳索枪射向舱顶的橙色锚点";
        public string hintWin = "你被绳索拽上舱顶，从破口钻了出去";

        private enum Stage
        {
            Idle,
            Flooding,
            Escaped
        }

        private Stage stage = Stage.Idle;
        private float floodTime;
        private Text hud;
        private Text levelText;
        private Text aimInfoText;
        private Text objectiveText;
        private GameObject victoryPanel;
        private Image tint;
        private GameObject dialoguePanel;
        private Text dialogueText;
        private float shakeTime;
        private Transform shakeCamera;
        private Vector3 shakeBase;
        private Color holeLightColor;
        private float holeLightIntensity;
        private bool initialized;
        private bool inDialogue;
        private Light fillLight;
        private GameObject ropeGun;

        public bool IsFlooding
        {
            get { return stage == Stage.Flooding; }
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (initialized)
                return;
            initialized = true;

            if (playerCamera == null)
            {
                Camera[] cameras = Camera.allCameras;
                for (int i = 0; i < cameras.Length; i++)
                {
                    if (cameras[i].isActiveAndEnabled)
                    {
                        playerCamera = cameras[i];
                        break;
                    }
                }
            }

            GameObject fillGO = GameObject.Find("Underwater_FillLight");
            if (fillGO != null)
                fillLight = fillGO.GetComponent<Light>();

            // 绳索枪一开始不存在，打完电话才出现
            RopeGun[] guns = FindObjectsOfType<RopeGun>(true);
            if (guns.Length > 0)
                ropeGun = guns[0].gameObject;
            if (ropeGun != null)
                ropeGun.SetActive(false);

            ApplyDimLook();

            BuildHud();
            SetHint(hintStart);
            SetObjective("目标 1/7：找到破损的手电筒");

            if (exitZone != null)
                exitZone.enabled = false;

            if (water != null)
            {
                water.position = new Vector3(water.position.x, waterStartY, water.position.z);
                // 通电之前水根本不存在，免得水波穿出地板
                water.gameObject.SetActive(false);

                // 浮力脚本先关掉：水没来之前东西不该飘
                Floater[] floaters = FindObjectsOfType<Floater>(true);
                foreach (Floater floater in floaters)
                    floater.enabled = false;
            }

            if (holeLight != null)
            {
                holeLightColor = holeLight.color;
                holeLightIntensity = holeLight.intensity;
            }
        }

        private void Update()
        {
            if (stage == Stage.Flooding)
            {
                floodTime += Time.deltaTime;
                float k = Mathf.Clamp01(floodTime / Mathf.Max(0.01f, floodDuration));

                if (water != null)
                    water.position = new Vector3(water.position.x, Mathf.Lerp(waterStartY, waterEndY, k), water.position.z);

                if (holeLight != null)
                {
                    float flicker = 0.75f + 0.25f * Mathf.PerlinNoise(Time.time * 6f, 0.37f);
                    holeLight.color = Color.Lerp(holeLightColor, new Color(1f, 0.38f, 0.22f), Mathf.Clamp01(k * 1.4f));
                    holeLight.intensity = holeLightIntensity * (1f - 0.3f * k) * flicker;
                }
            }

            // 调试便利：任何时候按 R 都能重开一局（不用退出 Play）
            if (UnderwaterInput.RestartDown)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

            UpdateHud();
        }

        private void LateUpdate()
        {
            if (shakeTime <= 0f || shakeCamera == null)
                return;

            shakeTime -= Time.deltaTime;
            float strength = Mathf.Clamp01(shakeTime / 1.8f) * 0.09f;
            shakeCamera.localPosition = shakeBase + Random.insideUnitSphere * strength;

            if (shakeTime <= 0f)
                shakeCamera.localPosition = shakeBase;
        }

        private void ApplyDimLook()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.012f, 0.016f, 0.022f);

            if (fillLight != null)
                fillLight.intensity = 30f;
        }

        public void OnFlashlightAssembled()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.16f);

            if (fillLight != null)
                fillLight.intensity = 600f;

            SetHint("手电筒亮了 —— 整个舱内都看得清了");
        }

        public void OnGridPowered()
        {
            if (stage == Stage.Idle)
                EnterCrack();

            SetObjective("目标 6/7：拨打求救电话");
        }

        public void EnterCrack()
        {
            if (stage != Stage.Idle)
                return;

            stage = Stage.Flooding;
            floodTime = 0f;

            if (water != null)
            {
                water.position = new Vector3(water.position.x, waterStartY, water.position.z);
                water.gameObject.SetActive(true);

                // 水来了，东西才开始往上飘
                Floater[] floaters = FindObjectsOfType<Floater>(true);
                foreach (Floater floater in floaters)
                    floater.enabled = true;
            }

            if (exitZone != null)
                exitZone.enabled = true;

            SetObjective("目标 7/7：拿到绳索枪，射向舱顶的橙色锚点");
            SetHint(hintFlood);
            StartShake();
        }

        public void EnterExit()
        {
            SubmitEscape();
        }

        public void SubmitEscape()
        {
            if (stage == Stage.Escaped)
                return;

            stage = Stage.Escaped;
            SetHint("你被绳索拽上舱顶，从破口钻了出去");
            SetAimInfo("", false);
            ShowVictory();
        }

        private void ShowVictory()
        {
            if (victoryPanel == null)
            {
                GameObject canvasGO = GameObject.Find("Flow_HUD");
                Transform parent = canvasGO != null ? canvasGO.transform : transform;

                victoryPanel = new GameObject("VictoryPanel");
                victoryPanel.transform.SetParent(parent, false);

                Image background = victoryPanel.AddComponent<Image>();
                background.color = new Color(0f, 0f, 0f, 0.85f);
                background.raycastTarget = false;
                RectTransform rect = background.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Text title = CreateText(victoryPanel.transform, "VictoryTitle", 96, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(1600f, 180f), new Vector2(0.5f, 0.5f));
                title.text = "逃出生天";
                title.color = new Color(1f, 0.84f, 0.42f, 1f);

                Text subtitle = CreateText(victoryPanel.transform, "VictorySubtitle", 40, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1400f, 120f), new Vector2(0.5f, 0.5f));
                subtitle.text = "恭喜通关";
                subtitle.color = new Color(0.92f, 0.96f, 1f, 0.95f);

                Text hint = CreateText(victoryPanel.transform, "VictoryHint", 30, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(1400f, 80f), new Vector2(0.5f, 0.5f));
                hint.text = "通关 —— 可重新开始";
                hint.color = new Color(0.7f, 0.8f, 0.9f, 0.9f);
            }

            victoryPanel.SetActive(true);
        }

        public void SetAimInfo(string text, bool locked)
        {
            if (aimInfoText == null)
                return;

            aimInfoText.text = text;
            aimInfoText.color = locked ? new Color(1f, 0.62f, 0.28f, 1f) : new Color(0.85f, 0.95f, 1f, 0.9f);
        }

        public void SetHint(string text)
        {
            if (hud != null)
                hud.text = text;
        }

        public void SetObjective(string text)
        {
            if (objectiveText != null)
                objectiveText.text = text;
        }

        private void UpdateHud()
        {
            if (water == null)
                return;

            float level = Mathf.Max(0f, water.position.y - floorY);

            if (levelText != null)
                levelText.text = "水位  " + level.ToString("F2") + " m";

            if (tint != null && playerCamera != null)
            {
                float submerged = Mathf.Clamp01((water.position.y - playerCamera.transform.position.y) / 0.8f);
                Color c = tint.color;
                c.a = submerged * 0.38f;
                tint.color = c;
            }
        }

        public void PlayCallDialogue()
        {
            if (inDialogue)
                return;

            StartCoroutine(CallRoutine());
        }

        private System.Collections.IEnumerator CallRoutine()
        {
            inDialogue = true;

            string[] lines =
            {
                "……喂？有人吗！船在进水，我们在船舱里！",
                "收到，救援船 30 分钟后到 —— 你们先想办法上到舱顶！",
                "（电话挂了。水已经漫到脚边……得找到绳索枪，射向舱顶的锚点）"
            };

            for (int i = 0; i < lines.Length; i++)
            {
                if (dialoguePanel != null)
                    dialoguePanel.SetActive(true);
                if (dialogueText != null)
                    dialogueText.text = lines[i];

                SetHint("通话中……");
                yield return new WaitForSeconds(2.8f);
            }

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            // 通完话，绳索枪才出现
            if (ropeGun != null)
            {
                ropeGun.SetActive(true);
                SetHint("救援就在舱顶 —— 拿上绳索枪，射向顶上的橙色锚点");
            }

            inDialogue = false;
        }

        private void StartShake()
        {
            Camera cam = playerCamera;
            if (cam == null)
                return;

            shakeCamera = cam.transform;
            shakeBase = shakeCamera.localPosition;
            shakeTime = 1.8f;
        }

        private void BuildHud()
        {
            GameObject existing = GameObject.Find("Flow_HUD");
            if (existing != null)
            {
                hud = existing.GetComponentInChildren<Text>();
                if (hud != null)
                    return;
            }

            bool vr = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRRayInteractor>() != null;

            GameObject canvasGO = new GameObject("Flow_HUD");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.sortingOrder = 100;

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);

            if (vr && playerCamera != null)
            {
                // VR：世界空间，1.35 米外、略低于视线，跟着头走
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = playerCamera;
                canvasGO.transform.SetParent(playerCamera.transform, false);
                canvasGO.transform.localPosition = new Vector3(0f, -0.04f, 1.35f);
                canvasGO.transform.localRotation = Quaternion.identity;
                canvasGO.transform.localScale = Vector3.one * 0.0006f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            // 水下色偏（全屏蒙版）
            GameObject tintGO = new GameObject("Tint");
            tintGO.transform.SetParent(canvasGO.transform, false);
            tint = tintGO.AddComponent<Image>();
            tint.color = new Color(0.05f, 0.24f, 0.30f, 0f);
            tint.raycastTarget = false;
            RectTransform tintRect = tint.rectTransform;
            tintRect.anchorMin = Vector2.zero;
            tintRect.anchorMax = Vector2.one;
            tintRect.offsetMin = Vector2.zero;
            tintRect.offsetMax = Vector2.zero;

            // 准星
            GameObject crossGO = new GameObject("Crosshair");
            crossGO.transform.SetParent(canvasGO.transform, false);
            Image crosshair = crossGO.AddComponent<Image>();
            crosshair.color = new Color(1f, 1f, 1f, 0.55f);
            crosshair.raycastTarget = false;
            RectTransform crossRect = crosshair.rectTransform;
            crossRect.anchorMin = new Vector2(0.5f, 0.5f);
            crossRect.anchorMax = new Vector2(0.5f, 0.5f);
            crossRect.pivot = new Vector2(0.5f, 0.5f);
            crossRect.sizeDelta = new Vector2(6f, 6f);
            crossRect.anchoredPosition = Vector2.zero;

            if (vr)
                crosshair.gameObject.SetActive(false);

            // 顶部提示
            hud = CreateText(canvasGO.transform, "Hint", 30, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(1500f, 140f), new Vector2(0.5f, 1f));
            hud.text = hintStart;

            // 左上角当前目标
            objectiveText = CreateText(canvasGO.transform, "Objective", 26, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(48f, -40f), new Vector2(900f, 60f), new Vector2(0f, 1f));
            objectiveText.color = new Color(1f, 0.82f, 0.4f, 0.95f);
            objectiveText.text = "目标 1/7：找到破损的手电筒";

            // 左下水位
            levelText = CreateText(canvasGO.transform, "WaterLevel", 28, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(48f, 42f), new Vector2(420f, 60f), new Vector2(0f, 0f));
            levelText.text = "水位  0.00 m";

            // 底部对话框（打电话时用）
            GameObject panelGO = new GameObject("Dialogue");
            panelGO.transform.SetParent(canvasGO.transform, false);
            Image panel = panelGO.AddComponent<Image>();
            panel.color = new Color(0f, 0f, 0f, 0.62f);
            panel.raycastTarget = false;
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 150f);
            panelRect.sizeDelta = new Vector2(1240f, 140f);

            dialogueText = CreateText(panelGO.transform, "DialogueText", 32, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1160f, 120f), new Vector2(0.5f, 0.5f));
            dialogueText.text = "";

            dialoguePanel = panelGO;
            dialoguePanel.SetActive(false);

            // 准星下方的"指向"信息
            aimInfoText = CreateText(canvasGO.transform, "AimInfo", 24, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -46f), new Vector2(900f, 50f), new Vector2(0.5f, 0.5f));
            aimInfoText.color = new Color(0.85f, 0.95f, 1f, 0.9f);
            aimInfoText.text = "";
        }

        private Text CreateText(Transform parent, string name, int size, TextAnchor anchor, Vector2 anchorPoint, Vector2 position, Vector2 sizeDelta, Vector2 pivot)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            Text text = go.AddComponent<Text>();
            text.font = UnderwaterFont.Get();
            text.fontSize = size;
            text.color = new Color(0.86f, 0.95f, 1f, 0.95f);
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchorPoint;
            rect.anchorMax = anchorPoint;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = sizeDelta;

            return text;
        }
    }
}