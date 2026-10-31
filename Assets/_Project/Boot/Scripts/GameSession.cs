using System;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.Boot
{
    public sealed class GameSession
    {
        private readonly SaveService _saveService;
        private readonly NewGameConfig _newGame;

        public PlayerSave Save { get; }
        public Wallet Wallet { get; }
        public Roster Roster { get; }
        public bool IsNewGame { get; private set; }

        public GameSession(SaveService saveService, CharacterCatalog catalog, NewGameConfig newGame)
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            _newGame = newGame != null ? newGame : throw new ArgumentNullException(nameof(newGame));

            var loaded = _saveService.Load();
            IsNewGame = loaded == null;
            Save = loaded ?? new PlayerSave();
            Wallet = new Wallet(Save);
            Roster = new Roster(Save, catalog);

            if (!IsNewGame) return;

            GrantNewGame();
            // Lưu ngay để mở lại không bị cấp lại đồ ban đầu.
            Persist();
        }

        public bool Persist() => _saveService.Save(Save);

        // Đưa MỌI thứ đã lưu về như game mới: xoá cả file sao lưu, rồi ghi đè cả PlayerSave thay vì xoá từng field,
        // nên field thêm về sau tự được reset mà không ai phải nhớ. Chỉ gọi sau khi người chơi đã xác nhận.
        public bool ResetToDefaults()
        {
            // Xoá file thất bại thì giữ nguyên bộ nhớ, để bộ nhớ và đĩa không lệch nhau.
            if (!_saveService.DeleteAll()) return false;

            // Wallet và Roster giữ tham chiếu tới cùng object Save nên phải ghi đè tại chỗ.
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new PlayerSave()), Save);
            IsNewGame = true;
            GrantNewGame();
            Wallet.NotifyChanged();
            Roster.NotifyChanged();
            return Persist();
        }

        private void GrantNewGame()
        {
            foreach (var line in _newGame.StartingCurrency)
                if (line.Amount > 0) Wallet.Add(line.Type, line.Amount);
            foreach (var starter in _newGame.StarterCharacters)
                if (starter != null) Roster.Add(starter.Id);
        }
    }
}
