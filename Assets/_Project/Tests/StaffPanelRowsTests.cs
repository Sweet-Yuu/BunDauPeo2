using System.Globalization;
using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    // Cấu hình để chỉ số quyết định thu nhập: khách đến mỗi giây, 50 bàn, người phục vụ và thu ngân chỉ số 100, nên đầu bếp là nút thắt.
    // Một đầu bếp chỉ số s cho (100 + s) / 500 khách/giây, mỗi khách 20 Coin, tức (100 + s) / 25 Coin/giây.
    public class StaffPanelRowsTests
    {
        private sealed class Fixture
        {
            public RestaurantConfig Config;
            public PlayerSave Save;
            public Roster Roster;
            public CharacterCatalog Catalog;

            public RestaurantProfile Profile => RestaurantProfile.Build(Config, Save, Roster);

            public CandidateRow Row(StaffRole role, string id, string replacingId = null) =>
                StaffPanelRows.Candidates(role, Config, Profile, Roster, replacingId).Single(r => r.Character.Id == id);
        }

        private static Fixture Make(bool ghost = false)
        {
            var config = RestaurantTestData.Config();
            TestData.Edit(config, so =>
            {
                so.FindProperty("_baseArrivalInterval").floatValue = 1f;
                so.FindProperty("_baseTables").intValue = 50;
            });

            var cookA = RestaurantTestData.Worker("cookA", 30, 0, 0);
            var cookB = RestaurantTestData.Worker("cookB", 50, 0, 0);
            var cookC = RestaurantTestData.Worker("cookC", 10, 0, 0);
            var server = RestaurantTestData.Worker("server", 5, 100, 0);
            var cashier = RestaurantTestData.Worker("cashier", 40, 0, 100);
            var spare = RestaurantTestData.Worker("spare", 20, 0, 0);
            var notOwned = RestaurantTestData.Worker("notOwned", 99, 0, 0);
            var catalog = TestData.Catalog(cookA, cookB, cookC, server, cashier, spare, notOwned);

            var save = new PlayerSave();
            var roster = new Roster(save, catalog);
            foreach (var id in new[] { "cookA", "cookB", "cookC", "server", "cashier", "spare" }) roster.Add(id);
            roster.AssignStaff("cookA", StaffRole.Cook);
            roster.AssignStaff("server", StaffRole.Server);
            roster.AssignStaff("cashier", StaffRole.Cashier);
            return new Fixture { Config = config, Save = save, Roster = roster, Catalog = catalog };
        }

        [Test]
        public void Candidates_SortedByRoleStatDescending()
        {
            var f = Make();
            var rows = StaffPanelRows.Candidates(StaffRole.Cook, f.Config, f.Profile, f.Roster, null);

            CollectionAssert.AreEqual(new[] { 50, 40, 30, 20, 10, 5 }, rows.Select(r => r.Stat).ToArray());
            Assert.AreEqual("cookB", rows[0].Character.Id);
        }

        [Test]
        public void Candidates_IncludeOnlyOwned()
        {
            var f = Make();
            var rows = StaffPanelRows.Candidates(StaffRole.Cook, f.Config, f.Profile, f.Roster, null);

            Assert.AreEqual(6, rows.Count);
            Assert.IsFalse(rows.Any(r => r.Character.Id == "notOwned"));
        }

        [Test]
        public void Candidates_BattleTeamMember_NeedsConfirm()
        {
            var f = Make();
            Assert.IsTrue(f.Roster.AddToTeam("cookB"));

            var rows = StaffPanelRows.Candidates(StaffRole.Cook, f.Config, f.Profile, f.Roster, null);

            Assert.IsTrue(rows.Single(r => r.Character.Id == "cookB").NeedsConfirm);
            Assert.AreEqual(1, rows.Count(r => r.NeedsConfirm));
        }

        [Test]
        public void Candidates_IncomeDelta_PositiveWhenBetterStat()
        {
            var f = Make();

            // Ô trống: thêm đầu bếp thứ hai nâng sức nấu từ 0,26 lên 0,26 + (100 + s) / 500, bị chặn bởi người phục vụ ở 0,5 khách/giây.
            Assert.AreEqual(4.8, f.Row(StaffRole.Cook, "cookB").IncomeDelta, 1e-6);
            // Người yếu hơn (0,22) cộng lên 0,48 chưa chạm trần 0,5 nên delta nhỏ hơn: không phải cùng một số cho mọi người.
            Assert.AreEqual(4.4, f.Row(StaffRole.Cook, "cookC").IncomeDelta, 1e-6);
        }

        [Test]
        public void Candidates_IncomeDelta_CountsLeavingOtherRole()
        {
            var f = Make();
            var current = IncomeCalculator.Estimate(f.Config, f.Profile).CoinsPerSecond;
            Assert.AreEqual(5.2, current, 1e-6);

            // Thu ngân duy nhất chuyển sang nấu thì vai thu ngân trống, quán không kiếm được gì.
            var row = f.Row(StaffRole.Cook, "cashier");
            Assert.AreEqual(-current, row.IncomeDelta, 1e-6);
            Assert.AreEqual(StaffRole.Cashier, row.CurrentRole);
        }

        [Test]
        public void Candidates_ReplacingId_ComparesAgainstReplacedCharacter()
        {
            var f = Make();

            Assert.AreEqual(0.8, f.Row(StaffRole.Cook, "cookB", replacingId: "cookA").IncomeDelta, 1e-6);
            Assert.AreEqual(-0.8, f.Row(StaffRole.Cook, "cookC", replacingId: "cookA").IncomeDelta, 1e-6);
        }

        [Test]
        public void Candidates_CurrentRole_ReportedForCharacterAlreadyStaffed()
        {
            var f = Make();

            Assert.AreEqual(StaffRole.Cook, f.Row(StaffRole.Server, "cookA").CurrentRole);
            Assert.AreEqual(StaffRole.Server, f.Row(StaffRole.Cook, "server").CurrentRole);
            Assert.IsNull(f.Row(StaffRole.Cook, "spare").CurrentRole);
        }

        [Test]
        public void RowLabel_ShowsNameRarityStatAndSignedDelta_WithCommaDecimal()
        {
            var f = Make();
            TestData.Edit(f.Catalog.Items.Single(c => c.Id == "cookB"), so =>
                so.FindProperty("_displayName").stringValue = "Mắm Tôm");
            f.Roster.AddToTeam("cookB");
            var saved = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                var better = StaffPanelRows.RowLabel(f.Row(StaffRole.Cook, "cookB", replacingId: "cookA"), StaffRole.Cook);
                StringAssert.Contains("Mắm Tôm", better);
                StringAssert.Contains("[R]", better);
                StringAssert.Contains("Đầu bếp 50", better);
                StringAssert.Contains("+0,8 Coin/giây", better);
                StringAssert.Contains("Đội chiến đấu", better);

                var worse = StaffPanelRows.RowLabel(f.Row(StaffRole.Cook, "cookC", replacingId: "cookA"), StaffRole.Cook);
                StringAssert.Contains("cookC", worse, "name falls back to the Id when the display name is blank");
                StringAssert.Contains("-0,8 Coin/giây", worse);
                StringAssert.DoesNotContain("Đội chiến đấu", worse);

                var elsewhere = StaffPanelRows.RowLabel(f.Row(StaffRole.Cook, "cashier"), StaffRole.Cook);
                StringAssert.Contains("đang là Thu ngân", elsewhere);
            }
            finally
            {
                CultureInfo.CurrentCulture = saved;
            }
        }

        [Test]
        public void LockedHint_NamesTheUpgradeThatOpensTheSlot()
        {
            var cookSlots = RestaurantTestData.Upgrade("up_cook_slots", UpgradeEffect.CookSlots, RestaurantTestData.Level(2, 1, 1));
            TestData.Edit(cookSlots, so => so.FindProperty("_displayName").stringValue = "Ô đầu bếp");
            var config = RestaurantTestData.Config(RestaurantTestData.UpgradeCatalog(cookSlots));

            StringAssert.Contains("Ô đầu bếp", StaffPanelRows.LockedHint(config, StaffRole.Cook));
            // Vai không có nâng cấp ô thì vẫn có chữ báo khoá, không rỗng.
            Assert.IsNotEmpty(StaffPanelRows.LockedHint(config, StaffRole.Server));
        }
    }
}
