using System;
using System.IO;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves;

namespace BlightMod.Core
{
    public enum BlightRunSlot
    {
        Unknown = 0,
        Standard = 1,
        Blight = 2,
    }

    public static class BlightRunSaveSlotManager
    {
        public const string StandardRunFileName = "current_run_standard.save";
        public const string BlightRunFileName = "current_run_blight.save";

        public static BlightRunSlot CurrentRunSlot { get; private set; } = BlightRunSlot.Unknown;

        public static BlightRunSlot LastContinueSlot { get; private set; } = BlightRunSlot.Unknown;

        private static bool _loggedInferFailure;

        public static bool HasAnyRunSave()
        {
            return FileExists(GetCurrentRunPath()) || FileExists(GetStandardRunPath()) || FileExists(GetBlightRunPath());
        }

        public static void PrepareCurrentRunForContinue()
        {
            try
            {
                BlightRunSlot selected = SelectBestContinueSlot();
                if (selected == BlightRunSlot.Unknown)
                {
                    return;
                }

                string srcPath = GetSlotPath(selected);
                if (!FileExists(srcPath))
                {
                    return;
                }

                CopyTextFile(srcPath, GetCurrentRunPath());
                LastContinueSlot = selected;
                CurrentRunSlot = selected;
                Log.Info($"[Blight] Continue routed to {selected} slot.");
            }
            catch (Exception e)
            {
                Log.Error($"[Blight] PrepareCurrentRunForContinue failed: {e}");
            }
        }

        public static void MirrorCurrentRunToModeSlot()
        {
            try
            {
                string currentPath = GetCurrentRunPath();
                if (!FileExists(currentPath))
                {
                    return;
                }

                BlightRunSlot slot = CurrentRunSlot;
                if (slot == BlightRunSlot.Unknown)
                {
                    slot = InferSlotFromCurrentRun();
                }

                if (slot == BlightRunSlot.Unknown)
                {
                    slot = BlightModeManager.IsBlightModeActive ? BlightRunSlot.Blight : BlightRunSlot.Standard;
                }

                CopyTextFile(currentPath, GetSlotPath(slot));
                CurrentRunSlot = slot;
            }
            catch (Exception e)
            {
                Log.Error($"[Blight] MirrorCurrentRunToModeSlot failed: {e}");
            }
        }

        public static void DeleteActiveSlotSave(bool fallbackDeleteBothIfUnknown = false)
        {
            try
            {
                BlightRunSlot slot = ResolveActiveSlotForDeletion();
                if (slot == BlightRunSlot.Unknown)
                {
                    if (fallbackDeleteBothIfUnknown)
                    {
                        DeleteFileIfExists(GetStandardRunPath());
                        DeleteFileIfExists(GetBlightRunPath());
                        Log.Info("[Blight] Active slot unknown during delete; removed both mode slots as fallback.");
                    }
                    return;
                }

                string path = GetSlotPath(slot);
                DeleteFileIfExists(path);
            }
            catch (Exception e)
            {
                Log.Error($"[Blight] DeleteActiveSlotSave failed: {e}");
            }
        }

        public static void DeleteAllSingleplayerRunSavesForCurrentProfile()
        {
            try
            {
                DeleteFileIfExists(GetCurrentRunPath());
                DeleteFileIfExists(GetStandardRunPath());
                DeleteFileIfExists(GetBlightRunPath());
                CurrentRunSlot = BlightRunSlot.Unknown;
                LastContinueSlot = BlightRunSlot.Unknown;
                Log.Info("[Blight] Cleared all singleplayer run save slots for current profile.");
            }
            catch (Exception e)
            {
                Log.Error($"[Blight] DeleteAllSingleplayerRunSavesForCurrentProfile failed: {e}");
            }
        }

        private static BlightRunSlot ResolveActiveSlotForDeletion()
        {
            if (CurrentRunSlot != BlightRunSlot.Unknown)
            {
                return CurrentRunSlot;
            }

            if (LastContinueSlot != BlightRunSlot.Unknown)
            {
                return LastContinueSlot;
            }

            BlightRunSlot inferred = InferSlotFromCurrentRun();
            if (inferred != BlightRunSlot.Unknown)
            {
                CurrentRunSlot = inferred;
                LastContinueSlot = inferred;
            }

            return inferred;
        }

        public static void SetCurrentRunSlotByMode(bool isBlight)
        {
            CurrentRunSlot = isBlight ? BlightRunSlot.Blight : BlightRunSlot.Standard;
        }

        public static void SetCurrentRunSlotFromLoadedSave(bool isBlight)
        {
            BlightRunSlot slot = isBlight ? BlightRunSlot.Blight : BlightRunSlot.Standard;
            CurrentRunSlot = slot;
            LastContinueSlot = slot;
        }

        public static bool IsBlightRunFromSaveJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("modifiers", out JsonElement modifiers) || modifiers.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }

