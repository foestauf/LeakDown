using DV.LocoRestoration;
using LocoSim.Implementations;

namespace DvMod.LeakDown
{
    /// <summary>
    /// Calculates wear multipliers for locomotive systems based on restoration state
    /// </summary>
    public static class WearCalculator
    {
        /// <summary>
        /// Gets the wear multiplier for boiler leak rates based on locomotive condition
        /// </summary>
        /// <param name="trainCar">The train car to check (nullable)</param>
        /// <param name="boiler">The boiler to check for broken state (optional)</param>
        /// <returns>Multiplier for leak rate (1.0 = baseline, higher = more leaks)</returns>
        public static float GetBoilerWearMultiplier(TrainCar? trainCar, Boiler? boiler = null)
        {
            // Check boiler broken state first - this has highest priority
            if (boiler != null && boiler.isBrokenReadOut.Value != 0f)
            {
                return 4.0f; // Broken boiler = 4x leak rate
            }

            // Check restoration state if we have a valid TrainCar
            if (trainCar != null && trainCar._isLoco.HasValue && trainCar._isLoco.Value)
            {
                float baseMultiplier = GetBaseWearMultiplier(trainCar);

                // Combine broken boiler effect with restoration state if both apply
                if (boiler != null && boiler.isBrokenReadOut.Value != 0f)
                {
                    return baseMultiplier * 2.0f; // Broken + restoration state
                }

                return baseMultiplier;
            }

            return 1.0f; // Default multiplier if no information available
        }

        /// <summary>
        /// Gets the wear multiplier for brake leak rates based on locomotive condition
        /// </summary>
        /// <param name="trainCar">The train car to check</param>
        /// <returns>Multiplier for leak rate (1.0 = baseline, higher = more leaks)</returns>
        public static float GetBrakeWearMultiplier(TrainCar trainCar)
        {
            if (trainCar == null || !trainCar._isLoco.HasValue || !trainCar._isLoco.Value)
            {
                return 1.0f; // Default for non-locomotive cars
            }

            return GetBaseWearMultiplier(trainCar);
        }

        /// <summary>
        /// Gets the base wear multiplier based on locomotive restoration state
        /// </summary>
        /// <param name="trainCar">The locomotive to check</param>
        /// <returns>Wear multiplier based on restoration state</returns>
        private static float GetBaseWearMultiplier(TrainCar trainCar)
        {
            var restorationController = LocoRestorationController.GetForTrainCar(trainCar);
            if (restorationController == null)
            {
                return 1.0f; // No restoration controller = baseline
            }

            var state = restorationController.State;

            // Use string comparison to avoid hardcoding enum values that might change
            string stateName = state.ToString();

            return stateName switch
            {
                // Most damaged states (highest multipliers)
                var s when s.Contains("S1") || s.Contains("Derailed") => 2.5f,
                var s when s.Contains("S2") || s.Contains("Rerailed") => 2.0f,
                var s when s.Contains("S3") || s.Contains("RerailedCars") => 1.5f,

                // Partially restored states (moderate multipliers)
                var s when s.Contains("S4") || s.Contains("Heated") => 1.2f,
                var s when s.Contains("S5") || s.Contains("Repair") => 1.2f,

                // Fully restored state (reduced multiplier)
                var s when s.Contains("S6") || s.Contains("FullyRestored") || s.Contains("Restored") => 0.8f,

                // Default for unknown states
                _ => 1.0f
            };
        }

#if DEBUG
        /// <summary>
        /// Gets a human-readable description of the locomotive's condition for debugging
        /// </summary>
        /// <param name="trainCar">The train car to check</param>
        /// <param name="multiplier">The calculated wear multiplier</param>
        /// <returns>Description string</returns>
        public static string GetConditionDescription(TrainCar trainCar, float multiplier)
        {
            if (trainCar == null || !trainCar._isLoco.HasValue || !trainCar._isLoco.Value)
            {
                return "Not a locomotive";
            }

            var restorationController = LocoRestorationController.GetForTrainCar(trainCar);
            if (restorationController == null)
            {
                return "No restoration data (baseline condition)";
            }

            var state = restorationController.State;
            return $"Restoration: {state} (wear multiplier: {multiplier:F2}x)";
        }
#endif
    }
}
