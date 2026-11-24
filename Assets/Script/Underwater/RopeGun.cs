using UnityEngine;

namespace UnderwaterOptimization
{
    public class RopeGun : MonoBehaviour
    {
        [Header("手感")]
        public float pickupRange = 2.5f;
        public float maxRange = 45f;
        public float zipDuration = 1.1f;
        public float zipArc = 0.7f;
        public float heldScale = 0.32f;

        public KeyCode pickupKey = KeyCode.E;

        private UnderwaterFlow flow;
        private Transform playerRoot;
        private Camera playerCamera;
        private UnityEngine.CharacterController controller;
        private DebugPlayerMotor motor;
        private Rigidbody body;
        private LineRenderer line;
        private TextMesh label;
        private Transform aimDot;
        private Material aimMaterial;
        private LineRenderer laser;
        private Material laserMaterial;

        private bool held;
        private bool zipping;
        private Vector3 zipFrom;
        private Vector3 zipEnd;
        private Vector3 anchorPoint;
        private float zipT;
        private float messageUntil;
        private bool wasInRange;

        private void Start()
        {
            flow = FindObjectOfType<UnderwaterFlow>();
            body = GetComponent<Rigidbody>();

            motor = FindObjectOfType<DebugPlayerMotor>();
            if (motor != null)
            {
                playerRoot = motor.transform;
                playerCamera = motor.cameraTransform != null ? motor.cameraTransform.GetComponent<Camera>() : null;
                controller = motor.GetComponent<UnityEngine.CharacterController>();
            }
            else if (Camera.main != null)
            {
                playerRoot = Camera.main.transform;
                playerCamera = Camera.main;
            }

            if (playerCamera == null)
                playerCamera = Camera.main;

            BuildLine();
            BuildLabel();
            BuildAimDot();
            BuildLaser();
        }

        private void BuildLabel()
        {
            GameObject go = new GameObject("RopeGunLabel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.55f, 0f);

            label = go.AddComponent<TextMesh>();
            label.text = "绳索枪";
            label.fontSize = 48;
            label.characterSize = 0.055f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.95f, 0.9f, 0.75f, 1f);

            Font font = UnderwaterFont.Get();
            if (font != null)
            {
                label.font = font;
                MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null && font.material != null)
                    renderer.sharedMaterial = font.material;
            }
        }

