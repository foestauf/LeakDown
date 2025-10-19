Realistic leakdown simulation for Derail Valley.

This mod simulates natural pressure loss in steam boilers and brake air reservoirs. Steam engines and air brake systems in real life lose pressure over time due to small leaks—this mod brings that realism into the game!

⭐ What's New in v0.3.1
• CRITICAL FIX: Wear system now actually works! (v0.3.0 had a bug)
• Damaged/derailed locos now properly leak 2.0-2.5x faster
• Fully restored locomotives properly leak 20% slower (0.8x rate)
• Fixed architecture - now patches at SimController level for proper TrainCar access
• Removed warning messages and broken reflection code

⭐ What's in v0.3.x
• STEAM BOILER LEAKDOWN! Boilers lose pressure over time when not firing
• Locomotive wear matters! Condition affects leak rates
• Broken boilers leak catastrophically at 4x rate
• Both brake AND boiler systems fully operational!

✅ Features
- Realistic steam boiler leakdown: Only the actual steam mass (not water) is lost, matching real-world physics. Pressure drops slowly, especially in large, water-filled boilers (~10% per in-game hour at default).
- Realistic brake reservoir leakdown: Air mass is removed from the main reservoir, causing pressure to fall at a physically accurate rate (~10% per in-game hour at default).
- Locomotive wear system: Leak rates dynamically adjust based on locomotive condition!
  • Fully restored locos: 0.8x leak rate (better seals)
  • Normal condition: 1.0x leak rate
  • Damaged/derailed: 2.0-2.5x leak rate (significant leaks)
  • Broken boiler: 4.0x leak rate (catastrophic failure)
- Configurable leak rates (0%–500% of realistic baseline) for both steam and brake systems.
- Time-acceleration aware (scales to the game's in-game day speed).
- Modular design: supports both boiler and brake system leakdown.

🔧 Installation
Install Unity Mod Manager (UMM) (or your specific mod loader).

Download the latest LeakDown mod release.

Place the mod folder (or zip) into your game’s Mods directory.

Enable LeakDown in UMM.

⚙️ Configuration
Open the mod settings UI via Unity Mod Manager.

Two independent sliders control the leak rates:
- Steam Leakdown Rate: 0-500% (default 100% = ~10% pressure loss per in-game hour)
- Brake Leakdown Rate: 0-500% (default 100% = ~10% pressure loss per in-game hour)

Note: The actual leak rate is automatically adjusted based on locomotive condition (wear system). A damaged loco at 100% slider setting will leak faster than a restored loco at 100%!

🚨 Requirements
Unity Mod Manager

Tested on Derail Valley version b99.4

🚂 Roadmap
✅ Steam boiler leakdown (v0.3.0 - FULLY WORKING!)
✅ Brake system air reservoir leakdown (v0.2.0)
✅ Locomotive wear-based leak rates (v0.3.0)

🔄 Changelog
v0.3.1:
- 🔧 CRITICAL FIX: Wear system now actually works!
- Fixed patch architecture: Changed from Boiler.Tick() to SimController.Update()
- Removed broken reflection code causing warnings
- Wear multipliers now properly applied (damaged locos leak 2.5x faster, restored leak 0.8x)
- Performance improvements with cached Boiler lookups
- Clean logs with no error messages

v0.3.0:
- ✨ MAJOR: Steam boiler leakdown now fully operational!
- ✨ NEW: Locomotive wear system - damaged locos leak faster, restored ones leak slower
- Added WearCalculator to determine leak rates based on restoration state
- Resolved long-standing TrainCar access issue for boiler simulation
- Code refactoring: Split boiler logic into separate BoilerLeakPatch.cs
- Performance optimizations with cached reflection
- Added comprehensive debug logging (DEBUG builds)

v0.2.0:
- Added realistic brake reservoir leakdown (mass-based, exponential decay).
- Dynamic time scaling based on game day length settings.
- Improved boiler leakdown to remove only steam mass, not water.

v0.1.0:
- Initial release with configurable brake leakdown.

📝 License
This mod is licensed under the GNU General Public License v3.0 (GPLv3).

You are free to modify and redistribute under the same license, but you must credit the author and keep all derivative works open-source.