using MapForageDemo.Core;
using System;
using UnityEngine;

namespace MapForageDemo.Gameplay.Manager
{
    public sealed class TimeHopManager : SceneInstance<TimeHopManager>
    {
        public const int MAX_DAY = 1000000;
        public int Day { get; private set; } = 1;
        public event Action<int> OnDayChanged;

        public void AdvanceDay()
        {
            if (Day < MAX_DAY)
                SetDay(Day + 1);
        }

        internal void SetDay(int day)
        {
            Day = day;
            OnDayChanged?.Invoke(day);
        }
    }
}
