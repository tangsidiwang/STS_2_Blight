using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using BlightMod.Enchantments.CustomEnchantments;

namespace BlightMod.Enchantments
{
    public sealed class BlightCompositeEnchantment : EnchantmentModel
    {
        public const int MaxEntries = 5;

        private const char IdSeparator = '|';

        private List<BlightEnchantmentEntry> _entries = new List<BlightEnchantmentEntry>();
        private List<EnchantmentModel> _runtimeSubEnchantments = new List<EnchantmentModel>();
        private string _runtimeEntryCacheKey = string.Empty;
        private CardModel? _runtimeCacheCard;

        private bool _isSynchronizing;

        private string _serializedEntryIds = string.Empty;
        private int[] _serializedEntryAmounts = Array.Empty<int>();
        private int[] _serializedEntryRarities = Array.Empty<int>();
        private int[] _serializedEntryFlags = Array.Empty<int>();
        private int[] _serializedDisabledEntryIndices = Array.Empty<int>();
        private HashSet<int> _disabledEntryIndices = new HashSet<int>();

        public IReadOnlyList<BlightEnchantmentEntry> Entries => _entries;

        public bool IsEntryDisabled(int index)
        {
            return _disabledEntryIndices.Contains(index);
        }

        public override bool ShowAmount => true;

        public override int DisplayAmount => _entries.Count;

        protected override IEnumerable<IHoverTip> ExtraHoverTips => BuildUniqueHoverTips();

        [SavedProperty]
        private string SerializedEntryIds
        {
            get => _serializedEntryIds;
            set
            {
                _serializedEntryIds = value ?? string.Empty;
                TryLoadEntriesFromSerialized();
            }
        }

        [SavedProperty]
        private int[] SerializedEntryAmounts
        {
            get => _serializedEntryAmounts;
            set
            {
                _serializedEntryAmounts = value ?? Array.Empty<int>();
                TryLoadEntriesFromSerialized();
            }
        }

        [SavedProperty]
        private int[] SerializedEntryRarities
        {
            get => _serializedEntryRarities;
            set
            {
                _serializedEntryRarities = value ?? Array.Empty<int>();
                TryLoadEntriesFromSerialized();
            }
        }

        [SavedProperty]
        private int[] SerializedEntryFlags
        {
            get => _serializedEntryFlags;
            set
            {
                _serializedEntryFlags = value ?? Array.Empty<int>();
                TryLoadEntriesFromSerialized();
            }
        }

        [SavedProperty]
        private int[] SerializedDisabledEntryIndices
        {
            get => _serializedDisabledEntryIndices;
            set
            {
                _serializedDisabledEntryIndices = value ?? Array.Empty<int>();
                TryLoadEntriesFromSerialized();
            }
        }

        public bool TryAddEntry(BlightEnchantmentEntry entry)
        {
            if (entry == null || entry.EnchantmentId == null)
            {
                return false;
            }

            if (_entries.Count >= MaxEntries)
            {
                return false;
            }

            _entries.Add(entry);
            RecalculateValues();
            return true;
        }

        public decimal EnchantmentsDamagePreview(decimal originalDamage, ValueProp props)
        {
            return EnchantDamageAdditive(originalDamage, props);
        }

        public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
        {
            decimal total = 0m;
            foreach (EnchantmentModel sub in BuildRuntimeSubEnchantments().ToArray())
            {
                total += sub.EnchantDamageAdditive(originalDamage + total, props);
            }

            return total;
        }

        public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props)
        {
            decimal multiplier = 1m;
            foreach (EnchantmentModel sub in BuildRuntimeSubEnchantments().ToArray())
            {
                multiplier *= sub.EnchantDamageMultiplicative(originalDamage * multiplier, props);
            }

            return multiplier;
        }

        public override decimal EnchantBlockAdditive(decimal originalBlock, ValueProp props)
        {
            decimal total = 0m;
            foreach (EnchantmentModel sub in BuildRuntimeSubEnchantments().ToArray())
            {
                total += sub.EnchantBlockAdditive(originalBlock + total, props);
            }

            return total;
        }

        public override decimal EnchantBlockMultiplicative(decimal originalBlock, ValueProp props)
        {
            decimal multiplier = 1m;
            foreach (EnchantmentModel sub in BuildRuntimeSubEnchantments().ToArray())
            {
                multiplier *= sub.EnchantBlockMultiplicative(originalBlock * multiplier, props);
            }

            return multiplier;
        }

        public override int EnchantPlayCount(int originalPlayCount)
        {
            int total = originalPlayCount;
            EnchantmentModel[] subEnchantments = BuildRuntimeSubEnchantments().ToArray();
            for (int i = 0; i < subEnchantments.Length; i++)
            {
                EnchantmentModel sub = subEnchantments[i];
                if (_disabledEntryIndices.Contains(i) && sub is BlightGlamEnchantment glam)
                {
                    glam.Status = EnchantmentStatus.Disabled;
                    continue;
                }

                total = sub.EnchantPlayCount(total);
            }

            return total;
        }

