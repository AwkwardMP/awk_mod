

using UnityEngine;

using AWK;
using UnityEngine.UI;
using System.Net;
using WebSocketSharp;
using System.Runtime.Remoting.Messaging;
using System;
using AwkwardMP.MessageTypes;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

namespace AwkwardMP
{
    namespace MessageTypes
    {
        internal class AwkMessage
        {
            public string _type { get; set; }
            public object _params { get; set; }

            public AwkMessage(string _type, object _params = null)
            {
                this._type = _type;
                if (_params == null)
                {
                    this._params = new object();
                }
                else
                {
                    this._params = _params;
                }
            }
        }
    }

    internal static class AwkwardClient
    {
        private static InGameSceneLogic inGameSceneLogic = null;

        private static GameObject connectStatusObject = null;
        private static GameObject roomCodeObject = null;

        private static TMPro.TMP_Text roomCodeTextComponent = null;
        private static TMPro.TMP_Text connectedTextObject = null;


        private static WebSocket wsClient = null;
        private static string RoomCode;

        private static bool _initialized = false;
        private static Dictionary<int, string> connectedPlayers = new Dictionary<int, string>();

        public static void Init()
        {
            if (!_initialized)
            {
                AddToUserInterface();
                _initialized = true;
            }
        }

        public static void Enable()
        {
            if(inGameSceneLogic == null)
            {
                var sceneLogic = GameObject.Find("SceneLogic");
                if (sceneLogic == null) return;

                inGameSceneLogic = sceneLogic.GetComponent<InGameSceneLogic>();
            }
        }


        private static void AddToUserInterface()
        {
            var topMostCanvas = GameObject.Find("Globals/TopMostCanvas(Clone)/SafeAreaScaler");
            if (topMostCanvas == null) return;

            var startScreen = GameObject.Find("StartScreen");
            if (startScreen == null) return;

            var template = startScreen.transform.Find("VersionNumber");
            if (template == null) return;

            var connectedObj = GameObject.Instantiate<Transform>(template, topMostCanvas.transform);
            connectedObj.name = "ConnectStatus";
            connectedObj.transform.localPosition = new Vector3(-1500, 1000.0f, connectedObj.transform.localPosition.z);
            var connectedText = connectedObj.transform.GetComponent<TMPro.TMP_Text>();
            connectedText.text = "Disconnected";
            connectedText.alignment = TMPro.TextAlignmentOptions.Left;




            connectStatusObject = GameObject.Find("ConnectStatus");
            connectedTextObject = connectedText;


            var roomCodeObj = GameObject.Instantiate<Transform>(connectedObj, connectedObj.parent);
            roomCodeObj.name = "RoomCode";
            roomCodeObj.transform.localPosition = new Vector3(-1645.0f, 900.0f, roomCodeObj.transform.localPosition.z);

            roomCodeTextComponent = roomCodeObj.transform.GetComponent<TMPro.TMP_Text>();
            roomCodeTextComponent.text = "Room:";
            roomCodeTextComponent.fontSize = 64;
            roomCodeTextComponent.alignment = TMPro.TextAlignmentOptions.Center;

            roomCodeObject = GameObject.Find("RoomCode");

            roomCodeObject.AddComponent<Button>();
            roomCodeObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                GUIUtility.systemCopyBuffer = RoomCode;
            });

