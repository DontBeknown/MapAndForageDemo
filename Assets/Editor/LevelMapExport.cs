using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapForageDemo.Gameplay.Entity;
using MapForageDemo.Gameplay.Level;
using MapForageDemo.Gameplay.Manager;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

[CustomEditor(typeof(MapManager))]
internal sealed class MapManagerInspector : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        var button = new Button(LevelMapExport.GenerateAll) { text = "Generate All Level Maps" };
        button.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
        UnityEditor.UIElements.InspectorElement.FillDefaultInspector(root, serializedObject, this);
        root.Add(button);
        return root;
    }
}

public static class LevelMapExport
{
    internal const string GroupName = "PrefabLevel";
    private const string OutputFolder = "Assets/Art/MapForage/Generated";
    private const string MapManagerPrefab = "Assets/Prefab/Manager/MapManager.prefab";
    private const string DemoLevel = "Assets/Prefab/Gameplay/Level/LevelInstance.prefab";

    [MenuItem("Tools/Map and Forage/Register Demo Level")]
    public static void RegisterDemoLevel()
    {
        var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        var group = settings.FindGroup(GroupName) ?? settings.CreateGroup(GroupName, false, false, true,
            null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(DemoLevel), group).SetAddress("DemoLevel");
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }

    public static void GenerateAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var group = AddressableAssetSettingsDefaultObject.Settings?.FindGroup(GroupName);
        if (group == null)
        {
            Debug.LogError($"Map export requires the {GroupName} Addressables group.");
            return;
        }

        if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            Debug.LogError("Close Prefab Mode before generating maps so prefab edits are saved first.");
            return;
        }

        var manager = AssetDatabase.LoadAssetAtPath<GameObject>(MapManagerPrefab).GetComponent<MapManager>();
        using var serialized = new SerializedObject(manager);
        var maps = serialized.FindProperty("_maps");
        var exportedIds = new HashSet<string>(StringComparer.Ordinal);
        Directory.CreateDirectory(OutputFolder);
        foreach (var entry in group.entries.Where(entry => entry.AssetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                ExportLevel(entry.AssetPath, entry.guid, maps, exportedIds);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Map export failed for {entry.address}: {exception.Message}");
            }
        }
        serialized.ApplyModifiedProperties();
        AssetDatabase.SaveAssetIfDirty(manager);
    }

    private static void ExportLevel(string path, string guid, SerializedProperty maps, HashSet<string> exportedIds)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var level = root.GetComponentInChildren<LevelInstance>(true);
            if (level == null)
                return;

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var area in level.Areas)
            {
                if (area == null || string.IsNullOrWhiteSpace(area.Id) ||
                    area.Id.Any(character => !(character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9') && character != '-' && character != '_') ||
                    !ids.Add(area.Id))
                    throw new InvalidOperationException("Every area needs a unique ID containing letters, digits, hyphens or underscores.");

                if (exportedIds.Contains(area.Id))
                    throw new InvalidOperationException($"Area ID '{area.Id}' is already used by another exported level.");

                Rect bounds = area.WorldBounds;
                if (!float.IsFinite(bounds.xMin) || !float.IsFinite(bounds.yMin) ||
                    !float.IsFinite(bounds.xMax) || !float.IsFinite(bounds.yMax) ||
                    bounds.width <= 0 || bounds.height <= 0)
                    throw new InvalidOperationException($"{area.name} has invalid capture bounds.");
            }

            exportedIds.UnionWith(ids);
            foreach (var area in level.Areas)
            {
                try
                {
                    var image = ExportArea(level, area, guid);
                    var bounds = area.WorldBounds;
                    bounds.position -= (Vector2)area.transform.position;
                    AssignMap(maps, area.Id, image, bounds);
                    Debug.Log($"Exported and assigned map image for {path}/{area.name}.");
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Map export failed for {path}/{area.name}: {exception.Message}");
                }
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void AssignMap(SerializedProperty maps, string id, Texture2D image, Rect localBounds)
    {
        int index = 0;
        while (index < maps.arraySize && maps.GetArrayElementAtIndex(index).FindPropertyRelative("AreaId").stringValue != id)
            index++;

        if (index == maps.arraySize)
            maps.arraySize++;

        var entry = maps.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("AreaId").stringValue = id;
        entry.FindPropertyRelative("Image").objectReferenceValue = image;
        entry.FindPropertyRelative("LocalCaptureBounds").rectValue = localBounds;
    }

    private static Texture2D ExportArea(LevelInstance level, Area area, string guid)
    {
        foreach (var other in level.Areas)
            other.gameObject.SetActive(other == area);

        foreach (var plant in area.GetComponentsInChildren<BreakableObject>(true))
            plant.gameObject.SetActive(false);

        string path = $"{OutputFolder}/{guid}-{area.Id}.png";
        File.WriteAllBytes(path, Render(area));
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.npotScale != TextureImporterNPOTScale.None || importer.mipmapEnabled ||
            importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path) ??
            throw new IOException($"Could not import {path}.");
    }

    private static byte[] Render(Area area)
    {
        Rect bounds = area.WorldBounds;
        float scale = Mathf.Min(1024, SystemInfo.maxTextureSize) / Mathf.Max(bounds.width, bounds.height);
        int width = Mathf.Max(1, Mathf.RoundToInt(bounds.width * scale));
        int height = Mathf.Max(1, Mathf.RoundToInt(bounds.height * scale));
        var cameraObject = new GameObject("Map Capture Camera");
        var previous = RenderTexture.active;
        RenderTexture target = null;
        Texture2D image = null;
        try
        {
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, area.gameObject.scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = area.gameObject.scene;
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, area.transform.position.z - 10f);
            camera.orthographic = true;
            camera.orthographicSize = bounds.height / 2;
            camera.aspect = bounds.width / bounds.height;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(16, 28, 35, 255);
            target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            if (!RenderPipeline.SupportsRenderRequest(camera, request))
                throw new InvalidOperationException("The current render pipeline cannot capture the map.");

            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture.active = target;
            image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            return image.EncodeToPNG();
        }
        finally
        {
            RenderTexture.active = previous;
            if (target != null)
                RenderTexture.ReleaseTemporary(target);

            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
