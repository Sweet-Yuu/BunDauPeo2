using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.Restaurant
{
    [CreateAssetMenu(menuName = "BunDauPeo/Restaurant/Customer")]
    public class CustomerDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Khách thường";
        [SerializeField] private string _displayNameEn;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private float _spawnWeight = 6f;
        [SerializeField] private float _patienceSeconds = 40f;
        [SerializeField] private float _payMultiplier = 1f;

        public string DisplayName => GameLanguage.Pick(_displayName, _displayNameEn);
        public Sprite Sprite => _sprite;
        public float SpawnWeight => _spawnWeight;
        public float PatienceSeconds => _patienceSeconds;
        public float PayMultiplier => _payMultiplier;
    }
}
