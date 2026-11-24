using UnityEngine;

namespace UnderwaterOptimization
{
    public class KeyPickup : MonoBehaviour
    {
        public static bool HasKey;

        public float pickupRange = 2.2f;
        public float holdSeconds = 0.5f;

        private UnderwaterFlow flow;
        private FlashlightQuest quest;
        private Transform playerRoot;
        private Camera playerCamera;
        private TextMesh label;
        private bool picked;
        private float holdTimer;

        private void Start()
        {
            flow = FindObjectOfType<UnderwaterFlow>();
            quest = FindObjectOfType<FlashlightQuest>();

            DebugPlayerMotor motor = FindObjectOfType<DebugPlayerMotor>();
            if (motor != null)
            {
                playerRoot = motor.transform;
                playerCamera = motor.cameraTransform != null ? motor.cameraTransform.GetComponent<Camera>() : null;
            }
            else if (Camera.main != null)
            {
                playerRoot = Camera.main.transform;
                playerCamera = Camera.main;
            }

            BuildLabel();
        }

        private void BuildLabel()
        {
            GameObject go = new GameObject("KeyLabel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.35f, 0f);

            label = go.AddComponent<TextMesh>();
            label.text = "钥匙";
            label.fontSize = 48;
            label.characterSize = 0.045f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(1f, 0.9f, 0.55f, 1f);

            Font font = UnderwaterFont.Get();
            if (font != null)
            {
                label.font = font;
                MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null && font.material != null)
                    renderer.sharedMaterial = font.material;
            }
        }

        private void Update()
        {
            if (picked || playerRoot == null)
                return;

            if (quest != null && !quest.HasLight)
            {
                if (Vector3.Distance(playerRoot.position, transform.position) <= pickupRange && flow != null)
                    flow.SetHint("太黑了，什么都看不清 —— 得先弄到光");
                return;
            }

            float keyDistance = Vector3.Distance(playerRoot.position, transform.position);

            bool aiming = keyDistance <= pickupRange;
            if (aiming)
            {
                if (flow != null)
                    flow.SetHint("请捡取该物品");

                if (UnderwaterInput.InteractDown)
                {
                    Pick();
                    return;
                }
            }

            if (label != null && playerCamera != null)
            {
                label.transform.position = transform.position + Vector3.up * 0.35f;
                label.transform.rotation = playerCamera.transform.rotation;
            }
        }

        private void Pick()
        {
            picked = true;
            HasKey = true;
            gameObject.SetActive(false);

            if (flow != null)
                flow.SetHint("拿到钥匙了 —— 去开墙上的呼救箱");
                flow.SetObjective("目标 4/7：用钥匙打开墙上的呼救箱");

        }
    }
}