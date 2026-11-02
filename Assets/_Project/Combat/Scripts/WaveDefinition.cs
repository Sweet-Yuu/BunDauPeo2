using System;
using System.Collections.Generic;
using UnityEngine;

namespace BunDauPeo.Combat
{
    // Thứ tự trong mảng là thứ tự ô của quái.
    [Serializable]
    public class WaveDefinition
    {
        public const int MaxEnemies = 5;

        [SerializeField] private EnemyDefinition[] _enemies = Array.Empty<EnemyDefinition>();

        public IReadOnlyList<EnemyDefinition> Enemies => _enemies;
    }
}
