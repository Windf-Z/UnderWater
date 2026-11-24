using UnityEngine;

namespace UnderwaterOptimization
{
    public class RopeAnchor : MonoBehaviour
    {
        public Transform landingPoint;
        public float spinSpeed = 40f;
        public float pulseAmount = 0.06f;
        public float pulseSpeed = 2.5f;

        private void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);

            if (pulseAmount > 0f)
            {
                float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                transform.localScale = Vector3.one * scale;
            }
        }
    }
}