                foreach (JsonElement modifier in modifiers.EnumerateArray())
                {
                    if (!modifier.TryGetProperty("id", out JsonElement idElement))
                    {
                        continue;
                    }

                    string? entry = ReadModifierEntry(idElement);
                    if (string.IsNullOrEmpty(entry))
                    {
                        continue;
                    }

                    if (entry.IndexOf("BLIGHT_RUN_TAG", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }

                    if (entry.StartsWith("BLIGHT_", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryInferBlightFromAnyRunSave(out int ascensionLevel)
        {
            ascensionLevel = 0;
            try
            {
                string[] paths = new[] { GetCurrentRunPath(), GetBlightRunPath(), GetStandardRunPath() };
                foreach (string path in paths)
                {
                    bool exists = FileExists(path);
                    if (!exists)
                    {
                        continue;
                    }

                    string? json = ReadTextFile(path);
                    if (string.IsNullOrEmpty(json))
                    {
                        continue;
                    }

                    if (!IsBlightRunFromSaveJson(json))
                    {
                        continue;
                    }

                    ascensionLevel = ReadAscensionOrDefault(json);
                    CurrentRunSlot = path == GetBlightRunPath() ? BlightRunSlot.Blight : CurrentRunSlot;
                    LastContinueSlot = BlightRunSlot.Blight;
                    Log.Info($"[Blight] Raw-save inference matched blight at {ProjectSettings.GlobalizePath(path)}, asc={ascensionLevel}.");
                    return true;
                }

                if (!_loggedInferFailure)
                {
                    _loggedInferFailure = true;
                    string details = string.Join(" | ", paths.Select(p => $"{ProjectSettings.GlobalizePath(p)} exists={FileExists(p)}"));
                    Log.Info($"[Blight] Raw-save inference found no blight saves. {details}");
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Blight] TryInferBlightFromAnyRunSave failed: {e}");
            }

            return false;
        }

        private static int ReadAscensionOrDefault(string json)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("ascension", out JsonElement ascensionElement) && ascensionElement.ValueKind == JsonValueKind.Number)
                {
                    return Math.Clamp(ascensionElement.GetInt32(), 1, 5);
                }
            }
            catch
            {
            }

            return 1;
        }

        private static string? ReadModifierEntry(JsonElement idElement)
        {
            if (idElement.ValueKind == JsonValueKind.String)
            {
                return idElement.GetString();
            }

            if (idElement.ValueKind == JsonValueKind.Object)
            {
                if (idElement.TryGetProperty("entry", out JsonElement entryElement) && entryElement.ValueKind == JsonValueKind.String)
                {
                    return entryElement.GetString();
                }

                if (idElement.TryGetProperty("Entry", out JsonElement entryElementUpper) && entryElementUpper.ValueKind == JsonValueKind.String)
                {
                    return entryElementUpper.GetString();
                }
            }

            return null;
        }

        private static BlightRunSlot SelectBestContinueSlot()
        {
            bool hasStandard = FileExists(GetStandardRunPath());
            bool hasBlight = FileExists(GetBlightRunPath());

            if (hasStandard && hasBlight)
            {
                long standardTime = ReadSaveTimeOrMin(GetStandardRunPath());
                long blightTime = ReadSaveTimeOrMin(GetBlightRunPath());
                return blightTime > standardTime ? BlightRunSlot.Blight : BlightRunSlot.Standard;
            }

            if (hasStandard)
            {
                return BlightRunSlot.Standard;
            }

            if (hasBlight)
            {
                return BlightRunSlot.Blight;
            }

            if (FileExists(GetCurrentRunPath()))
            {
                return InferSlotFromCurrentRun();
            }

            return BlightRunSlot.Unknown;
        }

        private static BlightRunSlot InferSlotFromCurrentRun()
        {
            string currentPath = GetCurrentRunPath();
            if (!FileExists(currentPath))
            {
                return BlightRunSlot.Unknown;
            }

            string? json = ReadTextFile(currentPath);
            if (string.IsNullOrEmpty(json))
            {
                return BlightRunSlot.Unknown;
            }

            return IsBlightRunFromSaveJson(json) ? BlightRunSlot.Blight : BlightRunSlot.Standard;
        }

        private static long ReadSaveTimeOrMin(string path)
        {
            string? json = ReadTextFile(path);
            if (string.IsNullOrEmpty(json))
            {
                return long.MinValue;
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("save_time", out JsonElement saveTime))
                {
                    return saveTime.GetInt64();
                }
            }
            catch
            {
            }

            return long.MinValue;
        }

        private static string GetCurrentRunPath()
        {
            return GetProfileScopedSaveUserPath("current_run.save");
        }

        private static string GetStandardRunPath()
        {
            return GetProfileScopedSaveUserPath(StandardRunFileName);
        }

        private static string GetBlightRunPath()
        {
            return GetProfileScopedSaveUserPath(BlightRunFileName);
        }

        private static string GetProfileScopedSaveUserPath(string fileName)
        {
            return UserDataPathProvider.GetProfileScopedPath(
                SaveManager.Instance.CurrentProfileId,
                Path.Combine(UserDataPathProvider.SavesDir, fileName));
        }

        private static string GetSlotPath(BlightRunSlot slot)
        {
            return slot == BlightRunSlot.Blight ? GetBlightRunPath() : GetStandardRunPath();
        }

        private static bool FileExists(string userPath)
        {
            return File.Exists(ProjectSettings.GlobalizePath(userPath));
        }

        private static void CopyTextFile(string srcUserPath, string dstUserPath)
        {
            string srcAbs = ProjectSettings.GlobalizePath(srcUserPath);
            string dstAbs = ProjectSettings.GlobalizePath(dstUserPath);
            string? parent = Path.GetDirectoryName(dstAbs);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            File.Copy(srcAbs, dstAbs, overwrite: true);
        }

        private static string? ReadTextFile(string userPath)
        {
            string absPath = ProjectSettings.GlobalizePath(userPath);
            if (!File.Exists(absPath))
            {
                return null;
            }

            return File.ReadAllText(absPath);
        }

        private static void DeleteFile(string userPath)
        {
            string absPath = ProjectSettings.GlobalizePath(userPath);
            if (File.Exists(absPath))
            {
                File.Delete(absPath);
            }
        }

        private static void DeleteFileIfExists(string userPath)
        {
            if (FileExists(userPath))
            {
                DeleteFile(userPath);
            }
        }
    }
}