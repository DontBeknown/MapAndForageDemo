using MapForageDemo.Gameplay.Manager;
using UnityEngine;
#if UNITY_EDITOR
using MapForageDemo.Core;
#endif

namespace MapForageDemo.Gameplay.Entity
{
    public sealed class BreakableObject : MonoBehaviour
#if UNITY_EDITOR
        , IPrefabUidOwner
#endif
    {
        [SerializeField] private string _pointId;
        [SerializeField] private Collider2D _interactionCollider;
        [SerializeField] private GameObject _activeVisual;
        [SerializeField] private GameObject _brokenVisual;

        public string PointId => _pointId;
        public Collider2D InteractionCollider => _interactionCollider;

#if UNITY_EDITOR
        bool IPrefabUidOwner.RequiresUid => true;
        string IPrefabUidOwner.UidPropertyPath => nameof(_pointId);

        private void OnValidate() => PrefabUidValidation.Notify(this);
#endif

        public bool TryHarvest(BreakableObjectManager manager, string areaId) =>
            manager.TryHarvest(areaId, _pointId);

        internal void Apply(ForageState state)
        {
            _interactionCollider.enabled = state == ForageState.Active;
            _activeVisual.SetActive(state == ForageState.Active);
            _brokenVisual.SetActive(state == ForageState.Broken);
        }
    }
}
