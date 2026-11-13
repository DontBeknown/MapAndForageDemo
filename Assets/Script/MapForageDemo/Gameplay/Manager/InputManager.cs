using MapForageDemo.Core;
using MapForageDemo.Core.Input;
using UnityEngine;

namespace MapForageDemo.Gameplay.Manager
{
    public sealed class InputManager : SceneInstance<InputManager>
    {
        public DemoInputActions InputActions { get; private set; }
        public Vector2 Movement => Vector2.ClampMagnitude(InputActions.Player.Move.ReadValue<Vector2>(), 1f);

        protected override void Awake()
        {
            base.Awake();
            if (Instance == this)
                InputActions = new DemoInputActions();
        }

        private void OnEnable() => InputActions?.Enable();
        private void OnDisable() => InputActions?.Disable();

        protected override void OnDestroy()
        {
            InputActions?.Dispose();
            base.OnDestroy();
        }
    }
}
