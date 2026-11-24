using System;

namespace BunDauPeo.UI
{
    public static class PlayerLevelBarRule
    {
        // Phần đã đầy của thanh EXP, 0..1; cấp tối đa thì đầy.
        public static float Fill(long exp, long toNext, bool isMax)
        {
            if (isMax) return 1f;
            if (toNext <= 0) return 0f;
            return Math.Clamp((float)exp / toNext, 0f, 1f);
        }
    }
}
