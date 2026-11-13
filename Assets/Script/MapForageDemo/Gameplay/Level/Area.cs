using UnityEngine;

namespace MapForageDemo.Gameplay.Level
{
    public sealed class Area : MonoBehaviour
    {
        [SerializeField] private string _areaId;
        [SerializeField] private string _displayName;
        [SerializeField] private Rect _localBounds;

        public string Id => _areaId;
        public string DisplayName => _displayName;
        public Rect WorldBounds => new Rect(_localBounds.position + (Vector2)transform.position, _localBounds.size);
    }
}
