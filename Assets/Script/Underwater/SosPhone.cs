using UnityEngine;

namespace UnderwaterOptimization
{
    public class SosPhone : MonoBehaviour
    {
        public UnderwaterFlow flow;
        public float useRange = 4f;
        public KeyCode useKey = KeyCode.E;

        private bool used;
        public PowerGridPanel powerGrid;
        private TextMesh label;
        private Transform playerRoot;
        private Camera playerCamera;

        private void Start()
        {
            if (flow == null)
                flow = FindObjectOfType<UnderwaterFlow>();

            if (powerGrid == null)
            {
                // 箱子没打开时面板是隐藏的，要把 inactive 的也找出来
                PowerGridPanel[] panels = FindObjectsOfType<PowerGridPanel>(true);
                if (panels.Length > 0)
                    powerGrid = panels[0];
            }

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
            GameObject go = new GameObject("PhoneLabel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.6f, 0f);

            label = go.AddComponent<TextMesh>();
            label.text = "求救电话";
            label.fontSize = 48;
            label.characterSize = 0.05f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(1f, 0.85f, 0.6f, 1f);

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
            if (used || playerRoot == null)
                return;

            float distance = Vector3.Distance(playerRoot.position, transform.position);
            bool near = distance <= useRange;
            // 只有电网真的通了才能打电话（找不到面板也不算通）
            bool powered = powerGrid != null && powerGrid.IsPowered;

            if (near)
            {
                if (!powered)
                {
                    if (flow != null)
                        flow.SetHint("电话没电 —— 先去左边墙上把电网接好");
                }
                else
                {
                    if (flow != null)
                        flow.SetHint("请拨打求救电话");

                    if (UnderwaterInput.InteractDown)
                        Call();
                }
            }

            if (label != null)
        {
            string want = "求救电话";
            if (label.text != want) label.text = want;
        }

        if (label != null && playerCamera != null)
            {
                label.transform.position = transform.position + Vector3.up * 0.6f;
                label.transform.rotation = playerCamera.transform.rotation;
            }
        }

        private void Call()
        {
            used = true;

            if (label != null)
                label.gameObject.SetActive(false);

            if (flow != null)
                flow.PlayCallDialogue();
                flow.SetObjective("目标 7/7：水在涨 —— 拿绳索枪射向舱顶锚点");

        }
    }
}