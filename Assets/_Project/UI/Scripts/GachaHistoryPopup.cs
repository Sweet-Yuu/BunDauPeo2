using System;
using System.Collections.Generic;
using System.Text;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Chỉ đọc lịch sử. Danh sách nằm trong ScrollRect ở scene, nên một Text dài vẫn cuộn được.
    public class GachaHistoryPopup : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _listText;
        [SerializeField] private Button _closeButton;

        public event Action CloseClicked;

        private void Awake() => _closeButton.onClick.AddListener(() => CloseClicked?.Invoke());

        private void OnDestroy() => _closeButton.onClick.RemoveAllListeners();

        // Mới nhất trên cùng.
        public void Open(IReadOnlyList<GachaHistoryEntry> history, Func<string, string> nameOf)
        {
            var text = new StringBuilder();
            for (var i = history.Count - 1; i >= 0; i--)
            {
                text.Append(GachaViewText.HistoryLine(history[i], nameOf));
                if (i > 0) text.Append('\n');
            }

            _listText.text = history.Count == 0 ? "Chưa có lượt quay nào." : text.ToString();
            _root.SetActive(true);
        }

        public void Close() => _root.SetActive(false);
    }
}
