// filepath: f:\dv-mods\LeakDown\Main.cs
using System;
using System.Reflection;
using HarmonyLib;
using LocoSim.Implementations;
using UnityEngine;
using UnityModManagerNet;
using System.Xml.Serialization;

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

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Draw(modEntry);
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool isToggled)
        {
            if (isToggled)
            {
                HarmonyInstance = new Harmony(modEntry.Info.Id);
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                ModEntry!.Logger.Log("Harmony patches applied");
            }
            else
            {
                HarmonyInstance?.UnpatchAll(modEntry.Info.Id);
                HarmonyInstance = null;
                ModEntry!.Logger.Log("Harmony patches removed");
            }
            return true;
        }

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            HarmonyInstance?.UnpatchAll(modEntry.Info.Id);
            return true;
        }
    }

    public class Settings : UnityModManager.ModSettings
    {
        // Steam boiler leakdown: 0–500% of real-world baseline (~10% per in-game hour)
        [Draw("Steam Leakdown Rate %")]
        public float percentOfRealistic = 100f;

        // Brake reservoir leakdown: 0–500% of real-world baseline (~10% per in-game hour)
        [Draw("Brake Leakdown Rate %")]
        public float percentBrakeOfRealistic = 100f;

        public const float BASELINE_K = 0.0000293f;
        public const float TIME_SCALE = 12f;     // 24 IGh / 2 RT h

        public Settings()
        {
            percentOfRealistic = 100f;
            percentBrakeOfRealistic = 100f;
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {

            UnityModManager.ModSettings.Save(this, modEntry);
        }

        public void Draw(UnityModManager.ModEntry modEntry)
        {
            // Steam leakdown slider
            GUILayout.Label($"Steam Leakdown: {percentOfRealistic:F0}% of real-world (~10%/hr)");
            float newSteam = GUILayout.HorizontalSlider(percentOfRealistic, 0f, 500f);
            if (newSteam != percentOfRealistic)
            {
                percentOfRealistic = newSteam;
            }

            GUILayout.Space(10);

            // Brake leakdown slider
            GUILayout.Label($"Brake Leakdown: {percentBrakeOfRealistic:F0}% of real-world (~10%/hr)");
            float newBrake = GUILayout.HorizontalSlider(percentBrakeOfRealistic, 0f, 500f);
            if (newBrake != percentBrakeOfRealistic)
            {
                percentBrakeOfRealistic = newBrake;
            }
        }
    }

    public static class BoilerExtensions
    {
        // Cache reflection for performance
        private static readonly System.Reflection.FieldInfo VesselField = typeof(Boiler).GetField("vessel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        public static void SimulateLeakdown(this Boiler boiler, float delta)
        {
            // Only skip leakdown if boiler is broken AND pressure is already at minimum
            if (boiler.isBrokenReadOut.Value != 0f && boiler.pressureReadOut.Value <= 1f) return;

            float userFrac = Main.Settings.percentOfRealistic / 100f;
            float k = Settings.BASELINE_K * userFrac * Settings.TIME_SCALE;

            float P0 = boiler.pressureReadOut.Value;
            // guard against invalid pressure and divide by zero
            if (P0 <= 0f) return;

            float P1 = Mathf.Max(P0 * Mathf.Exp(-k * delta), 1f);  // never below 1 bar
            float fracDrop = 1f - (P1 / P0);

            if (fracDrop <= 0f) return;

            var vessel = (WaterPressureVessel)VesselField.GetValue(boiler);

            if (vessel == null) return;

            float m0 = vessel.mass;

            // compute steam-only mass and remove proportionally
            float waterSpecificVol = SteamTables.WaterSpecificVolume(P0);
            float waterMass = vessel.waterVolume / waterSpecificVol;
            float steamMass = m0 - waterMass;
            float massToRemove = steamMass * fracDrop;

            if (massToRemove > 0f)
            {
                // occasional debug logging (~1% of ticks)
#if DEBUG
                if (UnityEngine.Random.value < 0.01f)
                {
                    Main.ModEntry?.Logger.Log(
                        $"[LeakDown DEBUG] Δt={delta:F4}, k={k:E6}, P0={P0:F6}, P1={P1:F6}, ΔP={P1 - P0:F6}, fracDrop={fracDrop:E6}, m0={m0:F3}, remove={massToRemove:F6}"
                    );
                }
#endif

                // remove steam directly from the vessel
                vessel.RemoveSteam(massToRemove);
                vessel.Update();
                // propagate new pressure back into the boiler readout
                boiler.pressureReadOut.Value = vessel.pressure;
                // verify that mass and pressure actually changed
                float newMass = vessel.mass;
                float newPressure = vessel.pressure;
            }
        }
    }

    [HarmonyPatch(typeof(Boiler))]
    [HarmonyPatch("Tick")]
    public static class BoilerTickPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Boiler __instance, float delta)
        {
            __instance.SimulateLeakdown(delta);
        }
    }
}