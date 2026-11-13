using MapForageDemo.Core;
using System;
using System.IO;
using UnityEngine;

namespace MapForageDemo.Gameplay.Manager
{
    public sealed class UserDataManager : SceneInstance<UserDataManager>
    {
        private static TimeHopManager Time => TimeHopManager.Instance;
        private static BreakableObjectManager Forage => BreakableObjectManager.Instance;

        public event Action<string> OnStatusChanged;

        private static string SavePath => Path.Combine(Application.persistentDataPath, "map-forage-demo.json");

        [Serializable]
        private sealed class SaveData
        {
            public int Version;
            public int Day;
            public int Seed;
            public ForageRecord[] Records;
        }

        public void Save() => OnStatusChanged?.Invoke(SaveTo(SavePath));
        public void Load() => OnStatusChanged?.Invoke(LoadFrom(SavePath));

        public void StartOver()
        {
            Forage.ResetForage();
            Time.SetDay(1);
            OnStatusChanged?.Invoke("Started a new session.");
        }

        internal string SaveTo(string path)
        {
            string pendingPath = path + ".tmp";
            try
            {
                var data = new SaveData { Version = 1, Day = Time.Day, Seed = Forage.Seed, Records = Forage.Capture() };
                File.WriteAllText(pendingPath, JsonUtility.ToJson(data, true));
                if (File.Exists(path))
                    File.Replace(pendingPath, path, null);
                else
                    File.Move(pendingPath, path);

                return "Saved.";
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return $"Could not save: {exception.Message}";
            }
        }

        internal string LoadFrom(string path)
        {
            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (FileNotFoundException)
            {
                return "No saved session.";
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return $"Could not load: {exception.Message}";
            }

            if (data == null || data.Version != 1 || data.Day < 1 || data.Day > TimeHopManager.MAX_DAY ||
                !Forage.IsValid(data.Day, data.Records))
                return "Save data is invalid.";

            Forage.Restore(data.Day, data.Seed, data.Records);
            Time.SetDay(data.Day);
            return "Loaded.";
        }
    }
}
