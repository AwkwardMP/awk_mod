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
    [HarmonyPatch(typeof(NumberOfPlayersMenu))]
    public class NumberOfPlayersMenuPatch
    {

        [HarmonyPostfix]
        [HarmonyPatch(nameof(NumberOfPlayersMenu.Show))]
        private static void PostfixShow(NumberOfPlayersMenu __instance)
        {
            InputManagerPatch.ResetButton();
        }
    }
}
