# Parse Lord Architecture Notes
## Learned from RSR and Wrath Combo Analysis

### RSR Key Patterns

1. **IBaseAction Interface** - Rich action abstraction with:
   - `CanUse()` with many skip flags for flexible checking
   - `Target` and `PreviewTarget` for targeting
   - `Cooldown`, `Setting`, `Config` for per-action configuration
   - Static flags like `ForceEnable`, `AutoHealCheck`, `IgnoreClipping`

2. **GCD Decision Flow** (CustomRotation_GCD.cs):
   - Command override → Emergency → Interrupt → Dispel → Raise → MoveForward → Heal → Defense → General
   - Uses `IBaseAction.TargetOverride` to switch targeting modes
   - `AutoHealCheck` flag for conditional healing logic
   - Separates HealAreaGCD vs HealSingleGCD

3. **DataCenter Pattern**:
   - Centralized game state access
   - `DataCenter.CommandStatus`, `DataCenter.AutoStatus` for state flags
   - `DataCenter.DefaultGCDRemain` for timing

### Wrath Combo Key Patterns

1. **Enum-Based Presets**:
   - Each combo option is an enum value with attributes
   - `[CustomComboInfo]` - name, description, job
   - `[ParentCombo]` - creates hierarchy
   - `[ConflictingCombos]` - mutual exclusion
   - `[ReplaceSkill]` - which action to replace

2. **DRG Implementation**:
   - `CanDRGWeave()` helper for weave timing
   - `CanLifeSurge()` helper for optimal Life Surge usage
   - `LoTDActive` property for Life of the Dragon state
   - `FirstmindsFocus` property for gauge reading
   - `ComboTimer > 0` and `ComboAction` for combo state

3. **Clean Helper Methods**:
   - `ActionReady()` - cooldown check
   - `HasStatusEffect()` - buff check
   - `LevelChecked()` - level sync check
   - `InMeleeRange()` - distance check
   - `OnTargetsRear()`, `OnTargetsFlank()` - positional checks

### What Parse Lord Should Adopt

1. **From RSR**:
   - Priority-based decision flow (Emergency → Heal → Defense → DPS)
   - Target override system for different action types
   - Rich action interface with CanUse flexibility

2. **From Wrath**:
   - Clean helper methods that read like English
   - Enum-based configuration with parent/child hierarchy
   - Job-specific helper properties (LoTDActive, FirstmindsFocus)

3. **Our Innovation** (Per-Ability Heal Priority):
   - Each healing ability has its own target priority chain
   - More granular than Wrath's global heal stack
   - Allows different priorities for different heals

### Elegant Refactoring Ideas

1. **Replace scattered checks with fluent helpers**:
   ```csharp
   // Instead of:
   if (actionManager.CanUseAction(actionId) && GetStatusRemaining(...) < 3)
   
   // Use:
   if (Action.LanceCharge.IsReady() && Buff.PowerSurge.RemainingTime < 3)
   ```

2. **Job-specific state as properties**:
   ```csharp
   public static class DRGState
   {
       public static bool LoTDActive => JobGaugeReader.DRG_IsLOTDActive;
       public static int EyeCount => JobGaugeReader.DRG_EyeCount;
       public static bool CanEnterLOTD => EyeCount >= 2;
   }
   ```

3. **Decision tree pattern**:
   ```csharp
   return Emergency() ?? Heal() ?? Defense() ?? Burst() ?? Filler();
   ```
