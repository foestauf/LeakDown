// filepath: f:\dv-mods\LeakDown\BrakeLeakPatch.cs
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace DvMod.LeakDown
{
    [HarmonyPatch(typeof(DV.Simulation.Brake.BrakeSystem))]
    [HarmonyPatch("SimulateLeakage")]
    public static class BrakeSystemSimulateLeakagePatch
    {


        [HarmonyPostfix]
        public static void Postfix(DV.Simulation.Brake.BrakeSystem __instance, float dt)
        {
            try
            {
                // Attempt to get the TrainCar this BrakeSystem is associated with.
                // This assumes BrakeSystem is a UnityEngine.Object (e.g., MonoBehaviour).
                if (__instance.gameObject == null) return; // Not a UnityEngine.Object or no GameObject associated

                var trainCar = __instance.gameObject.GetComponentInParent<TrainCar>();
                if (trainCar == null)
                {
                    // Not part of a TrainCar or TrainCar component not found in parents.
                    return;
                }

                if (trainCar._isLoco == false)
                {
                    // Not a locomotive, so skip the leak logic.
                    return;
                }

                if (__instance.hasCompressor == false)
                {
                    return;
                }

                float pressureBefore = __instance.mainReservoirPressure;
                if (pressureBefore < 1f) return;                // Calculate leak rate with wear multiplier
                float sliderFrac = Main.Settings.percentBrakeOfRealistic / 100f;
                float wearMultiplier = WearCalculator.GetBrakeWearMultiplier(trainCar);
                float k = Settings.BASELINE_K * sliderFrac * wearMultiplier * Main.TimeScale;

                // Mass-based exponential leak: m = V·P
                float massBefore = __instance.MainResAirMass;
                float massAfter = massBefore * Mathf.Exp(-k * dt);
                float massRemoved = massBefore - massAfter;
                // derive new pressure from remaining mass
                float pressureAfter = massAfter / __instance.mainResVolume;
                // apply new pressure
                __instance.SetMainReservoirPressure(pressureAfter);

#if DEBUG                // Occasional debug
                if (UnityEngine.Random.value < 0.01f)
                {
                    Main.ModEntry?.Logger.Log(
                        $"[BrakeLeak DEBUG] P0={pressureBefore:F3}→{pressureAfter:F3}, m0={massBefore:F4}→{massAfter:F4}, removed={massRemoved:F4} (k={k:F6}, wear={wearMultiplier:F2})"
                    );
                }
#endif
            }
            catch (Exception ex)
            {
                Main.ModEntry?.Logger.Log($"Error in BrakeLeakPatch: {ex.Message}");
            }
        }
    }
}