using System;
using System.Collections.Generic;
using BunDauPeo.Core;
using UnityEngine;

namespace Hoang.Remake
{
    public enum SkillUpgradeStatus { Success, UnknownSkill, Locked, MaxLevel, NotEnoughPoints, SaveFailed, BalanceLimit }

    public sealed class SkillTreeManager : IDisposable
    {
        private readonly RemakeContext _context;
        private readonly SkillSO[] _skills;
        private readonly Dictionary<string, SkillSO> _byId = new(StringComparer.Ordinal);
        private bool _disposed;

        public SkillTreeManager(RemakeContext context, SkillSO[] skills)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _skills = skills == null ? Array.Empty<SkillSO>() : (SkillSO[])skills.Clone();
            foreach (var skill in _skills)
            {
                if (skill == null || skill.Validate().Count > 0)
                    throw new ArgumentException("Skill configuration is invalid.", nameof(skills));
                if (!_byId.TryAdd(skill.Id, skill))
                    throw new ArgumentException("Skill IDs must be unique.", nameof(skills));
            }
            foreach (var skill in _skills) CheckGraph(skill, new HashSet<string>(), new HashSet<string>());
            _context.Changed += OnContextChanged;
        }

        public IReadOnlyList<SkillSO> Skills => _skills;
        public int AvailablePoints => Math.Max(0, _context.Save.Skills?.AvailablePoints ?? 0);
        public int AvailableSpecialPoints => Math.Max(0, _context.Save.Skills?.AvailableSpecialPoints ?? 0);
        public event Action Changed;

        public int GetLevel(string id)
        {
            if (id == null || !_byId.TryGetValue(id, out var skill)) return 0;
            var level = 0;
            foreach (var entry in _context.Save.Skills?.Slots ?? new List<SkillSlotSaveEntry>())
                if (entry != null && entry.SkillId == id) level = Math.Max(level, entry.CurrentLevel);
            return Mathf.Clamp(level, 0, skill.MaxLevel);
        }

        public bool IsUnlocked(SkillSO skill)
        {
            if (skill == null || !_byId.ContainsKey(skill.Id)) return false;
            foreach (var prerequisite in skill.Prerequisites ?? Array.Empty<SkillPrerequisite>())
                if (prerequisite == null || prerequisite.Skill == null
                    || GetLevel(prerequisite.Skill.Id) < prerequisite.MinimumLevel) return false;
            return true;
        }

        public SkillUpgradeStatus TryUpgrade(string id)
        {
            if (_disposed) return SkillUpgradeStatus.UnknownSkill;
            if (id == null || !_byId.TryGetValue(id, out var skill)) return SkillUpgradeStatus.UnknownSkill;
            if (!IsUnlocked(skill)) return SkillUpgradeStatus.Locked;
            var level = GetLevel(id);
            if (level >= skill.MaxLevel) return SkillUpgradeStatus.MaxLevel;
            var normal = skill.IsExclusive ? 0 : skill.NormalPointCost;
            var special = skill.IsExclusive ? skill.SpecialPointCost : 0;
            if (normal < 1 && special < 1) return SkillUpgradeStatus.NotEnoughPoints;
            if (AvailablePoints < normal || AvailableSpecialPoints < special) return SkillUpgradeStatus.NotEnoughPoints;
            var previous = FindEntry(_context.Save.Skills, id);
            if ((long)(previous?.NormalPointsSpent ?? 0) + normal > int.MaxValue
                || (long)(previous?.SpecialPointsSpent ?? 0) + special > int.MaxValue)
                return SkillUpgradeStatus.BalanceLimit;
            return _context.Commit(proposed =>
            {
                proposed.Skills ??= new SkillTreeSaveState();
                proposed.Skills.Slots ??= new List<SkillSlotSaveEntry>();
                var entry = FindEntry(proposed.Skills, id);
                if (entry == null)
                {
                    entry = new SkillSlotSaveEntry { SkillId = id };
                    proposed.Skills.Slots.Add(entry);
                }
                entry.SkillName = skill.SkillName;
                entry.CurrentLevel = level + 1;
                entry.IsUnlocked = true;
                entry.NormalPointsSpent += normal;
                entry.SpecialPointsSpent += special;
                proposed.Skills.AvailablePoints -= normal;
                proposed.Skills.AvailableSpecialPoints -= special;
            }) ? SkillUpgradeStatus.Success : SkillUpgradeStatus.SaveFailed;
        }

