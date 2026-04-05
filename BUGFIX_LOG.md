# Bugfix Log

## 2026-04-02 - Multiplayer Swift Enchantment State Divergence

### Symptom
- In multiplayer, playing a card with composite Swift enchantments could trigger checksum divergence (`StateDivergence`) and disconnect players.
- Typical context: after executing `PlayCardAction` for a card carrying both `BLIGHT_SWIFT2_ENCHANTMENT` and `BLIGHT_SWIFT1_ENCHANTMENT`.

### Root Cause
- Composite enchantment sub-entry disabled state could be written as `0,1` and then incorrectly reverted to `0` in a later callback within the same action flow.
- Runtime sub-enchantment cache key did not include disabled indices, allowing stale runtime state reuse after status updates.

### Fix
- File: `Scripts/Enchantments/Models/BlightCompositeEnchantment.cs`
- Changes:
  - Runtime cache key now includes disabled indices.
  - Re-enable attempts for already-disabled composite sub-entries are ignored (one-way consumable behavior for this mod's current composite entries).
  - Cache invalidation is forced when disabled state changes.

### Validation
- Reproduced the original scenario and confirmed no more `StateDivergence` on Swift play after fix.
- `dotnet build` succeeds.

### Notes
- If future enchantments need true `Disabled -> Normal` transitions, add explicit allowlist logic before applying this protection to those entries.
