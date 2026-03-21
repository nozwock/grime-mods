using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace AlwaysOnWarp;

[BepInAutoPlugin(id: "nozwock.AlwaysOnWarp")]
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
        [HarmonyPatch(typeof(GUI_CheckpointOptionsMenu), nameof(GUI_CheckpointOptionsMenu.OpenMenu))]
        class Patch_SetActiveTeleport
        {
            static void Prefix(GUI_CheckpointOptionsMenu __instance, out bool __state)
            {
                __state = __instance.isActive;
            }

            static void Postfix(GUI_CheckpointOptionsMenu __instance, bool __state)
            {
                if (__state) return;

                __instance.panelButtons
                    .FirstOrDefault(it => it.name.IndexOf("Teleport", StringComparison.OrdinalIgnoreCase) >= 0)
                    ?.gameObject
                    .SetActive(true);
            }
        }
    }
}

