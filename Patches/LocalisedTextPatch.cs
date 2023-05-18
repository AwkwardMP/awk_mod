using AWK;

using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace AwkwardMP.Patches
{

    [HarmonyPatch]
    public class LocalisedTextPatch
    {
        private static bool Initialised = false;
        public static MethodBase TargetMethod()
        {
            return AccessTools.FirstMethod(typeof(LocalisedText), method => method.Name.Contains("InitTextLookup"));
        }

        // your patches
        public static void Postfix(ref LocalisedTextData ___m_textData)
        {
            if(!Initialised)
            {
                AwkwardMP.Log.LogInfo($"Patching LocalisedText");

                LocalisedTextEntry entry = new LocalisedTextEntry();
                entry.L[0] = "Create Room";
                entry.L[1] = "Create Room";
                entry.L[2] = "Raum Erstellen";

                ___m_textData.TextLookup.Add("General_CreateRoom", entry);

                Initialised = true;
            }
        }
    }
}
