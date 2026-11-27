using System.Collections.Generic;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class GiftFormatTests
    {
        [TearDown]
        public void TearDown() => GameLanguage.Current = Language.Vi;

        [Test]
        public void Time_IsUtcPlus7_FromTicks()
        {
            var end = new System.DateTime(2027, 1, 13, 17, 0, 0, System.DateTimeKind.Utc).Ticks; // 00:00 14/01 giờ Việt Nam
            Assert.AreEqual("14/01 00:00", GiftFormat.Short(end));
            Assert.AreEqual("14/01/2027 00:00", GiftFormat.Long(end));
        }

        [Test]
        public void Time_NearYear9999_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => GiftFormat.Short(System.DateTime.MaxValue.Ticks));
            Assert.DoesNotThrow(() => GiftFormat.Long(long.MaxValue));
            Assert.DoesNotThrow(() => GiftFormat.Long(-1));
        }

        [Test]
        public void Lines_MergeByType_InFixedOrder_SkipUnknown()
        {
            var lines = GiftFormat.Lines(new List<GiftReward>
            {
                new() { Type = "Coin", Amount = 5000 }, new() { Type = "Gem", Amount = 300 },
                new() { Type = "Coin", Amount = 1 }, new() { Type = "Star", Amount = 9 }, null,
            });

            CollectionAssert.AreEqual(new[] { "Gem 300", "Coin 5.001" }, lines);
        }

        [Test]
        public void Amount_ThousandsSeparator_FollowsLanguage()
        {
            GameLanguage.Current = Language.Vi;
            Assert.AreEqual("1.000.000", GiftFormat.Amount(1_000_000));
            GameLanguage.Current = Language.En;
            Assert.AreEqual("1,000,000", GiftFormat.Amount(1_000_000));
        }
    }
}
