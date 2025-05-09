using System;
using System.Reflection;
using DV.Simulation;
using HarmonyLib;
using LocoSim.Implementations;
using UnityEngine;
using UnityModManagerNet;

namespace DvMod.LeakDown
{
    [EnableReloading]
    public static class Main
    {
        public static UnityModManager.ModEntry? ModEntry;
        public static Settings Settings = new Settings();
        public static Harmony? HarmonyInstance;

        // Called when the mod is loaded
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnToggle = OnToggle;
            modEntry.OnUnload = OnUnload;

            // Load settings
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);

            return true;
        }

        // GUI for mod settings
        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Draw(modEntry);
        }

        // Save settings
        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }

        // Called when the mod is toggled
        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool isToggled)
        {
            if (isToggled)
            {
                // Initialize Harmony
                HarmonyInstance = new Harmony(modEntry.Info.Id);
                // Apply all patches in this assembly
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                ModEntry!.Logger.Log("Harmony patches applied");
            }
            else
            {
                // Remove all patches
                HarmonyInstance?.UnpatchAll(modEntry.Info.Id);
                HarmonyInstance = null;
                ModEntry!.Logger.Log("Harmony patches removed");
            }
            return true;
        }

        // Called when the mod is unloaded
        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            // Clean up resources
            HarmonyInstance?.UnpatchAll(modEntry.Info.Id);
            return true;
        }
    }

    public class Settings : UnityModManager.ModSettings, UnityModManagerNet.IDrawable
    {
        [Draw("Leak Rate Percentage")]
        public float percentOfRealistic = 100f;  // Make this a saved field

        public float LeakRate = 0.0000293f; // This will be calculated from percentOfRealistic

        const float BaselineDecayRate = 0.0000293f; // ~10% per hour baseline

        public Settings()
        {
            // Calculate LeakRate from percentOfRealistic
            UpdateLeakRate();
        }

        private void UpdateLeakRate()
        {
            LeakRate = percentOfRealistic / 100f * BaselineDecayRate;
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            // Make sure LeakRate is updated before saving
            UpdateLeakRate();
            UnityModManager.ModSettings.Save<Settings>(this, modEntry);
        }

        public void OnChange()
        {
            // Update LeakRate when settings change
            UpdateLeakRate();
        }

        public void Draw(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Label($"Leakdown Rate: {percentOfRealistic:F0}% of real-world (~10%/hr)");
            float newPercent = GUILayout.HorizontalSlider(percentOfRealistic, 0f, 500f); // 0% to 500%

            // Only update if the value changed
            if (newPercent != percentOfRealistic)
            {
                percentOfRealistic = newPercent;
                UpdateLeakRate();
            }
        }
    }

    public static class BoilerExtensions
    {

        // Add the SimulateLeakdown method to the Boiler class
        public static void SimulateLeakdown(this Boiler boiler, float delta)
        {
            const float TimeAccelerationFactor = 12f;
            // Only simulate leakdown if the boiler isn't broken and pressure is above atmospheric pressure
            if (boiler.isBrokenReadOut.Value == 0f && boiler.pressureReadOut.Value > 1f)
            {
                // Base decay rate - represents about 10% per hour at 100% setting
                float baseDecayRate = Main.Settings.LeakRate * TimeAccelerationFactor;

                float pressureBefore = boiler.pressureReadOut.Value;
                float pressureLossRate = pressureBefore * baseDecayRate;

                // Apply more aggressive scaling at higher percentages
                if (Main.Settings.percentOfRealistic > 100f)
                {
                    // More aggressive curve: 1.0x at 100%, 10.0x at 500%
                    float scaleFactor = 1f + (Main.Settings.percentOfRealistic - 100f) / 100f * 2.25f;
                    pressureLossRate *= scaleFactor;

#if DEBUG
                    // Add debug logging to show the actual multiplier being applied
                    if (UnityEngine.Random.value < 0.01f)
                    {
                        Main.ModEntry?.Logger.Log($"[LeakDown] Using enhanced scale factor: {scaleFactor:F2}x at {Main.Settings.percentOfRealistic:F0}%");
                    }
#endif
                }

                // Calculate amount of steam to remove, ensuring we don't go below 1 bar
                float steamToRemove = Math.Min(pressureLossRate, pressureBefore - 1f);

                // Limit the loss rate to prevent pressure going below 1 bar
                float maxAllowableLossRate = (pressureBefore - 1f) / delta;
                float steamConsumptionRate = Math.Min(pressureLossRate, maxAllowableLossRate);

                // Remove a small amount of steam to simulate leakdown
                if (steamToRemove > 0f && boiler.pressureReadOut.Value > 0)
                {
                    try
                    {
                        // Get the vessel field using reflection
                        FieldInfo vesselField = typeof(Boiler).GetField("vessel",
                            BindingFlags.NonPublic | BindingFlags.Instance);

                        if (vesselField != null)
                        {
                            // Get the vessel instance
                            WaterPressureVessel vessel = (WaterPressureVessel)vesselField.GetValue(boiler);

                            if (vessel != null)
                            {
                                // Call the public RemoveSteam method directly
                                // Apply a minimum effect to ensure it's noticeable
                                float actualRemovalRate = Math.Max(steamConsumptionRate, 0.1f * baseDecayRate) * delta;
                                vessel.RemoveSteam(actualRemovalRate);

#if DEBUG
                                // More frequent and informative debug logging
                                if (UnityEngine.Random.value < 0.05f) // Log more frequently
                                {
                                    Main.ModEntry?.Logger.Log(
                                       $"[LeakDown] Removing {actualRemovalRate:F6} steam mass (delta={delta:F3}), " +
                                       $"Current pressure: {boiler.pressureReadOut.Value:F2} bar, " +
                                       $"Base rate: {baseDecayRate:F6}, Percent: {Main.Settings.percentOfRealistic:F0}%"
                                   );
                                }
#endif
                            }
                            else
                            {
                                Main.ModEntry?.Logger.Log("Vessel object is null");
                            }
                        }
                        else
                        {
                            Main.ModEntry?.Logger.Log("Could not find vessel field");
                        }
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        if (UnityEngine.Random.value < 0.001f) // Log errors, but not too often
                        {
                            Main.ModEntry?.Logger.Log($"Error in SimulateLeakdown: {ex.Message}");
                        }
#endif
                    }
                }
            }
        }
    }

    // Patch for the Boiler.Tick method to call our SimulateLeakdown method
    [HarmonyPatch(typeof(Boiler))]
    [HarmonyPatch("Tick")]
    public static class BoilerTickPatch
    {
        // This method will run after the original Tick method
        [HarmonyPostfix]
        public static void Postfix(Boiler __instance, float delta)
        {
            // Call our SimulateLeakdown method
            __instance.SimulateLeakdown(delta);
        }
    }

    // Patch for the BrakeSystem.SimulateLeakage method to add natural brake system leakage
    [HarmonyPatch(typeof(DV.Simulation.Brake.BrakeSystem))]
    [HarmonyPatch("SimulateLeakage")]
    public static class BrakeSystemSimulateLeakagePatch
    {
        // This method will run after the original SimulateLeakage method
        [HarmonyPostfix]
        public static void Postfix(DV.Simulation.Brake.BrakeSystem __instance, float dt)
        {
            // Get the private fields using reflection
            var mainResPressureField = typeof(DV.Simulation.Brake.BrakeSystem).GetField("mainReservoirPressureUnsmoothed", BindingFlags.NonPublic | BindingFlags.Instance);
            var hasCompressorField = typeof(DV.Simulation.Brake.BrakeSystem).GetField("hasCompressor", BindingFlags.NonPublic | BindingFlags.Instance);
            var mainResVolumeField = typeof(DV.Simulation.Brake.BrakeSystem).GetField("mainResVolume", BindingFlags.NonPublic | BindingFlags.Instance);

            if (mainResPressureField != null &&
                hasCompressorField != null && mainResVolumeField != null)
            {
#if DEBUG
                Main.ModEntry?.Logger.Log("Brake system fields found");
#endif
                // Get the field values
                float mainReservoirPressure = (float)mainResPressureField.GetValue(__instance);
                bool hasCompressor = (bool)hasCompressorField.GetValue(__instance);
                float mainResVolume = (float)mainResVolumeField.GetValue(__instance);

                // Get the VentToAtmosphere method using reflection
                var ventMethod = typeof(DV.Simulation.Brake.BrakeSystem).GetMethod("VentToAtmosphere",
                    BindingFlags.Public | BindingFlags.Instance);

#if DEBUG
                Main.ModEntry?.Logger.Log($"VentToAtmosphere method found: {ventMethod != null}");
#endif

                if (ventMethod != null)
                {
                    // Add very slow natural leakage for main reservoir
                    if (hasCompressor)
                    {
                        // The parameters for VentToAtmosphere are (float dt, ref float pressure, float volume, float speedMultiplier)
                        // We need to pass mainReservoirPressure by reference
                        object[] ventParams = new object[] { dt, mainReservoirPressure, mainResVolume, 0.0005f };
                        ventMethod.Invoke(__instance, ventParams);

                        // Update the value after the method call
                        mainResPressureField.SetValue(__instance, ventParams[1]);
                    }

#if DEBUG
                    // Debug logging occasionally
                    if (UnityEngine.Random.value < 0.001f)
                    {
                        Main.ModEntry?.Logger.Log($"Brake system leak: Main res: {mainReservoirPressure}");
                    }
#endif
                }
            }
        }
    }
}