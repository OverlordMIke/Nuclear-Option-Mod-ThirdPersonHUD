using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace ThirdPersonHUD
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class ThirdPersonHUD : BaseUnityPlugin
    {
        // Mod identification
        private const string MyGUID = "com.gnol.thirdpersonhud";
        private const string PluginName = "ThirdPersonHUD";
        private const string VersionString = "1.1.1";

        public static ManualLogSource Log { get; private set; }

        // Config entries
        private static ConfigEntry<bool> Enabled { get; set; }

        // Runtime state
        private bool isEnabled;

        // Camera References
        private readonly string[] orbit = { "COIN", "trainer", "UtilityHelo1", "CAS1", "AttackHelo1", "Fighter1", "SmallFighter1", "QuadVTOL1", "Multirole1", "EW1", "Darkreach" };
        
        private void Awake()
        {
            Log = Logger;
            Logger.LogInfo($"{PluginName} v{VersionString} is loading...");

            // Bind config options
            Enabled = Config.Bind(
                section: "General",
                key: "Enabled",
                defaultValue: true,
                configDescription: new ConfigDescription(
                    "Whether or not the mod is active."
                )
            );

            // Initialize runtime state from config
            isEnabled = Enabled.Value;

            // React if user changes "Enabled" directly in Configuration Manager
            Enabled.SettingChanged += (sender, args) =>
            {
                bool newState = Enabled.Value;
                if (newState != isEnabled)
                {
                    isEnabled = newState;
                    Log.LogInfo($"Mod enabled state changed via config: {isEnabled}");
                }
            };

            Logger.LogInfo($"{PluginName} v{VersionString} loaded successfully. Initial state: {(isEnabled ? "Enabled" : "Disabled")}");
        }

        private void Update()
        {
            // Only force-enable if currently enabled
            if (!isEnabled)
                return;

            // Check Camera Mode
            string cameraName = "Main Camera";
            GameObject cameraObject = GameObject.Find(cameraName);
            if (cameraObject == null)
            {
                return;
            }

            string camParentName = cameraObject.transform?
                              .parent?
                              .parent?
                              .name;

            if (camParentName == null)
            {
                return;
            }

            string currentMode = "";

            for (int i = 0; i < orbit.Length; i++)
            {
                if (camParentName == orbit[i])
                {
                    currentMode = "orbit";
                }
            }

            if (camParentName == "helmetCamPoint")
            {
                currentMode = "cockpit";
            }

            if (camParentName == "Datum")
            {
                currentMode = "flyby";
            }

            if (currentMode == "")
            {
                return;
            }

            if (currentMode == "cockpit")
            {
                ShowGameObjectByPath("SceneEssentials/Canvas/HUDCanvas");
                ShowGameObjectByPath("SceneEssentials/Canvas/HUDCanvas/HUDCenter/pitchCompassCenter");
                ShowGameObjectByPath("SceneEssentials/Canvas/HUDCanvas/HMDCenter/LowerLeftPanel/HUDMapAnchor/MapCanvas");
            }
            
            if (currentMode == "orbit")
            {
                ShowGameObjectByPath("SceneEssentials/Canvas/HUDCanvas");
                HideGameObjectByPath("SceneEssentials/Canvas/HUDCanvas/HUDCenter/pitchCompassCenter");
                ShowGameObjectByPath("SceneEssentials/Canvas/HUDCanvas/HMDCenter/LowerLeftPanel/HUDMapAnchor/MapCanvas");
            }

            if (currentMode == "flyby")
            {
                HideGameObjectByPath("SceneEssentials/Canvas/HUDCanvas");
            }
        }

        private void HideGameObjectByPath(string path)
        {
            GameObject go = GameObject.Find(path);
            if (go != null && go.activeSelf)
            {
                go.SetActive(false);
            }
        }

        private void ShowGameObjectByPath(string path)
        {
            GameObject go = GameObject.Find(path);
            if (go != null && !go.activeSelf)
            {
                go.SetActive(true);
            }
        }
    }
}