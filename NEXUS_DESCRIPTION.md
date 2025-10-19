# LeakDown - Realistic Pressure Loss Simulation

Adds realistic pressure loss to steam boilers and brake systems in Derail Valley. No more infinite pressure - now you need to actively maintain your locomotives!

## What Does This Do?

In real life, steam boilers and air brake systems constantly lose pressure through seals, gaskets, and fittings. **LeakDown** brings this realism to Derail Valley:

- 🔥 **Steam boilers** slowly lose pressure when not firing (~10% per in-game hour)
- 🛑 **Brake reservoirs** gradually depressurize over time (~10% per in-game hour)
- 🔧 **Locomotive condition matters!** Damaged locos leak faster, restored ones leak slower

## Key Features

### Dynamic Leak Rates Based on Condition
Your locomotive's condition directly affects how fast it leaks:

- **Fully Restored Locomotives**: Leak 20% slower (0.8x) - those new seals work!
- **Normal Wear**: Standard leak rate (1.0x)
- **Damaged/Derailed**: Leak 2-2.5x faster - fix those seals!
- **Broken Boiler**: Catastrophic 4x leak rate - get to a workshop!

### Realistic Physics
- Steam mass removal only (water stays in the boiler where it belongs)
- Exponential pressure decay matching real thermodynamics
- Time-acceleration aware (scales with your day length settings)

### Fully Configurable
Two independent sliders in Unity Mod Manager:
- **Steam Leakdown Rate**: 0-500% (default 100%)
- **Brake Leakdown Rate**: 0-500% (default 100%)

Set to 0% to disable, or crank up to 500% for hardcore realism!

## Why Use This Mod?

### Adds Strategic Depth
- Can't leave locomotives sitting indefinitely
- Need to plan stops and maintenance
- Adds consequence to taking damage
- Rewards keeping locomotives in good condition

### Enhances Immersion
- Hear that hiss? Your pressure's dropping!
- Need to fire up the boiler before departure
- Brake pressure management becomes important
- Realistic steam locomotive operation

### Balanced Gameplay
- ~10% loss per in-game hour at default settings
- Enough to matter, not enough to be annoying
- Fully customizable to your preference
- Works seamlessly with time acceleration

## Installation

1. Install [Unity Mod Manager](https://www.nexusmods.com/site/mods/21)
2. Download LeakDown
3. Install via UMM's "Mods" tab
4. Enable the mod
5. Adjust settings to taste (optional)

## Configuration

Open Unity Mod Manager (Ctrl+F10 by default):

1. Find "LeakDown" in the mods list
2. Adjust **Steam Leakdown Rate**: Controls boiler pressure loss
3. Adjust **Brake Leakdown Rate**: Controls brake reservoir loss
4. Values are % of realistic baseline (100% = ~10% pressure loss per in-game hour)

**Note:** The actual leak rate automatically adjusts based on locomotive condition!

## Compatibility

- ✅ Works with all locomotives
- ✅ Compatible with time compression/acceleration
- ✅ Works during sleep and time travel
- ✅ No conflicts with other mods (uses Harmony patches)
- ✅ Tested on Derail Valley version b99.4+

## FAQ

**Q: My pressure drops too fast/slow!**
A: Adjust the sliders in UMM settings. 50% = half as fast, 200% = twice as fast.

**Q: Does this work on cargo cars?**
A: Brake leakdown applies to all cars with compressors (locomotives only for now).

**Q: Can I disable just boiler or just brakes?**
A: Yes! Set the unwanted slider to 0%.

**Q: Does locomotive condition really matter?**
A: Absolutely! A derailed loco leaks 2.5x faster than normal. Restore your equipment!

**Q: Will this break my saves?**
A: No, it's completely safe. Disable the mod and pressure behavior returns to vanilla.

## Version History

### v0.3.1 (Current) - Critical Fix
- **FIXED**: Wear system now actually works!
- Damaged locomotives properly leak 2.5x faster
- Restored locomotives properly leak 0.8x slower
- Removed warning messages
- Performance improvements

### v0.3.0 - Major Feature Release
- Added steam boiler leakdown
- Added locomotive wear/damage detection system
- Integrated with Derail Valley's restoration mechanics
- Broke wear system (fixed in 0.3.1)

### v0.2.0
- Added brake reservoir leakdown
- Dynamic time scaling

### v0.1.0
- Initial release

## Support & Feedback

Found a bug? Have a suggestion? Please report on the [Bugs tab](https://www.nexusmods.com/derailvalley/mods/YourModID/?tab=bugs) or in the [Posts section](https://www.nexusmods.com/derailvalley/mods/YourModID/?tab=posts).

## Credits

Created with [Claude Code](https://claude.com/claude-code)

## License

GNU General Public License v3.0 (GPLv3)
