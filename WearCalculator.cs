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
            if (trainCar != null && trainCar.IsLoco)
            {
                return GetBaseWearMultiplier(trainCar);
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
            if (trainCar == null || !trainCar.IsLoco)
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

            return restorationController.State switch
            {
                // Wreck states - not yet recovered (highest multipliers)
                LocoRestorationController.RestorationState.S0_Initialized => 2.5f,
                LocoRestorationController.RestorationState.S1_UnlockedRestorationLicense => 2.5f,
                LocoRestorationController.RestorationState.S2_LocoUnblocked => 2.0f,
                LocoRestorationController.RestorationState.S3_RerailedCars => 1.5f,

                // Restoration in progress (moderate multipliers)
                LocoRestorationController.RestorationState.S4_OnDestinationTrack => 1.2f,
                LocoRestorationController.RestorationState.S5_PartOrdered => 1.2f,
                LocoRestorationController.RestorationState.S6_PartPickedUp => 1.2f,
                LocoRestorationController.RestorationState.S7_PartDelivered => 1.2f,
                LocoRestorationController.RestorationState.S8_PartInstalled => 1.0f,

                // Fully serviced - better seals (reduced multiplier)
                LocoRestorationController.RestorationState.S9_LocoServiced => 0.8f,

                // Default for any states added in future game versions
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
            if (trainCar == null || !trainCar.IsLoco)
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
