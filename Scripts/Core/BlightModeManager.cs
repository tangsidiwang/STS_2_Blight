using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using BlightMod.Modifiers;

namespace BlightMod.Core
{
    public static class BlightModeManager
    {
        public static bool IsBlightModeActive { get; set; } = false;
        public static int BlightAscensionLevel { get; set; } = 0;
        private static readonly HashSet<string> TriggeredDoubleWaveNodes = new HashSet<string>();
        private static readonly char[] DoubleWaveNodeKeySeparators = new[] { ';' };
        private static bool PendingSecondWaveTransition;
        private const int EarlyFloorRowCutoff = 2; // row 0-2: each act's first 3 fights
        private const double EarlyFloorWeightMultiplier = 0.45d;

        public static bool IsAtLeastAscension(int level)
        {
            return IsBlightModeActive && BlightAscensionLevel >= level;
        }

        public static void ResetRuntimeState()
        {
            TriggeredDoubleWaveNodes.Clear();
            PendingSecondWaveTransition = false;
        }

        public static string SerializeTriggeredDoubleWaveNodeKeys()
        {
            return string.Join(";", TriggeredDoubleWaveNodes.OrderBy(static key => key, StringComparer.Ordinal));
        }

        public static void RestoreTriggeredDoubleWaveNodeKeys(string? serializedKeys)
        {
            TriggeredDoubleWaveNodes.Clear();

            if (string.IsNullOrWhiteSpace(serializedKeys))
            {
                return;
            }

            foreach (string key in serializedKeys.Split(DoubleWaveNodeKeySeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                TriggeredDoubleWaveNodes.Add(key);
            }
        }

        public static void SyncRuntimeStateToRunTag(RunState? state)
        {
            BlightRunTagModifier? runTag = state?.Modifiers?.OfType<BlightRunTagModifier>().FirstOrDefault();
            if (runTag == null)
            {
                return;
            }

            runTag.BlightAscensionLevel = Math.Clamp(BlightAscensionLevel, 0, 5);
            runTag.TriggeredDoubleWaveNodeKeys = SerializeTriggeredDoubleWaveNodeKeys();
        }

        public static bool TryStartSecondWave(RunState state, MapPoint point)
        {
            if (!IsAtLeastAscension(4) || state == null || point == null) return false;
            if (point.PointType != MapPointType.Monster && point.PointType != MapPointType.Elite) return false;

            string seed = state.Rng.StringSeed;
            if (!IsNodeDoubleWave(point, seed)) return false;

            string key = BuildMapNodeKey(state.CurrentActIndex, point.coord);
            if (TriggeredDoubleWaveNodes.Contains(key)) return false;

            TriggeredDoubleWaveNodes.Add(key);
            PendingSecondWaveTransition = true;
            SyncRuntimeStateToRunTag(state);
            return true;
        }

        public static bool ConsumePendingSecondWaveTransition()
        {
            bool pending = PendingSecondWaveTransition;
            PendingSecondWaveTransition = false;
            return pending;
        }

        public static bool CanStartSecondWave(RunState? state, MapPoint? point)
        {
            if (!IsAtLeastAscension(4) || state == null || point == null)
            {
                return false;
            }

            if (point.PointType != MapPointType.Monster && point.PointType != MapPointType.Elite)
            {
                return false;
            }

            string seed = state.Rng.StringSeed;
            if (!IsNodeDoubleWave(point, seed))
            {
                return false;
            }

            string key = BuildMapNodeKey(state.CurrentActIndex, point.coord);
            return !TriggeredDoubleWaveNodes.Contains(key);
        }

        private static string BuildMapNodeKey(int actIndex, MapCoord coord)
        {
            return $"{actIndex}:{coord.col}:{coord.row}";
        }

        // 以原版随机种子与节点坐标做为核心判定变异/双波次的算法（Determinism）
        public static bool IsNodeMutant(MapPoint point, string seed)
        {
            if (!IsAtLeastAscension(1)) return false;
            if (point == null || string.IsNullOrEmpty(seed)) return false;
            if (!IsMutantEligiblePoint(point)) return false;

            return IsSelectedByDeterministicRange(
                point,
                seed,
                "BlightMutant",
                GetMutantRangeForAscension(BlightAscensionLevel),
                IsMutantEligiblePoint,
                fallbackChance: 0.20d);
        }

        public static bool IsNodeDoubleWave(MapPoint point, string seed)
        {
            if (!IsAtLeastAscension(4)) return false;
            if (point == null || string.IsNullOrEmpty(seed)) return false;
            if (!IsDoubleWaveEligiblePoint(point)) return false;

            (int min, int max) range = GetDoubleWaveRangeForAscension(BlightAscensionLevel);
            double fallbackChance = BlightAscensionLevel >= 5 ? 0.14d : 0.10d;

            return IsSelectedByDeterministicRange(
                point,
                seed,
                "BlightDouble",
                range,
                IsDoubleWaveEligiblePoint,
                fallbackChance);
        }

        private static (int min, int max) GetMutantRangeForAscension(int ascension)
        {
            // A1: 1-5, A2: 2-6, A3: 3-7, A4: 4-8, A5+: 5-10
            return ascension switch
            {
                <= 0 => (0, 0),
                1 => (1, 5),
                2 => (2, 6),
                3 => (3, 7),
                4 => (4, 8),
                _ => (5, 10)
            };
        }

        private static (int min, int max) GetDoubleWaveRangeForAscension(int ascension)
        {
            // A4: 2-4, A5+: 3-6
            return ascension switch
            {
                <= 3 => (0, 0),
                4 => (2, 4),
                _ => (3, 6)
            };
        }

        private static bool IsMutantEligiblePoint(MapPoint point)
        {
            return point.PointType == MapPointType.Monster || point.PointType == MapPointType.Elite;
        }

        private static bool IsDoubleWaveEligiblePoint(MapPoint point)
        {
            return point.PointType == MapPointType.Monster || point.PointType == MapPointType.Elite;
        }

        private static bool IsSelectedByDeterministicRange(
            MapPoint point,
            string seed,
            string tag,
            (int min, int max) range,
            Func<MapPoint, bool> eligibility,
            double fallbackChance)
        {
            if (range.max <= 0)
            {
                return false;
            }

            RunState? state = RunManager.Instance?.DebugOnlyGetState();
            if (state?.Map == null)
            {
                return IsLegacyProbabilityFallback(point, seed, tag, fallbackChance);
            }

            List<MapPoint> candidates = state.Map
                .GetAllMapPoints()
                .Where(eligibility)
                .ToList();

            if (candidates.Count == 0)
            {
                return false;
            }

            int targetCount = GetDeterministicCount(seed, state.CurrentActIndex, tag, range.min, range.max);
            targetCount = Math.Clamp(targetCount, 0, candidates.Count);
            if (targetCount <= 0)
            {
                return false;
            }

            HashSet<MapPoint> selected = candidates
                .Select(c => new
                {
                    Point = c,
                    Key = ComputeWeightedSelectionKey(seed, state.CurrentActIndex, tag, c)
                })
                .OrderBy(x => x.Key)
                .Take(targetCount)
                .Select(x => x.Point)
                .ToHashSet();

            return selected.Contains(point);
        }

        private static int GetDeterministicCount(string seed, int actIndex, string tag, int min, int max)
        {
            if (max <= 0)
            {
                return 0;
            }

            if (max <= min)
            {
                return Math.Max(0, min);
            }

            string countId = $"{seed}_{tag}_COUNT_A{BlightAscensionLevel}_ACT{actIndex}";
            int hash = StringHelper.GetDeterministicHashCode(countId);
            var random = new Random(hash);
            return random.Next(min, max + 1);
        }

        private static double ComputeWeightedSelectionKey(string seed, int actIndex, string tag, MapPoint point)
        {
            string uniqueId = $"{seed}_{tag}_ACT{actIndex}_{point.coord.col}_{point.coord.row}";
            int hash = StringHelper.GetDeterministicHashCode(uniqueId);
            var random = new Random(hash);

            // Efraimidis weighted sampling key: smaller key = more likely selected.
            double u = Math.Max(random.NextDouble(), 1e-12d);
            double weight = point.coord.row <= EarlyFloorRowCutoff ? EarlyFloorWeightMultiplier : 1d;
            return -Math.Log(u) / weight;
        }

        private static bool IsLegacyProbabilityFallback(MapPoint point, string seed, string tag, double chance)
        {
            string uniqueId = $"{seed}_{tag}_{point.coord.col}_{point.coord.row}";
            int hash = StringHelper.GetDeterministicHashCode(uniqueId);
            var random = new Random(hash);
            return random.NextDouble() < chance;
        }
    }
}
