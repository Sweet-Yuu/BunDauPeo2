using System.Collections.Generic;
using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.Missions
{
    // Giá trị số lưu trong asset và gửi sang server (MissionData.Tab/Counter): chỉ thêm ở cuối.
    public enum MissionTab { Daily = 0, Newbie = 1 }

    public enum MissionCounter
    {
        Wins = 0, Pulls = 1, LoginWindow = 2, BattleStagesCleared = 3, GachaEver = 4, StarUpEver = 5, ClientFlag = 6, None = 7,
    }

    // Số khởi điểm theo spec đợt 3 mục 7; MissionCatalog giữ thứ tự. Chữ hiển thị nằm trong UiText theo Id, không ở đây.
    [CreateAssetMenu(menuName = "BunDauPeo/Missions/Mission")]
    public class MissionDefinition : GameDefinition
    {
        [SerializeField] private MissionTab _tab;
        [SerializeField] private MissionCounter _counter;
        [SerializeField] private int _target = 1;
        [SerializeField] private bool _locked;
        [SerializeField] private long _rewardGem;
        [SerializeField] private int _rewardStamina;
        // Giờ VN [start, end); cả hai 0 = không có khung giờ.
        [SerializeField] private int _windowStartHour;
        [SerializeField] private int _windowEndHour;

        public MissionTab Tab => _tab;
        public MissionCounter Counter => _counter;
        public int Target => _target;
        public bool Locked => _locked;
        public long RewardGem => _rewardGem;
        public int RewardStamina => _rewardStamina;
        public int WindowStartHour => _windowStartHour;
        public int WindowEndHour => _windowEndHour;

        // Mỗi lỗi bắt đầu bằng tên field.
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Id)) errors.Add("Id: must not be blank");
            if (_rewardGem < 0) errors.Add("RewardGem: must be >= 0");
            if (_rewardStamina < 0) errors.Add("RewardStamina: must be >= 0");
            if (!_locked && _target <= 0) errors.Add("Target: an unlocked mission needs >= 1");
            if (_windowStartHour < 0 || _windowEndHour > 24 || _windowEndHour < _windowStartHour)
                errors.Add("Window: hours must satisfy 0 <= start <= end <= 24");
            return errors;
        }
    }
}
