using System;
using System.Reflection;
using DV.Simulation.Cars;
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

    /// <summary>
    /// Patch at SimController level to access both TrainCar and Boiler
    /// This is the correct architecture since SimController has:
    /// - .train field (TrainCar)
    /// - .simFlow field (SimulationFlow containing Boiler)
    /// </summary>
    [HarmonyPatch(typeof(SimController))]
    [HarmonyPatch("Update")]
    public static class SimControllerUpdatePatch
    {
        // Cache the boiler component ID for each steam loco to avoid repeated lookups
        private static readonly System.Collections.Generic.Dictionary<SimController, Boiler> _controllerToBoiler =
            new System.Collections.Generic.Dictionary<SimController, Boiler>();

        [HarmonyPostfix]
        public static void Postfix(SimController __instance)
        {
            // Only process steam locomotives
            if (__instance.train == null || __instance.train._isLoco != true)
                return;

            // Check if this is a steam loco by trying to find a boiler
            Boiler? boiler = null;

            if (_controllerToBoiler.TryGetValue(__instance, out var cachedBoiler))
            {
                boiler = cachedBoiler;
            }
            else
            {
                // Search for Boiler component in the SimulationFlow
                if (__instance.simFlow != null && __instance.simFlow.OrderedSimComps != null)
                {
                    foreach (var component in __instance.simFlow.OrderedSimComps)
                    {
                        if (component is Boiler b)
                        {
                            boiler = b;
                            _controllerToBoiler[__instance] = b;
                            break;
                        }
                    }
                }
            }

            // If we found a boiler, apply leakdown with full TrainCar access for wear calculation
            if (boiler != null)
            {
                // Note: We use Time.deltaTime here instead of the Update's parameter
                // because SimController.Update doesn't take a delta parameter
                boiler.SimulateLeakdown(__instance.train, Time.deltaTime);
            }
        }
    }
}