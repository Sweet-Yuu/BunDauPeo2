using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.Missions
{
    // Thứ tự mảng là thứ tự hiển thị trong từng tab (spec đợt 3 mục 7).
    [CreateAssetMenu(menuName = "BunDauPeo/Missions/Mission Catalog")]
    public class MissionCatalog : Catalog<MissionDefinition>
    {
    }
}
