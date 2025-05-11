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
        // Match your boiler’s 24h→2h time scale
        const float TimeAccelerationFactor = 12f;
        // ~10% (0.1) per hour real world → per second:
        const float BaselineBrakeDecay = 0.0000293f;

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

                // Now we are reasonably sure this is a locomotive's main reservoir
                // and its compressor is off. Proceed with custom leak logic.

                float mainResPressure = __instance.mainReservoirPressure;

                var field = typeof(DV.Simulation.Brake.BrakeSystem)
                    .GetField("mainReservoirPressureUnsmoothed", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null) return;

                float pressureBefore = (float)field.GetValue(__instance);
                if (pressureBefore < 1f) return;

                // BrakeLeakRate should include the slider percentage, but the logs show it doesn't
                // Let's calculate the correct value directly
                float sliderMultiplier = Main.Settings.percentBrakeOfRealistic / 100f;
                float correctRate = BaselineBrakeDecay * sliderMultiplier;
                float decayRate = correctRate * TimeAccelerationFactor;

#if DEBUG
                // Occasional debug of raw values
                if (UnityEngine.Random.value < 0.01f)
                {
                    float expectedRate = Main.Settings.percentBrakeOfRealistic / 100f * BaselineBrakeDecay;
                    Main.ModEntry?.Logger.Log(
                        $"[BrakeLeak] Slider: {Main.Settings.percentBrakeOfRealistic}%, MainRate: {Main.Settings.BrakeLeakRate}, " +
                        $"Calculated: {correctRate:E}, Final: {decayRate:F6}"
                    );
                }

#endif

                // Exponential decay: Pnew = P0 * e^(–k·dt)
                float pressureAfter = pressureBefore * Mathf.Exp(-decayRate * dt);

                // Write it back
                __instance.SetMainReservoirPressure(pressureAfter);

#if DEBUG
                // Occasional debug
                if (UnityEngine.Random.value < 0.01f)
                {
                    float lost = pressureBefore - pressureAfter;
                    Main.ModEntry?.Logger.Log(
                        $"[BrakeLeak] Lost {lost:F3} psi → New pressure: {pressureAfter:F3} psi (decayRate={decayRate:F6})"
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