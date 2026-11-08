using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Nhãn cố định do builder dựng. Không gắn vào Text mà view ghi lúc chạy: OnEnable sẽ đè chữ của view.
    [RequireComponent(typeof(Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _key;

        public string Key => _key;

        // Builder gán key lúc dựng scene; không vẽ lại ở đây vì OnEnable sẽ làm.
        public void SetKey(string key) => _key = key;

        private void OnEnable() => GetComponent<Text>().text = UiText.Get(_key);
    }
}
