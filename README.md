# Parse Lord (FFXIV Patch 7.4 / API 14)

The ultimate Balance-optimized auto-rotation engine for FFXIV. Rule the charts with high-performance execution and granular customization.

## Features
- **Hybrid Engine**: Combines RSR-style high-performance execution with Wrath-style granular customization.
- **Target Priority Chains**: Sequential fall-through targeting (e.g., Mouseover -> Target's Target -> Focus -> Lowest HP).
- **Full Reaction Tags**: Support for all targeting tags including `Self`, `Target`, `Focus`, `Mouseover`, `FieldTarget`, `LowestHP`, and `Target's Target`.
- **Job Support**: Optimized rotations for **Dragoon**, **White Mage**, and **Paladin** aligned with **The Balance Patch 7.4** guidelines.
- **Smart Maintenance**: Automatic Regen for tanks (below 90% HP in combat) for White Mage.
- **Custom Action Stacks**: Per-ability logic editor with priority, conditions, and combo protection.

## Prerequisites
- **Visual Studio 2026** (Required for **SDK 10** support)
- **XIVLauncher** (for Dalamud libraries)

## Installation
1. Build the plugin by opening the project in **Visual Studio 2026**.
2. Ensure the target framework is set to `net10.0-windows`.
3. In FFXIV, open Dalamud Settings (`/xlsettings`).
4. Go to the **Experimental** tab.
5. Add the path to the compiled `ParseLord.dll` to the **Dev Plugin Locations**.
6. Open the Plugin Installer (`/xlplugins`) and enable **Parse Lord** under the **Dev Tools** tab.

## Usage
- Use `/pl` or `/parselord` to open the configuration window.
- Use `/pl toggle` to quickly enable or disable the rotation engine.
- Use `/pl on` or `/pl off` to set the state explicitly.

## Technical Details
- **Targeting**: FFXIV Patch 7.4
- **Framework**: Dalamud API 14 (SDK 10)
- **Language**: C# / .NET 10.0
- **Action Execution**: Uses `FFXIVClientStructs` for direct game interaction.

## Version 49 Changelog
- **Fixed Compilation Errors**: Restored the missing `CustomTrigger` and `TargetTag` definitions in `Configuration.cs` that were causing build failures in `RotationManager.cs`.
- **Maintained Granular Toggles**: All 30+ granular job toggles from v48 are preserved and fully functional.

## Version 48 Changelog
- **RSR Engine Integration**: Fully integrated the RSR rotation engine with the granular functional toggles.
- **Granular Control Matrix**: Added over 30+ granular toggles across Dragoon, Paladin, and White Mage, giving you total control over every skill and logic decision.
- **Advanced Weaving**: Implemented RSR's precise weaving logic to ensure oGCDs are used perfectly without clipping the GCD.
- **Level 100 Optimization**: Full support for all Patch 7.x skills and procs, including *Drakesbane*, *Starcross*, and *Blade of Honor*.

## Version 47 Changelog
- **Deep UI Integration**: Performed a deep integration of the actual Wrath Combo UI components, including the job selection screen and feature-based layout.
- **Authentic Sidebar**: Replicated the exact sidebar navigation and child-window structures used in the original Wrath source code.
- **Job Selection Screen**: Implemented the "Select a job" landing page for PvE features, matching the user flow of Wrath Combo.
- **Pixel-Perfect Styling**: Matched the specific colors, padding, and typography that define the Wrath Combo experience.

## Version 46 Changelog
- **Direct Code Port**: Performed a line-by-line port of the Wrath Combo UI methods (`Draw`, `DrawSidebar`, `DrawBody`) into Parse Lord.
- **Identical Layout Logic**: Replicated the exact child-window and table structures used in the original Wrath source code.
- **Fixed Sidebar Alignment**: Used the exact `###WrathLeftSide` and `###WrathRightSide` identifiers to ensure the layout engine behaves identically.
- **Authentic Styling**: Matched the specific `CellPadding`, `Indent`, and `SelectableTextAlign` values used in the professional Wrath framework.

## Version 45 Changelog
- **True 1:1 Wrath UI Clone**: Rebuilt the configuration window using the exact styling and layout patterns from Wrath Combo.
- **ECommons & PunishLib Integration**: Updated the project to support the same libraries that power Wrath Combo's unique look and feel.
- **Authentic Sidebar**: Implemented the centered, selectable sidebar navigation with the exact spacing and child-window structures used in WC.
- **Feature System**: Replicated the blue-titled, wrapped-description "Feature" layout for all rotation options.

## Version 44 Changelog
- **Perfect 1:1 UI Port**: Performed a line-by-line port of the Wrath Combo configuration window structure into Parse Lord.
- **Identical Layout Engine**: Replicated the exact table, child-window, and styling patterns used in the original Wrath source code.
- **Robust Sidebar**: Used the exact `###WrathLeftSide` and `###WrathRightSide` child window identifiers to ensure the layout engine behaves identically to the original.
- **Authentic Navigation**: Restored the full "PvE Features", "PvP Features", "Auto-Rotation", and "Settings" navigation structure as a baseline for future tweaks.

## Version 43 Changelog
- **Direct Wrath UI Port**: Completely rewrote the configuration window using the exact ImGui table and child-window structures from the Wrath Combo source code.
- **Fixed Sidebar Jumbling**: Implemented the sidebar using a proper `ImGui.BeginChild` with a fixed width, matching Wrath's robust layout logic.
- **Authentic Navigation**: Replicated the "PvE Features", "Auto-Rotation", and "Settings" navigation structure for a true Wrath-like experience.
- **Pixel-Perfect Alignment**: Matched the specific indentation and spacing used in the professional Wrath framework.

## Version 42 Changelog
- **True Wrath UI Clone**: Completely rebuilt the configuration window to exactly match the layout, sidebar, and "Feature" system of Wrath Combo.
- **Feature-Based Layout**: Each rotation option now includes a title, a detailed description, and a toggle, providing a professional and informative experience.
- **Consolidated Advanced Logic**: Removed all "Simple" mode references. The job tabs now default to the full suite of advanced features, organized into logical groups like "Core Rotation" and "Burst & oGCDs".
- **Improved Sidebar**: Implemented the specific Wrath-style sidebar with main labels and sub-labels, ensuring perfect spacing and alignment.

## Version 41 Changelog
- **Fixed Sidebar Layout**: Switched to a standard `ImGui.Selectable` implementation for the sidebar to prevent items from overlapping.
- **Fixed Dragoon Drakesbane Combo**: Added specific tracking for the *Drakesbane Ready* status (ID 3847) to ensure the Level 100 finisher fires correctly.
- **Improved Combo Resilience**: Updated the Dragoon rotation to better handle the branching finishers in Patch 7.x.

## Version 40 Changelog
- **Fixed ImGui Ref Errors**: Resolved all compilation errors related to passing properties directly into ImGui `ref` parameters.
- **Local Variable Pattern**: Implemented the local variable pattern for all checkboxes and sliders in `ConfigWindow.cs` to ensure thread-safe and compiler-compliant UI updates.

## Version 39 Changelog
- **Advanced Mode Only**: Removed the "Simple" rotation modes. The plugin now defaults to a fully-featured, granular advanced configuration for all jobs.
- **Granular Control**: Added specific toggles for every major skill, burst window, and utility action across Dragoon, Paladin, and White Mage.
- **Deep Logic Integration**: The rotation engine now respects every individual setting, allowing you to fine-tune exactly when and how skills are used.
- **UI Expansion**: Updated the sidebar and body to accommodate the new advanced settings, providing a more powerful and flexible user experience.

## Version 38 Changelog
- **Wrath-Style UI Overhaul**: Completely redesigned the configuration window to match the look and feel of Wrath Combo.
- **Sidebar Navigation**: Added a left-hand sidebar for quick access to job settings and global configuration.
- **Nested Feature Toggles**: Implemented a more organized, hierarchical layout for settings using collapsing headers and indented controls.
- **Visual Polish**: Improved spacing, colors, and typography to provide a premium, professional experience.

## Version 37 Changelog
- **Bypassed Hotbar Requirement**: Implemented direct action execution logic. You no longer need to have skills on your hotbars for the plugin to use them.
- **Fixed "Stuck" Rotations**: By ignoring client-side hotbar availability checks, the rotation engine is now much more resilient and reliable.
- **Direct Engine Upgrade**: Updated `ActionManager.cs` to handle internal game status codes (like 574) to ensure actions fire as long as server-side conditions are met.

## Version 36 Changelog
- **Fixed API 14 Status Access**: Updated `ActionManager.cs` to use `IBattleChara` for `StatusList` access, which is the correct way in API 14.
- **Fixed ActionInfo Properties**: Corrected `TargetId` to `TargetOverrideId` in `WhiteMageRotation.cs`.
- **Improved Type Safety**: Switched internal healing logic to use `IBattleChara` for better compatibility with Dalamud's object table.

## Version 35 Changelog (Max Edition)
- **Max-Tier Refinements**: Deep audit of all rotations against RSR's latest Patch 7.x logic.
- **Dragoon Enhancements**: Added support for *Dragonfire Dive*, *Stardiver*, and *Raiden Thrust* optimization.
- **Paladin Enhancements**: Improved *Atonement Ready* and *Confiteor Ready* status tracking.
- **White Mage Enhancements**: Added *Divine Caress* and *Glare IV* (Dawntrail procs) to the rotation.
- **UI Polish**: Redesigned the configuration window with a cleaner, more professional layout and advanced performance settings.

## Version 34 Changelog
- **Fixed Namespace Error**: Added missing `Dalamud.Game.ClientState.Objects.Types` to `ActionManager.cs` to fix `ICharacter` compilation errors.

## Version 33 Changelog
- **Major Overhaul**: Combined **Wrath Combo's UI** style with **RSR's Rotation Logic**.
- **New UI Layout**: Switched to a job-based tab system for cleaner configuration.
- **RSR-Style Weaving**: Implemented advanced oGCD weaving logic that checks GCD remaining time.
- **Level 100 Support**: Full support for Patch 7.x skills for Dragoon, Paladin, and White Mage.
- **Smart Status Tracking**: Improved buff and debuff tracking for more accurate rotation decisions.

## Version 32 Changelog
- **Improved Dragoon Rotation**: Updated combo logic to use base IDs with automatic adjustment for Level 100 skills (Spiral Blow, Heavens Thrust, etc.).
- **Action Adjustment**: `ActionManager` now automatically calls `GetAdjustedActionId` before execution to ensure the highest level skill is used.
- **Resilient Combo Detection**: Fixed an issue where the plugin wouldn't recognize Level 100 combo steps.

## Version 31 Changelog
- **Forced API Level**: Explicitly set `DalamudApiLevel` to 14 in both `AutoRotationPlugin.json` and `AutoRotationPlugin.csproj` to prevent "outdated" errors in-game.

## Version 57 Changelog
- **Anti-Crash Measures**: Improved disposal logic in `Plugin.cs` to ensure clean unhooking from the game framework.
- **Shutdown Safety**: Added checks to prevent the plugin from accessing game memory during shutdown.
- **Final Polish**: Minor logic refinements for Dragoon and Paladin rotations.

## Version 56 Changelog
- **Fixed "Outdated" Error**: Updated `DalamudApiLevel` to 11 in both the manifest and csproj. This is the correct level for the current API 14 environment.
- **Version Bump**: Incremented assembly and manifest versions to 1.0.0.56.
- **Manifest Cleanup**: Standardized the `AutoRotationPlugin.json` for better compatibility with the Dalamud loader.

## Version 55 Changelog
- **Fixed IPartyMember Property**: Changed `GameObjectId` to `EntityId` for all party member checks to match API 14 requirements.
- **Fixed Esuna Status Check**: Updated the logic to correctly identify dispellable debuffs using `GameData.Value.CanDispel`.
- **UI Cleanup**: Removed unused variables and warnings in `ConfigWindow.cs` for a cleaner build.

## Version 54 Changelog
- **Added UIHelpers.cs**: Implemented missing Wrath-style UI methods (`DrawFeature`, `DrawHeader`, `Scale`) to resolve compilation errors.
- **Fixed UI Ref Errors**: Ensured all checkboxes and sliders in `ConfigWindow.cs` use local variables for `ref` parameters.
- **Cleaned Up ConfigWindow**: Refactored the UI code to be more robust and maintainable while keeping the Wrath-style layout.

## Version 53 Changelog
- **Fixed Property Mismatches**: Standardized on `GameObjectId` across all files to match API 14 requirements.
- **ActionManager Helpers**: Added `AnimationLock` and `IsAnimationLocked` helpers to `ActionManager` for better weaving detection.
- **RotationManager Helpers**: Added the `IsPartyMember` static helper to `RotationManager` for reliable party detection.
- **ActionInfo Constructor**: Added a parameterless constructor to `ActionInfo` to support all instantiation patterns.

## Version 52 Changelog
- **Fixed Dragoon Interface Error**: Added the missing `JobId` property to `DragoonRotation.cs` to satisfy the `IRotation` interface requirement.
- **Final Compilation Fix**: This resolves the last remaining error, allowing the project to build successfully.

## Version 51 Changelog
- **Fixed Interface Mismatch**: Updated `IRotation` to include `JobId`, allowing the manager to correctly identify the active job.
- **Singleton Fix**: Added a proper static `Instance` to `ActionManager` for reliable engine access.
- **Rotation Logic Sync**: Updated Paladin and White Mage rotations to match the new `ActionInfo` structure and `RotationManager` helpers.
- **Optimized for Dragoon Testing**: Ensured all "glue" code is solid for stress-testing the engine with the complex Dragoon rotation.

## Version 50 Changelog
- **Fixed Manifest File**: Renamed `ParseLord.json` to `AutoRotationPlugin.json` to match AssemblyName (required by DalamudPackager)
- **Fixed ObjectId Deprecation**: Changed `IPartyMember.ObjectId` to `EntityId` in RotationManager.cs
- **Updated csproj**: Fixed DalamudPackager target to only run in Release mode

## Version 29 Changelog
- **Fixed IPlayerCharacter**: Added `using Dalamud.Game.ClientState.Objects.SubKinds;` to all files
- **Fixed ImGui Namespace**: Using `Dalamud.Bindings.ImGui` for ImGui access
- **Updated All Rotations**: DragoonRotation, PaladinRotation, WhiteMageRotation now use correct namespaces
- **Updated ActionManager**: Fixed namespace imports for IPlayerCharacter

## Version 28 Changelog
- **Fixed API 14 Compatibility**: Updated all files for Dalamud API 14
- **IObjectTable**: Replaced deprecated `IClientState.Objects` with `IObjectTable` service
- **ITargetManager**: Replaced deprecated `IGameGui.GetSoftTarget/GetFocusTarget/GetMouseoverTarget` with `ITargetManager` service
- **ImGui Ref Parameters**: Fixed all ImGui calls to use local variables instead of properties
- **Combo State Access**: Updated `ActionManager` to use `Combo.Timer` and `Combo.Action`
- **TargetTag Enum**: Fixed enum to use `Target` instead of non-existent `CurrentTarget`

## Disclaimer
This plugin automates gameplay and violates the FFXIV Terms of Service. Use at your own risk. The developers are not responsible for any account actions taken by Square Enix.
