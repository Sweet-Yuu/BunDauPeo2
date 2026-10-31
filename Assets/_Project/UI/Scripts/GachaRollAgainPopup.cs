using System;
using BunDauPeo.Gacha;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Popup sau trang tổng: quay tiếp hoặc thôi. Quyết định bật nút nằm ở GachaViewText.CanRollAgain.
    public class GachaRollAgainPopup : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _warningText;
        [SerializeField] private Text _rollAgainLabel;
        [SerializeField] private Button _rollAgainButton;
        [SerializeField] private Button _dismissButton;

        public event Action RollAgainClicked;
        public event Action DismissClicked;

        private void Awake()
        {
            _rollAgainButton.onClick.AddListener(() => RollAgainClicked?.Invoke());
            _dismissButton.onClick.AddListener(() => DismissClicked?.Invoke());
        }

        private void OnDestroy()
        {
            _rollAgainButton.onClick.RemoveAllListeners();
            _dismissButton.onClick.RemoveAllListeners();
        }

        public void Open(string label, bool canRollAgain, long needed)
        {
            _rollAgainLabel.text = label;
            _rollAgainButton.interactable = canRollAgain;
            _warningText.text = canRollAgain ? "" : GachaViewText.InsufficientGemText(needed);
            _root.SetActive(true);
        }

        public void Close() => _root.SetActive(false);
    }
}
