using UnityEngine;
namespace BunDauPeo.UI
{
    // Nằm trên panel kín màn (rect đổi theo canvas nên nhận được OnRectTransformDimensionsChange); đặt cỡ và vị trí cho khung nội dung `Content`
    // dựng theo 1920 px bề ngang, neo trên-trái, pivot trên-trái. Màn hẹp hơn 16:9 (canvas khớp chiều cao nên rộng dưới 1920) thì co đều để không mất cột phải;
    // màn rộng hơn 16:9 giữ cỡ và canh giữa theo chiều ngang. Không đặt lên chính khung neo điểm: nó không bao giờ nhận sự kiện đổi cỡ.
    public class FitWidthScale : MonoBehaviour
    {
        public const float DesignWidth = 1920f;
        public RectTransform Content;
        public static float ScaleFor(float parentWidth) => parentWidth <= 0f ? 1f : Mathf.Min(1f, parentWidth / DesignWidth);
        // Độ dời x của khung sau khi co: 0 khi màn hẹp hơn hoặc bằng 16:9, nửa phần dư khi rộng hơn.
        public static float OffsetFor(float parentWidth) => parentWidth <= 0f ? 0f : (parentWidth - DesignWidth * ScaleFor(parentWidth)) / 2f;
        private void OnEnable() => Apply();
        private void OnRectTransformDimensionsChange() => Apply();
        private void Apply()
        {
            if (Content == null || transform is not RectTransform self) return;
            var width = self.rect.width;
            var scale = ScaleFor(width);
            Content.localScale = new Vector3(scale, scale, 1f);
            Content.anchoredPosition = new Vector2(OffsetFor(width), 0f);
        }
    }
}
