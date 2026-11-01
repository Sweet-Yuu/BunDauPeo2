using UnityEngine;

namespace BunDauPeo.Restaurant
{
    // Không có Id: save không tham chiếu món, mô phỏng bốc món từ danh sách của RestaurantConfig.
    [CreateAssetMenu(menuName = "BunDauPeo/Restaurant/Dish")]
    public class DishDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Món ăn";
        [SerializeField] private Sprite _icon;
        [SerializeField] private long _basePrice = 20;
        [SerializeField] private float _cookSeconds = 5f;

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public long BasePrice => _basePrice;
        public float CookSeconds => _cookSeconds;
    }
}