        public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
        {
            EnchantmentModel[] subEnchantments = BuildRuntimeSubEnchantments().ToArray();
            for (int i = 0; i < subEnchantments.Length; i++)
            {
                EnchantmentModel sub = subEnchantments[i];
                await sub.OnPlay(choiceContext, cardPlay);
                PersistRuntimeSubEnchantmentState(i, sub);
            }
        }
        public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
        {
            EnchantmentModel[] subEnchantments = BuildRuntimeSubEnchantments().ToArray();
            for (int i = 0; i < subEnchantments.Length; i++)
            {
                EnchantmentModel sub = subEnchantments[i];
                await sub.AfterCardDrawn(choiceContext, card, fromHandDraw);
                PersistRuntimeSubEnchantmentState(i, sub);
            }
        }

        public override async Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
        {
            EnchantmentModel[] subEnchantments = BuildRuntimeSubEnchantments().ToArray();
            for (int i = 0; i < subEnchantments.Length; i++)
            {
                EnchantmentModel sub = subEnchantments[i];
                await sub.BeforeFlush(choiceContext, player);
                PersistRuntimeSubEnchantmentState(i, sub);
            }
        }
        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            EnchantmentModel[] subEnchantments = BuildRuntimeSubEnchantments().ToArray();
            for (int i = 0; i < subEnchantments.Length; i++)
            {
                EnchantmentModel sub = subEnchantments[i];
                await sub.AfterPlayerTurnStart(choiceContext, player);
                PersistRuntimeSubEnchantmentState(i, sub);
            }
        }

        public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
        {
            EnchantmentModel[] subEnchantments = BuildRuntimeSubEnchantments().ToArray();
            for (int i = 0; i < subEnchantments.Length; i++)
            {
                EnchantmentModel sub = subEnchantments[i];
                await sub.AfterCardPlayed(context, cardPlay);

                PersistRuntimeSubEnchantmentState(i, sub);
            }
        }

        protected override void OnEnchant()
        {
            EnchantmentModel[] subEnchantments = BuildRuntimeSubEnchantments().ToArray();
            for (int i = 0; i < subEnchantments.Length; i++)
            {
                EnchantmentModel sub = subEnchantments[i];
                sub.ModifyCard();
                PersistRuntimeSubEnchantmentState(i, sub);
            }
        }
        
        public override void RecalculateValues()
        {
            Amount = _entries.Count;
            _runtimeEntryCacheKey = string.Empty;
            _runtimeCacheCard = null;
            BuildRuntimeSubEnchantments();
            RefreshSerializedProps();
        }

        protected override void DeepCloneFields()
        {
            base.DeepCloneFields();

            _entries = _entries
                .Select(e => new BlightEnchantmentEntry
                {
                    EnchantmentId = e.EnchantmentId,
                    Amount = e.Amount,
                    Rarity = e.Rarity,
                    IsNegative = e.IsNegative,
                })
                .ToList();

            _runtimeSubEnchantments = new List<EnchantmentModel>();
            _runtimeEntryCacheKey = string.Empty;
            _runtimeCacheCard = null;
            _serializedDisabledEntryIndices = _serializedDisabledEntryIndices?.ToArray() ?? Array.Empty<int>();
            _disabledEntryIndices = new HashSet<int>(_disabledEntryIndices);
            _serializedEntryAmounts = _serializedEntryAmounts?.ToArray() ?? Array.Empty<int>();
            _serializedEntryRarities = _serializedEntryRarities?.ToArray() ?? Array.Empty<int>();
            _serializedEntryFlags = _serializedEntryFlags?.ToArray() ?? Array.Empty<int>();
        }

        private IReadOnlyList<EnchantmentModel> BuildRuntimeSubEnchantments()
        {
            string entryCacheKey = BuildRuntimeEntryCacheKey();
            if (_runtimeSubEnchantments.Count > 0
                && _runtimeCacheCard == Card
                && string.Equals(_runtimeEntryCacheKey, entryCacheKey, StringComparison.Ordinal))
            {
                return _runtimeSubEnchantments;
            }

            _runtimeSubEnchantments.Clear();

            for (int entryIndex = 0; entryIndex < _entries.Count; entryIndex++)
            {
                BlightEnchantmentEntry entry = _entries[entryIndex];
                if (entry.EnchantmentId == null)
                {
                    continue;
                }

                EnchantmentModel sub;
                try
                {
                    sub = SaveUtil.EnchantmentOrDeprecated(entry.EnchantmentId).ToMutable();
                }
                catch
                {
                    continue;
                }

                if (sub is BlightCompositeEnchantment)
                {
                    continue;
                }

                try
                {
                    if (HasCard)
                    {
                        sub.ApplyInternal(Card, entry.Amount);
                    }
                    else
                    {
                        sub.Amount = entry.Amount;
                    }
                }
                catch
                {
                    continue;
                }

                if (_disabledEntryIndices.Contains(_runtimeSubEnchantments.Count))
                {
                    sub.Status = EnchantmentStatus.Disabled;
                }

                _runtimeSubEnchantments.Add(sub);
            }

            _runtimeEntryCacheKey = entryCacheKey;
            _runtimeCacheCard = Card;

            return _runtimeSubEnchantments;
        }

        private string BuildRuntimeEntryCacheKey()
        {
            if (_entries.Count == 0)
            {
                return string.Empty;
            }

            string entriesKey = string.Join(";", _entries.Select(e => $"{e.EnchantmentId}:{e.Amount}:{(int)e.Rarity}:{(e.IsNegative ? 1 : 0)}"));
            string disabledKey = string.Join(",", _disabledEntryIndices.OrderBy(i => i));
            return $"{entriesKey}|D:{disabledKey}";
        }

        private IEnumerable<IHoverTip> BuildUniqueHoverTips()
        {
            EnchantmentModel[] subEnchantments = BuildRuntimeSubEnchantments().ToArray();
            for (int i = 0; i < subEnchantments.Length; i++)
            {
                foreach (IHoverTip tip in subEnchantments[i].HoverTips)
                {
                    if (tip is HoverTip hoverTip)
                    {
                        // Vanilla hover tip set dedupes by Id unless IsInstanced is true.
                        hoverTip.IsInstanced = true;
                        hoverTip.Id = $"{hoverTip.Id}#BLIGHT_{i}";
                        yield return hoverTip;
                        continue;
                    }

                    yield return tip;
                }
            }
        }

        private void PersistRuntimeSubEnchantmentState(int index, EnchantmentModel sub)
        {
            if (index < 0 || index >= _entries.Count || sub == null)
            {
                return;
            }

            bool stateChanged = false;
            if (sub.Status == EnchantmentStatus.Disabled)
            {
                stateChanged = _disabledEntryIndices.Add(index);
            }
            else if (_disabledEntryIndices.Contains(index))
            {
                // Composite sub-enchantments in this mod are one-way consumables.
                // Ignore stale callbacks that report Normal after a sub-entry has already been disabled.
                sub.Status = EnchantmentStatus.Disabled;
            }

            BlightEnchantmentEntry entry = _entries[index];
            if (entry.Amount == sub.Amount && !stateChanged)
            {
                return;
            }

            int oldAmount = entry.Amount;
            entry.Amount = sub.Amount;
            if (stateChanged)
            {
                _runtimeEntryCacheKey = string.Empty;
                _runtimeCacheCard = null;
            }
            RefreshSerializedProps();
        }

        private void RefreshSerializedProps()
        {
            _isSynchronizing = true;
            try
            {
                _serializedEntryIds = string.Join(IdSeparator, _entries.Select(e => e.EnchantmentId.ToString()));
                _serializedEntryAmounts = _entries.Select(e => e.Amount).ToArray();
                _serializedEntryRarities = _entries.Select(e => (int)e.Rarity).ToArray();
                _serializedEntryFlags = _entries.Select(e => e.IsNegative ? 1 : 0).ToArray();
                _serializedDisabledEntryIndices = _disabledEntryIndices.OrderBy(i => i).ToArray();
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        private void TryLoadEntriesFromSerialized()
        {
            if (_isSynchronizing)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_serializedEntryIds))
            {
                _entries.Clear();
                _disabledEntryIndices.Clear();
                return;
            }

            string[] idTokens = _serializedEntryIds.Split(IdSeparator, StringSplitOptions.RemoveEmptyEntries);
            if (idTokens.Length == 0)
            {
                _entries.Clear();
                _disabledEntryIndices.Clear();
                return;
            }

            if (_serializedEntryAmounts.Length < idTokens.Length
                || _serializedEntryRarities.Length < idTokens.Length
                || _serializedEntryFlags.Length < idTokens.Length)
            {
                return;
            }

            _entries.Clear();
            _disabledEntryIndices = new HashSet<int>(_serializedDisabledEntryIndices.Where(i => i >= 0 && i < idTokens.Length));
            for (int i = 0; i < idTokens.Length && i < MaxEntries; i++)
            {
                if (!TryParseModelId(idTokens[i], out ModelId? id) || id == null)
                {
                    continue;
                }

                _entries.Add(new BlightEnchantmentEntry
                {
                    EnchantmentId = id,
                    Amount = _serializedEntryAmounts[i],
                    Rarity = Enum.IsDefined(typeof(BlightEnchantmentRarity), _serializedEntryRarities[i])
                        ? (BlightEnchantmentRarity)_serializedEntryRarities[i]
                        : BlightEnchantmentRarity.Common,
                    IsNegative = _serializedEntryFlags[i] != 0,
                });
            }

            Amount = _entries.Count;
        }

        private static bool TryParseModelId(string token, out ModelId? modelId)
        {
            modelId = null;
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            string[] parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return false;
            }

            modelId = new ModelId(parts[0], parts[1]);
            return true;
        }
    }
}
