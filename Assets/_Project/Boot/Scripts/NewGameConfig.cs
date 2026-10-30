using System;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.Boot
{
    [CreateAssetMenu(menuName = "BunDauPeo/Boot/New Game Config")]
    public class NewGameConfig : ScriptableObject
    {
        [SerializeField] private CurrencyAmount[] _startingCurrency = Array.Empty<CurrencyAmount>();
        [SerializeField] private CharacterDefinition[] _starterCharacters = Array.Empty<CharacterDefinition>();

        public CurrencyAmount[] StartingCurrency => _startingCurrency;
        public CharacterDefinition[] StarterCharacters => _starterCharacters;
    }
}
