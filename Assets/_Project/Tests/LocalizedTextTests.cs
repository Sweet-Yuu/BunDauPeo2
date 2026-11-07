using System.Reflection;
using BunDauPeo.Core;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.Tests
{
    public class LocalizedTextTests
    {
        private GameObject _go;
        private LocalizedText _label;

        [TearDown]
        public void TearDown()
        {
            GameLanguage.Current = Language.Vi;
            if (_go != null) Object.DestroyImmediate(_go);
        }

        // EditMode không tự gọi OnEnable của MonoBehaviour thường (không ExecuteAlways), nên gọi tay đúng hàm Unity sẽ gọi khi bật.
        private void Enable() =>
            typeof(LocalizedText).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_label, null);

        private Text Create()
        {
            _go = new GameObject("label");
            var text = _go.AddComponent<Text>();
            _label = _go.AddComponent<LocalizedText>();
            _label.SetKey("common.close");
            return text;
        }

        [Test]
        public void OnEnable_SetsTextForCurrentLanguage()
        {
            var text = Create();
            Enable();
            Assert.AreEqual("Đóng", text.text);
        }

        [Test]
        public void Reenable_AfterLanguageChange_UpdatesText()
        {
            var text = Create();
            Enable();
            GameLanguage.Current = Language.En;
            Enable();
            Assert.AreEqual("Close", text.text);
        }
    }
}
