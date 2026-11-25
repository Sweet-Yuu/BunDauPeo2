using System;
using BunDauPeo.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Một dòng nhiệm vụ (spec đợt 3 8.1). Chỉ vẽ; bấm nút thì báo ra ngoài kèm id và trạng thái.
    public class MissionRowView : MonoBehaviour
    {
        private static readonly Color ClaimColor = new(0.20f, 0.65f, 0.35f);
        private static readonly Color GotoColor = new(0.92f, 0.93f, 0.96f);
        private static readonly Color DisabledColor = new(0.55f, 0.57f, 0.63f);

        [SerializeField] private Text _tagText;
        [SerializeField] private Text _titleText;
        [SerializeField] private RectTransform _barFill;
        [SerializeField] private Text _progressText;
        [SerializeField] private Text _rewardText;
        [SerializeField] private Button _button;
        [SerializeField] private Text _buttonText;
        [SerializeField] private GameObject _lock;

        public string Id { get; private set; } = "";
        public MissionButtonState State { get; private set; }
        public Button Button => _button;

        public event Action<MissionRowView> Clicked;

        private void OnEnable() => _button.onClick.AddListener(OnClick);

        private void OnDisable() => _button.onClick.RemoveListener(OnClick);

        private void OnClick() => Clicked?.Invoke(this);

        public void Bind(MissionRow row, MissionData def)
        {
            Id = row.Id;
            State = MissionRowRule.State(row);
            _tagText.text = UiText.Get(def.Tab == 0 ? "missions.tab_daily" : "missions.tab_newbie");
            _titleText.text = UiText.Get("missions." + row.Id);
            var fraction = row.Target > 0 ? Mathf.Clamp01((float)row.Progress / row.Target) : 0f;
            _barFill.anchorMax = new Vector2(fraction, 1f);
            _progressText.text = row.Locked ? "" : $"{row.Progress}/{row.Target}";
            _rewardText.text = RewardText(def);
            _lock.SetActive(State == MissionButtonState.Locked);

            _button.interactable = State == MissionButtonState.Goto || State == MissionButtonState.Claim;
            _button.targetGraphic.color = State == MissionButtonState.Claim ? ClaimColor : State == MissionButtonState.Goto ? GotoColor : DisabledColor;
            _buttonText.color = State == MissionButtonState.Goto ? new Color(0.2f, 0.22f, 0.3f) : Color.white;
            _buttonText.text = UiText.Get(State switch
            {
                MissionButtonState.Goto => "missions.goto",
                MissionButtonState.Claim => "missions.claim",
                MissionButtonState.Claimed => "missions.claimed",
                MissionButtonState.NotYet => "missions.not_yet",
                MissionButtonState.Missed => "missions.missed",
                _ => "missions.locked",
            });
            // Thể lực theo khung giờ không có nơi để "Đi tới": chỉ Nhận hoặc mờ kèm lý do.
            if (State == MissionButtonState.Goto && MissionGoto.Destination(row.Id) == HubGoto.None) _button.gameObject.SetActive(false);
            else _button.gameObject.SetActive(true);
        }

        private static string RewardText(MissionData def)
        {
            if (def.RewardGem > 0 && def.RewardStamina > 0)
                return UiText.Format("missions.gem", def.RewardGem) + "\n" + UiText.Format("missions.stamina", def.RewardStamina);
            if (def.RewardStamina > 0) return UiText.Format("missions.stamina", def.RewardStamina);
            return def.RewardGem > 0 ? UiText.Format("missions.gem", def.RewardGem) : "";
        }
    }
}
