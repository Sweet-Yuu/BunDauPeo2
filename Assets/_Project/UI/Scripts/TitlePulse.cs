using UnityEngine;

namespace BunDauPeo.UI
{
    public static class TitlePulse
    {
        public const float PeriodSeconds = 1.6f, MinAlpha = 0.35f;

        // MinAlpha..1 theo |cos(π·t/PeriodSeconds)|; t = 0 → 1.
        public static float Alpha(float seconds) =>
            MinAlpha + (1f - MinAlpha) * Mathf.Abs(Mathf.Cos(Mathf.PI * seconds / PeriodSeconds));
    }
}
