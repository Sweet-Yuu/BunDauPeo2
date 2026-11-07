using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.Restaurant
{
    [CreateAssetMenu(menuName = "BunDauPeo/Restaurant/Incident")]
    public class IncidentDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Sự cố";
        [SerializeField] private string _displayNameEn;
        [SerializeField] private StaffRole _role;
        [SerializeField] private float _weight = 1f;

        public string DisplayName => GameLanguage.Pick(_displayName, _displayNameEn);
        public StaffRole Role => _role;
        public float Weight => _weight;
    }
}
