using MapForageDemo.Gameplay.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace MapForageDemo.UI.Gameplay
{
    public sealed class DemoHUD : MonoBehaviour
    {
        [SerializeField] private Text _day;
        [SerializeField] private Text _status;
        [SerializeField] private Button _openMap;
        [SerializeField] private Button _advanceDay;
        [SerializeField] private Button _save;
        [SerializeField] private Button _load;
        [SerializeField] private Button _startOver;

        private TimeHopManager _time;
        private UserDataManager _userData;
        private MapManager _map;

        private void Start()
        {
            _time = TimeHopManager.Instance;
            _userData = UserDataManager.Instance;
            _map = MapManager.Instance;
            OnEnable();
        }

        private void OnEnable()
        {
            if (_time == null)
                return;

            _time.OnDayChanged += OnDayChangedHandler;
            _userData.OnStatusChanged += OnStatusChangedHandler;
            _openMap.onClick.AddListener(_map.ToggleMap);
            _advanceDay.onClick.AddListener(_time.AdvanceDay);
            _save.onClick.AddListener(_userData.Save);
            _load.onClick.AddListener(_userData.Load);
            _startOver.onClick.AddListener(_userData.StartOver);
            OnDayChangedHandler(_time.Day);
        }

        private void OnDisable()
        {
            if (_time == null)
                return;

            _time.OnDayChanged -= OnDayChangedHandler;
            _userData.OnStatusChanged -= OnStatusChangedHandler;
            _openMap.onClick.RemoveListener(_map.ToggleMap);
            _advanceDay.onClick.RemoveListener(_time.AdvanceDay);
            _save.onClick.RemoveListener(_userData.Save);
            _load.onClick.RemoveListener(_userData.Load);
            _startOver.onClick.RemoveListener(_userData.StartOver);
        }

        private void OnDayChangedHandler(int day) => _day.text = $"Day {day}";
        private void OnStatusChangedHandler(string status) => _status.text = status;
    }
}
