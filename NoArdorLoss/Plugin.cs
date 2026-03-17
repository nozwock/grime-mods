using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
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

    void Awake()
    {
        Logger = base.Logger;

        Logger.LogInfo($"Plugin {Id} is loaded!");

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
    }
}

