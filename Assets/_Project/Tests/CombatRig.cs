using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using UnityEditor;
using UnityEngine;

namespace BunDauPeo.Tests
{
    // Ảnh chụp bộ nhớ ngay lúc persist chạy: để test thấy "đã đổi xong chưa" và "đã có trận mới chưa".
    internal readonly struct PersistSnapshot
    {
        public PersistSnapshot(long material, long gem, string cleared, int staminaCurrent, long staminaAnchor, bool hasBattle, bool? watchedClaimed)
        {
            Material = material;
            Gem = gem;
            Cleared = cleared;
            StaminaCurrent = staminaCurrent;
            StaminaAnchor = staminaAnchor;
            HasBattle = hasBattle;
            WatchedClaimed = watchedClaimed;
        }

        public long Material { get; }
        public long Gem { get; }
        public string Cleared { get; }
        public int StaminaCurrent { get; }
        public long StaminaAnchor { get; }
        public bool HasBattle { get; }
        public bool? WatchedClaimed { get; }
    }

    // Một save, một ví, một đồng hồ giả và mọi dịch vụ Combat nối đúng như GameBootstrap; persist là stub đếm và chụp.
    internal sealed class CombatRig
    {
        public static readonly DateTime T0 = new DateTime(2026, 11, 3, 8, 0, 0, DateTimeKind.Utc);

        private readonly List<PersistSnapshot> _snapshots = new List<PersistSnapshot>();

        public CombatRig(StageDefinition[] stages, CharacterDefinition[] team = null, Action<SerializedObject> configEdit = null)
        {
            Now = T0;
            Save = new PlayerSave();
            Wallet = new Wallet(Save);
            team ??= new[] { CombatTestData.Hero("a") };
            var catalog = TestData.Catalog(team);
            Roster = new Roster(Save, catalog);
            foreach (var hero in team)
            {
                Roster.Add(hero.Id);
                Roster.AddToTeam(hero.Id);
            }

            Config = CombatTestData.Config(so =>
            {
                so.FindProperty("_stages").objectReferenceValue = CombatTestData.Catalog(stages);
                configEdit?.Invoke(so);
            });
            // Server trong bộ nhớ chạy lệnh thật, đồng hồ theo Now; mốc ban đầu là save của rig (đội, thể lực, màn đã qua).
            Server = new LocalGameServer(() => Now, ServerData(catalog, Config));
            Gate = new CommandGate(_ => new TaskCompletionSource<bool>().Task);
            Server.SignInAsync().Sync();
            Server.Seed(JsonUtility.ToJson(Save));
            Server.BootstrapAsync().Sync();
            Stamina = new StaminaService(Save, Config, () => Now);
            Progress = new StageProgressService(Save, Wallet, Config.Stages, Stamina, Server, Gate, Apply);
            Stars = ServerRig.IdleStars(Save, Roster, catalog, ScriptableObject.CreateInstance<StarConfig>());
            LevelConfig = ScriptableObject.CreateInstance<LevelConfig>();
            StarConfig = ScriptableObject.CreateInstance<StarConfig>();
            PlayerLevel = new PlayerLevelService(Save, Wallet, LevelConfig, Persist);
            Levels = new CharacterLevelService(Save, Roster, Wallet, catalog, LevelConfig, PlayerLevel, Persist);
            Service = CombatTestData.Service(Config, Roster, Persist, Stamina, Progress, Stars, Server, Gate, Apply,
                Levels, PlayerLevel, LevelConfig, StarConfig);
        }

        public LocalGameServer Server { get; }
        public CommandGate Gate { get; }

        // Giống GameSession.ApplyServerState: chép state server vào save, báo view, lưu một lần.
        public bool Apply(ServerState state)
        {
            ServerStateApplier.Apply(Save, state);
            Wallet.NotifyChanged();
            Roster.NotifyChanged();
            return Persist();
        }

