using UnityEngine;

namespace UnderwaterOptimization
{
    public static class UnderwaterFont
    {
        private static Font cached;

        public static Font Get()
        {
            if (cached != null)
                return cached;

            string[] candidates =
            {
                "Microsoft YaHei UI",
                "Microsoft YaHei",
                "SimHei",
                "SimSun",
                "Noto Sans CJK SC",
                "Arial Unicode MS"
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                Font font = Font.CreateDynamicFontFromOSFont(candidates[i], 24);
                if (font != null)
                {
                    cached = font;
                    return cached;
                }
            }

            cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cached == null)
                cached = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return cached;
        }
    }
}