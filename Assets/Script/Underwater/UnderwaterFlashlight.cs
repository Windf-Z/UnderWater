using UnityEngine;

namespace UnderwaterOptimization
{
    public class UnderwaterFlashlight : MonoBehaviour
    {
        public KeyCode toggleKey = KeyCode.F;
        public bool startOn = true;
        [Range(0f, 0.3f)] public float flickerAmount = 0.06f;
        [Range(0f, 5f)] public float bobAmount = 1.2f;

        private Light spot;
        private float baseIntensity;
        private float seed;
        private Vector3 baseLocalPosition;

        private void Awake()
        {
            spot = GetComponent<Light>();
            if (spot == null)
                spot = GetComponentInChildren<Light>();

            if (spot != null)
            {
                baseIntensity = spot.intensity;
                spot.enabled = startOn;
            }

            baseLocalPosition = transform.localPosition;
            seed = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (spot == null)
                return;

            if (Input.GetKeyDown(toggleKey))
            {
                spot.enabled = !spot.enabled;
            }

            if (!spot.enabled)
                return;

            float n = Mathf.PerlinNoise(seed, Time.time * 0.9f);
            spot.intensity = baseIntensity * (1f - n * flickerAmount);

            // 走路时的轻微晃动（VR 里也能感知到手在动）
            float bob = Mathf.Sin(Time.time * 5f) * 0.0015f * bobAmount;
            float sway = Mathf.Cos(Time.time * 3.3f) * 0.0015f * bobAmount;
            transform.localPosition = baseLocalPosition + new Vector3(sway, bob, 0f);
        }
    }
}