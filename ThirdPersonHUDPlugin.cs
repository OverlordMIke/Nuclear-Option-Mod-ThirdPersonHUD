using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirage;
using System.Linq;

namespace ThirdPersonHUD
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class ThirdPersonHUD : BaseUnityPlugin
    {
        // Mod identification
        private const string MyGUID = "com.gnol.thirdpersonhud";
        private const string PluginName = "ThirdPersonHUD";
        private const string VersionString = "1.2.2";

        public static ManualLogSource Log { get; private set; }

        // Config entries
        private const string CurrentConfigVersion = "c1.0";
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<bool> OrbitPitchLadderHidden { get; set; }

        // Variables
        public static string currentCameraMode = "";

        public static bool isSpectating = false;

        private void Awake()
        {
            Log = Logger;
            Logger.LogInfo($"{PluginName} v{VersionString} is loading...");

            var savedVersion = Config.Bind(
                section: "Internal",
                key: "ConfigVersion",
                defaultValue: "",
                "Do not touch - used for auto-updating config."
            ).Value;

            Enabled = Config.Bind(
                section: "General",
                key: "Enabled",
                defaultValue: true,
                configDescription: new ConfigDescription(
                    "Whether the mod is active."
                )
            );

            OrbitPitchLadderHidden = Config.Bind(
                section: "General",
                key: "HidePitchLadder",
                defaultValue: true,
                configDescription: new ConfigDescription(
                    "If true, the pitch ladder will be hidden when in orbit/chase."
                )
            );

            if (savedVersion != CurrentConfigVersion)
            {
                Logger.LogInfo($"Config version changed ({savedVersion} -> {CurrentConfigVersion}). Updating config.");

                var savedVersionConfig = Config.Bind(
                    section: "Internal",
                    key: "ConfigVersion",
                    defaultValue: CurrentConfigVersion,
                    configDescription: new ConfigDescription(
                        "Do not touch - used for auto-updating config."
                    )
                );

                savedVersionConfig.Value = CurrentConfigVersion;

                Config.Save();

                Logger.LogInfo("Config automatically updated.");
            }

            var harmony = new Harmony(MyGUID);
            harmony.PatchAll();

            Logger.LogInfo($"{PluginName} v{VersionString} loaded successfully. State: {(Enabled.Value ? "Enabled" : "Disabled")}");
            //(added System.Linq for this)Log.LogInfo($"Patched SetFollowingUnit: {harmony.GetPatchedMethods().Any(m => m.Name == nameof(CameraStateManager.SetFollowingUnit))}");
        }

        public static void ApplyHUDVisibility()
        {
            if (currentCameraMode == "cockpit")
            {
                if (isSpectating)
                {
                    Show("SceneEssentials/Canvas/HUDCanvas/HMDCenter/LowerLeftPanel/HUDMapAnchor/MapCanvas");
                    Hide("SceneEssentials/Canvas/HUDCanvas");
                }
                else
                {
                    Show("SceneEssentials/Canvas/HUDCanvas");
                    Show("SceneEssentials/Canvas/HUDCanvas/HUDCenter/pitchCompassCenter");
                    Show("SceneEssentials/Canvas/HUDCanvas/HMDCenter/LowerLeftPanel/HUDMapAnchor/MapCanvas");
                }
            }
            else if ((currentCameraMode == "orbit" || currentCameraMode == "chase"))
            {
                if (isSpectating)
                {
                    Hide("SceneEssentials/Canvas/HUDCanvas");
                }
                else
                {
                    Show("SceneEssentials/Canvas/HUDCanvas");
                    Show("SceneEssentials/Canvas/HUDCanvas/HMDCenter/LowerLeftPanel/HUDMapAnchor/MapCanvas");
                    if (ThirdPersonHUD.OrbitPitchLadderHidden.Value)
                    {
                        Hide("SceneEssentials/Canvas/HUDCanvas/HUDCenter/pitchCompassCenter");
                    }
                    else
                    {
                        Show("SceneEssentials/Canvas/HUDCanvas/HUDCenter/pitchCompassCenter");
                    }
                }
            }
            else
            {
                Hide("SceneEssentials/Canvas/HUDCanvas");
            }
        }

        private static void Show(string path)
        {
            var go = GameObject.Find(path);
            if (go != null && !go.activeSelf)
                go.SetActive(true);
        }

        private static void Hide(string path)
        {
            var go = GameObject.Find(path);
            if (go != null && go.activeSelf)
                go.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(CameraStateManager), nameof(CameraStateManager.SwitchState))]
    internal static class CameraStateManager_SwitchState_Patch
    {
        private static readonly string[] OrbitParents =
        {
            "COIN", "trainer", "UtilityHelo1", "CAS1", "AttackHelo1",
            "Fighter1", "SmallFighter1", "QuadVTOL1", "Multirole1",
            "EW1", "Darkreach"
        };

        static void Postfix(CameraStateManager __instance, CameraBaseState state)
        {
            if (!ThirdPersonHUD.Enabled.Value)
                return;

            SetCameraModeFromState(__instance, state);
            ThirdPersonHUD.ApplyHUDVisibility();
            //ThirdPersonHUD.Log.LogInfo($"\nCamera Mode: {ThirdPersonHUD.currentCameraMode}\nSpectating: {ThirdPersonHUD.isSpectating}");
        }

        private static void SetCameraModeFromState(CameraStateManager camManager, CameraBaseState newState)
        {
            switch (newState)
            {
                case var s when s == camManager.cockpitState:
                    ThirdPersonHUD.currentCameraMode = "cockpit";
                    break;

                case var s when s == camManager.TVState:
                    ThirdPersonHUD.currentCameraMode = "flyby";
                    break;

                case var s when s == camManager.orbitState:
                    ThirdPersonHUD.currentCameraMode = "orbit";
                    break;

                case var s when s == camManager.freeState:
                    ThirdPersonHUD.currentCameraMode = "free";
                    break;

                case var s when s == camManager.controlledState:
                    ThirdPersonHUD.currentCameraMode = "control";
                    break;

                case var s when s == camManager.chaseState:
                    ThirdPersonHUD.currentCameraMode = "chase";
                    break;

                default:
                    ThirdPersonHUD.Log.LogInfo("Error: Could not determine camera mode.");
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(GameplayUI), nameof(GameplayUI.ResumeGame))]
    internal static class GameplayUI_ResumeGame_Patch
    {
        static void Postfix()
        {
            if (!ThirdPersonHUD.Enabled.Value)
                return;
            ThirdPersonHUD.ApplyHUDVisibility();
        }
    }

    [HarmonyPatch(typeof(DynamicMap), nameof(DynamicMap.Minimize))]
    internal static class DynamicMap_Minimize_Patch
    {
        static void Postfix()
        {
            if (!ThirdPersonHUD.Enabled.Value)
                return;
            ThirdPersonHUD.ApplyHUDVisibility();
        }
    }

    /*[HarmonyPatch(typeof(DynamicMap), nameof(DynamicMap.Maximize))]
    internal static class DynamicMap_Maximize_Patch
    {
        static void Postfix()
        {
        }
    }*/
    // Unused for now

    [HarmonyPatch(typeof(GameplayUI), nameof(GameplayUI.SelectAircraft))]
    internal static class GameplayUI_SelectAircraft_Patch
    {
        static void Postfix()
        {
            if (!ThirdPersonHUD.Enabled.Value)
                return;
            ThirdPersonHUD.isSpectating = false;
        }
    }

    [HarmonyPatch(typeof(CameraStateManager), nameof(CameraStateManager.SetFollowingUnit))]
    internal static class CameraStateManager_SetFollowingUnit_Patch
    {
        static void Postfix(Unit unit)
        {
            if (!ThirdPersonHUD.Enabled.Value)
                return;
            var localAircraft = SceneSingleton<CombatHUD>.i?.aircraft;
            bool isLocalPlayer = localAircraft != null && localAircraft == unit;
            ThirdPersonHUD.isSpectating = !isLocalPlayer;
        }
    }
}