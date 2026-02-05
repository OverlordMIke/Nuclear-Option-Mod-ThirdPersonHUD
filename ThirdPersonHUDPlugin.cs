using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ThirdPersonHUD
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class ThirdPersonHUD : BaseUnityPlugin
    {
        // Mod identification
        private const string MyGUID = "com.gnol.thirdpersonhud";
        private const string PluginName = "ThirdPersonHUD";
        private const string VersionString = "1.2.1";

        public static ManualLogSource Log { get; private set; }

        // Config entries
        private const string CurrentConfigVersion = "c1.0";
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<bool> OrbitPitchLadderHidden { get; set; }

        // Variables
        public static string currentCameraMode = "";

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
        }

        public static void ApplyHUDVisibility()
        {
            if (currentCameraMode == "cockpit")
            {
                Show("SceneEssentials/Canvas/HUDCanvas");
                Show("SceneEssentials/Canvas/HUDCanvas/HMDCenter/LowerLeftPanel/HUDMapAnchor/MapCanvas");
                if (ThirdPersonHUD.OrbitPitchLadderHidden.Value)
                {
                    Show("SceneEssentials/Canvas/HUDCanvas/HUDCenter/pitchCompassCenter");
                }
            }
            else if (currentCameraMode == "orbit" || currentCameraMode == "chase")
            {
                Show("SceneEssentials/Canvas/HUDCanvas");
                Show("SceneEssentials/Canvas/HUDCanvas/HMDCenter/LowerLeftPanel/HUDMapAnchor/MapCanvas");
                if (ThirdPersonHUD.OrbitPitchLadderHidden.Value)
                {
                    Hide("SceneEssentials/Canvas/HUDCanvas/HUDCenter/pitchCompassCenter");
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
        }

        private static void SetCameraModeFromState(CameraStateManager camManager, CameraBaseState newState)
        {
            if (newState == camManager.cockpitState)
            {
                ThirdPersonHUD.currentCameraMode = "cockpit";
            }
            else if (newState == camManager.TVState)
            {
                ThirdPersonHUD.currentCameraMode = "flyby";
            }
            else if (newState == camManager.orbitState)
            {
                ThirdPersonHUD.currentCameraMode = "orbit";
            }
            else if (newState == camManager.freeState)
            {
                ThirdPersonHUD.currentCameraMode = "free";
            }
            else if (newState == camManager.controlledState)
            {
                ThirdPersonHUD.currentCameraMode = "control";
            }else if (newState == camManager.chaseState)
            {
                ThirdPersonHUD.currentCameraMode = "chase";
            }

            /* Fallback
            var mainCam = Camera.main;
            if (mainCam == null) return;

            var parent = mainCam.transform?.parent?.parent;
            if (parent == null) return;

            string parentName = parent.name;

            if (parentName == "helmetCamPoint")
                ThirdPersonHUD.currentCameraMode = "cockpit";

            if (parentName == "Datum")
                ThirdPersonHUD.currentCameraMode = "flyby";

            foreach (var orbitName in OrbitParents)
            {
                if (parentName == orbitName)
                {
                    ThirdPersonHUD.currentCameraMode = "orbit";
                    break;
                }
            }*/
        }
    }

    [HarmonyPatch(typeof(GameplayUI), nameof(GameplayUI.ResumeGame))]
    internal static class GameplayUI_ResumeGame_Patch
    {
        static void Postfix()
        {
            ThirdPersonHUD.ApplyHUDVisibility();
        }
    }
}