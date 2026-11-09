using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hoang.Remake
{
    [CreateAssetMenu(fileName = "Facility", menuName = "Hoang/Remake/Facility")]
    public sealed class FacilityItemSO : ScriptableObject
    {
        public string Id;
        public string ItemName = "Facility";
        public string ShortName;
        [TextArea] public string Description;
        public Color ThemeColor = new(0.25f, 0.6f, 0.75f, 1f);
        public Sprite Icon;
        public List<Vector2Int> ShapeCoords = new() { Vector2Int.zero };
        public bool IsPowerSource;
        public bool CanConductPower = true;
        public bool IsRelay;
        public bool RequiresRelay;
        [Min(0)] public int PowerOutput;
        [Min(0)] public int PowerConsumption;
        public FacilityStats Stats;

        public string DisplayName => string.IsNullOrWhiteSpace(ShortName) ? ItemName : ShortName;

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Id)) errors.Add("Id is required.");
            if (ShapeCoords == null || ShapeCoords.Count == 0) errors.Add("Shape must contain at least one cell.");
            else
            {
                if (new HashSet<Vector2Int>(ShapeCoords).Count != ShapeCoords.Count) errors.Add("Shape contains duplicate cells.");
                var min = ShapeCoords[0];
                var max = min;
                foreach (var cell in ShapeCoords)
                {
                    min = Vector2Int.Min(min, cell);
                    max = Vector2Int.Max(max, cell);
                }

                if ((long)max.x - min.x >= FacilityGrid.GridSize || (long)max.y - min.y >= FacilityGrid.GridSize)
                    errors.Add("Shape must fit the 11x11 grid.");
            }
            if (PowerOutput < 0 || PowerConsumption < 0) errors.Add("Power values cannot be negative.");
            if (!FiniteNonNegative(Stats.ProductionSpeedBonus) || !FiniteNonNegative(Stats.RevenueBonus)
                || !FiniteNonNegative(Stats.CustomerPatienceBonus) || !FiniteNonNegative(Stats.IngredientCostReduction)
                || Stats.CleanlinessBonus < 0) errors.Add("Buff values must be finite and nonnegative.");
            return errors;
        }

        public List<Vector2Int> GetRotatedCoords(int rotationIndex)
        {
            var result = new List<Vector2Int>();
            if (ShapeCoords == null || ShapeCoords.Count == 0) return result;
            var transformed = new List<(long X, long Y)>();
            var rotation = (rotationIndex % 4 + 4) % 4;
            long minX = long.MaxValue;
            long minY = long.MaxValue;
            foreach (var cell in ShapeCoords)
            {
                long x = cell.x;
                long y = cell.y;
                for (var turn = 0; turn < rotation; turn++) (x, y) = (y, -x);
                transformed.Add((x, y));
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
            }

            foreach (var cell in transformed)
            {
                if (cell.X - minX > int.MaxValue || cell.Y - minY > int.MaxValue) return new List<Vector2Int>();
                result.Add(new Vector2Int((int)(cell.X - minX), (int)(cell.Y - minY)));
            }
            return result;
        }

        public Vector2Int GetDimensions(int rotationIndex)
        {
            var max = new Vector2Int(-1, -1);
            foreach (var cell in GetRotatedCoords(rotationIndex)) max = Vector2Int.Max(max, cell);
            return max + Vector2Int.one;
        }

        private static bool FiniteNonNegative(float value) => value >= 0 && !float.IsInfinity(value) && !float.IsNaN(value);
    }
}
