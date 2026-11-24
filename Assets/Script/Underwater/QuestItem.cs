using UnityEngine;

namespace UnderwaterOptimization
{
    public enum QuestItemType
    {
        BrokenFlashlight,
        Battery
    }

    public class QuestItem : MonoBehaviour
    {
        public QuestItemType type;
        public float range = 2.2f;
        public float holdSeconds = 0.5f;

        private FlashlightQuest quest;
        private UnderwaterFlow flow;
        private Transform playerRoot;
        private Camera playerCamera;
        private TextMesh label;
        private bool picked;
        private float holdTimer;

        private string LabelText
        {
            get { return type == QuestItemType.BrokenFlashlight ? "破损的手电筒" : "电池"; }
        }

        private void Start()
        {
            quest = FindObjectOfType<FlashlightQuest>();
            flow = FindObjectOfType<UnderwaterFlow>();

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

            AddGlow();
            BuildLabel();
        }

        private void AddGlow()
        {
            // 一点点自发光，暗到看不见时也能注意到它
            Light glow = gameObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.85f, 0.55f);
            glow.intensity = 3f;
            glow.range = 1.4f;
            glow.shadows = LightShadows.None;
        }

        private void BuildLabel()
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.35f, 0f);

            label = go.AddComponent<TextMesh>();
            label.text = LabelText;
            label.fontSize = 48;
            label.characterSize = 0.045f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.95f, 0.9f, 0.7f, 1f);

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

            float distance = Vector3.Distance(playerRoot.position, transform.position);
            bool aiming = distance <= range;

            if (aiming)
            {
                string itemName = type == QuestItemType.BrokenFlashlight ? "手电筒" : "电池";

                if (flow != null)
                    flow.SetHint("请捡取该物品");

                if (UnderwaterInput.InteractDown)
                {
                    Pick();
                    return;
                }
            }

            if (label != null && label.text != LabelText)
                label.text = LabelText;

            if (label != null && playerCamera != null)
            {
                label.transform.position = transform.position + Vector3.up * 0.35f;
                label.transform.rotation = playerCamera.transform.rotation;
            }
        }

        private void Pick()
        {
            if (picked)
                return;

            picked = true;
            gameObject.SetActive(false);

            if (quest != null)
                quest.Pick(type);

        }
    }
}