using UnityEngine;

namespace BunDauPeo.Restaurant
{
    // Tick bằng unscaledDeltaTime để tăng tốc hay tạm dừng (Time.timeScale) không đổi tốc độ quán.
    public sealed class RestaurantRunner : MonoBehaviour
    {
        // Khung đứng hình (nạp scene) không được làm khách đến dồn một lúc.
        public const float MaxDelta = 0.5f;

        private RestaurantService _service;

        public static float Clamp(float unscaledDeltaTime) => Mathf.Min(unscaledDeltaTime, MaxDelta);

        public void Init(RestaurantService service) => _service = service;

        private void Update() => _service?.Update(Clamp(Time.unscaledDeltaTime));
    }
}
