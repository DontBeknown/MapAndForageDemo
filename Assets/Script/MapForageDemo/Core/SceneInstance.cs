using UnityEngine;

namespace MapForageDemo.Core
{
    public abstract class SceneInstance<T> : MonoBehaviour where T : SceneInstance<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"Duplicate {typeof(T).Name} in the scene.", this);
                enabled = false;
                Destroy(this);
                return;
            }

            Instance = (T)this;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
