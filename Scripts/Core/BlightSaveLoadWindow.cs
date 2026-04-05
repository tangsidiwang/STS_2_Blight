using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlightMod.Enchantments;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BlightMod.Core
{
    public interface IBlightSaveLoadExtension
    {
        int Priority => 0;

        void OnCardSerializing(CardModel card, SerializableCard save)
        {
        }

        void OnCardDeserialized(SerializableCard save, CardModel card)
        {
        }

        void OnRunSaveLoaded(SerializableRun save)
        {
        }

        void OnRunStateReadyForLoadedSave(RunState state, SerializableRun save)
        {
        }
    }

    public static class BlightSaveLoadWindow
    {
        private static readonly object _gate = new object();
        private static readonly List<IBlightSaveLoadExtension> _extensions = new List<IBlightSaveLoadExtension>();

        private static bool _initialized;

        public static void EnsureInitialized()
        {
            lock (_gate)
            {
                if (_initialized)
                {
                    return;
                }

                InjectSavedPropertyTypesFromAssembly(Assembly.GetExecutingAssembly());
                Register(new BlightCompositeEnchantmentSaveExtension());

                _initialized = true;
                Log.Info($"[Blight][SaveLoad] Initialized save/load window with {_extensions.Count} extension(s).");
            }
        }

        public static void Register(IBlightSaveLoadExtension extension)
        {
            if (extension == null)
            {
                return;
            }

            lock (_gate)
            {
                if (_extensions.Contains(extension))
                {
                    return;
                }

                _extensions.Add(extension);
                _extensions.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }
        }

        public static void OnCardSerializing(CardModel card, SerializableCard save)
        {
            EnsureInitialized();
            foreach (IBlightSaveLoadExtension extension in Snapshot())
            {
                TryInvoke(() => extension.OnCardSerializing(card, save), extension, "OnCardSerializing");
            }
        }

        public static void OnCardDeserialized(SerializableCard save, CardModel card)
        {
            EnsureInitialized();
            foreach (IBlightSaveLoadExtension extension in Snapshot())
            {
                TryInvoke(() => extension.OnCardDeserialized(save, card), extension, "OnCardDeserialized");
            }
        }

        public static void OnRunSaveLoaded(SerializableRun save)
        {
            EnsureInitialized();
            foreach (IBlightSaveLoadExtension extension in Snapshot())
            {
                TryInvoke(() => extension.OnRunSaveLoaded(save), extension, "OnRunSaveLoaded");
            }
        }

        public static void OnRunStateReadyForLoadedSave(RunState state, SerializableRun save)
        {
            EnsureInitialized();
            foreach (IBlightSaveLoadExtension extension in Snapshot())
            {
                TryInvoke(() => extension.OnRunStateReadyForLoadedSave(state, save), extension, "OnRunStateReadyForLoadedSave");
            }
        }

        private static List<IBlightSaveLoadExtension> Snapshot()
        {
            lock (_gate)
            {
                return _extensions.ToList();
            }
        }

        private static void InjectSavedPropertyTypesFromAssembly(Assembly assembly)
        {
            int injectedCount = 0;
            IEnumerable<Type> types = SafeGetTypes(assembly);
            foreach (Type type in types)
            {
                if (type == null || type.IsAbstract || !typeof(AbstractModel).IsAssignableFrom(type))
                {
                    continue;
                }

                bool hasSavedProperty = type
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Any(p => p.GetCustomAttribute<SavedPropertyAttribute>() != null);

                if (!hasSavedProperty)
                {
                    continue;
                }

                SavedPropertiesTypeCache.InjectTypeIntoCache(type);
                injectedCount++;
            }

            Log.Info($"[Blight][SaveLoad] Injected {injectedCount} model type(s) into SavedPropertiesTypeCache.");
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null)!;
            }
        }

        private static void TryInvoke(Action callback, IBlightSaveLoadExtension extension, string hook)
        {
            try
            {
                callback();
            }
            catch (Exception e)
            {
                Log.Error($"[Blight][SaveLoad] Extension {extension.GetType().Name}.{hook} failed: {e}");
            }
        }
    }
}
