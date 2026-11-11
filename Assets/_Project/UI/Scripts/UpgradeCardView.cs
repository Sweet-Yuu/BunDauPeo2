using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Thẻ nâng cấp: chỉ giữ tham chiếu tới tên, cấp, giá và nút Mua; UpgradeTestPanel ghi chữ vào đây.
    public class UpgradeCardView : MonoBehaviour
    {
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _costText;
        [SerializeField] private Button _buyButton;

        public Text NameText => _nameText;
        public Text LevelText => _levelText;
        public Text CostText => _costText;
        public Button BuyButton => _buyButton;
    }
}
