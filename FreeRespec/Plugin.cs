using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace FreeRespec;

[BepInAutoPlugin(id: "nozwock.FreeRespec")]
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
        // Searched for:
        // GUI_Menu_GrowMenu
        //      .resetButtonGO
        //      .resetAttCostText
        [HarmonyPostfix]
        [HarmonyPatch(
            typeof(Character_CustomModule_PlayerAttributesHandler),
            nameof(Character_CustomModule_PlayerAttributesHandler.getResetAttributesCost),
            MethodType.Getter)]
        static void Patch_Character_CustomModule_PlayerAttributesHandler_get_getResetAttributesCost_Postfix(ref int __result)
        {
            __result = 0;
        }

        [HarmonyPatch]
        class Patch_HuntRefund
        {
            // // Or, Could do this to always pass (getRefundPoints != 0) cases.
            // // But this would show the owned refund points as 1 in UI when 0 are owned.
            // //
            // // Searched for:
            // // GUI_Menu_GrowMenu
            // //      .refundCanvasGroup
            // //      .refundCostText
            // [HarmonyPostfix]
            // [HarmonyPatch(
            //     typeof(PlayerData_Hunt),
            //     nameof(PlayerData_Hunt.getRefundPoints),
            //     MethodType.Getter)]
            // static void Patch_PlayerData_Hunt_get_getRefundPoints_Postfix(ref int __result)
            // {
            //     if (__result == 0)
            //         __result = 1;
            // }

            [HarmonyPostfix]
            [HarmonyPatch(
                typeof(GUI_Menu_GrowMenu),
                nameof(GUI_Menu_GrowMenu.ShowFamilyInfo))]
            static void Patch_GUI_Menu_GrowMenu_ShowFamilyInfo_Postfix(GUI_Menu_GrowMenu __instance)
            {
                __instance.refundCostText.text = $"<color=#E7753E>0</color>";
            }

            [HarmonyTranspiler]
            [HarmonyPatch(
                typeof(GUI_Menu_GrowMenu),
                nameof(GUI_Menu_GrowMenu.StartRefundingHunt))]
            static IEnumerable<CodeInstruction> Patch_GUI_Menu_GrowMenu_StartRefundingHunt_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToArray();
                for (var i = 0; i < codes.Length; i++)
                {
                    yield return codes[i];

                    if (codes[i].Calls(AccessTools.PropertyGetter(
                            typeof(PlayerData_Hunt),
                            nameof(PlayerData_Hunt.getRefundPoints))))
                    {
                        yield return new CodeInstruction(OpCodes.Pop);
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    }
                }
            }

            [HarmonyTranspiler]
            [HarmonyPatch(
                typeof(GUI_Menu_GrowMenu),
                nameof(GUI_Menu_GrowMenu.RefundCurrentFamilyTalent))]
            static IEnumerable<CodeInstruction> Patch_GUI_Menu_GrowMenu_RefundCurrentFamilyTalent_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToArray();
                for (var i = 0; i < codes.Length; i++)
                {
                    if (codes[i].Calls(AccessTools.PropertyGetter(
                            typeof(PlayerData_Inventory),
                            nameof(PlayerData_Inventory.instance)))
                        && codes[i + 1].Calls(AccessTools.PropertyGetter(
                                typeof(GlobalParameters),
                                nameof(GlobalParameters.instance)))
                        && codes[i + 2].Calls(AccessTools.PropertyGetter(
                                typeof(GlobalParameters),
                                nameof(GlobalParameters.getItemParameters)))
                        && codes[i + 3].Calls(AccessTools.PropertyGetter(
                                typeof(GlobalParameters.ItemParameters),
                                nameof(GlobalParameters.ItemParameters.getRefundPointItem)))
                        && codes[i + 4].LoadsConstant(1)
                        && codes[i + 5].Calls(AccessTools.Method(
                                typeof(PlayerData_Inventory),
                                nameof(PlayerData_Inventory.RemoveItem)))
                    )
                    {
                        codes[i + 4] = new CodeInstruction(OpCodes.Ldc_I4_0);
                    }

                    yield return codes[i];
                }
            }
        }
    }
}

