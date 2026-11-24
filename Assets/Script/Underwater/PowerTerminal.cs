using UnityEngine;

namespace UnderwaterOptimization
{
    public class PowerTerminal : MonoBehaviour
    {
        public PowerGridPanel panel;
        public int colorIndex;
        public bool wired;
        public bool dragging;

        private Renderer cachedRenderer;
        private Material instance;

        public static Color ColorOf(int index)
        {
            switch (index)
            {
                case 0: return new Color(0.90f, 0.10f, 0.07f);   // 红
                case 1: return new Color(0.10f, 0.78f, 0.26f);   // 绿
                default: return new Color(0.08f, 0.32f, 0.90f);  // 蓝
            }
        }

        public static string NameOf(int index)
        {
            switch (index)
            {
                case 0: return "红色";
                case 1: return "绿色";
                default: return "蓝色";
            }
        }

        private void Awake()
        {
            cachedRenderer = GetComponent<Renderer>();
            if (cachedRenderer != null)
                instance = cachedRenderer.material;

            ApplyVisual();
        }

        private void Update()
        {
            if (!dragging || instance == null)
                return;

            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 9f);
            instance.SetColor("_BaseColor", Color.Lerp(Color.white, ColorOf(colorIndex), pulse));
        }

        public void SetWired(bool value)
        {
            wired = value;
            dragging = false;
            ApplyVisual();
        }

        public void SetDragging(bool value)
        {
            dragging = value;
            if (!value)
                ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (instance == null)
                return;

            Color color = ColorOf(colorIndex);
            instance.SetColor("_BaseColor", wired ? color * 1.25f : color * 0.75f);
        }
    }
}