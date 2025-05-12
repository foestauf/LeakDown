Realistic leakdown simulation for Derail Valley.

This mod simulates natural pressure loss in steam boilers and brake air reservoirs. Steam engines and air brake systems in real life lose pressure over time due to small leaks—this mod brings that realism into the game!

✅ Features
- Realistic steam boiler leakdown: Only the actual steam mass (not water) is lost, matching real-world physics. Pressure drops slowly, especially in large, water-filled boilers.
- Realistic brake reservoir leakdown: Air mass is removed from the main reservoir, causing pressure to fall at a physically accurate rate.
- Configurable leak rates (0%–500% of realistic baseline) for both systems.
- Time-acceleration aware (scales to the game’s in-game day speed).
- Modular design: supports both boiler and brake system leakdown.

🔧 Installation
Install Unity Mod Manager (UMM) (or your specific mod loader).

Download the latest LeakDown mod release.

Place the mod folder (or zip) into your game’s Mods directory.

Enable LeakDown in UMM.

⚙️ Configuration
Open the mod settings UI via Unity Mod Manager.

Use the sliders to adjust the leakdown rates for both the steam boiler and brake reservoir (expressed as a % of the real-world baseline of ~10% pressure loss per hour).

🚨 Requirements
Unity Mod Manager

Tested on Derail Valley version b99.4

🚂 Roadmap
✅ Steam boiler leakdown (v0.1.0)
✅ Brake system air reservoir leakdown (v0.2.0)

🔄 Changelog
v0.2.0:
- Added realistic brake reservoir leakdown (mass-based, exponential decay).
- Improved boiler leakdown to remove only steam mass, not water.

v0.1.0:
- Initial release with configurable boiler leakdown.

📝 License
This mod is licensed under the GNU General Public License v3.0 (GPLv3).

You are free to modify and redistribute under the same license, but you must credit the author and keep all derivative works open-source.