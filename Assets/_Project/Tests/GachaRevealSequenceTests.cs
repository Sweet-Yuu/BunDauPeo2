using System.Collections.Generic;
using BunDauPeo.Characters;
using BunDauPeo.Gacha;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class GachaRevealSequenceTests
    {
        private static GachaPullEntry E(string id, Rarity rarity) =>
            new GachaPullEntry(TestData.Character(id, rarity), true, 0);

        // Nhãn ngắn: C = cutscene thường, Lk = cutscene SSR của entry k, Rk = reveal entry k, T = trang tổng.
        private static string Label(RevealStep s) => s.Kind switch
        {
            RevealStepKind.CommonCutscene => "C",
            RevealStepKind.LegendaryCutscene => "L" + s.EntryIndex,
            RevealStepKind.Reveal => "R" + s.EntryIndex,
            _ => "T",
        };

        // Đi hết chuỗi bằng hành vi thật: cutscene kết thúc theo clip, reveal kết thúc theo chạm.
        private static List<string> Walk(GachaRevealSequence seq)
        {
            var labels = new List<string> { Label(seq.Current) };
            while (seq.Current.Kind != RevealStepKind.Total)
            {
                if (seq.Current.Kind is RevealStepKind.CommonCutscene or RevealStepKind.LegendaryCutscene) seq.ClipFinished();
                else seq.Tap();
                labels.Add(Label(seq.Current));
            }
            return labels;
        }

        [Test]
        public void Steps_NormalsInRollOrder_SsrLast()
        {
            var seq = new GachaRevealSequence(new[] { E("a", Rarity.R), E("b", Rarity.SSR), E("c", Rarity.R), E("d", Rarity.SR) }, isTen: true);
            CollectionAssert.AreEqual(new[] { "C", "R0", "R2", "R3", "L1", "R1", "T" }, Walk(seq));
        }

        [Test]
        public void Steps_MultipleSsrInRollOrder()
        {
            var seq = new GachaRevealSequence(new[] { E("a", Rarity.SSR), E("b", Rarity.R), E("c", Rarity.SSR) }, isTen: true);
            CollectionAssert.AreEqual(new[] { "C", "R1", "L0", "R0", "L2", "R2", "T" }, Walk(seq));
        }

        [Test]
        public void SingleRoll_StillEndsAtTotal()
        {
            var seq = new GachaRevealSequence(new[] { E("a", Rarity.R) }, isTen: false);
            CollectionAssert.AreEqual(new[] { "C", "R0", "T" }, Walk(seq));
        }

        [Test]
        public void Tap_OnCommonCutscene_GoesToFirstReveal()
        {
            var seq = new GachaRevealSequence(new[] { E("a", Rarity.R), E("b", Rarity.SSR) }, isTen: true);
            seq.Tap();
            Assert.AreEqual("R0", Label(seq.Current));
        }

        [Test]
        public void ClipFinished_OnCommonCutscene_GoesToFirstReveal()
        {
            var seq = new GachaRevealSequence(new[] { E("a", Rarity.R), E("b", Rarity.SSR) }, isTen: true);
            seq.ClipFinished();
            Assert.AreEqual("R0", Label(seq.Current));
        }

        [Test]
        public void Tap_OnLegendaryCutscene_DoesNothing()
        {
            var seq = new GachaRevealSequence(new[] { E("a", Rarity.SSR) }, isTen: true);
            seq.ClipFinished();
            Assert.AreEqual("L0", Label(seq.Current));
            // false = không đổi bước: view dựa vào đây để không phát lại cutscene khi chạm.
            Assert.IsFalse(seq.Tap());
            Assert.AreEqual("L0", Label(seq.Current));
        }

        [Test]
        public void SkipToSsrOrTotal_WithSsr_GoesToNextLegendary()
        {
            var seq = new GachaRevealSequence(new[] { E("a", Rarity.R), E("b", Rarity.SSR), E("c", Rarity.SSR) }, isTen: true);
            seq.Tap();
            Assert.IsTrue(seq.SkipToSsrOrTotal());
            Assert.AreEqual("L1", Label(seq.Current));
        }

        [Test]
        public void AfterSkip_EveryLaterSsrStillPlaysItsCutscene()
        {
            var seq = new GachaRevealSequence(new[] { E("a", Rarity.R), E("b", Rarity.SSR), E("c", Rarity.SSR) }, isTen: true);
            seq.Tap();
            seq.SkipToSsrOrTotal();
            var labels = new List<string> { Label(seq.Current) };
            while (seq.Current.Kind != RevealStepKind.Total)
            {
                if (seq.Current.Kind == RevealStepKind.LegendaryCutscene) seq.ClipFinished();
                else seq.Tap();
                labels.Add(Label(seq.Current));
            }
            CollectionAssert.AreEqual(new[] { "L1", "R1", "L2", "R2", "T" }, labels);
        }

        [Test]
        public void SkipToSsrOrTotal_FromSsrReveal_GoesToNextSsr()
        {
            var seq = new GachaRevealSequence(new[] { E("a", Rarity.SSR), E("b", Rarity.SSR) }, isTen: true);
            seq.ClipFinished();
            seq.ClipFinished();
            Assert.AreEqual("R0", Label(seq.Current));
            seq.SkipToSsrOrTotal();
            Assert.AreEqual("L1", Label(seq.Current));
        }

        [Test]
        public void SkipToSsrOrTotal_WithoutSsr_GoesToTotal()
        {
            var seq = new GachaRevealSequence(new[] { E("a", Rarity.R), E("b", Rarity.SR) }, isTen: true);
            seq.Tap();
            seq.SkipToSsrOrTotal();
            Assert.AreEqual("T", Label(seq.Current));
        }

        [Test]
        public void SkipToSsrOrTotal_OnLegendaryCutscene_DoesNothing()
        {
            var seq = new GachaRevealSequence(new[] { E("a", Rarity.SSR), E("b", Rarity.SSR) }, isTen: true);
            seq.ClipFinished();
            Assert.IsFalse(seq.SkipToSsrOrTotal());
            Assert.AreEqual("L0", Label(seq.Current));
        }

        [Test]
        public void SkipToSsrOrTotal_SingleRoll_DoesNothing()
        {
            var seq = new GachaRevealSequence(new[] { E("a", Rarity.SSR) }, isTen: false);
            seq.ClipFinished();
            Assert.AreEqual("L0", Label(seq.Current));
            Assert.IsFalse(seq.SkipToSsrOrTotal());
            Assert.AreEqual("L0", Label(seq.Current), "single roll never skips, even onto a legendary step");

            var single = new GachaRevealSequence(new[] { E("a", Rarity.R) }, isTen: false);
            single.Tap();
            single.SkipToSsrOrTotal();
            Assert.AreEqual("R0", Label(single.Current));
        }

        [Test]
        public void CanSkipAll_OnlyTenRollOutsideLegendaryAndTotal()
        {
            var ten = new GachaRevealSequence(new[] { E("a", Rarity.R), E("b", Rarity.SSR) }, isTen: true);
            Assert.IsTrue(ten.CanSkipAll, "common cutscene");
            ten.Tap();
            Assert.IsTrue(ten.CanSkipAll, "reveal");
            ten.Tap();
            Assert.IsFalse(ten.CanSkipAll, "legendary cutscene");
            ten.ClipFinished();
            Assert.IsTrue(ten.CanSkipAll, "ssr reveal");
            ten.Tap();
            Assert.AreEqual("T", Label(ten.Current));
            Assert.IsFalse(ten.CanSkipAll, "total");

            var single = new GachaRevealSequence(new[] { E("a", Rarity.R) }, isTen: false);
            Assert.IsFalse(single.CanSkipAll);
        }

        [Test]
        public void Tap_OnTotal_DoesNothing()
        {
            var seq = new GachaRevealSequence(new[] { E("a", Rarity.R) }, isTen: false);
            Walk(seq);
            Assert.IsFalse(seq.Tap());
            Assert.AreEqual("T", Label(seq.Current));
        }

        [Test]
        public void ClipFinished_OnReveal_DoesNothing()
        {
            var seq = new GachaRevealSequence(new[] { E("a", Rarity.R) }, isTen: false);
            seq.Tap();
            Assert.IsFalse(seq.ClipFinished());
            Assert.AreEqual("R0", Label(seq.Current));
        }
    }
}
