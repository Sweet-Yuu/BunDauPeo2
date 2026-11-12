using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Core;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class NotifyRuleTests
    {
        [TearDown]
        public void ResetLanguage() => GameLanguage.Current = Language.Vi;

        private static NotifySnapshot Snap(bool incident = false, StaffRole? missing = null, long afk = 0, int stamina = 0, int staminaMax = 0,
            IReadOnlyList<string> starUp = null) => new NotifySnapshot(incident, missing, afk, stamina, staminaMax, starUp);

        [Test]
        public void Rows_AllFive_InEnumOrder()
        {
            var all = Snap(true, StaffRole.Server, 5, 100, 100, new[] { "a" });
            CollectionAssert.AreEqual(
                new[] { NotifyKind.Incident, NotifyKind.StaffMissing, NotifyKind.AfkCoins, NotifyKind.StaminaFull, NotifyKind.StarUp },
                NotifyRule.Rows(all));

            var two = Snap(incident: true, starUp: new[] { "a" });
            CollectionAssert.AreEqual(new[] { NotifyKind.Incident, NotifyKind.StarUp }, NotifyRule.Rows(two));
        }

        [Test]
        public void Rows_Boundaries()
        {
            Assert.IsFalse(NotifyRule.Rows(Snap(afk: 0)).Contains(NotifyKind.AfkCoins), "afk 0");
            Assert.IsTrue(NotifyRule.Rows(Snap(afk: 1)).Contains(NotifyKind.AfkCoins), "afk 1");

            foreach (var (stamina, max, expected) in new[] { (99, 100, false), (100, 100, true), (120, 100, true), (0, 0, false) })
                Assert.AreEqual(expected, NotifyRule.Rows(Snap(stamina: stamina, staminaMax: max)).Contains(NotifyKind.StaminaFull), $"stamina {stamina}/{max}");

            Assert.IsFalse(NotifyRule.Rows(Snap(starUp: null)).Contains(NotifyKind.StarUp), "null ids");
            Assert.IsFalse(NotifyRule.Rows(Snap(starUp: new string[0])).Contains(NotifyKind.StarUp), "empty ids");
            Assert.IsTrue(NotifyRule.Rows(Snap(starUp: new[] { "a" })).Contains(NotifyKind.StarUp), "one id");

            // Vai đầu tiên của enum (giá trị 0) vẫn là "có thiếu vai".
            Assert.IsTrue(NotifyRule.Rows(Snap(missing: StaffRole.Cook)).Contains(NotifyKind.StaffMissing), "Cook = 0 still counts");
            Assert.IsFalse(NotifyRule.Rows(Snap(missing: null)).Contains(NotifyKind.StaffMissing), "no missing role");

            var empty = Snap();
            Assert.AreEqual(0, NotifyRule.Rows(empty).Count);
            Assert.IsFalse(NotifyRule.HasAny(empty));

            Assert.IsTrue(NotifyRule.HasAny(Snap(incident: true)), "incident");
            Assert.IsTrue(NotifyRule.HasAny(Snap(missing: StaffRole.Cook)), "missing");
            Assert.IsTrue(NotifyRule.HasAny(Snap(afk: 1)), "afk");
            Assert.IsTrue(NotifyRule.HasAny(Snap(stamina: 1, staminaMax: 1)), "stamina");
            Assert.IsTrue(NotifyRule.HasAny(Snap(starUp: new[] { "a" })), "star up");
        }

        [Test]
        public void Text_FillsNumbersAndRole_ViAndEn()
        {
            var afk = Snap(afk: 320);
            var cashier = Snap(missing: StaffRole.Cashier);
            var cook = Snap(missing: StaffRole.Cook);
            var two = Snap(starUp: new[] { "a", "b" });
            var three = Snap(starUp: new[] { "a", "b", "c" });

            GameLanguage.Current = Language.Vi;
            Assert.AreEqual("Có 320 Coin AFK chờ thu", NotifyRule.Text(NotifyKind.AfkCoins, afk));
            Assert.AreEqual("Thiếu Thu ngân, quán đứng yên", NotifyRule.Text(NotifyKind.StaffMissing, cashier));
            Assert.AreEqual("Thiếu Đầu bếp, quán đứng yên", NotifyRule.Text(NotifyKind.StaffMissing, cook));
            Assert.AreEqual("2 nhân vật đủ mảnh nâng sao", NotifyRule.Text(NotifyKind.StarUp, two));
            Assert.AreEqual("3 nhân vật đủ mảnh nâng sao", NotifyRule.Text(NotifyKind.StarUp, three));
            Assert.AreEqual("Quán có sự cố đang chờ", NotifyRule.Text(NotifyKind.Incident, Snap(incident: true)));
            Assert.AreEqual("Thể lực đã đầy", NotifyRule.Text(NotifyKind.StaminaFull, Snap(stamina: 10, staminaMax: 10)));

            GameLanguage.Current = Language.En;
            Assert.AreEqual("320 AFK Coin to collect", NotifyRule.Text(NotifyKind.AfkCoins, afk));
            Assert.AreEqual("No Cashier, the shop is stopped", NotifyRule.Text(NotifyKind.StaffMissing, cashier));
            Assert.AreEqual("No Cook, the shop is stopped", NotifyRule.Text(NotifyKind.StaffMissing, cook));
            Assert.AreEqual("Ready to star up: 2", NotifyRule.Text(NotifyKind.StarUp, two));
            Assert.AreEqual("Ready to star up: 1", NotifyRule.Text(NotifyKind.StarUp, Snap(starUp: new[] { "a" })), "no plural to get wrong at 1");
            Assert.AreEqual("An incident needs you", NotifyRule.Text(NotifyKind.Incident, Snap(incident: true)));
            Assert.AreEqual("Stamina is full", NotifyRule.Text(NotifyKind.StaminaFull, Snap(stamina: 10, staminaMax: 10)));
        }

        [Test]
        public void StarUpReady_SkipsMaxedAndShort_KeepsOrder()
        {
            var cost = new Dictionary<string, int> { ["a"] = 10, ["b"] = 0, ["c"] = 10, ["d"] = 10 };
            var fragments = new Dictionary<string, int> { ["a"] = 10, ["b"] = 99, ["c"] = 9, ["d"] = 50 };

            var ready = NotifyRule.StarUpReady(new[] { "a", "b", "c", "d" }, id => cost[id], id => fragments[id]);

            CollectionAssert.AreEqual(new[] { "a", "d" }, ready);

            // Thứ tự theo danh sách đưa vào, không theo bảng tra hay thứ tự chữ cái.
            var reversed = NotifyRule.StarUpReady(new[] { "d", "c", "b", "a" }, id => cost[id], id => fragments[id]);
            CollectionAssert.AreEqual(new[] { "d", "a" }, reversed);
        }
    }
}
