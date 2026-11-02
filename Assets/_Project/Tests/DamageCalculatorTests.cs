using BunDauPeo.Combat;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class DamageCalculatorTests
    {
        [Test]
        public void Damage_MatchesHandComputedValues()
        {
            var rows = new (int Attack, float Power, int Defense, int Expected)[]
            {
                (100, 1.0f, 0, 100), (80, 1.0f, 30, 62), (60, 1.0f, 40, 43), (80, 1.8f, 30, 111), (110, 1.8f, 40, 141),
            };
            foreach (var r in rows)
                Assert.AreEqual(r.Expected, DamageCalculator.Damage(r.Attack, r.Power, r.Defense), $"({r.Attack}, {r.Power}, {r.Defense})");
        }

        // 1.3f thật ra là 1,2999999523: không chuẩn hoá thì 5 × 1,3 ra 6; làm tròn về chẵn thì 4,5 ra 4 và 2,5 ra 2.
        [Test]
        public void Damage_RoundsHalfUp()
        {
            Assert.AreEqual(5, DamageCalculator.Damage(3, 1.5f, 0), "3 x 1.5 = 4.5");
            Assert.AreEqual(3, DamageCalculator.Damage(5, 0.5f, 0), "5 x 0.5 = 2.5");
            Assert.AreEqual(7, DamageCalculator.Damage(5, 1.3f, 0), "5 x 1.3f = 6.5");
        }

        [Test]
        public void Damage_MinimumIsOne() => Assert.AreEqual(1, DamageCalculator.Damage(1, 0.1f, 1000));

        [Test]
        public void Heal_RoundsHalfUp()
        {
            Assert.AreEqual(3, DamageCalculator.Heal(5, 0.5f, 0, 100), "5 x 0.5 = 2.5");
            Assert.AreEqual(72, DamageCalculator.Heal(60, 1.2f, 0, 1000), "60 x 1.2f = 72");
        }

        [Test]
        public void Heal_CappedAtMissingHp() => Assert.AreEqual(20, DamageCalculator.Heal(60, 1.2f, 980, 1000));

        [Test]
        public void Heal_FullHp_IsZero() => Assert.AreEqual(0, DamageCalculator.Heal(60, 1.2f, 1000, 1000));
    }
}
