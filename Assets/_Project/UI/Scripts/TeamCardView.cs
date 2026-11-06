using BunDauPeo.Characters;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Thẻ một nhân vật trong lưới chọn đội: chân dung ô vuông màu class, tên, độ hiếm kèm sao, nhãn "Quán: <vai>"; dấu tích khi đã trong đội (class đọc qua màu ô và chip).
    public class TeamCardView : MonoBehaviour
    {
        private static readonly Color InTeamFrame = new(1f, 0.85f, 0.25f);
        private static readonly Color OutFrame = new(0.30f, 0.33f, 0.45f);

        [SerializeField] private Button _button;
        [SerializeField] private Image _frame;
        [SerializeField] private Image _portrait;
        [SerializeField] private Text _name;
        [SerializeField] private Text _info;
        [SerializeField] private Text _tag;
        [SerializeField] private GameObject _check;

        public Button Button => _button;
        public TeamSelectRow Row { get; private set; }
        public bool HasRow { get; private set; }

        public void Bind(TeamSelectRow row)
        {
            Row = row;
            HasRow = true;
            var character = row.Character;
            gameObject.SetActive(true);
            _frame.color = row.InTeam ? InTeamFrame : OutFrame;
            _portrait.color = BattleUnitView.ClassColor(character.Class);
            _name.text = string.IsNullOrEmpty(character.DisplayName) ? character.Id : character.DisplayName;
            _info.text = $"{character.Rarity} {new string('★', character.Stars)}";
            _check.SetActive(row.InTeam);
            _tag.text = !row.InTeam && row.StaffRole.HasValue ? CombatViewText.StaffTag(row.StaffRole.Value) : "";
        }

        public void Hide()
        {
            HasRow = false;
            gameObject.SetActive(false);
        }
    }
}
