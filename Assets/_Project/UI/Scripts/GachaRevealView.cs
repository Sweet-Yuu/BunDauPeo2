using System;
using System.Collections;
using System.Collections.Generic;
using BunDauPeo.Core;
using BunDauPeo.Gacha;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Chỉ hiển thị: thứ tự và luật bỏ qua nằm trong GachaRevealSequence.
    public class GachaRevealView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Animator _commonAnimator;
        [SerializeField] private Animator _legendaryAnimator;
        [SerializeField] private GameObject _revealPanel;
        [SerializeField] private Image _flash;
        [SerializeField] private Image _portrait;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _starsText;

        // Chỉnh trong Inspector: lóe sáng và khoảng chờ trước khi chạm có tác dụng, để không lướt qua nhân vật khi bấm nhanh.
        [SerializeField] private float _flashSeconds = 1.0f;
        [SerializeField] private float _tapDelaySeconds = 0.7f;

        private const float FlashPeak = 0.85f;
        private const float PopSeconds = 0.35f;
        private const float PopFromScale = 0.88f;

        private IReadOnlyList<GachaPullEntry> _entries;
        private GachaRevealSequence _sequence;
        private Coroutine _flashRoutine;
        private float _tapReadyAt;

        public event Action Finished;

        public bool CanSkipAll => _sequence != null && _sequence.CanSkipAll;

        private void Awake()
        {
            // Cutscene dùng unscaled time để không bị đứng khi Time.timeScale = 0.
            _commonAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            _legendaryAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        public void Play(IReadOnlyList<GachaPullEntry> entries, bool isTen)
        {
            _entries = entries;
            _sequence = new GachaRevealSequence(entries, isTen);
            _root.SetActive(true);
            Enter();
        }

        public void SkipToSsrOrTotal()
        {
            if (_sequence == null) return;
            if (_sequence.SkipToSsrOrTotal()) Enter();
        }

        private void Update()
        {
            if (_sequence == null) return;

            var kind = _sequence.Current.Kind;
            if (kind is RevealStepKind.CommonCutscene or RevealStepKind.LegendaryCutscene)
            {
                var animator = kind == RevealStepKind.CommonCutscene ? _commonAnimator : _legendaryAnimator;
                if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f && !animator.IsInTransition(0))
                {
                    if (_sequence.ClipFinished()) Enter();
                    return;
                }
            }

            if (Time.unscaledTime >= _tapReadyAt && WasTapped() && _sequence.Tap()) Enter();
        }

        private static bool WasTapped() =>
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);

        private void Enter()
        {
            var step = _sequence.Current;
            _tapReadyAt = Time.unscaledTime + _tapDelaySeconds;
            // Lóe sáng dở dang không được kéo sang cutscene hay lượt quay sau.
            if (step.Kind != RevealStepKind.Reveal) ClearFlash();
            _commonAnimator.gameObject.SetActive(step.Kind == RevealStepKind.CommonCutscene);
            _legendaryAnimator.gameObject.SetActive(step.Kind == RevealStepKind.LegendaryCutscene);
            _revealPanel.SetActive(step.Kind == RevealStepKind.Reveal);

            if (step.Kind == RevealStepKind.CommonCutscene) _commonAnimator.Play("Summon", 0, 0f);
            else if (step.Kind == RevealStepKind.LegendaryCutscene) _legendaryAnimator.Play("Summon", 0, 0f);
            else if (step.Kind == RevealStepKind.Reveal) ShowReveal(_entries[step.EntryIndex]);
            else if (step.Kind == RevealStepKind.Total)
            {
                _root.SetActive(false);
                _sequence = null;
                Finished.SafeInvoke();
            }
        }

        private void ShowReveal(GachaPullEntry entry)
        {
            var character = entry.Character;
            _portrait.sprite = character.Portrait;
            _nameText.text = string.IsNullOrEmpty(character.DisplayName) ? character.Id : character.DisplayName;
            _starsText.text = GachaViewText.StarsText(character.Stars);
            // Chạm nhanh liên tiếp: dừng lóe sáng cũ để hai lần không chồng lên nhau.
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(Flash(GachaViewText.RarityColor(character.Rarity)));
        }

        private void ClearFlash()
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = null;
            _revealPanel.transform.localScale = Vector3.one;
            var clear = _flash.color;
            clear.a = 0f;
            _flash.color = clear;
        }

        private IEnumerator Flash(Color color)
        {
            color.a = 0f;
            _flash.color = color;
            var panel = _revealPanel.transform;
            for (var t = 0f; t < _flashSeconds; t += Time.unscaledDeltaTime)
            {
                // Thẻ phóng nhẹ vào trong lúc lóe, rồi lóe sáng mờ dần.
                var pop = Mathf.Clamp01(t / PopSeconds);
                panel.localScale = Vector3.one * Mathf.Lerp(PopFromScale, 1f, 1f - (1f - pop) * (1f - pop));
                color.a = Mathf.Sin(t / _flashSeconds * Mathf.PI) * FlashPeak;
                _flash.color = color;
                yield return null;
            }

            panel.localScale = Vector3.one;
            color.a = 0f;
            _flash.color = color;
        }
    }
}
