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

    public class Settings : UnityModManager.ModSettings, UnityModManagerNet.IDrawable
    {
        // Steam boiler leakdown: 0–500% of real-world baseline (~10% per in-game hour)
        [Draw("Steam Leakdown Rate %")]
        public float percentOfRealistic = 100f;

        // Brake reservoir leakdown: 0–500% of real-world baseline (~10% per in-game hour)
        [Draw("Brake Leakdown Rate %")]
        public float percentBrakeOfRealistic = 100f;

        // Exposed decay rates (per second, real-world base)
        [XmlIgnore]
        public float LeakRate { get; private set; }
        [XmlIgnore]
        public float BrakeLeakRate { get; private set; }

        const float BaselineDecayRate = 0.0000293f; // ~=10% per hour in real time

        public Settings()
        {
            UpdateLeakRates();
        }

        private void UpdateLeakRates()
        {
            LeakRate = percentOfRealistic / 100f * BaselineDecayRate;
            BrakeLeakRate = percentBrakeOfRealistic / 100f * BaselineDecayRate;
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            UpdateLeakRates();
            UnityModManager.ModSettings.Save(this, modEntry);
        }

        public void OnChange()
        {
            UpdateLeakRates();
        }

        public void Draw(UnityModManager.ModEntry modEntry)
        {
            // Steam leakdown slider
            GUILayout.Label($"Steam Leakdown: {percentOfRealistic:F0}% of real-world (~10%/hr)");
            float newSteam = GUILayout.HorizontalSlider(percentOfRealistic, 0f, 500f);
            if (newSteam != percentOfRealistic)
            {
                percentOfRealistic = newSteam;
                UpdateLeakRates();
            }

            GUILayout.Space(10);

            // Brake leakdown slider
            GUILayout.Label($"Brake Leakdown: {percentBrakeOfRealistic:F0}% of real-world (~10%/hr)");
            float newBrake = GUILayout.HorizontalSlider(percentBrakeOfRealistic, 0f, 500f);
            if (newBrake != percentBrakeOfRealistic)
            {
                percentBrakeOfRealistic = newBrake;
                UpdateLeakRates();
            }
        }
    }

    public static class BoilerExtensions
    {
        public static void SimulateLeakdown(this Boiler boiler, float delta)
        {
            const float TimeAccelerationFactor = 12f;
            if (boiler.isBrokenReadOut.Value == 0f && boiler.pressureReadOut.Value > 1f)
            {
                float baseDecayRate = Main.Settings.LeakRate * TimeAccelerationFactor;
                float pressureBefore = boiler.pressureReadOut.Value;
                float pressureLossRate = pressureBefore * baseDecayRate;

                if (Main.Settings.percentOfRealistic > 100f)
                {
                    float scaleFactor = 1f + (Main.Settings.percentOfRealistic - 100f) / 100f * 2.25f;
                    pressureLossRate *= scaleFactor;
                }

                float steamToRemove = Math.Min(pressureLossRate, pressureBefore - 1f);
                float maxAllowable = (pressureBefore - 1f) / delta;
                float consumption = Math.Min(pressureLossRate, maxAllowable);

                if (steamToRemove > 0f && boiler.pressureReadOut.Value > 0f)
                {
                    try
                    {
                        FieldInfo vesselField = typeof(Boiler).GetField("vessel", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (vesselField != null)
                        {
                            var vessel = (WaterPressureVessel)vesselField.GetValue(boiler);
                            if (vessel != null)
                            {
                                float actualRemoval = Math.Max(consumption, 0.1f * baseDecayRate) * delta;
                                vessel.RemoveSteam(actualRemoval);
                            }
                        }
                    }
                    catch { }
                }
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