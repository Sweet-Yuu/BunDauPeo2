using System;

namespace BunDauPeo.Combat
{
    // Công thức theo spec Combat 4.2: không chí mạng, không né, không ngẫu nhiên.
    public static class DamageCalculator
    {
        public static int Damage(int attack, float power, int defense) =>
            Math.Max(1, RoundHalfUp(attack * NormalizePower(power) * 100.0 / (100 + defense)));

        // Số máu thực sự hồi, nên không vượt phần còn thiếu và không âm.
        public static int Heal(int attack, float power, int currentHp, int maxHp) =>
            Math.Max(0, Math.Min(RoundHalfUp(attack * NormalizePower(power)), maxHp - currentHp));

        // 1.3f thật ra là 1,2999999523: không chuẩn hoá thì 5 x 1,3 = 6,4999998 ra 6 thay vì 7.
        private static double NormalizePower(float power) => Math.Round((double)power, 4);

        // Math.Round mặc định làm tròn về chẵn (2,5 ra 2), không phải nửa lên.
        private static int RoundHalfUp(double value) => (int)Math.Floor(value + 0.5);
    }
}
