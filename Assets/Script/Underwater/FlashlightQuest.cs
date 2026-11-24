using UnityEngine;

namespace UnderwaterOptimization
{
    public class FlashlightQuest : MonoBehaviour
    {
        public UnderwaterFlow flow;
        public GameObject keyObject;

        private Light playerLight;
        private UnderwaterFlashlight toggle;
        private bool hasBroken;
        private bool hasBattery;

        public bool HasLight
        {
            get { return playerLight != null && playerLight.enabled; }
        }

        public GameObject batteryObject;

        private void Start()
        {
            if (keyObject != null)
                keyObject.SetActive(false);

            // 电池一开始不存在：捡到手电筒之后才出现
            if (batteryObject == null)
            {
                QuestItem[] items = FindObjectsOfType<QuestItem>(true);
                foreach (QuestItem item in items)
                {
                    if (item.type == QuestItemType.Battery)
                    {
                        batteryObject = item.gameObject;
                        break;
                    }
                }
            }

            if (batteryObject != null)
                batteryObject.SetActive(false);

            toggle = Object.FindObjectOfType<UnderwaterFlashlight>();
            if (toggle != null)
                toggle.enabled = false;

            playerLight = FindPlayerLight();
            if (playerLight != null)
                playerLight.enabled = false;
        }

        private Light FindPlayerLight()
        {
            GameObject existing = GameObject.Find("Underwater_Flashlight");
            if (existing != null)
            {
                Light light = existing.GetComponentInChildren<Light>();
                if (light != null)
                    return light;
            }

            // VR 里没有现成的手电，在相机上新建一盏
            Camera cam = Camera.main;
            if (cam == null)
                return null;

            GameObject go = new GameObject("QuestFlashlight");
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0.15f, -0.1f, 0.2f);

            Light created = go.AddComponent<Light>();
            created.type = LightType.Spot;
            created.color = new Color(1f, 0.94f, 0.82f);
            created.intensity = 260f;
            created.range = 30f;
            created.spotAngle = 62f;
            created.shadows = LightShadows.Soft;
            return created;
        }

        public void Pick(QuestItemType type)
        {
            if (type == QuestItemType.BrokenFlashlight)
                hasBroken = true;
            else
                hasBattery = true;

            if (hasBroken && hasBattery)
            {
                Assemble();
                return;
            }

            // 捡完手电筒，电池才出现
            if (type == QuestItemType.BrokenFlashlight && batteryObject != null)
                batteryObject.SetActive(true);

            if (flow == null)
                return;

            flow.SetHint(type == QuestItemType.BrokenFlashlight
                ? "捡到破损的手电筒 —— 还缺电池"
                : "捡到电池 —— 回去装进手电筒");

            if (flow != null && type == QuestItemType.BrokenFlashlight)
                flow.SetObjective("目标 2/7：找到电池");
        }

        private void Assemble()
        {
            if (toggle != null)
                toggle.enabled = true;

            if (playerLight != null)
            {
                playerLight.enabled = true;
                // 别刺眼：亮度压低、光锥放宽
                playerLight.intensity = 300f;
                playerLight.range = 26f;
                playerLight.spotAngle = 62f;
            }

            if (keyObject != null)
                keyObject.SetActive(true);

            if (flow != null)
                flow.SetHint("手电筒亮了 —— 有光照着，角落里的钥匙才看得出来");
                flow.SetObjective("目标 3/7：有光了 —— 找到钥匙");
                flow.OnFlashlightAssembled();

        }
    }
}