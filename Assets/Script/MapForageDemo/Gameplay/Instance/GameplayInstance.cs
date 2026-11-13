using MapForageDemo.Core;
using MapForageDemo.Gameplay.Level;
using MapForageDemo.Gameplay.Entity;
using MapForageDemo.Gameplay.Manager;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MapForageDemo.Gameplay.Instance
{
    public sealed class GameplayInstance : SceneInstance<GameplayInstance>
    {
        [SerializeField, Min(0f)] private float _harvestRange = 1.1f;

        public Player Player { get; private set; }
        public LevelInstance Level { get; private set; }

        private InputAction _harvest;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
                return;

            Player = GetComponentInChildren<Player>();
            Level = GetComponentInChildren<LevelInstance>();
        }

        private void Start()
        {
            _harvest = InputManager.Instance.InputActions.Player.Harvest;
            OnEnable();
        }

        private void OnEnable()
        {
            if (_harvest != null)
                _harvest.performed += OnHarvestHandler;
        }

        private void OnDisable()
        {
            if (_harvest != null)
                _harvest.performed -= OnHarvestHandler;
        }

        private void OnHarvestHandler(InputAction.CallbackContext context)
        {
            if (MapManager.Instance.IsOpen)
                return;

            var area = MapManager.Instance.CurrentArea;
            if (area == null)
                return;

            BreakableObject nearest = null;
            float bestDistance = _harvestRange * _harvestRange;
            Vector2 origin = Player.transform.position;
            foreach (var point in BreakableObjectManager.Instance.GetPoints(area.Id))
            {
                if (!point.InteractionCollider.enabled)
                    continue;

                float distance = (point.InteractionCollider.ClosestPoint(origin) - origin).sqrMagnitude;
                if (distance > bestDistance)
                    continue;

                nearest = point;
                bestDistance = distance;
            }

            if (nearest != null)
                nearest.TryHarvest(BreakableObjectManager.Instance, area.Id);
        }
    }
}
