#if UNITY_EDITOR
using System;
using UnityEngine;

namespace MapForageDemo.Core
{
    public interface IPrefabUidOwner
    {
        public bool RequiresUid { get; }
        public string UidPropertyPath { get; }
    }

    public static class PrefabUidValidation
    {
        public static event Action<MonoBehaviour> OnValidated;

        public static void Notify(MonoBehaviour component)
        {
            OnValidated?.Invoke(component);
        }
    }
}
#endif
