using System;

namespace BunDauPeo.UI
{
    // Tư thế đi của người trong quán (spec 5.6): mỗi bước nhún lên rồi về, nghiêng một bên; bước sau nghiêng bên kia.
    public static class WalkPose
    {
        public const float StepsPerSecond = 2f;
        public const float LiftPx = 6f;
        public const float SwayDegrees = 6f;

        public static readonly (float Lift, float Angle) Idle = (0f, 0f);

        public static (float Lift, float Angle) At(float seconds)
        {
            var wave = (float)Math.Sin(Math.PI * seconds * StepsPerSecond);
            return (LiftPx * Math.Abs(wave), SwayDegrees * wave);
        }
    }
}
