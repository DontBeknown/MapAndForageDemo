using System;
using System.Linq;
using MapForageDemo.Core;
using MapForageDemo.Gameplay.Instance;
using MapForageDemo.Gameplay.Level;
using MapForageDemo.UI.Gameplay.Map;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MapForageDemo.Gameplay.Manager
{
    public sealed class MapManager : SceneInstance<MapManager>
    {
        [Serializable]
        private struct MapEntry
        {
            public string AreaId;
            public Texture2D Image;
            public Rect LocalCaptureBounds;
        }

        [SerializeField] private MapEntry[] _maps = Array.Empty<MapEntry>();

        private BreakableObjectManager _forage;
        private InputAction _toggleMap;
        private UIAreaMap _view;
        private static LevelInstance Level => GameplayInstance.Instance.Level;
        private static Transform Player => GameplayInstance.Instance.Player.transform;

        public bool IsOpen => _view != null && _view.gameObject.activeSelf;
        public Area CurrentArea => Level.FindArea(Player.position);

        private int _selectedIndex;
        private Area SelectedArea => Level.Areas[_selectedIndex];

        public Texture2D GetMapImage(string areaId) => _maps.FirstOrDefault(entry => entry.AreaId == areaId).Image;

        public Vector2 Project(Area area, Vector2 worldPosition)
        {
            var entry = _maps.First(item => item.AreaId == area.Id);
            return Rect.PointToNormalized(entry.LocalCaptureBounds, worldPosition - (Vector2)area.transform.position);
        }

        private void Start()
        {
            _toggleMap = InputManager.Instance.InputActions.Map.ToggleAreaMap;
            _forage = BreakableObjectManager.Instance;
            _view = UICanvasGameplayInstance.Instance.AreaMap;
            OnEnable();
        }

        private void OnEnable()
        {
            if (_toggleMap == null)
                return;

            _toggleMap.performed += OnToggleMapHandler;
            _forage.OnForageChanged += RefreshForagePins;
            _view.OnBrowse += Browse;
        }

        private void OnDisable()
        {
            if (_toggleMap == null)
                return;

            _toggleMap.performed -= OnToggleMapHandler;
            _forage.OnForageChanged -= RefreshForagePins;
            if (_view != null)
            {
                _view.OnBrowse -= Browse;
                _view.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (IsOpen)
                RefreshPlayerPin();
        }

        public void ToggleMap()
        {
            if (IsOpen)
            {
                _view.gameObject.SetActive(false);
                return;
            }

            Area current = CurrentArea;
            _selectedIndex = 0;
            for (int index = 0; index < Level.Areas.Count; index++)
            {
                if (Level.Areas[index] == current)
                    _selectedIndex = index;
            }

            _view.gameObject.SetActive(true);
            DisplaySelection();
        }

        internal void Browse(int direction)
        {
            int count = Level.Areas.Count;
            _selectedIndex = (_selectedIndex + direction + count) % count;
            DisplaySelection();
        }

        private void OnToggleMapHandler(InputAction.CallbackContext context) => ToggleMap();

        private void DisplaySelection()
        {
            var area = SelectedArea;
            var points = _forage.GetPoints(area.Id);
            _view.ShowArea(area.DisplayName, GetMapImage(area.Id),
                points.Select(point => Project(area, point.transform.position)).ToArray());
            RefreshForagePins();
            RefreshPlayerPin();
        }

        private void RefreshForagePins()
        {
            if (!IsOpen)
                return;

            Area area = SelectedArea;
            var points = _forage.GetPoints(area.Id);
            for (int index = 0; index < points.Count; index++)
                _view.SetObjectPinVisible(index, _forage.GetState(area.Id, points[index].PointId) == ForageState.Active);
        }

        private void RefreshPlayerPin()
        {
            Area area = SelectedArea;
            _view.SetPlayerPin(CurrentArea == area, Project(area, Player.position));
        }
    }
}
