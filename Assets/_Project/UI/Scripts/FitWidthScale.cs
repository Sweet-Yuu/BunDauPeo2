using UnityEngine;

namespace BunDauPeo.UI
{
    // Khung nội dung dựng theo 1920 px bề ngang: màn hẹp hơn 16:9 (canvas khớp chiều cao nên rộng dưới 1920) thì co đều để không mất cột phải.
    // Neo trên-trái, pivot trên-trái; màn rộng hơn 16:9 giữ nguyên cỡ.
    public class FitWidthScale : MonoBehaviour
    {
        public const float DesignWidth = 1920f;

        public static float ScaleFor(float parentWidth) => parentWidth <= 0f ? 1f : Mathf.Min(1f, parentWidth / DesignWidth);

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (transform.parent is not RectTransform parent) return;

            var scale = ScaleFor(parent.rect.width);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
