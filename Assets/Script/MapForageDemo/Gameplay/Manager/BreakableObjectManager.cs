using MapForageDemo.Core;
using MapForageDemo.Gameplay.Instance;
using MapForageDemo.Gameplay.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using MapForageDemo.Gameplay.Level;
using UnityEngine;

namespace MapForageDemo.Gameplay.Manager
{
    public enum ForageState { Inactive, Active, Broken }

    [Serializable]
    internal sealed class ForageRecord
    {
        public string AreaId;
        public string PointId;
        public ForageState State;
        public int StateDay;
        public int EligibleDay;
    }

    public sealed class BreakableObjectManager : SceneInstance<BreakableObjectManager>
    {
        [SerializeField] private int _demoSeed = 1729;

        public int Seed { get; private set; }
        public event Action OnForageChanged;

        private readonly Dictionary<(string Area, string Point), ForageRecord> _records = new();
        private readonly Dictionary<string, BreakableObject[]> _pointsByArea = new(StringComparer.Ordinal);
        private int _day;

        private const int _AREA_CAP = 2;

        private TimeHopManager _time;
        private static LevelInstance Level => GameplayInstance.Instance.Level;

        private void Start()
        {
            _time = TimeHopManager.Instance;
            OnEnable();
            ResetForage();
        }

        private void OnEnable()
        {
            if (_time != null)
                _time.OnDayChanged += AdvanceToDay;
        }

        private void OnDisable()
        {
            if (_time != null)
                _time.OnDayChanged -= AdvanceToDay;
        }

        internal void ResetForage()
        {
            _records.Clear();
            _pointsByArea.Clear();
            Seed = _demoSeed;
            _day = 0;
            var areaIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var area in Level.Areas)
            {
                if (string.IsNullOrWhiteSpace(area.Id) || !areaIds.Add(area.Id))
                    throw new InvalidOperationException($"Missing or duplicate area ID on {area.name}.");

                var points = FindPoints(area);
                _pointsByArea.Add(area.Id, points);
                foreach (var point in points)
                {
                    if (string.IsNullOrWhiteSpace(point.PointId) || !_records.TryAdd((area.Id, point.PointId),
                        new ForageRecord { AreaId = area.Id, PointId = point.PointId }))
                        throw new InvalidOperationException($"Missing or duplicate point ID on {area.name}/{point.name}.");
                }
            }

            AdvanceToDay(1);
        }

        public static BreakableObject[] FindPoints(Area area) => area.GetComponentsInChildren<BreakableObject>(true)
            .Where(point => point.GetComponentInParent<Area>(true) == area).ToArray();

        public IReadOnlyList<BreakableObject> GetPoints(string areaId) => _pointsByArea[areaId];

        public ForageState GetState(string areaId, string pointId) => _records[(areaId, pointId)].State;

        public bool TryHarvest(string areaId, string pointId)
        {
            if (!_records.TryGetValue((areaId, pointId), out var record) || record.State != ForageState.Active)
                return false;

            record.State = ForageState.Broken;
            record.StateDay = _day;
            record.EligibleDay = _day + 2;
            RefreshViews();
            return true;
        }

        public void AdvanceToDay(int day)
        {
            if (day <= _day)
                return;

            for (int nextDay = _day + 1; nextDay <= day; nextDay++)
            {
                foreach (var area in Level.Areas)
                    AdvanceArea(area.Id, nextDay);
            }

            _day = day;
            RefreshViews();
        }

        internal ForageRecord[] Capture() => _records.Values.Select(record => new ForageRecord
        {
            AreaId = record.AreaId,
            PointId = record.PointId,
            State = record.State,
            StateDay = record.StateDay,
            EligibleDay = record.EligibleDay
        }).ToArray();

        internal bool IsValid(int day, ForageRecord[] records)
        {
            if (records == null || records.Length != _records.Count)
                return false;

            var keys = new HashSet<(string, string)>();
            foreach (var record in records)
            {
                if (record == null || !_records.ContainsKey((record.AreaId, record.PointId)) ||
                    !keys.Add((record.AreaId, record.PointId)) || record.StateDay < 0 || record.StateDay > day)
                    return false;

                bool validState = record.State switch
                {
                    ForageState.Active => record.StateDay > 0 && day - record.StateDay < 3 && record.EligibleDay == 0,
                    ForageState.Broken => record.StateDay == day && record.EligibleDay == day + 2,
                    ForageState.Inactive => record.EligibleDay >= 0 && record.EligibleDay <= day + 1,
                    _ => false
                };
                if (!validState)
                    return false;
            }

            return records.Where(record => record.State == ForageState.Active)
                .GroupBy(record => record.AreaId).All(group => group.Count() <= _AREA_CAP);
        }

        internal void Restore(int day, int seed, ForageRecord[] records)
        {
            _records.Clear();
            foreach (var record in records)
                _records.Add((record.AreaId, record.PointId), record);

            Seed = seed;
            _day = day;
            RefreshViews();
        }

        private void AdvanceArea(string areaId, int day)
        {
            var records = _records.Values.Where(record => record.AreaId == areaId).ToArray();
            foreach (var record in records)
            {
                if (record.State == ForageState.Broken)
                {
                    record.State = ForageState.Inactive;
                    record.StateDay = day;
                }
                else if (record.State == ForageState.Active && day - record.StateDay >= 3)
                {
                    record.State = ForageState.Inactive;
                    record.StateDay = day;
                    record.EligibleDay = day + 1;
                }
            }

            int slots = _AREA_CAP - records.Count(record => record.State == ForageState.Active);
            foreach (var record in records.Where(record => record.State == ForageState.Inactive && day >= record.EligibleDay)
                .OrderBy(record => SelectionScore(record, day))
                .ThenBy(record => record.PointId, StringComparer.Ordinal).Take(slots))
            {
                record.State = ForageState.Active;
                record.StateDay = day;
                record.EligibleDay = 0;
            }
        }

        private uint SelectionScore(ForageRecord record, int day)
        {
            unchecked
            {
                uint hash = (uint)Seed * 397u + (uint)day;
                foreach (char character in record.AreaId + "/" + record.PointId)
                    hash = (hash ^ character) * 16777619u;

                return hash ^ (hash >> 16);
            }
        }

        private void RefreshViews()
        {
            foreach (var area in Level.Areas)
            {
                foreach (var point in GetPoints(area.Id))
                    point.Apply(GetState(area.Id, point.PointId));
            }

            OnForageChanged?.Invoke();
        }
    }
}
