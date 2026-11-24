using UnityEngine;

namespace UnderwaterOptimization
{
    [RequireComponent(typeof(Light))]
    public class UnderwaterLightFlicker : MonoBehaviour
    {
        [Range(0f, 0.6f)] public float intensityAmount = 0.12f;
        [Range(0f, 3f)] public float intensitySpeed = 0.35f;
        [Range(0f, 10f)] public float swayAngle = 2.5f;
        [Range(0f, 2f)] public float swaySpeed = 0.18f;

        private Light target;
        private float baseIntensity;
        private Quaternion baseRotation;
        private float seed;

        private void Awake()
        {
            target = GetComponent<Light>();
            if (target != null)
                baseIntensity = target.intensity;
            baseRotation = transform.localRotation;
            seed = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (target == null)
                return;

            float n = Mathf.PerlinNoise(seed, Time.time * intensitySpeed);
            target.intensity = baseIntensity * (1f - n * intensityAmount);

            float yaw = (Mathf.PerlinNoise(seed + 13.7f, Time.time * swaySpeed) - 0.5f) * 2f * swayAngle;
            float pitch = (Mathf.PerlinNoise(seed + 31.1f, Time.time * swaySpeed * 0.8f) - 0.5f) * 2f * swayAngle;
            transform.localRotation = baseRotation * Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}