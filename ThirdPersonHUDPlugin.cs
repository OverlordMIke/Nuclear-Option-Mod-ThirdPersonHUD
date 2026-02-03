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
        private const string VersionString = "1.2.0";

        public static ManualLogSource Log { get; private set; }

        // Config entries
        private const string CurrentConfigVersion = "c1.0";
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<bool> OrbitPitchLadderHidden { get; set; }

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

            string newMode = GetCameraModeFromState(__instance, state);

            if (string.IsNullOrEmpty(newMode))
                return;

            ApplyHUDVisibility(newMode);
        }

        private static string GetCameraModeFromState(CameraStateManager camManager, CameraBaseState newState)
        {
            if (newState == camManager.cockpitState)
            {
                return "cockpit";
            }

            if (newState == camManager.TVState)
            {
                return "flyby";
            }

            if (newState == camManager.orbitState)
            {
                return "orbit";
            }

            // Fallback
            var mainCam = Camera.main;
            if (mainCam == null) return null;

            var parent = mainCam.transform?.parent?.parent;
            if (parent == null) return null;

            string parentName = parent.name;

            if (parentName == "helmetCamPoint")
                return "cockpit";

            if (parentName == "Datum")
                return "flyby";

            foreach (var orbitName in OrbitParents)
            {
                if (parentName == orbitName)
                    return "orbit";
            }

            return null;
        }

        private static void ApplyHUDVisibility(string mode)
        {
            if (mode == "cockpit")
            {
                Show("SceneEssentials/Canvas/HUDCanvas");
                Show("SceneEssentials/Canvas/HUDCanvas/HMDCenter/LowerLeftPanel/HUDMapAnchor/MapCanvas");
                if (ThirdPersonHUD.OrbitPitchLadderHidden.Value)
                {
                    Show("SceneEssentials/Canvas/HUDCanvas/HUDCenter/pitchCompassCenter");
                }
            }
            else if (mode == "orbit")
            {
                Show("SceneEssentials/Canvas/HUDCanvas");
                Show("SceneEssentials/Canvas/HUDCanvas/HMDCenter/LowerLeftPanel/HUDMapAnchor/MapCanvas");
                if (ThirdPersonHUD.OrbitPitchLadderHidden.Value)
                {
                    Hide("SceneEssentials/Canvas/HUDCanvas/HUDCenter/pitchCompassCenter");
                }
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
}