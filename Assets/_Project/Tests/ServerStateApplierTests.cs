using System.Collections.Generic;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class ServerStateApplierTests
    {
        // Bảng "server giữ" của spec con 4.3: đúng 8 field của ServerState chảy sang PlayerSave.
        private static readonly string[] ServerHeld =
            { "Gem", "OwnedCharacterIds", "Fragments", "StarUps", "GachaPity", "GachaHistory", "Stamina", "ClearedStages" };

        private static ServerState Sample() => new ServerState
        {
            Gem = 321,
            OwnedCharacterIds = new List<string> { "a", "b" },
            Fragments = new List<FragmentCount> { new() { CharacterId = "a", Count = 4 } },
            StarUps = new List<StarUpEntry> { new() { CharacterId = "a", Count = 2 } },
            GachaPity = new List<PityEntry> { new() { BannerId = "b1", Pulls = 9 } },
            GachaHistory = new List<GachaHistoryEntry> { new() { Utc = 638_000_000_000_000_000L, BannerId = "b1", Count = 1, CharacterIds = new[] { "a" } } },
            Stamina = new StaminaState { Current = 40, AnchorUtcTicks = 777 },
            ClearedStages = new List<string> { "s1" },
        };

        [Test]
        public void Apply_CopiesEveryServerField()
        {
            var state = Sample();
            foreach (var name in ServerHeld)
            {
                var value = typeof(ServerState).GetField(name).GetValue(state);
                Assert.IsNotNull(value, name);
                var empty = value is System.Collections.ICollection c ? c.Count == 0 : value is long l && l == 0;
                Assert.IsFalse(empty, $"sample must fill {name}, or this test proves nothing");
            }

            var save = new PlayerSave();
            save.Balances.Add(new CurrencyAmount(CurrencyType.Gem, 1));
            save.Balances.Add(new CurrencyAmount(CurrencyType.Coin, 50));
            save.OwnedCharacterIds.Add("old");

            ServerStateApplier.Apply(save, state);

            Assert.AreEqual(1, save.Balances.FindAll(b => b.Type == CurrencyType.Gem).Count, "the old Gem entry is replaced, not added to");
            Assert.AreEqual(321, new Wallet(save).Get(CurrencyType.Gem));
            Assert.AreEqual(50, new Wallet(save).Get(CurrencyType.Coin));
            // Đi vòng về state: nếu thiếu field nào thì JSON lệch.
            Assert.AreEqual(JsonUtility.ToJson(state), JsonUtility.ToJson(ServerStateApplier.FromSave(save)));
        }

        // Bản đệm đợt 1 ghi giây Unix; GachaViewText đọc ticks, nên FromSave đổi sang ticks, còn ticks thật giữ nguyên.
        [Test]
        public void FromSave_OldUnixSecondsHistory_BecomesTicks_TicksKept()
        {
            var save = new PlayerSave();
            var when = new System.DateTime(2026, 11, 3, 8, 0, 0, System.DateTimeKind.Utc);
            save.GachaHistory.Add(new GachaHistoryEntry { Utc = new System.DateTimeOffset(when).ToUnixTimeSeconds(), BannerId = "b", Count = 1 });
            save.GachaHistory.Add(new GachaHistoryEntry { Utc = when.Ticks, BannerId = "b", Count = 1 });

            var history = ServerStateApplier.FromSave(save).GachaHistory;

            Assert.AreEqual(when.Ticks, history[0].Utc, "seconds converted");
            Assert.AreEqual(when.Ticks, history[1].Utc, "ticks untouched");
        }

        [Test]
        public void UploadJson_HasNoServerFields()
        {
            var save = new PlayerSave { PlayerName = "Peo" };
            save.Balances.Add(new CurrencyAmount(CurrencyType.Coin, 50));
            save.Balances.Add(new CurrencyAmount(CurrencyType.Material, 7));
            save.BattleTeam.Add("a");
            ServerStateApplier.Apply(save, Sample());

            var upload = JsonUtility.FromJson<PlayerSave>(ServerStateApplier.ToUploadJson(save));

            Assert.AreEqual(0, new Wallet(upload).Get(CurrencyType.Gem));
            Assert.IsFalse(upload.Balances.Exists(b => b.Type == CurrencyType.Gem), "no Gem entry at all");
            Assert.AreEqual(0, upload.OwnedCharacterIds.Count);
            Assert.AreEqual(0, upload.Fragments.Count);
            Assert.AreEqual(0, upload.StarUps.Count);
            Assert.AreEqual(0, upload.GachaPity.Count);
            Assert.AreEqual(0, upload.GachaHistory.Count);
            Assert.AreEqual(0, upload.ClearedStages.Count);
            Assert.AreEqual(0, upload.Stamina.Current);
            Assert.AreEqual(50, new Wallet(upload).Get(CurrencyType.Coin));
            Assert.AreEqual(7, new Wallet(upload).Get(CurrencyType.Material));
            CollectionAssert.AreEqual(new[] { "a" }, upload.BattleTeam);
            Assert.AreEqual("Peo", upload.PlayerName);
            Assert.AreEqual(321, new Wallet(save).Get(CurrencyType.Gem), "the in-memory save is not touched");
        }

        [Test]
        public void Prune_RemovesUnownedFromTeamStaffAvatar()
        {
            var save = new PlayerSave { AvatarCharacterId = "x" };
            save.OwnedCharacterIds.AddRange(new[] { "a", "b" });
            save.BattleTeam.AddRange(new[] { "a", "x", "b" });
            save.Staff.Add(new StaffAssignment { CharacterId = "x", Role = StaffRole.Cook });
            save.Staff.Add(new StaffAssignment { CharacterId = "b", Role = StaffRole.Server });

            Assert.IsTrue(ServerStateApplier.PruneUnowned(save));

            CollectionAssert.AreEqual(new[] { "a", "b" }, save.BattleTeam, "order of the rest is kept");
            Assert.AreEqual(1, save.Staff.Count);
            Assert.AreEqual("b", save.Staff[0].CharacterId);
            Assert.AreEqual("", save.AvatarCharacterId);
            Assert.IsFalse(ServerStateApplier.PruneUnowned(save), "nothing left to remove");

            var owned = new PlayerSave { AvatarCharacterId = "a" };
            owned.OwnedCharacterIds.Add("a");
            Assert.IsFalse(ServerStateApplier.PruneUnowned(owned));
            Assert.AreEqual("a", owned.AvatarCharacterId, "an owned avatar stays");
        }

        [Test]
        public void PruneUnowned_RemovesCharacterLevelsOfUnownedIds()
        {
            var save = new PlayerSave();
            save.OwnedCharacterIds.Add("a");
            save.CharacterLevels.Add(new CharacterLevelEntry { CharacterId = "a", Level = 4 });
            save.CharacterLevels.Add(new CharacterLevelEntry { CharacterId = "x", Level = 9 });

            Assert.IsTrue(ServerStateApplier.PruneUnowned(save));

            Assert.AreEqual(1, save.CharacterLevels.Count);
            Assert.AreEqual(("a", 4), (save.CharacterLevels[0].CharacterId, save.CharacterLevels[0].Level));
            Assert.IsFalse(ServerStateApplier.PruneUnowned(save));
        }

        [Test]
        public void Apply_CopiesMissions()
        {
            var state = Sample();
            state.Missions = new MissionState { DailyDay = 20_000, DailyWins = 3, FreePullDay = 19_999 };
            state.Missions.DailyClaimed.Add("daily_win_5");
            state.Missions.NewbieClaimed.Add("new_name");
            var save = new PlayerSave();

            ServerStateApplier.Apply(save, state);
            state.Missions.DailyClaimed.Clear();

            Assert.AreEqual(20_000, save.Missions.DailyDay);
            Assert.AreEqual(3, save.Missions.DailyWins);
            Assert.AreEqual(19_999, save.Missions.FreePullDay);
            CollectionAssert.AreEqual(new[] { "daily_win_5" }, save.Missions.DailyClaimed, "the save holds a copy, not the state's list");
            CollectionAssert.AreEqual(new[] { "new_name" }, save.Missions.NewbieClaimed);
        }

        [Test]
        public void UploadCopy_StripsMissions()
        {
            var save = new PlayerSave { AfkCollected = true };
            save.Missions.DailyWins = 4;
            save.Missions.NewbieClaimed.Add("new_name");

            var copy = JsonUtility.FromJson<PlayerSave>(ServerStateApplier.ToUploadJson(save));

            Assert.AreEqual(0, copy.Missions.DailyWins);
            Assert.IsEmpty(copy.Missions.NewbieClaimed);
            Assert.IsTrue(copy.AfkCollected, "client-held flags still go up");
            Assert.AreEqual(4, save.Missions.DailyWins, "the live save is untouched");
        }

        // Review Focus 5: state đợt 2 thật không có field Missions.
        [Test]
        public void Phase2State_WithoutMissions_DefaultsAndKeepsGem()
        {
            var json = JsonUtility.ToJson(Sample());
            var stripped = System.Text.RegularExpressions.Regex.Replace(json, @"""Missions"":\{[^{}]*\},?", "");
            Assert.AreNotEqual(json, stripped, "the phase-2 JSON must really lack Missions, or this test proves nothing");
            var state = JsonUtility.FromJson<ServerState>(stripped);
            var save = new PlayerSave();
            save.Missions.DailyWins = 9;

            ServerStateApplier.Apply(save, state);

            Assert.AreEqual(0, save.Missions.DailyWins);
            Assert.IsEmpty(save.Missions.DailyClaimed);
            Assert.AreEqual(321, new Wallet(save).Get(CurrencyType.Gem));
            CollectionAssert.AreEqual(new[] { "a", "b" }, save.OwnedCharacterIds);
        }

        [Test]
        public void UploadJson_KeepsWeather()
        {
            var save = new PlayerSave();
            save.Weather.RainEndsUtcTicks = 5000;
            save.Weather.ContinuousSeconds = 321;

            var upload = JsonUtility.FromJson<PlayerSave>(ServerStateApplier.ToUploadJson(save));

            Assert.AreEqual(5000, upload.Weather.RainEndsUtcTicks);
            Assert.AreEqual(321, upload.Weather.ContinuousSeconds);
        }
}
}
