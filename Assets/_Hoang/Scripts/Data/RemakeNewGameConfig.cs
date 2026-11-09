using System;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using UnityEngine;

namespace Hoang.Remake
{
    [CreateAssetMenu(menuName = "Hoang/Remake/New game config")]
    public sealed class RemakeNewGameConfig : ScriptableObject
    {
        [SerializeField] private CurrencyAmount[] _startingCurrency =
        {
            new(CurrencyType.Coin, 1000), new(CurrencyType.Gem, 5000), new(CurrencyType.Material, 0),
        };
        [SerializeField] private CharacterDefinition[] _starterCharacters = Array.Empty<CharacterDefinition>();
        public CurrencyAmount[] StartingCurrency => _startingCurrency;
        public CharacterDefinition[] StarterCharacters => _starterCharacters;
        public void SetStarters(CharacterDefinition[] characters) =>
            _starterCharacters = characters == null ? Array.Empty<CharacterDefinition>() : (CharacterDefinition[])characters.Clone();
    }
}
