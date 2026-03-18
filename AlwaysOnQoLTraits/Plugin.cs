using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace AlwaysOnQoLTraits;

[BepInAutoPlugin(id: "nozwock.AlwaysOnQoLTraits")]
public partial class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    static Harmony harmony;

    void Awake()
    {
        Logger = base.Logger;

        Logger.LogInfo($"Plugin {Id} is loaded!");

        try
        {
            harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Id);
            foreach (var method in harmony.GetPatchedMethods())
            {
                Logger.LogInfo($"Patched method: {method.DeclaringType.FullName}.{method.Name}");
            }
            if (harmony.GetPatchedMethods().Count() == 0)
            {
                Logger.LogError($"Failed to apply Harmony patches.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Harmony patching failed: {ex}");
        }
    }

    // void OnDestroy()
    // {
    //     Logger.LogInfo($"Unloading plugin {Id}");
    //     harmony?.UnpatchSelf();
    // }

    [HarmonyPatch]
    class Patches
    {
        [HarmonyTranspiler]
        [HarmonyPatch(
            typeof(Character_CustomModule_PlayerController),
            nameof(Character_CustomModule_PlayerController.SetWalkMode))]
        static IEnumerable<CodeInstruction> Patch_Character_CustomModule_PlayerController_SetWalkMode_Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToArray();
            for (var i = 0; i < codes.Length; i++)
            {
                yield return codes[i];

                if (codes[i].Calls(AccessTools.Method(
                        typeof(PlayerData_Talents),
                        nameof(PlayerData_Talents.IsTalentAquired)))
                    && codes[i - 1].LoadsField(AccessTools.Field(
                            typeof(Character_CustomModule_PlayerController),
                            nameof(Character_CustomModule_PlayerController.walk_talentRequired))))
                {
                    Logger.LogDebug("Making IsTalentAquired() in SetWalkMode() return true");

                    // Always return true
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                }
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(
            typeof(Effect_SecretPlaceIndicator),
            nameof(Effect_SecretPlaceIndicator.HandleEmissionRate))]
        static IEnumerable<CodeInstruction> Patch_Effect_SecretPlaceIndicator_HandleEmissionRate_Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToArray();
            for (var i = 0; i < codes.Length; i++)
            {
                yield return codes[i];

                if (codes[i].Calls(AccessTools.Method(
                        typeof(PlayerData_Talents),
                        nameof(PlayerData_Talents.IsTalentAquired))))
                {
                    Logger.LogDebug("Making IsTalentAquired() in "
                        + "Effect_SecretPlaceIndicator.HandleEmissionRate() return true");

                    // Always return true
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                }
            }
        }
    }
}