        public SkillUpgradeStatus Upgrade(string id) => TryUpgrade(id);

        public bool ResetSkills()
        {
            if (_disposed) return false;
            long normal = AvailablePoints;
            long special = AvailableSpecialPoints;
            var slots = _context.Save.Skills?.Slots;
            if (slots == null || slots.Count == 0) return true;
            foreach (var entry in slots)
            {
                if (entry == null) continue;
                normal += Math.Max(0, entry.NormalPointsSpent);
                special += Math.Max(0, entry.SpecialPointsSpent);
            }
            if (normal > int.MaxValue || special > int.MaxValue) return false;
            return _context.Commit(proposed =>
            {
                proposed.Skills.AvailablePoints = (int)normal;
                proposed.Skills.AvailableSpecialPoints = (int)special;
                proposed.Skills.Slots.Clear();
            });
        }

        public bool AddPoints(int normal, int special = 0)
        {
            if (_disposed || normal < 0 || special < 0 || (long)AvailablePoints + normal > int.MaxValue
                || (long)AvailableSpecialPoints + special > int.MaxValue) return false;
            if (normal == 0 && special == 0) return true;
            return _context.Commit(proposed =>
            {
                proposed.Skills ??= new SkillTreeSaveState();
                proposed.Skills.AvailablePoints = AvailablePoints + normal;
                proposed.Skills.AvailableSpecialPoints = AvailableSpecialPoints + special;
            });
        }

        public float GetTotalProductionSpeedBonus() => Sum(s => s.ProductionSpeedBonusPerLevel);
        public float GetTotalRevenueBonus() => Sum(s => s.RevenueBonusPerLevel);
        public float GetTotalCustomerPatienceBonus() => Sum(s => s.CustomerPatienceBonusPerLevel);
        public float GetTotalCostReduction() => Sum(s => s.IngredientCostReductionPerLevel);
        public int GetTotalCleanlinessBonus()
        {
            long total = 0;
            foreach (var skill in _skills) total = Math.Min(int.MaxValue, total + (long)GetLevel(skill.Id) * skill.CleanlinessBonusPerLevel);
            return (int)total;
        }

        public void Dispose()
        {
            _disposed = true;
            _context.Changed -= OnContextChanged;
        }

        private float Sum(Func<SkillSO, float> bonus)
        {
            double total = 0;
            foreach (var skill in _skills) total += (double)GetLevel(skill.Id) * bonus(skill);
            return (float)Math.Min(float.MaxValue, total);
        }

        private static SkillSlotSaveEntry FindEntry(SkillTreeSaveState state, string id) =>
            state?.Slots?.Find(entry => entry != null && entry.SkillId == id);

        private void CheckGraph(SkillSO skill, HashSet<string> visiting, HashSet<string> visited)
        {
            if (visited.Contains(skill.Id)) return;
            if (!visiting.Add(skill.Id)) throw new ArgumentException("Skill prerequisites contain a cycle.");
            foreach (var prerequisite in skill.Prerequisites ?? Array.Empty<SkillPrerequisite>())
            {
                if (!_byId.TryGetValue(prerequisite.Skill.Id, out var known) || known != prerequisite.Skill)
                    throw new ArgumentException("Prerequisite is outside the skill catalog.");
                CheckGraph(prerequisite.Skill, visiting, visited);
            }
            visiting.Remove(skill.Id);
            visited.Add(skill.Id);
        }

        private void OnContextChanged() => Changed.SafeInvoke();
    }
}
