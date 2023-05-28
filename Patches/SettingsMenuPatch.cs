using AWK;
using HarmonyLib;
using System;
using UnityEngine;

namespace AwkwardMP.Patches
{
    [HarmonyPatch(typeof(SettingsMenu))]
    public class SettingsMenuPatch
    {
        public static SettingsMenu _instance;

        public static MenuButton _wsSettingsButton = null;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SettingsMenu.Show))]
        private static void PostfixShow(SettingsMenu __instance)
        {
            _instance = __instance;

            MenuButton templateButton = GameObject.Find("CreditsButton").GetComponent<MenuButton>();
            if (templateButton == null) return;

            if(_wsSettingsButton == null)
            {
                var wsSettingsButton = GameObject.Instantiate<MenuButton>(templateButton, templateButton.transform.parent);
                wsSettingsButton.name = "WebSocketSettingButton";
                wsSettingsButton.transform.localPosition = new Vector3(wsSettingsButton.transform.localPosition.x + 100, wsSettingsButton.transform.localPosition.y, wsSettingsButton.transform.localPosition.z);

                wsSettingsButton.SetTextFromData("AwkwardMP", true);
                wsSettingsButton.AssignButtonClickHandler(delegate { EditWebSocketURL(); });
                wsSettingsButton.EnableInput(true);

                _wsSettingsButton = wsSettingsButton.GetComponent<MenuButton>();
            }

            InputManagerPatch.ResetButton();
        }

        private static void EditWebSocketURL()
        {
            string webSocketURL = AwkwardMP.WebSocketURL.Value;
            Globals.TextInputWrapper.ShowOnScreenKeyboard("Enter WebSocket URL", 80, webSocketURL, delegate
            {
                OnEditWebSocketURL();
            }, false, true);
        }

        private static void OnEditWebSocketURL()
        {
            string latestInput = Globals.TextInputWrapper.GetLatestInput();

            AwkwardMP.WebSocketURL.Value = latestInput;
            AwkwardClient.StopSocket();
            AwkwardClient.StartSocket();
        }

        [HarmonyPostfix]
        [HarmonyPatch("SetButtonsEnabled")]
        private static void PostfixSetButtonsEnabled(bool enable)
        {
            if (_wsSettingsButton == null) return;
            _wsSettingsButton.EnableInput(enable);
        }

    }
}
