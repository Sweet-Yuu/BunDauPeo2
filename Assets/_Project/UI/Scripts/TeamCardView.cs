using BunDauPeo.Characters;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Thẻ một nhân vật trong lưới chọn đội: chân dung ô vuông màu class, tên, class và độ hiếm, sao, nhãn "Quán: <vai>" hoặc "Trong đội".
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
            _info.text = $"{CombatViewText.ClassName(character.Class)} · {character.Rarity} · {new string('★', character.Stars)}";
            _tag.text = row.InTeam ? "Trong đội" : row.StaffRole.HasValue ? CombatViewText.StaffTag(row.StaffRole.Value) : "";
        }

        public void Hide()
        {
            HasRow = false;
            gameObject.SetActive(false);
        }
    }
}
