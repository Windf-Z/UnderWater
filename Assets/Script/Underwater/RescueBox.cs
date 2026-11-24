using UnityEngine;

namespace UnderwaterOptimization
{
    public class RescueBox : MonoBehaviour
    {
        public Transform door;
        public PowerGridPanel panel;
        public float useRange = 4.5f;
        public KeyCode useKey = KeyCode.E;

        private UnderwaterFlow flow;
        private Transform playerRoot;
        private bool opened;
        private Vector3 doorClosedPosition;
        private Vector3 doorOpenPosition;

        private void Start()
        {
            flow = FindObjectOfType<UnderwaterFlow>();

            DebugPlayerMotor motor = FindObjectOfType<DebugPlayerMotor>();
            if (motor != null)
                playerRoot = motor.transform;
            else if (Camera.main != null)
                playerRoot = Camera.main.transform;

            if (panel != null)
                panel.gameObject.SetActive(false);

            if (door != null)
            {
                doorClosedPosition = door.position;
                doorOpenPosition = doorClosedPosition + new Vector3(0f, 0f, -2.4f);
            }
        }

        private void Update()
        {
            if (opened || playerRoot == null)
                return;

            float distance = Vector3.Distance(playerRoot.position, transform.position);
            if (distance > useRange)
                return;

            if (!KeyPickup.HasKey)
            {
                if (flow != null)
                    flow.SetHint("呼救箱锁着 —— 先找到钥匙");
                return;
            }

            if (flow != null)
                flow.SetHint("请打开呼救箱");

            if (UnderwaterInput.InteractDown)
                Open();
        }

        private void Open()
        {
            opened = true;

            if (panel != null)
                panel.gameObject.SetActive(true);

            // 门直接收起来，别挡视线也别挡射线
            if (door != null)
                door.gameObject.SetActive(false);

            if (flow != null)
            {
                flow.SetHint("箱子里是接线板 —— 把线拖到同色的端子上");
                flow.SetObjective("目标 5/7：接好三组电线");
            }

        }
    }
}