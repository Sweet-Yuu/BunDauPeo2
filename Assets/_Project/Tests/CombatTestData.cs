using System;
using BunDauPeo.Combat;
using UnityEngine;

namespace BunDauPeo.Tests
{
    // Số khởi điểm của CombatConfig lấy từ field initializer, đúng bảng spec 3.4; edit chỉ ghi đè cái test cần.
    internal static class CombatTestData
    {
        public static CombatConfig Config(Action<UnityEditor.SerializedObject> edit = null)
        {
            var config = ScriptableObject.CreateInstance<CombatConfig>();
            if (edit != null) TestData.Edit(config, edit);
            return config;
        }
    }
}
