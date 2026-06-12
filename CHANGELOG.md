# Changelog

All notable changes to LeakDown will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.2] - 2026-06-11

### Fixed
- **Wear multipliers now match the game's actual restoration states**
  - State detection reworked against the game's
    `LocoRestorationController.RestorationState` values
  - Fully serviced locomotives (S9_LocoServiced) now correctly get the 0.8x bonus
    (previously they got 1.0x and the advertised bonus never applied)
  - Wreck-state locos (S0_Initialized) now correctly leak 2.5x faster
    (previously 1.0x)
  - S3_RerailedCars now correctly gets 1.5x (previously 2.0x)
  - Wear now decreases monotonically as restoration progresses
- Locomotive detection now uses the public `IsLoco` property instead of its lazy
  backing field, which could be unset and silently skip real locomotives
- Brake patch errors are now logged once instead of every sim tick

### Technical
- Restoration state matching switched from string `Contains` to a direct enum
  switch on `LocoRestorationController.RestorationState`
- Boiler cache switched to `ConditionalWeakTable` so despawned locomotives no
  longer keep their simulation graph alive (memory leak)
- Non-steam locomotives now cache the "no boiler" result instead of scanning
  the simulation flow every frame
- Brake patch caches the `BrakeSystem` → `TrainCar` lookup instead of walking
  the component hierarchy every sim tick
- Removed unreachable "broken + restoration" branch and dead locals

## [0.3.1] - 2025-01-18

### Fixed
- **CRITICAL FIX: Wear system now actually works!**
  - Changed patch target from `Boiler.Tick()` to `SimController.Update()`
  - Previous implementation couldn't access TrainCar, always used 1.0x fallback multiplier
  - Now properly detects locomotive condition and applies dynamic wear multipliers
  - Damaged/derailed locomotives now correctly leak 2.0-2.5x faster
  - Fully restored locomotives now correctly leak 20% slower (0.8x)
- Removed broken reflection code that was causing warning messages
- Architecture now properly navigates SimController → TrainCar + SimulationFlow → Boiler

### Technical
- Simplified implementation: removed 40+ lines of broken reflection logic
- Patch at correct architectural level where both TrainCar and Boiler are accessible
- Performance improvement: cached Boiler lookups per SimController instance
- Clean logs: no more "Could not find SimController field" warnings

## [0.3.0] - 2025-01-18

### Added
- **Steam boiler leakdown simulation** - Boilers now lose pressure over time when not firing
  - Exponential pressure decay (~10% per in-game hour at default settings)
  - Properly accounts for steam-only mass removal (doesn't affect water)
  - Separate slider control (0-500% of realistic rate)
- **Locomotive wear system** - Leak rates now vary based on locomotive condition
  - Fully restored locomotives: 0.8x leak rate (better seals)
  - Baseline condition: 1.0x leak rate
  - Partially damaged: 1.2-1.5x leak rate
  - Heavily damaged/derailed: 2.0-2.5x leak rate
  - Broken boiler: 4.0x leak rate (catastrophic leaks)
- **WearCalculator system** - Centralized wear multiplier calculation
  - Integrates with Derail Valley's restoration system
  - Supports both boiler and brake systems
  - Graceful fallback if restoration data unavailable

### Changed
- Refactored boiler leak code into separate `BoilerLeakPatch.cs` file
- Brake leak patch now uses centralized WearCalculator
- Improved code organization and maintainability
- Added comprehensive debug logging (DEBUG builds only)

### Technical
- Implemented cached reflection for efficient TrainCar access from Boiler instances
- Boiler → SimController → TrainCar navigation pattern
- Performance optimized: reflection lookups cached on first run
- Safe fallback behavior when TrainCar access fails

### Fixed
- Resolved long-standing issue accessing TrainCar from Boiler class
- Boiler leakdown now fully functional after multiple previous attempts

## [0.2.0] - 2024-XX-XX

### Added
- Dynamic time scale based on game settings
- Brake system leakdown for locomotives

### Changed
- Adjusted leakdown rates for more realism
- Time compression now adapts to current game day length

## [0.1.0] - 2024-XX-XX

### Added
- Initial release
- Basic brake leakdown simulation
- Configurable leak rates via mod settings

[0.3.2]: https://github.com/foestauf/LeakDown/compare/v0.3.1...v0.3.2
[0.3.1]: https://github.com/foestauf/LeakDown/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/foestauf/LeakDown/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/foestauf/LeakDown/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/foestauf/LeakDown/releases/tag/v0.1.0
