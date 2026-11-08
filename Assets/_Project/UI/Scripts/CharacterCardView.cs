using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Một thẻ ở lưới trái màn Nhân vật. Chỉ vẽ; luật ở CharacterListRows / CharactersViewText.
    public class CharacterCardView : MonoBehaviour
    {
        private static readonly Color SelectedFrame = new(1f, 0.82f, 0.25f);
        private static readonly Color PlainFrame = new(0.30f, 0.32f, 0.40f);

        [SerializeField] private Button _button;
        [SerializeField] private Image _frame;
        [SerializeField] private Image _portrait;
        [SerializeField] private Text _name;
        [SerializeField] private Text _info;
        [SerializeField] private Text _fragments;

        public Button Button => _button;

        public void Bind(CharacterListRow row, bool selected)
        {
            var character = row.Character;
            gameObject.SetActive(true);
            _frame.color = selected ? SelectedFrame : PlainFrame;
            _portrait.color = GachaViewText.RarityColor(character.Rarity);
            _name.text = string.IsNullOrEmpty(character.DisplayName) ? character.Id : character.DisplayName;
            _info.text = $"{character.Rarity} {new string('★', row.Stars)}";
            _fragments.text = CharactersViewText.CardFragments(row);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
