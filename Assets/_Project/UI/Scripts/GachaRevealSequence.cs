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

        // Các hàm dưới trả true khi bước hiện tại đã đổi. View chỉ vào bước mới khi true: chạm vào cutscene SSR
        // (không đổi bước) mà view vẫn phát lại clip thì cutscene bị chạy lại từ đầu.
        public bool Tap() => Current.Kind is RevealStepKind.CommonCutscene or RevealStepKind.Reveal && Advance();

        public bool ClipFinished() => Current.Kind is RevealStepKind.CommonCutscene or RevealStepKind.LegendaryCutscene && Advance();

        public bool SkipToSsrOrTotal()
        {
            if (!_isTen || Current.Kind == RevealStepKind.LegendaryCutscene) return false;

            // Tìm SSR phía sau; không có thì tới trang tổng. Không bao giờ lùi về SSR đã xem.
            var next = _steps.FindIndex(_index + 1, s => s.Kind == RevealStepKind.LegendaryCutscene);
            var target = next >= 0 ? next : _steps.Count - 1;
            var moved = target != _index;
            _index = target;
            return moved;
        }

        private bool Advance()
        {
            if (_index >= _steps.Count - 1) return false;
            _index++;
            return true;
        }
    }
}
