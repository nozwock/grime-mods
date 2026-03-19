using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LegibleMapMarkers;

[BepInAutoPlugin(id: "nozwock.LegibleMapMarkers")]
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
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapMarkers), nameof(MapMarkers.Awake))]
        static void MapMarkers_Awake_Postfix(MapMarkers __instance)
        {
            foreach (var kvp in __instance.markerPrefabs)
            {
                if (!kvp.Key.Contains("Custom Marker")) continue;

                var go = kvp.Value;

                try
                {
                    var icon = go.transform.Find("Icon").GetComponent<RectTransform>();
                    var bg = go.transform.Find("Background").GetComponent<RectTransform>();

                    bg.gameObject.SetActive(true);

                    var img = bg.gameObject.GetComponent<UnityEngine.UI.Image>();
                    img.color = Color.black;

                    bg.anchorMin = icon.anchorMin;
                    bg.anchorMax = icon.anchorMax;
                    bg.pivot = icon.pivot;
                    bg.anchoredPosition = icon.anchoredPosition - new Vector2(0.3f, 0f);
                    bg.sizeDelta -= new Vector2(12f, 12f);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
        }
    }
}

