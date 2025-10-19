# LeakDown Boiler Implementation Notes

## Problem Summary
Previous attempts to implement steam boiler leakdown failed due to difficulty accessing the `TrainCar` from the `Boiler` instance. The `Boiler` class is NOT a MonoBehaviour, so standard Unity component hierarchy methods don't work.

## Solution Implemented (Approach 2) ✅
**Status: WORKING - Confirmed in-game 2025-01-18**

Used cached reflection to navigate the object graph:

```csharp
// Boiler → SimController (via reflection) → train field → TrainCar
var simController = _simControllerField.GetValue(__instance);
trainCar = _trainField.GetValue(simController) as TrainCar;
```

### Why Approach 1 Failed
Attempted Unity component hierarchy:
```csharp
trainCar = __instance.gameObject?.GetComponentInParent<TrainCar>();
```

**Error:** `'Boiler' does not contain a definition for 'gameObject'`

Boiler is a plain C# class, not a MonoBehaviour, so it has no `gameObject` property.

### Changed Files
1. **BoilerLeakPatch.cs** (NEW)
   - Extracted from Main.cs
   - Simplified TrainCar access (lines 96-111)
   - Reduced from ~30 lines of reflection to 3 lines

2. **WearCalculator.cs** (NEW)
   - Calculates wear multipliers based on locomotive restoration state
   - Used by both Boiler and Brake leak patches

3. **BrakeLeakPatch.cs** (MODIFIED)
   - Minor cleanup to use WearCalculator

4. **Main.cs** (MODIFIED)
   - Removed BoilerExtensions (moved to BoilerLeakPatch.cs)

## Code Changes Comparison

### Before (Complex Reflection):
```csharp
var simControllerField = typeof(Boiler).GetField("simController", ...);
if (simControllerField == null) {
    simControllerField = typeof(Boiler).GetField("controller", ...);
}
if (simControllerField != null) {
    var simController = simControllerField.GetValue(__instance);
    if (simController != null) {
        var trainCarField = simController.GetType().GetField("train", ...);
        if (trainCarField != null) {
            trainCar = trainCarField.GetValue(simController) as TrainCar;
        }
    }
}
```

### After (Simple Component Hierarchy):
```csharp
trainCar = __instance.gameObject?.GetComponentInParent<TrainCar>();
```

## Testing Recommendations

### 1. Build Test
```bash
dotnet build
# or
msbuild LeakDown.csproj
```

Expected: No compilation errors

### 2. In-Game Testing

#### Test Case 1: Basic Leakdown
1. Spawn a steam locomotive (S060 or S282)
2. Build up boiler pressure
3. Turn off fire/close throttle
4. **Expected**: Pressure should slowly decrease over time (~10% per in-game hour at 100% setting)
5. **Monitor debug logs** if compiled with DEBUG flag

#### Test Case 2: Wear Multiplier
1. Spawn a derailed or damaged steam loco
2. Build up boiler pressure
3. **Expected**: Faster leakdown rate based on wear state (see WearCalculator.cs:76-90)

#### Test Case 3: Broken Boiler
1. Run a steam loco until boiler breaks
2. **Expected**: 4x leak rate (BoilerLeakPatch.cs:28-29)

#### Test Case 4: Settings
1. Adjust "Steam Leakdown Rate %" slider in mod settings
2. **Expected**: Leak rate changes proportionally (0% = no leak, 500% = 5x baseline)

### 3. Debugging

If leakdown doesn't work:

#### Check 1: Boiler is a MonoBehaviour?
- Add debug logging to catch block (BoilerLeakPatch.cs:104-108)
- If exception occurs, Boiler is NOT a MonoBehaviour → need Approach 2

#### Check 2: Verify Patch Applied
- Check UMM log for "Harmony patches applied"
- Use Harmony debug mode to list active patches

#### Check 3: Verify TrainCar Access
Add temporary logging:
```csharp
if (trainCar != null)
    Main.ModEntry?.Logger.Log($"Found TrainCar: {trainCar.ID}");
else
    Main.ModEntry?.Logger.Warning("TrainCar is null!");
```

## Fallback Plan (Approach 2)

If Approach 1 fails (Boiler is not a MonoBehaviour):

### Option A: Patch SteamLocoSimulation
Patch at a higher level where both TrainCar and Boiler are accessible:

```csharp
[HarmonyPatch(typeof(SteamLocoSimulation), "Update")]
public static class SteamLocoSimPatch
{
    [HarmonyPostfix]
    public static void Postfix(SteamLocoSimulation __instance, float delta)
    {
        TrainCar trainCar = __instance.train;  // Direct access!
        Boiler boiler = /* access from __instance */;
        boiler.SimulateLeakdown(trainCar, delta);
    }
}
```

### Option B: Keep Reflection Approach
Revert to the reflection code but add better error handling and caching.

## Architecture Notes

Based on investigation of other DV mods:

```
TrainCar (MonoBehaviour)
  └─ SimController (base class)
      ├─ SteamLocoSimulation (for steam locos)
      │   └─ has Boiler reference
      ├─ LocoControllerDiesel (for diesel locos)
      └─ LocoControllerShunter (for shunters)
```

Key findings:
- `TrainCar.SimController` provides access to simulation controller
- `SimController.train` provides access back to TrainCar
- Many components use `GetComponentInParent<TrainCar>()` pattern
- Boiler appears to be a component (evidence: `trainCar.GetComponent<Boiler>()`)

## References
- BrakeLeakPatch.cs:24 - Working example of GetComponentInParent pattern
- dv-remote-dispatch/CarUpdater.cs:55 - SimController.train access pattern
- SimController_TrainCar_Relationship.md - Architecture documentation
