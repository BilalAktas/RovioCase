using UnityEngine;

namespace Core
{
    public static class Helpers
    {
        public static Color AdjustBrightness(Color color, float delta)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            v = Mathf.Clamp01(v + delta);
            var result = Color.HSVToRGB(h, s, v);
            result.a = color.a;
            return result;
        }
    }
}