using MapForageDemo.Gameplay.Instance;
using MapForageDemo.Gameplay.Manager;
using UnityEngine;

namespace MapForageDemo.Gameplay.Entity
{
    public sealed class Player : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _speed = 5f;

        private void Update()
        {
            if (MapManager.Instance.IsOpen)
                return;

            Vector2 next = (Vector2)transform.position + InputManager.Instance.Movement * (_speed * Time.deltaTime);
            transform.position = GameplayInstance.Instance.Level.Constrain(next);
        }
    }
}
