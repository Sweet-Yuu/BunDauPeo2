using System;
using System.Collections;
using System.Collections.Generic;
using BunDauPeo.Core;
using BunDauPeo.Gacha;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Trang tổng: icon hiện lần lượt, nút OK. Không có logic quay.
    public class GachaTotalView : MonoBehaviour
    {
        private const float IconInterval = 0.08f;

        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _grid;
        [SerializeField] private GameObject _iconTemplate;
        [SerializeField] private Button _okButton;

        private Coroutine _routine;

        public event Action OkClicked;

        private void Awake()
        {
            _iconTemplate.SetActive(false);
            _okButton.onClick.AddListener(() => OkClicked.SafeInvoke());
        }

        private void OnDestroy() => _okButton.onClick.RemoveAllListeners();

        public void Show(IReadOnlyList<GachaPullEntry> entries)
        {
            if (_routine != null) StopCoroutine(_routine);
            for (var i = _grid.childCount - 1; i >= 0; i--)
            {
                var child = _grid.GetChild(i).gameObject;
                if (child != _iconTemplate) Destroy(child);
            }

            _root.SetActive(true);
            _routine = StartCoroutine(RevealIcons(entries));
        }

        public void Hide()
        {
            if (_routine != null) StopCoroutine(_routine);
            _root.SetActive(false);
        }

        private IEnumerator RevealIcons(IReadOnlyList<GachaPullEntry> entries)
        {
            foreach (var entry in entries)
            {
                var icon = Instantiate(_iconTemplate, _grid);
                icon.SetActive(true);
                icon.GetComponent<Image>().color = GachaViewText.RarityColor(entry.Character.Rarity);

                var portrait = icon.transform.Find("Portrait").GetComponent<Image>();
                portrait.sprite = entry.Character.Portrait;

                icon.transform.Find("Label").GetComponent<Text>().text = GachaViewText.Describe(entry);
                yield return new WaitForSecondsRealtime(IconInterval);
            }
        }
    }
}
