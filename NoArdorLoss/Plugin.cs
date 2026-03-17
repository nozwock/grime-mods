using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace NoArdorLoss;

[BepInAutoPlugin(id: "nozwock.NoArdorLoss")]
public partial class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    static Harmony harmony;

    const string configCategoryGeneral = "General";
    static ConfigEntry<bool> configNoArdorLossOnDeath;

    void Awake()
    {
        Logger = base.Logger;

        configNoArdorLossOnDeath = Config.Bind(
            configCategoryGeneral,
            "NoArdorLossOnDeath",
            false,
            "Requires restart to take effect.");

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
            typeof(Gameplay_Player_DroppedCurrency),
            nameof(Gameplay_Player_DroppedCurrency.HealDelay),
            MethodType.Enumerator)]
        static IEnumerable<CodeInstruction> Patch_Gameplay_Player_DroppedCurrency_HealDelay_Transpiler(
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
                            typeof(Gameplay_Player_DroppedCurrency),
                            nameof(Gameplay_Player_DroppedCurrency.fullArdorReturnTalent))))
                {
                    Logger.LogDebug("Making IsTalentAquired() in HealDelay() return true");

                    // Always return true
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                }
            }
        }

        [HarmonyPatch(
            typeof(Character_CustomModule_PlayerController),
            nameof(Character_CustomModule_PlayerController.OnPlayerDied))]
        class Patch_Character_CustomModule_PlayerController_OnPlayerDied
        {
            static bool Prepare()
            {
                return configNoArdorLossOnDeath.Value;
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var justMatchArdorZeroed = false;

                var codes = instructions.ToArray();
                for (var i = 0; i < codes.Length; i++)
                {
                    justMatchArdorZeroed = false;

                    // Prevent ardor from being reset to 0
                    // NOTE: Not doing Nops because of jump label being messed (isn't Harmony supposed to handle that?
                    // Maybe it's because this is older BepInEx/Harmony?)
                    if (codes[i].Calls(AccessTools.PropertySetter(
                            typeof(Character_CustomModule_PlayerAttributesHandler),
                            nameof(Character_CustomModule_PlayerAttributesHandler.ardor))))
                    {
                        justMatchArdorZeroed = true;

                        Logger.LogDebug("Altering instruction `attributesHandler.ardor = 0`"
                            + " -> `attributesHandler.ardor = attributesHandler.ardor`");
                        yield return new CodeInstruction(OpCodes.Pop);
                        yield return Transpilers.EmitDelegate(() =>
                        {
                            return Character_CustomModule_PlayerController.instance.attributesHandler.ardor;
                        });
                    }

                    yield return codes[i];

                    if (justMatchArdorZeroed)
                    {
                        Logger.LogDebug("Setting `.deadVessel_ardorAmount = 0`");
                        yield return Transpilers.EmitDelegate(() =>
                        {
                            SyncHandler.getGeneralData.deadVessel_ardorAmount = 0;
                        });
                    }
                }
            }
        }
    }
}