        private static GameData ServerData(CharacterCatalog characters, CombatConfig config)
        {
            var data = new GameData
            {
                MaxStars = 5,
                MaxTeamSize = 5,
                StarCosts = new List<int> { 2, 4, 6, 8 },
                Stamina = new StaminaData { Max = config.MaxStamina, RegenSeconds = config.StaminaRegenSeconds },
            };
            foreach (var c in characters.Items) data.Characters.Add(new CharacterData { Id = c.Id, BaseStars = Math.Max(c.Stars, 1) });
            if (config.Stages != null)
                foreach (var s in config.Stages.Items)
                {
                    if (s == null) continue;
                    data.Stages.Add(new StageData
                    {
                        Id = s.Id,
                        Kind = (int)s.Kind,
                        StaminaCost = s.StaminaCost,
                        FirstClear = Reward(s.FirstClearReward),
                        Repeat = Reward(s.RepeatReward),
                    });
                }

            return data;
        }

        private static RewardData Reward(IReadOnlyList<CurrencyAmount> lines) => new()
        {
            Gem = lines.Where(l => l.Type == CurrencyType.Gem).Sum(l => l.Amount),
            Material = lines.Where(l => l.Type == CurrencyType.Material).Sum(l => l.Amount),
            ExpPotion = lines.Where(l => l.Type == CurrencyType.ExpPotion).Sum(l => l.Amount),
        };

        public DateTime Now { get; set; }
        public PlayerSave Save { get; }
        public Wallet Wallet { get; }
        public Roster Roster { get; }
        public CombatConfig Config { get; }
        public StaminaService Stamina { get; }
        public StageProgressService Progress { get; }
        public StarService Stars { get; }
        public LevelConfig LevelConfig { get; }
        public StarConfig StarConfig { get; }
        public PlayerLevelService PlayerLevel { get; }
        public CharacterLevelService Levels { get; }
        public CombatService Service { get; }
        public bool PersistResult { get; set; } = true;
        // Persist ném lỗi (đĩa hỏng giữa chừng); đếm lần gọi trước khi ném.
        public bool PersistThrows { get; set; }
        public int PersistCalls { get; private set; }
        // Trận mà test muốn biết RewardsClaimed của nó lúc persist chạy.
        public BattleSession Watched { get; set; }
        public IReadOnlyList<PersistSnapshot> Snapshots => _snapshots;

        public bool Persist()
        {
            PersistCalls++;
            if (PersistThrows) throw new InvalidOperationException("boom");
            _snapshots.Add(new PersistSnapshot(Wallet.Get(CurrencyType.Material), Wallet.Get(CurrencyType.Gem),
                string.Join(",", Save.ClearedStages), Save.Stamina.Current, Save.Stamina.AnchorUtcTicks,
                Service?.Current != null, Watched?.RewardsClaimed));
            return PersistResult;
        }

        // Đặt thể lực thô đang hồi dở: Current điểm, mốc cách bây giờ `secondsAgo` giây.
        public void SetStamina(int current, double secondsAgo = 0)
        {
            Save.Stamina.Current = current;
            Save.Stamina.AnchorUtcTicks = Now.Ticks - (long)(secondsAgo * TimeSpan.TicksPerSecond);
            Server.State.Stamina = new StaminaState { Current = Save.Stamina.Current, AnchorUtcTicks = Save.Stamina.AnchorUtcTicks };
        }

        // Mọi thứ một lần thử có thể làm đổi (trừ trận hiện tại và số lần persist): so hai chuỗi này để chứng minh "không đổi gì".
        public string State()
        {
            _ = Stamina.Current; // chốt phần hồi trước khi đọc số thô, vì đọc là chốt
            return string.Join("|", Wallet.Get(CurrencyType.Material), Wallet.Get(CurrencyType.Gem),
                string.Join(",", Save.ClearedStages), Save.Stamina.Current, Save.Stamina.AnchorUtcTicks, PersistCalls);
        }

        // Nâng sao do server làm: ghi ở cả hai nơi, vì mỗi lệnh áp state server đè lên save của máy.
        public void StarUp(string id, int count)
        {
            Save.StarUps.Add(new StarUpEntry { CharacterId = id, Count = count });
            Server.State.StarUps.Add(new StarUpEntry { CharacterId = id, Count = count });
        }

        public void Clear(params string[] ids)
        {
            Save.ClearedStages.AddRange(ids.Where(id => !Save.ClearedStages.Contains(id)));
            Server.State.ClearedStages = new List<string>(Save.ClearedStages);
        }
    }
}