        private void BuildLine()
        {
            line = gameObject.GetComponent<LineRenderer>();
            if (line == null)
                line = gameObject.AddComponent<LineRenderer>();

            line.positionCount = 2;
            line.widthMultiplier = 0.045f;
            line.useWorldSpace = true;
            line.enabled = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                Material mat = new Material(shader);
                mat.SetColor("_BaseColor", new Color(0.8f, 0.85f, 0.8f, 1f));
                line.material = mat;
            }
        }

        private void Update()
        {
            if (playerRoot == null || playerCamera == null)
                return;

            if (zipping)
            {
                UpdateZip();
                return;
            }

            if (!held)
            {
                UpdatePickup();
                UpdateLabel();
                return;
            }

            HoldInHand();
            UpdateAim();

            if (UnderwaterInput.FireDown)
                Fire();
        }

        private void UpdateAim()
        {
            if (aimDot == null)
                return;

            Ray ray = new Ray(UnderwaterInput.RayOrigin, UnderwaterInput.RayDirection);
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit, maxRange, ~0, QueryTriggerInteraction.Ignore))
            {
                aimDot.gameObject.SetActive(false);
                if (laser != null)
                    laser.enabled = false;
                if (flow != null)
                    flow.SetAimInfo("", false);
                return;
            }

            aimDot.gameObject.SetActive(true);
            aimDot.position = hit.point + hit.normal * 0.03f;

            // 瞄着接线端子时把界面让给接线逻辑
            if (hit.collider.GetComponentInParent<PowerTerminal>() != null)
            {
                aimDot.gameObject.SetActive(false);
                if (laser != null)
                    laser.enabled = false;
                return;
            }

            float distance = Mathf.Max(1f, hit.distance);
            float scale = Mathf.Clamp(distance * 0.02f, 0.18f, 0.9f);
            aimDot.localScale = Vector3.one * scale;

            bool onAnchor = hit.collider.GetComponentInParent<RopeAnchor>() != null;
            Color color = onAnchor ? new Color(3f, 1.2f, 0.25f) : new Color(1.2f, 1.35f, 1.5f, 1f);

            if (aimMaterial != null)
                aimMaterial.SetColor("_BaseColor", color);

            if (laser != null)
            {
                laser.enabled = true;
                laser.SetPosition(0, transform.position + transform.forward * 0.28f);
                laser.SetPosition(1, hit.point);
                if (laserMaterial != null)
                    laserMaterial.SetColor("_BaseColor", color);
            }

            if (flow != null && Time.time > messageUntil)
                flow.SetHint(onAnchor ? "锁定锚点 —— 可以发射" : "对准橙色锚点才能钩住");

            if (flow != null)
                flow.SetAimInfo(onAnchor
                    ? "✔ 锚点 · " + hit.distance.ToString("F1") + " m"
                    : "指向 " + hit.distance.ToString("F1") + " m", onAnchor);
        }

        private void BuildAimDot()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "AimDot";
            go.transform.SetParent(null);

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            Renderer renderer = go.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                aimMaterial = new Material(shader);
                aimMaterial.SetColor("_BaseColor", new Color(3f, 1.2f, 0.25f));
                if (renderer != null)
                    renderer.sharedMaterial = aimMaterial;
            }

            aimDot = go.transform;
            aimDot.gameObject.SetActive(false);
        }

        private void BuildLaser()
        {
            GameObject go = new GameObject("AimLaser");
            laser = go.AddComponent<LineRenderer>();
            laser.positionCount = 2;
            laser.widthMultiplier = 0.03f;
            laser.useWorldSpace = true;
            laser.enabled = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                laserMaterial = new Material(shader);
                laserMaterial.SetColor("_BaseColor", new Color(1.2f, 1.35f, 1.5f, 1f));
                laser.material = laserMaterial;
            }
        }

        private void UpdatePickup()
        {
            float distance = Vector3.Distance(playerRoot.position, transform.position);
            bool inRange = distance <= pickupRange;

            if (inRange && flow != null)
                flow.SetHint("请拾取绳索枪");

            if (inRange && UnderwaterInput.InteractDown)
            {
                held = true;
                if (body != null)
                {
                    body.isKinematic = true;
                    body.detectCollisions = false;
                }

                if (label != null)
                    label.gameObject.SetActive(false);

                // 手里的枪不能再挡住射线
                Collider[] colliders = GetComponentsInChildren<Collider>();
                for (int i = 0; i < colliders.Length; i++)
                    colliders[i].enabled = false;

                transform.localScale = Vector3.one * heldScale;

                if (flow != null)
                    flow.SetHint("朝高处的橙色锚点发射绳索");
                flow.SetObjective("目标 7/7：瞄准舱顶橙色锚点并发射");
            }

            wasInRange = inRange;
        }

        private void UpdateLabel()
        {
            if (label == null || held)
                return;

            label.transform.position = transform.position + Vector3.up * 0.55f;

            if (playerCamera == null)
                return;

            // 始终朝向玩家，别背面朝人
            label.transform.rotation = playerCamera.transform.rotation;
        }

        private void HoldInHand()
        {
            Transform cam = playerCamera.transform;

            // 手里的小枪：缩小、放到右下角，别挡住准星和视野
            transform.localScale = Vector3.one * heldScale;
            transform.position = cam.position + cam.forward * 0.55f + cam.right * 0.34f - cam.up * 0.32f;
            transform.rotation = cam.rotation * Quaternion.Euler(0f, -18f, 0f);
        }

        private void Fire()
        {
            Ray ray = new Ray(UnderwaterInput.RayOrigin, UnderwaterInput.RayDirection);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxRange, ~0, QueryTriggerInteraction.Ignore))
            {
                // 瞄着接线端子时不开枪，避免和接线抢输入
                if (hit.collider.GetComponentInParent<PowerTerminal>() != null)
                    return;

                RopeAnchor anchor = hit.collider.GetComponentInParent<RopeAnchor>();
                if (anchor != null)
                {
                    StartZip(anchor, hit.point);
                    return;
                }
            }

            if (flow != null)
                flow.SetHint("没钩中 —— 瞄准高处的橙色锚点");
            messageUntil = Time.time + 1.5f;
        }

        private void StartZip(RopeAnchor anchor, Vector3 hitPoint)
        {
            zipping = true;
            zipT = 0f;
            anchorPoint = hitPoint;
            zipFrom = playerRoot.position;
            zipEnd = anchor.landingPoint != null ? anchor.landingPoint.position : hitPoint;

            if (controller != null)
                controller.enabled = false;
            if (motor != null)
                motor.enabled = false;

            if (line != null)
                line.enabled = true;

            if (flow != null)
                flow.SetHint("绳索命中！");
        }

        private void UpdateZip()
        {
            zipT += Time.deltaTime / Mathf.Max(0.05f, zipDuration);
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(zipT));

            Vector3 position = Vector3.Lerp(zipFrom, zipEnd, t);
            position.y += Mathf.Sin(t * Mathf.PI) * zipArc;
            playerRoot.position = position;

            if (line != null)
            {
                line.SetPosition(0, transform.position);
                line.SetPosition(1, anchorPoint);
            }

            if (zipT < 1f)
                return;

            zipping = false;
            if (line != null)
                line.enabled = false;

            if (controller != null)
                controller.enabled = true;
            if (motor != null)
                motor.enabled = true;

            if (flow != null)
                flow.SubmitEscape();
        }
    }
}