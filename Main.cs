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
        // Add your settings properties here
        public float LeakRate = 0.1f;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            UnityModManager.ModSettings.Save<Settings>(this, modEntry);
        }

        public void OnChange()
        {
            // Called when settings are changed
        }

        public void Draw(UnityModManager.ModEntry modEntry)
        {
            // Simple settings drawing with IMGUI
            GUILayout.Label($"Leakdown Rate (bar/s): {LeakRate:F2}");
            LeakRate = GUILayout.HorizontalSlider(LeakRate, 0f, 1f);
        }
    }

    public static class BoilerExtensions
    {
        // Add the SimulateLeakdown method to the Boiler class
        public static void SimulateLeakdown(this Boiler boiler, float delta)
        {
            // Only simulate leakdown if the boiler isn't broken and pressure is above atmospheric pressure
            if (boiler.isBrokenReadOut.Value == 0f && boiler.pressureReadOut.Value > 1f)
            {
                // Base leakdown rate (bar per second)
                float baseLeakdownRate = 0.001f;

                // Apply user-configured leak rate from settings
                float leakRate = Main.Settings.LeakRate;
                if (leakRate > 0)
                {
                    baseLeakdownRate = leakRate;
                }

                // Leakdown increases with pressure (higher pressure = more leakage)
                float pressureMultiplier = boiler.pressureReadOut.Value / 10f;

                // Calculate the amount of steam to remove
                float steamToRemove = baseLeakdownRate * pressureMultiplier * delta;

                // Remove a small amount of steam to simulate leakdown
                if (steamToRemove > 0f && boiler.pressureReadOut.Value > 0)
                {
                    try
                    {
                        // Access the internal vessel to remove steam directly
                        typeof(Boiler).GetMethod("SimulateSteamConsumption", BindingFlags.NonPublic | BindingFlags.Instance)
                            ?.Invoke(boiler, new object[] { steamToRemove });

                        // Optional debug logging
                        if (UnityEngine.Random.value < 0.01f) // Only log occasionally
                        {
                            Main.ModEntry?.Logger.Log($"Boiler leak: {steamToRemove} bar/s, Current pressure: {boiler.pressureReadOut.Value}");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (UnityEngine.Random.value < 0.001f) // Log errors, but not too often
                        {
                            Main.ModEntry?.Logger.Log($"Error in SimulateLeakdown: {ex.Message}");
                        }
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
                Main.ModEntry?.Logger.Log("Brake system fields found");
                // Get the field values
                float mainReservoirPressure = (float)mainResPressureField.GetValue(__instance);
                bool hasCompressor = (bool)hasCompressorField.GetValue(__instance);
                float mainResVolume = (float)mainResVolumeField.GetValue(__instance);

                // Get the VentToAtmosphere method using reflection
                var ventMethod = typeof(DV.Simulation.Brake.BrakeSystem).GetMethod("VentToAtmosphere",
                    BindingFlags.Public | BindingFlags.Instance);

                Main.ModEntry?.Logger.Log($"VentToAtmosphere method found: {ventMethod != null}");

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

                    // Debug logging occasionally
                    if (UnityEngine.Random.value < 0.001f)
                    {
                        Main.ModEntry?.Logger.Log($"Brake system leak: Main res: {mainReservoirPressure}");
                    }
                }
            }
        }
    }
}