            ToggleRoomCode(false);
        }

        private static void UpdateConnectionSprite()
        {
            if(wsClient != null)
            {
                if (wsClient.ReadyState == WebSocketState.Open)
                {
                    if (connectedTextObject != null)
                    {
                        connectedTextObject.text = "Connected";
                    }
                }
                else
                {
                    if (connectedTextObject != null)
                    {
                        connectedTextObject.text = "Disconnected!";
                    }
                }
            }
            
        }

        public static void ToggleRoomCode(bool show)
        {
            if (show)
            {
                roomCodeTextComponent.text = "Room: " + RoomCode + "\n(F1 to Hide)";
                roomCodeObject.SetActive(true);
            }
            else
            {
                roomCodeTextComponent.text = "Room: XXXXXX" + "\n(F1 to Hide)";
                roomCodeObject.SetActive(false);
            }
        }

        

        /************************************************************************************
         *                         Server Events
         * **********************************************************************************/

        public static void CreateRoom()
        {
            if (wsClient == null) StartSocket();

            if(wsClient != null && wsClient.ReadyState == WebSocketState.Open)
                SendMessage("H_CreateRoom", new {});
        }

        private static void OnCreateRoomSuccess(string _roomCode)
        {
            RoomCode = _roomCode;

            ToggleRoomCode(true);
            AwkwardMP.Log.LogInfo($"RoomCode: {_roomCode}");
        }

        private static void OnCreateRoomFailed(string reason)
        {
            AwkwardMP.Log.LogError($"Failed to create Room! {reason}");
        }

        public static void DeleteRoom()
        {
            if (RoomCode == null) return;

            object _params = new
            {
                roomId = RoomCode,
            };

            SendMessage("H_DeleteRoom", _params);
            RoomCode = null;
        }

        private static void ReConnectToRoom()
        {
            if (RoomCode == null) return;

            object _params = new
            {
                roomId = RoomCode,
            };

            SendMessage("H_ReconnectRoom", _params);
        }

        public static void SetMaxPlayers(int maxPlayers)
        {
            object _params = new
            {
                roomId = RoomCode,
                maxPlayers = maxPlayers
            };

            SendMessage("H_SetMaxPlayers", _params);
        }

        public static void ShowScore(int m_AverageScorePercentage, object playerScore, bool isEndOfGame, bool isTeamGame)
        {
            
            object _params = new
            {
                roomId = RoomCode,
                avgScore = m_AverageScorePercentage,
                playerScore = playerScore,
                isEndOfGame = isEndOfGame,
                isTeamGame = isTeamGame
            };

            SendMessage("H_ShowScore", _params);
        }

        
        public static void ShowNextRound(int roundIndex)
        {

            object _params = new
            {
                roomId = RoomCode,
                roundIndex = roundIndex,
            };

            SendMessage("H_ShowNextRound", _params);
        }



       
        public static void RevealAnswer(int chosenAnswerID, int guessedAnswerID, bool isCorrect)
        {
            object _params = new
            {
                roomId = RoomCode,
                chosenAnswerID = chosenAnswerID,
                guessedAnswerID = guessedAnswerID,
                isCorrect = isCorrect
            };

            bWaitingForClient = false;
            SendMessage("H_RevealAnswer", _params);
        }

        public static void AnnounceStats(int answer1Percentage, int answer2Percentage)
        {
            object _params = new
            {
                roomId = RoomCode,
                answer1Percentage = answer1Percentage,
                answer2Percentage = answer2Percentage,
            };

            SendMessage("H_AnnounceStats", _params);
        }

        public static void StartNextTurn(bool isChoosing, string choosingPlayerName, string guessingPlayerName)
        {
            object _params = new
            {
                roomId = RoomCode,
                isChoosing = isChoosing,
                choosingPlayerName = choosingPlayerName,
                guessingPlayerName = guessingPlayerName
            };

            SendMessage("H_StartNextTurn", _params);
        }


        private static bool bWaitingForClient = false;
        public static void BroadcastQuestion(object question, int playerIndex, bool isChoosing)
        {
            object _params = new
            {
                roomId = RoomCode,
                question = question,
                playerIndex = playerIndex,
                isChoosing = isChoosing,
            };

            bWaitingForClient = true;
            SendMessage("H_BroadcastQuestion", _params);
        }



        private static void OnGetGameInfo(int playerIndex)
        {
            try
            {
                object _params = new
                {
                    roomId = RoomCode,
                    choosingPlayerName = Globals.GameState.GetChoosingPlayerNameForCurrentQuestion(),
                    guessingPlayerName = Globals.GameState.GetGuessingPlayerNameForCurrentQuestion(),
                    question = Helper.CurrentQuestion(),
                    WaitingForClient = bWaitingForClient,
                    playerIndex = playerIndex,
                };

                SendMessage("H_GetGameInfoSuccess", _params);
            }
            catch (Exception ex)
            {
                AwkwardMP.Log.LogError(ex);
                return;
            }
        }

        private static void OnAnswer(int _answer, int playerIndex)
        {
            if (inGameSceneLogic == null) return;

            bWaitingForClient = false;
            inGameSceneLogic.OnAnswerChoosen(_answer);
            
        }

        public static void FixPlayerNames()
        {
            foreach(KeyValuePair<int, string> pair in connectedPlayers)
            {
                Globals.GameState.ChangePlayerName(pair.Key, pair.Value);
            }
        }

        private static void OnChangePlayerName(int _playerIndex, string _newPlayerName)
        {
            try
            {
                Globals.GameState.ChangePlayerName(_playerIndex, _newPlayerName);
            } catch(Exception ex)
            {
                AwkwardMP.Log.LogInfo("Failed to change Player Name" + ex.Message);
            }


            object _params = new
            {
                roomId = RoomCode,
            };

            if(connectedPlayers.ContainsKey(_playerIndex))
            {
                connectedPlayers[_playerIndex] = _newPlayerName;
            } else {
                connectedPlayers.Add(_playerIndex, _newPlayerName);
            }
            SendMessage("H_ChangePlayerNameSuccess", _params);
        }

       
        /************************************************************************************
         *                         Socket
         * **********************************************************************************/
        public static void SendMessage(string _type, object _params )
        {
            if (wsClient == null) return;

            if (wsClient.ReadyState == WebSocketState.Open)
            {
                AwkMessage message = new AwkMessage(_type, _params);
                wsClient.Send(JsonConvert.SerializeObject(message));
            }
            else
            {
                AwkwardMP.Log.LogError($"Unable to send message - client not connected");
            }
        }

        public static void StartSocket()
        {
            if(wsClient == null)
            {
                try
                {
                    var url = AwkwardMP.WebSocketURL.Value;


                    wsClient = new WebSocket(url);
                    wsClient.OnMessage += HandleOnMessage;
                    wsClient.OnOpen += HandleOnOpen;
                    wsClient.OnClose += HandleOnClose;
                    wsClient.OnError += HandleOnError;
                   

                    ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                    wsClient.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                    wsClient.Connect();
                }
                catch (Exception ex)
                {
                    AwkwardMP.Log.LogInfo($"Error connecting to Server: {ex.ToString()}");
                    wsClient = null;
                }
            }
        }



        public static void StopSocket()
        {
            if(wsClient != null && wsClient.ReadyState == WebSocketState.Open)
            {
                wsClient.Close();
                wsClient = null;
            }
        }

        private static bool doNotReconnect = false;
        private static void HandleOnError(object sender, ErrorEventArgs e)
        {
            AwkwardMP.Log.LogInfo($"Error connecting! {e.Message}");
            wsClient = null;
            doNotReconnect = true;
        }

        private static void HandleOnOpen(object sender, EventArgs e)
        {
            AwkwardMP.Log.LogInfo("Connected");
            StartHeartbeatThread();
            UpdateConnectionSprite();

            if(RoomCode != null)
            {
                ReConnectToRoom();
            }
        }

        private static void HandleOnClose(object sender, CloseEventArgs e)
        {
            AwkwardMP.Log.LogInfo("Disconnected");
            UpdateConnectionSprite();

            StopHeartbeatThread();

            wsClient = null;

            if (doNotReconnect)
            {
                doNotReconnect = false;
                return;
            }
            StartSocket();
        }



        private static Stopwatch heartbeatStopWatch = new Stopwatch();

        private static Thread heartbeatThread = null;
        private static Thread heartbeatCheckThread = null;
        private static int timeOutCount = 0;

        private static void StopHeartbeatThread()
        {
            if(heartbeatThread != null) 
                heartbeatThread.Abort();

            if (heartbeatCheckThread != null)
                heartbeatCheckThread.Abort();

            heartbeatStopWatch.Stop();
            heartbeatStopWatch.Reset();

            timeOutCount = 0;
        }
        private static void StartHeartbeatThread()
        {
            timeOutCount = 0;
            heartbeatStopWatch.Reset();

            if (heartbeatThread != null)
                heartbeatThread.Abort();

            if (heartbeatCheckThread != null)
                heartbeatCheckThread.Abort();

            heartbeatThread = new Thread(new ThreadStart(HeartbeatThread));
            heartbeatCheckThread = new Thread(new ThreadStart(HeartbeatCheckThread));

            heartbeatThread.Start();
            heartbeatCheckThread.Start();
        }

        private static void HeartbeatCheckThread()
        {
            heartbeatStopWatch.Reset();
            heartbeatStopWatch.Start();

            while (true)
            {
                TimeSpan elapsedTime = heartbeatStopWatch.Elapsed;

                if (elapsedTime.Seconds > 5)
                {
                    timeOutCount += 1;
                    heartbeatStopWatch.Reset();
                }

                if(timeOutCount > 3)
                {
                    // Show Error Timeout
                    if (wsClient != null)
                    {
                        wsClient.Close();
                        wsClient = null;

                        StartSocket();
                        break;
                    }
                }

                Thread.Sleep(1000); // 1 Second
            }
        }

        private static void HeartbeatThread()
        {
            while (true)
            {
                Thread.Sleep(10000);
                SendMessage("ping", new {});
            }
        }

        private static void OnPong()
        {
            heartbeatStopWatch.Reset();
            timeOutCount = 0;
        }

        

        private static void HandleOnMessage(object sender, MessageEventArgs e)
        {
            var message = JsonConvert.DeserializeObject<JToken>(e.Data);
            string _type = (string)message["_type"];
           

            switch (_type)
            {
                case "pong":
                    {
                        AwkwardMP.Log.LogInfo("Received Pong");
                        OnPong();
                    }
                    break;
                case "S_ChangePlayerName":
                    {
                        AwkwardMP.Log.LogInfo($"Changing Name for PlayerIndex {message["_params"]["playerId"]} to {message["_params"]["newName"]}");
                        OnChangePlayerName((int)message["_params"]["playerId"], (string)message["_params"]["newName"]);
                    }
                    break;
                case "S_CreateRoomSuccess":
                    {
                        AwkwardMP.Log.LogInfo($"CreateRoom Success! {message["_params"]["roomCode"]}");
                        OnCreateRoomSuccess((string)message["_params"]["roomCode"]);
                    }
                    break;
                case "S_CreateRoomFailed":
                    {
                        AwkwardMP.Log.LogInfo($"CreateRoom Failed!");
                        OnCreateRoomFailed((string)message["_params"]["reason"]);
                    }
                    break;
                case "S_PlayerAnswer":
                    {
                        AwkwardMP.Log.LogInfo($"Answer! {message["_params"]["answer"]}");
                        OnAnswer((int)message["_params"]["answer"], (int)message["_params"]["playerIndex"]);
                    }
                    break;
                case "S_GetGameInfo":
                    {
                        AwkwardMP.Log.LogInfo($"GetGameInfo!");
                        OnGetGameInfo((int)message["_params"]["playerIndex"]);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
