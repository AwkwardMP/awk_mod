using System.Net;

using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;

using HarmonyLib;
using UnityEngine;

using UnityEngine.SceneManagement;
using System;

namespace AwkwardMP
{
    
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class AwkwardMP : BaseUnityPlugin
    {
        public static string VersionString = "1.0.0";
        public Harmony Harmony { get; } = new Harmony(PluginInfo.PLUGIN_GUID);

        internal static ManualLogSource Log;

        public static ConfigEntry<string> WebSocketURL { get; private set; }

        public static GameObject ModUpdaterObj;

        private void Awake()
        {
            Log = base.Logger;

            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;


            WebSocketURL = Config.Bind("General", "Websocket URL", "ws://localhost:3000/ws");
            Harmony.PatchAll();


            AddSceneChangeCallbacks();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        public static void AddSceneChangeCallbacks()
        {
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode _) =>
            {
                var sceneName = scene.name;

                AwkwardMP.Log.LogInfo(scene.name);

                if (sceneName.Equals("2_MainMenu", StringComparison.Ordinal))
                {
                    ModUpdaterObj = new("AWKUpdater") { layer = 5 };
                    ModUpdaterObj.AddComponent<ModUpdater>();


                    AwkwardClient.AddUI(GameObject.Find("Globals/TopMostCanvas(Clone)/SafeAreaScaler"));
                }

                if (sceneName.Equals("3_InGame", StringComparison.Ordinal))
                {
                    AwkwardClient.AddUI(GameObject.Find("Globals/TopMostCanvas(Clone)/SafeAreaScaler"));
                    AwkwardClient.Enable();
                }
            };
        }

        private static bool ShowRoomCode = true;
        internal void Update()
        {
            // check master toggle
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ShowRoomCode = !ShowRoomCode;
                AwkwardClient.ToggleRoomCode(ShowRoomCode);
            }
        }
    }
}
