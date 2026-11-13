using MapForageDemo.Core;
using MapForageDemo.UI.Gameplay;
using MapForageDemo.UI.Gameplay.Map;

namespace MapForageDemo.Gameplay.Instance
{
    public sealed class UICanvasGameplayInstance : SceneInstance<UICanvasGameplayInstance>
    {
        public UIAreaMap AreaMap { get; private set; }
        public DemoHUD HUD { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
                return;

            AreaMap = GetComponentInChildren<UIAreaMap>(true);
            HUD = GetComponentInChildren<DemoHUD>(true);
        }
    }
}
