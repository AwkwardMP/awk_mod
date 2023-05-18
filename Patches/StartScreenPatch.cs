using AWK;
using BepInEx;

using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

using System.Threading;
using System;
using System.Runtime.Remoting.Contexts;
using UnityEngine.UI;

namespace AwkwardMP.Patches
{
    [HarmonyPatch(typeof(StartScreen))]
    public class StartScreenPatch
    {

        private static GameObject modTextObject = null;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(StartScreen.Show))]
        private static void PostfixShow(StartScreen __instance)
        {
            var template = GameObject.Find("StartScreen");
            if (template == null) return;

            var versionText = template.transform.Find("VersionNumber");
            if (versionText == null) return;

            var modTextTransform = GameObject.Instantiate<Transform>(versionText, versionText.parent);
            modTextTransform.name = "ModText";
            modTextTransform.transform.localPosition = new Vector3(modTextTransform.transform.localPosition.x, modTextTransform.transform.localPosition.y + 150.0f, modTextTransform.transform.localPosition.z);

            var textModUpdate = modTextTransform.transform.GetComponent<TMPro.TMP_Text>();
            textModUpdate.text = "AwkwardMP";

            modTextObject = GameObject.Find("ModText");
            if (modTextObject == null) return;

            modTextObject.SetActive(false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(StartScreen.UpdateUserName))]
        private static void PostfixUpdateUserName(StartScreen __instance)
        {
            if (modTextObject == null) return;
            modTextObject.SetActive(true);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(StartScreen.Hide))]
        private static void PostfixHide(StartScreen __instance)
        {
            var modText = GameObject.Find("ModText");
            if (modText == null) return;

            modText.SetActive(false);
        }
    }
}
