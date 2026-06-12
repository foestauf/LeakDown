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
        // Weak-keyed cache to avoid a GetComponentInParent hierarchy walk per car per tick.
        // Only successful lookups are cached so cars still initializing get retried.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DV.Simulation.Brake.BrakeSystem, TrainCar> _brakeToCar =
            new System.Runtime.CompilerServices.ConditionalWeakTable<DV.Simulation.Brake.BrakeSystem, TrainCar>();

        // Log unexpected errors once instead of spamming every sim tick
        private static bool _errorLogged;

        [HarmonyPostfix]
        public static void Postfix(DV.Simulation.Brake.BrakeSystem __instance, float dt)
        {
            try
            {
                if (!_brakeToCar.TryGetValue(__instance, out var trainCar))
                {
                    if (__instance.gameObject == null) return; // No GameObject associated
                    trainCar = __instance.gameObject.GetComponentInParent<TrainCar>();
                    if (trainCar == null) return; // Not part of a TrainCar (yet) - retry next tick
                    _brakeToCar.Add(__instance, trainCar);
                }

                if (!trainCar.IsLoco)
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
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    Main.ModEntry?.Logger.Log($"Error in BrakeLeakPatch (further errors suppressed): {ex}");
                }
            }
        }
    }
}