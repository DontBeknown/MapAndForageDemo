using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MapForageDemo.Core;

namespace MapForageDemo.Editor
{
    public static class PrefabUidAssignment
    {
        private static readonly HashSet<MonoBehaviour> _createdObjects = new();
        private static readonly ConcurrentQueue<MonoBehaviour> _validatedObjects = new();

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            ObjectChangeEvents.changesPublished += OnObjectsChangedHandler;
            ObjectFactory.componentWasAdded += OnComponentAddedHandler;
            PrefabStage.prefabStageOpened += OnPrefabStageOpenedHandler;
            PrefabUidValidation.OnValidated += _validatedObjects.Enqueue;
            EditorApplication.update += ReconcileValidatedObjects;
            Undo.undoRedoPerformed += OnUndoRedoPerformedHandler;
        }

        public static void ReconcileAfterInspectorEdit(IEnumerable<UnityEngine.Object> targets)
        {
            var stage = GetEditableLevelStage();
            foreach (var target in targets)
            {
                if (target is MonoBehaviour component)
                    ReconcileUid(component, stage, recordUndo: true);
            }
        }

        private static void OnPrefabStageOpenedHandler(PrefabStage stage)
        {
            if (!IsLevelStage(stage))
                return;

            foreach (var owner in stage.prefabContentsRoot.GetComponentsInChildren<IPrefabUidOwner>(true))
            {
                if (owner is MonoBehaviour component)
                    _validatedObjects.Enqueue(component);
            }
        }

        private static void OnUndoRedoPerformedHandler()
        {
            // Keep validation triggered by Undo from overwriting the restored UID.
            _validatedObjects.Clear();
        }

        private static void OnObjectsChangedHandler(ref ObjectChangeEventStream stream)
        {
            var stage = GetEditableLevelStage();
            if (stage == null)
                return;

            for (int i = 0; i < stream.length; i++)
            {
                if (stream.GetEventType(i) != ObjectChangeKind.CreateGameObjectHierarchy)
                    continue;

                stream.GetCreateGameObjectHierarchyEvent(i, out var change);
                var created = EditorUtility.EntityIdToObject((EntityId)change.instanceId) as GameObject;
                if (created == null || !stage.IsPartOfPrefabContents(created))
                    continue;

                foreach (var owner in created.GetComponentsInChildren<IPrefabUidOwner>(true))
                {
                    if (owner is MonoBehaviour component)
                        QueueCreatedObject(component);
                }
            }
        }

        private static void OnComponentAddedHandler(Component component)
        {
            if (component is MonoBehaviour behaviour && behaviour is IPrefabUidOwner &&
                GetEditableLevelStage()?.IsPartOfPrefabContents(behaviour.gameObject) == true)
                QueueCreatedObject(behaviour);
        }

        private static void ReconcileValidatedObjects()
        {
            if (_validatedObjects.IsEmpty)
                return;

            var stage = GetEditableLevelStage();
            while (_validatedObjects.TryDequeue(out var component))
            {
                if (!_createdObjects.Contains(component))
                    ReconcileUid(component, stage);
            }
        }

        private static void QueueCreatedObject(MonoBehaviour component)
        {
            if (_createdObjects.Add(component) && _createdObjects.Count == 1)
                EditorApplication.delayCall += ReconcileCreatedObjects;
        }

        private static void ReconcileCreatedObjects()
        {
            var created = new List<MonoBehaviour>(_createdObjects);
            _createdObjects.Clear();
            var stage = GetEditableLevelStage();
            foreach (var component in created)
                ReconcileUid(component, stage, isNew: true);
        }

        private static PrefabStage GetEditableLevelStage()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Undo.isProcessing)
                return null;

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            return stage != null && IsLevelStage(stage) ? stage : null;
        }

        private static bool IsLevelStage(PrefabStage stage)
        {
            return stage.assetPath.Equals("Assets/Prefab/Gameplay/Level/LevelInstance.prefab", StringComparison.Ordinal) ||
                   stage.assetPath.StartsWith("Assets/Prefab/Level/", StringComparison.Ordinal) ||
                   stage.assetPath.StartsWith("Assets/Prefab/LevelSeason/", StringComparison.Ordinal);
        }

        private static void ReconcileUid(MonoBehaviour component, PrefabStage stage, bool isNew = false,
            bool recordUndo = false)
        {
            if (stage == null || component == null || component is not IPrefabUidOwner owner ||
                !stage.IsPartOfPrefabContents(component.gameObject))
                return;

            using var serializedObject = new SerializedObject(component);
            using var uid = serializedObject.FindProperty(owner.UidPropertyPath);
            if (uid == null || uid.propertyType != SerializedPropertyType.String)
            {
                Debug.LogError($"UID property '{owner.UidPropertyPath}' must be a serialized string.", component);
                return;
            }

            bool requiresUid = owner.RequiresUid;
            bool hasUid = !string.IsNullOrEmpty(uid.stringValue);
            if (requiresUid == hasUid && !(requiresUid && isNew))
                return;

            uid.stringValue = requiresUid ? Guid.NewGuid().ToString("N") : string.Empty;
            if (recordUndo)
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(component))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);

            EditorUtility.SetDirty(component);
            EditorSceneManager.MarkSceneDirty(stage.scene);
        }
    }
}
