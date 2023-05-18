using AWK;

using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace AwkwardMP.Patches
{
    [HarmonyPatch(typeof(InputManager))]
    public class InputManagerPatch
    {
        private static GameObject m_createRoomButton = null;
        private static GameObject m_playButton = null;

        private static Guid m_inputGuid = Guid.NewGuid();
        private static InputManager Instance = null;

        private static Vector3 oldPositionPlay = new Vector3(0, 0, 0);
        private static Vector3 oldPositionCreate = new Vector3(0, 0, 0);


        [HarmonyPostfix]
        [HarmonyPatch(nameof(InputManager.EnableInput))]
        private static void PostfixEnableInput(InputManager __instance, InputManager.Input rewiredAction, string displayTextKey, InputManager.DisplayPosition displayPosition, InputManager.InputDelegate inputDelegate, Guid guid)
        {
            if (displayTextKey == "General_Play")
            {
                if(m_createRoomButton == null)
                {
                    Instance = __instance;

                    var playButtonObj = GameObject.Find("InputDisplayButton_BR_0");
                    if (playButtonObj == null) return;
                    oldPositionPlay = playButtonObj.transform.localPosition;
                    playButtonObj.transform.localPosition = new Vector3(playButtonObj.transform.localPosition.x, playButtonObj.transform.localPosition.y + 150, playButtonObj.transform.localPosition.z);


                    var createRoomObj = GameObject.Find("InputDisplayButton_BR_1");
                    if (createRoomObj == null) return;
                    oldPositionCreate = createRoomObj.transform.localPosition;
                    createRoomObj.transform.localPosition = new Vector3(playButtonObj.transform.localPosition.x, playButtonObj.transform.localPosition.y - 150, playButtonObj.transform.localPosition.z);

                    m_createRoomButton = createRoomObj;
                    m_playButton = playButtonObj;

                }
                else
                {
                    ResetButton();

                    m_playButton.transform.localPosition = new Vector3(m_playButton.transform.localPosition.x, m_playButton.transform.localPosition.y + 150, m_playButton.transform.localPosition.z); ;
                    m_createRoomButton.transform.localPosition = new Vector3(m_playButton.transform.localPosition.x, m_playButton.transform.localPosition.y - 150, m_playButton.transform.localPosition.z); ;

                }

                __instance.EnableInput(InputManager.Input.AnswerRight, "General_CreateRoom", InputManager.DisplayPosition.BottomRight_1, new InputManager.InputDelegate(OnCreateRoomClicked), m_inputGuid);
            }
        }

        public static void ResetButton()
        {
            m_createRoomButton.transform.localPosition = oldPositionCreate;
            m_playButton.transform.localPosition = oldPositionPlay;
        }

        private static void OnCreateRoomClicked()
        {
            AwkwardMP.Log.LogInfo("Creating Room");
            AwkwardClient.DeleteRoom();
            AwkwardClient.CreateRoom();
        }
    }
}
