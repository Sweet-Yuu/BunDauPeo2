using System.Collections.Generic;
using BunDauPeo.Characters;
using BunDauPeo.Gacha;

namespace BunDauPeo.UI
{
    public enum RevealStepKind { CommonCutscene, LegendaryCutscene, Reveal, Total }

    public readonly struct RevealStep
    {
        public RevealStep(RevealStepKind kind, int entryIndex)
        {
            Kind = kind;
            EntryIndex = entryIndex;
        }

        public RevealStepKind Kind { get; }
        public int EntryIndex { get; }
    }

    // Thứ tự và luật bỏ qua của một lần quay. Không biết gì về hiển thị: view gọi Tap, ClipFinished, SkipToSsrOrTotal.
    public sealed class GachaRevealSequence
    {
        private readonly List<RevealStep> _steps = new();
        private readonly bool _isTen;
        private int _index;

        public GachaRevealSequence(IReadOnlyList<GachaPullEntry> entries, bool isTen)
        {
            _isTen = isTen;
            _steps.Add(new RevealStep(RevealStepKind.CommonCutscene, -1));
            for (var i = 0; i < entries.Count; i++)
                if (entries[i].Character.Rarity != Rarity.SSR) _steps.Add(new RevealStep(RevealStepKind.Reveal, i));

            // SSR đứng cuối theo thứ tự quay, mỗi SSR có cutscene riêng rồi reveal.
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].Character.Rarity != Rarity.SSR) continue;
                _steps.Add(new RevealStep(RevealStepKind.LegendaryCutscene, i));
                _steps.Add(new RevealStep(RevealStepKind.Reveal, i));
            }

            _steps.Add(new RevealStep(RevealStepKind.Total, -1));
        }

        public RevealStep Current => _steps[_index];

        // Nút Bỏ qua chỉ có ở Quay 10 và không hoạt động trong cutscene SSR hoặc trang tổng.
        public bool CanSkipAll => _isTen && Current.Kind != RevealStepKind.LegendaryCutscene && Current.Kind != RevealStepKind.Total;

        public void Tap()
        {
            if (Current.Kind is RevealStepKind.CommonCutscene or RevealStepKind.Reveal) Advance();
        }

        public void ClipFinished()
        {
            if (Current.Kind is RevealStepKind.CommonCutscene or RevealStepKind.LegendaryCutscene) Advance();
        }

        public void SkipToSsrOrTotal()
        {
            if (!_isTen || Current.Kind == RevealStepKind.LegendaryCutscene) return;

            // Tìm SSR phía sau; không có thì tới trang tổng. Không bao giờ lùi về SSR đã xem.
            var next = _steps.FindIndex(_index + 1, s => s.Kind == RevealStepKind.LegendaryCutscene);
            _index = next >= 0 ? next : _steps.Count - 1;
        }

        private void Advance()
        {
            if (_index < _steps.Count - 1) _index++;
        }
    }
}
