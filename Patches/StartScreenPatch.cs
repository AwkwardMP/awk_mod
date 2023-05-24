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
        [HarmonyPostfix]
        [HarmonyPatch(nameof(StartScreen.UpdateUserName))]
        private static void PostfixUpdateUserName(StartScreen __instance)
        {
            AwkwardClient.StartSocket();
        }

    }
}
