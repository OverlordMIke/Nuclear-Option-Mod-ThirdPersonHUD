using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace ThirdPersonHUD
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class ForceShowHUDPlugin : BaseUnityPlugin
    {
        // Mod identification
        private const string MyGUID = "com.gnol.thirdpersonhud";
        private const string PluginName = "ThirdPersonHUD";
        private const string VersionString = "1.0.2"; // Bumped for this change

        public static ManualLogSource Log { get; private set; }

        // Config entries
        private static ConfigEntry<bool> Enabled { get; set; }
        private static ConfigEntry<KeyboardShortcut> ToggleKey { get; set; }
        private static ConfigEntry<KeyboardShortcut> DebugKey { get; set; }

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
                defaultValue: false,  // Changed: starts disabled
                configDescription: new ConfigDescription(
                    "Whether the HUD forcing is active (forces HUD visible). Can be toggled in-game with the hotkey below."
                )
            );

            ToggleKey = Config.Bind(
                section: "Hotkeys",
                key: "Toggle Hotkey",
                defaultValue: new KeyboardShortcut(KeyCode.F10),
                configDescription: new ConfigDescription(
                    "Press this key (with optional modifiers) to toggle the mod on/off."
                )
            );

            DebugKey = Config.Bind(
                section: "Hotkeys",
                key: "Debug Hotkey",
                defaultValue: new KeyboardShortcut(KeyCode.F11),
                configDescription: new ConfigDescription(
                    "Press this key (with optional modifiers) to print debug message to console."
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
                    if (!isEnabled)
                    {
                        HideGameObjectByPath("SceneEssentials/Canvas/HUDCanvas");
                        HideGameObjectByPath("SceneEssentials/Canvas/HUDCanvas/HMDCenter/LowerLeftPanel/HUDMapAnchor/MapCanvas");
                    }
                }
            };

            Logger.LogInfo($"{PluginName} v{VersionString} loaded successfully. Initial state: {(isEnabled ? "Enabled" : "Disabled")}");
        }

        private void Update()
        {
            // Detect hotkey press (IsDown = fires once per press)
            if (ToggleKey.Value.IsDown())
            {
                isEnabled = !isEnabled;
                Enabled.Value = isEnabled; // Sync to config (auto-saves)
                Log.LogInfo($"ThirdPersonHUD toggled {(isEnabled ? "ON" : "OFF")} via hotkey");

                if (!isEnabled)
                {
                    HideGameObjectByPath("SceneEssentials/Canvas/HUDCanvas");
                    HideGameObjectByPath("SceneEssentials/Canvas/HUDCanvas/HMDCenter/LowerLeftPanel/HUDMapAnchor/MapCanvas");
                }
                // No immediate enable needed when turning ON → Update() will handle it next frames
            }

            if (DebugKey.Value.IsDown())
            {
                GameObject hudObject = GameObject.Find("Main Camera");
                if (hudObject != null)
                {
                    if (hudObject.transform.parent != null)
                    {
                        if (hudObject.transform.parent.transform.parent != null)
                        {
                            Log.LogInfo(hudObject.transform.parent.transform.parent.name);
                        }
                        else
                        {
                            Log.LogInfo("Main Camera Parent PARENT not found.");
                        }
                    }
                    else
                    {
                        Log.LogInfo("Main Camera Parent not found.");
                    }
                }
                else
                {
                    Log.LogInfo("Main Camera not found.");
                }
            }

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

        /// <summary>
        /// Immediately hides both HUD elements if they currently exist.
        /// Called when toggling off (or config changed to disabled).
        /// </summary>
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

        private void LogHierarchy(GameObject go, string indent = "")
        {
            if (go == null) return;

            Log.LogInfo($"{indent}→ {go.name} (active: {go.activeSelf})");

            foreach (Transform child in go.transform)
            {
                LogHierarchy(child.gameObject, indent + "  ");
            }
        }
    }
}