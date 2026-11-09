using System;
using System.Collections.Generic;
using System.IO;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using Hoang.Remake.RestaurantRuntime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Hoang.Remake
{
    [DefaultExecutionOrder(-100)]
    public sealed class RemakeBootstrap : MonoBehaviour
    {
        [SerializeField] private CharacterCatalog _characters;
        [SerializeField] private RemakeNewGameConfig _newGame;
        [SerializeField] private RestaurantConfig _restaurantConfig;
        [SerializeField] private SkillSO[] _skills = Array.Empty<SkillSO>();
        [SerializeField] private FacilityItemSO[] _facilities = Array.Empty<FacilityItemSO>();
        [SerializeField] private RemakeShell _shell;

        private static RemakeBootstrap _active;
        public static RemakeBootstrap Active => _active;
        public static string SavePathOverride { get; set; }
        public static string SavePath => SavePathOverride ?? Path.Combine(Application.persistentDataPath, "HoangRemake", "save.json");
        public RemakeContext Context { get; private set; }
        public SkillTreeManager Skills { get; private set; }
        public FacilityManager Facilities { get; private set; }
        public RemakeRestaurant Restaurant { get; private set; }
        public Roster Roster { get; private set; }

        public void Configure(CharacterCatalog characters, RemakeNewGameConfig newGame, RestaurantConfig restaurantConfig,
            SkillSO[] skills, FacilityItemSO[] facilities, RemakeShell shell)
        {
            _characters = characters;
            _newGame = newGame;
            _restaurantConfig = restaurantConfig;
            _skills = skills;
            _facilities = facilities;
            _shell = shell;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _active = null;

        private void Awake()
        {
            if (_active != null && _active != this) { Destroy(gameObject); return; }
            if (FindFirstObjectByType<GameBootstrap>(FindObjectsInactive.Include) != null)
            {
                Debug.LogError("Open the restaurant remake scene on its own; the original bootstrap is already loaded.", this);
                return;
            }
            if (_characters == null || _newGame == null || _restaurantConfig == null)
            {
                Debug.LogError("Remake bootstrap is missing configuration. Use Hoang > Restaurant Remake > Rebuild scene and prefab.", this);
                return;
            }
            try
            {
                ServiceLocator.Clear();
                var store = new RemakeSaveStore(SavePath);
                var save = store.Load();
                if (save == null)
                {
                    save = new HoangSaveData();
                    ApplyDefaults(save);
                    if (!store.Save(save)) throw new IOException("Could not create the restaurant save.");
                }
                Normalize(save);
                Context = new RemakeContext(save, store.Save, ApplyDefaults, store.Reset);
                Roster = new Roster(save, _characters);
                Skills = new SkillTreeManager(Context, _skills);
                Facilities = new FacilityManager(Context, _facilities);
                RestaurantBuffBridge.Initialize(Skills, Facilities);
                Restaurant = new RemakeRestaurant(_restaurantConfig, Context, Roster, new System.Random());
                Context.ResetCompleted += Roster.NotifyChanged;
                ServiceLocator.Register(Context);
                ServiceLocator.Register(Context.Wallet);
                ServiceLocator.Register(Roster);
                ServiceLocator.Register(new GameResetter(Context.Reset));
                _active = this;
                if (_shell == null)
                {
                    var template = Resources.Load<RemakeShell>("HoangRestaurantSystems");
                    if (template != null) _shell = Instantiate(template);
                }
                if (EventSystem.current == null)
                    new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                if (_shell != null) _shell.Bind(this);
                else Debug.LogError("Restaurant remake UI prefab is missing.", this);
            }
            catch (Exception exception)
            {
                // Không ghi default đè lên lỗi đọc/khóa file; giữ nguyên dữ liệu để lần sau phục hồi.
                Debug.LogException(exception, this);
            }
        }

        private void OnApplicationPause(bool paused) { if (paused) Context?.Persist(); }
        private void OnApplicationQuit() => Context?.Persist();

        private void OnDestroy()
        {
            if (Context != null && Roster != null) Context.ResetCompleted -= Roster.NotifyChanged;
            Restaurant?.Dispose();
            Skills?.Dispose();
            Facilities?.Dispose();
            if (_active == this) { RestaurantBuffBridge.Shutdown(); ServiceLocator.Clear(); _active = null; }
        }

        private void ApplyDefaults(HoangSaveData save)
        {
            foreach (var amount in _newGame.StartingCurrency)
                if (amount.Amount > 0) save.Balances.Add(amount);
            var index = 0;
            foreach (var starter in _newGame.StarterCharacters)
            {
                if (starter == null) continue;
                save.OwnedCharacterIds.Add(starter.Id);
                if (index < 3) save.Staff.Add(new StaffAssignment { CharacterId = starter.Id, Role = (StaffRole)index++ });
            }
            foreach (var item in _facilities)
                if (item != null) save.Facilities.InventoryItemIds.Add(item.Id);
            // Ba đoạn dây độc lập để người chơi thử nhánh điện và xoay vị trí.
            foreach (var item in _facilities)
                if (item != null && item.Id == "wire")
                { save.Facilities.InventoryItemIds.Add(item.Id); save.Facilities.InventoryItemIds.Add(item.Id); }
        }

        private static void Normalize(HoangSaveData save)
        {
            save.Balances ??= new List<CurrencyAmount>();
            save.OwnedCharacterIds ??= new List<string>();
            save.Staff ??= new List<StaffAssignment>();
            save.Upgrades ??= new List<UpgradeLevel>();
            save.Skills ??= new SkillTreeSaveState();
            save.Skills.Slots ??= new List<SkillSlotSaveEntry>();
            save.Facilities ??= new FacilitySaveState();
            save.Facilities.InventoryItemIds ??= new List<string>();
            save.Facilities.PlacedItems ??= new List<FacilityPlacedItemSaveEntry>();
            save.ShopDaily ??= new ShopDailySaveState();
        }
    }
}
