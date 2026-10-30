using System;
using BunDauPeo.Characters;
using BunDauPeo.Core;

namespace BunDauPeo.Boot
{
    public sealed class GameSession
    {
        private readonly SaveService _saveService;

        public PlayerSave Save { get; }
        public Wallet Wallet { get; }
        public Roster Roster { get; }
        public bool IsNewGame { get; }

        public GameSession(SaveService saveService, CharacterCatalog catalog, NewGameConfig newGame)
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (newGame == null) throw new ArgumentNullException(nameof(newGame));

            var loaded = _saveService.Load();
            IsNewGame = loaded == null;
            Save = loaded ?? new PlayerSave();
            Wallet = new Wallet(Save);
            Roster = new Roster(Save, catalog);

            if (!IsNewGame) return;

            foreach (var line in newGame.StartingCurrency)
                if (line.Amount > 0) Wallet.Add(line.Type, line.Amount);
            foreach (var starter in newGame.StarterCharacters)
                if (starter != null) Roster.Add(starter.Id);

            // Lưu ngay để mở lại không bị cấp lại đồ ban đầu.
            Persist();
        }

        public bool Persist() => _saveService.Save(Save);
    }
}
