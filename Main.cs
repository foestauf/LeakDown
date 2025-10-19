using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using DV;

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
        // Dynamic time compression based on current game day length
        public static float TimeScale => 1440f / Globals.G.GameParams.DayLengthInMinutes;
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

        public Settings()
        {
            percentOfRealistic = 100f;
            percentBrakeOfRealistic = 100f;
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
}