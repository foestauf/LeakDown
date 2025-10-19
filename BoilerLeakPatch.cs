using System;
using System.Reflection;
using HarmonyLib;
using LocoSim.Implementations;
using UnityEngine;

namespace DvMod.LeakDown
{
    public static class BoilerExtensions
    {
        // Cache reflection for performance
        private static readonly FieldInfo VesselField = typeof(Boiler).GetField("vessel", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Gets the wear multiplier for boiler leak rates based on locomotive condition
        /// </summary>
        /// <param name="trainCar">The train car to check</param>
        /// <param name="boiler">The boiler to check</param>
        /// <returns>Multiplier for leak rate (1.0 = baseline, higher = more leaks)</returns>
        private static float GetBoilerWearMultiplier(TrainCar? trainCar, Boiler boiler)
        {
            if (trainCar != null)
            {
                return WearCalculator.GetBoilerWearMultiplier(trainCar, boiler);
            }

            // Fallback: if we can't access TrainCar, just check boiler broken state
            if (boiler.isBrokenReadOut.Value != 0f)
            {
                return 4.0f; // Broken boiler = 4x leak rate
            }

            return 1.0f; // Default multiplier if no TrainCar access
        }

        public static void SimulateLeakdown(this Boiler boiler, TrainCar? trainCar, float delta)
        {
            // Only skip leakdown if boiler is broken AND pressure is already at minimum
            if (boiler.isBrokenReadOut.Value != 0f && boiler.pressureReadOut.Value <= 1f) return;

            float userFrac = Main.Settings.percentOfRealistic / 100f;
            float wearMultiplier = GetBoilerWearMultiplier(trainCar, boiler);
            float k = Settings.BASELINE_K * userFrac * wearMultiplier * Main.TimeScale;

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
                        $"[LeakDown DEBUG] Δt={delta:F4}, k={k:E6}, P0={P0:F6}, P1={P1:F6}, ΔP={P1 - P0:F6}, fracDrop={fracDrop:E6}, m0={m0:F3}, remove={massToRemove:F6}, wear={wearMultiplier:F2}"
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
        // Cache reflection FieldInfo for performance - only look up once
        private static FieldInfo? _simControllerField;
        private static FieldInfo? _trainField;
        private static bool _reflectionAttempted = false;

        [HarmonyPostfix]
        public static void Postfix(Boiler __instance, float delta)
        {
            // Boiler is NOT a MonoBehaviour, so we need to access TrainCar via SimController
            // Strategy: Boiler -> SimController (via reflection) -> train (public field)
            TrainCar? trainCar = null;

            if (!_reflectionAttempted)
            {
                // Cache the FieldInfo on first run for performance
                _simControllerField = typeof(Boiler).GetField("simController", BindingFlags.NonPublic | BindingFlags.Instance);
                if (_simControllerField == null)
                {
                    // Try alternative field names
                    _simControllerField = typeof(Boiler).GetField("controller", BindingFlags.NonPublic | BindingFlags.Instance);
                }
                if (_simControllerField == null)
                {
                    _simControllerField = typeof(Boiler).GetField("sim", BindingFlags.NonPublic | BindingFlags.Instance);
                }

                _reflectionAttempted = true;

                if (_simControllerField == null)
                {
                    Main.ModEntry?.Logger.Warning("[LeakDown] Could not find SimController field on Boiler - TrainCar access will fail");
                }
            }

            if (_simControllerField != null)
            {
                try
                {
                    // Get the SimController instance from the Boiler
                    var simController = _simControllerField.GetValue(__instance);
                    if (simController != null)
                    {
                        // Cache the train field lookup on first successful simController access
                        if (_trainField == null)
                        {
                            _trainField = simController.GetType().GetField("train", BindingFlags.Public | BindingFlags.Instance);
                            if (_trainField == null)
                            {
                                Main.ModEntry?.Logger.Warning("[LeakDown] Could not find 'train' field on SimController");
                            }
                        }

                        if (_trainField != null)
                        {
                            trainCar = _trainField.GetValue(simController) as TrainCar;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log occasionally to avoid spam, then continue with null trainCar
                    if (UnityEngine.Random.value < 0.001f) // Log ~0.1% of failures
                    {
                        Main.ModEntry?.Logger.Warning($"[LeakDown] Failed to get TrainCar: {ex.Message}");
                    }
                }
            }

            __instance.SimulateLeakdown(trainCar, delta);
        }
    }
}