

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
using System.Text.RegularExpressions;
using System.Drawing.Printing;
using System.Linq;

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

    internal class AwkwardPlayer
    {
        public string Name { get; set; }
        public int Index { get; set; }
    }

    internal static class AwkwardClient
    {
        private static InGameSceneLogic inGameSceneLogic = null;
        private static WebSocket wsClient = null;

        private static List<AwkwardPlayer> Players = new List<AwkwardPlayer>();
        private static Dictionary<int, AwkwardPlayer> ConnectedPlayers = new Dictionary<int, AwkwardPlayer>();

        private static GameObject _OverView = null;

        private static UnityEngine.UI.Text _StatusText;
        private static UnityEngine.UI.Button _RoomCodeButton;
        private static UnityEngine.UI.Text _RoomCodeText;
        private static UnityEngine.UI.Text _RoomCodeLabel;
        private static string RoomCode;
        private static bool showRoomCode = false;

        private static RectTransform _ConnectedPlayersRT;
        private static Dictionary<int, RectTransform> _PlayersRT = new Dictionary<int, RectTransform>();


        public static void AddUI(GameObject parentObject = null)
        {
            if (_OverView == null)
            {
                if (parentObject == null) return;

                _OverView = new GameObject("AWKMPOverview") { layer = 5 };
                _OverView.transform.SetParent(parentObject.transform);

                RectTransform overViewRT = _OverView.AddComponent<RectTransform>();
                overViewRT.SetAnchors(.5f, .5f, 1f, 1f);
                overViewRT.SetSizeDelta(400f, 500f);
                overViewRT.SetOffsets(-1900f, -1500f, -530f, -30f);
                overViewRT.localScale = default;
                overViewRT.localScale = new Vector3(1, 1, 1);

                Text titleText = overViewRT.AddText("AwkwardMP", TextAnchor.UpperCenter, Helper.WHITE);
                titleText.fontSize = 48;
                titleText.resizeTextMaxSize = 48;

                RectTransform statusRT = overViewRT.CreateRectTransform("Status");
                statusRT.ExpandAnchor(10);
                statusRT.localPosition = new Vector3(0f, -50f, 0f);

                _StatusText = statusRT.AddText("Disconnected", TextAnchor.UpperCenter, Helper.WHITE);
                _StatusText.fontSize = 36;
                _StatusText.resizeTextMaxSize = 36;

                RectTransform roomCodeRT = overViewRT.CreateRectTransform("RoomCode");
                roomCodeRT.ExpandTopAnchor(10);
                roomCodeRT.localPosition = new Vector3(0f, 75f, 0f);

                roomCodeRT.MakeButton(ref _RoomCodeButton, "RoomCode", "(Press F1 to Hide)", out _RoomCodeText, out _RoomCodeLabel, delegate
                {
                    GUIUtility.systemCopyBuffer = RoomCode;
                });

                _RoomCodeButton.GetComponent<RectTransform>()
                    .SetAnchors(.5f, .5f, 1f, 1f)
                    .SetOffsets(-150f, 150f, -125f, 125f)
                    .SetSizeDelta(300f, 250f)
                    .localPosition = new Vector3(0, 0, 0);

                _RoomCodeLabel.transform.localPosition = new Vector3(0f, -50f, 0f);
                _RoomCodeLabel.fontSize = 24;


                RectTransform playersRT = overViewRT.CreateRectTransform("Players");
                playersRT.ExpandAnchor(10);
                playersRT.localPosition = new Vector3(0f, -235f, 0f);

                Text playersText = playersRT.AddText("Connected Players:", TextAnchor.UpperCenter, Helper.WHITE);
                playersText.fontSize = 36;
                playersText.resizeTextMaxSize = 36;

                _ConnectedPlayersRT = overViewRT.CreateRectTransform("ConnectedPlayers");
                _ConnectedPlayersRT.ExpandAnchor(10);
                _ConnectedPlayersRT.localPosition = new Vector3(0f, -300f, 0f);

                _PlayersRT.Clear();
                for (int _playerIndex = 0; _playerIndex < 5; _playerIndex++)
                {
                    RectTransform newPlayerRT = _ConnectedPlayersRT.CreateRectTransform("Player_" + _playerIndex.ToString());
                    newPlayerRT.localScale = default;
                    newPlayerRT.localScale = new Vector3(1, 1, 1);
                    newPlayerRT.SetSizeDelta(400, 50);
                    newPlayerRT.SetAnchors(.5f, .5f, 1f, 1f);
                    newPlayerRT.localPosition = new Vector3(0, 200 - (_playerIndex * 75), 0);


                    RectTransform playerNameRT = newPlayerRT.CreateRectTransform("PlayerName", true);
                    Text playerNameText = playerNameRT.AddText("(Player " + (2 + _playerIndex) + ") ", TextAnchor.UpperLeft, Helper.WHITE);
                    playerNameText.resizeTextMaxSize = 36;
                    playerNameText.fontSize = 36;


                    Dropdown _playerIndexDD = newPlayerRT.CreateDropdown("DD_Player_" + _playerIndex.ToString(), 10, Helper.WHITE);
                    _playerIndexDD.transform.localPosition = new Vector3(0f, 0f, 0f);

                    _playerIndexDD.options.Add(new Dropdown.OptionData("Player 2"));
                    _playerIndexDD.options.Add(new Dropdown.OptionData("Player 3"));
                    _playerIndexDD.options.Add(new Dropdown.OptionData("Player 4"));
                    _playerIndexDD.options.Add(new Dropdown.OptionData("Player 5"));
                    _playerIndexDD.options.Add(new Dropdown.OptionData("Player 6"));

                    _playerIndexDD.RefreshShownValue();

                    _playerIndexDD.onValueChanged.AddListener(delegate
                    {
                        OnDropDownValueChanged(_playerIndexDD);
                    });

                    _PlayersRT.Add(_playerIndex, playerNameRT);
                }
            }
            else
            {
                _OverView.transform.SetParent(parentObject.transform);
            }

            UpdateConnection();
            ToggleRoomCode(showRoomCode);
        }

        private static void UpdatePlayerName(int index, string newName, bool active = true)
        {
            _PlayersRT[index].GetComponent<UnityEngine.UI.Text>().text = "(Player " + (2 + index) + ") " + newName;

            FixPlayerNames();
        }



        public static void Enable()
        {
            if (inGameSceneLogic == null)
            {
                var sceneLogic = GameObject.Find("SceneLogic");
                if (sceneLogic == null) return;

                inGameSceneLogic = sceneLogic.GetComponent<InGameSceneLogic>();
            }
        }

        private static void UpdateConnection()
        {
            if (wsClient != null)
            {
                if (wsClient.ReadyState == WebSocketState.Open)
                {
                    if (_StatusText != null)
                    {
                        _StatusText.text = "Connected";
                        return;
                    }
                }
            }
            _StatusText.text = "Disconnected!";
        }

        public static void ToggleRoomCode(bool show)
        {
            showRoomCode = show;
            if (showRoomCode)
            {
                _RoomCodeText.text = (RoomCode == null ? "<RoomCode>" : RoomCode);
                _RoomCodeButton.gameObject.SetActive(true);
            }
            else
            {
                _RoomCodeText.text = "<RoomCode>";
                _RoomCodeButton.gameObject.SetActive(false);
            }
        }

        /************************************************************************************
         *                         Server Events
         * **********************************************************************************/

        public static void CreateRoom()
        {
            if (wsClient == null) StartSocket();

            if (wsClient != null && wsClient.ReadyState == WebSocketState.Open)
                SendMessage("H_CreateRoom", new { });
        }

        private static void OnCreateRoomSuccess(string _roomCode)
        {
            RoomCode = _roomCode;

            ToggleRoomCode(true);
            AwkwardMP.Log.LogInfo($"RoomCode: {_roomCode}");

            Players.Clear();
            ConnectedPlayers.Clear();

            foreach(KeyValuePair<int, RectTransform> rt in _PlayersRT)
            {
                rt.Value.GetComponent<UnityEngine.UI.Text>().text = "(Player " + (2 + rt.Key) + ")";
            }
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
            foreach (AwkwardPlayer player in Players)
            {
                Globals.GameState.ChangePlayerName(player.Index + 1, player.Name);
            }
        }

        private static void OnPlayerLeave(int _playerIndex)
        {
            AwkwardPlayer player = Players.SingleOrDefault(x => x.Index == _playerIndex);
            if (player != null)
                Players.Remove(player);

            _PlayersRT[_playerIndex].GetComponent<UnityEngine.UI.Text>().text = "(Player " + (2 + _playerIndex) + ")";
        }


        private static void OnPlayerJoin(string _playerName, string _rndCode)
        {
            AwkwardPlayer player = Players.SingleOrDefault(x => x.Name == _playerName);


            if (player != null)
            {
                AwkwardMP.Log.LogInfo("Username already taken");
                SendMessage("H_PlayerJoinFailed", new { roomId = RoomCode, reason = "Username already taken!", rndCode = _rndCode });
            }
            else
            {
                int _newPlayerIndex = -1;
                for (int i = 0; i < 5; i++)
                {
                    if (Players.SingleOrDefault(x => x.Index == i) != null) continue;

                    _newPlayerIndex = i;
                    break;
                }

                if (_newPlayerIndex != -1)
                {
                    AwkwardPlayer _newPlayer = new AwkwardPlayer() { Index = _newPlayerIndex, Name = _playerName };
                    Players.Add(_newPlayer);

                    UpdatePlayerName(_newPlayerIndex, _playerName);

                    SendMessage("H_PlayerJoinSuccess", new { roomId = RoomCode, playerIndex = _newPlayer.Index, rndCode = _rndCode });
                }
                else
                {
                    SendMessage("H_PlayerJoinFailed", new { roomId = RoomCode, reason = "Room already full!", rndCode = _rndCode });
                }
            }
        }

        private static void OnDropDownValueChanged(UnityEngine.UI.Dropdown _dropDown)
        {

            AwkwardMP.Log.LogInfo($"Name: {_dropDown.name}, Value: {_dropDown.value}");
            switch (_dropDown.name)
            {
                case "DD_Player_0":
                    {
                        ChangePlayerIndex(0, _dropDown.value);
                    }
                    break;
                case "DD_Player_1":
                    {
                        ChangePlayerIndex(1, _dropDown.value);
                    }
                    break;
                case "DD_Player_2":
                    {
                        ChangePlayerIndex(2, _dropDown.value);
                    }
                    break;
                case "DD_Player_3":
                    {
                        ChangePlayerIndex(3, _dropDown.value);
                    }
                    break;
                case "DD_Player_4":
                    {
                        ChangePlayerIndex(4, _dropDown.value);
                    }
                    break;
                case "DD_Player_5":
                    {
                        ChangePlayerIndex(5, _dropDown.value);
                    }
                    break;
                default:
                    {
                        AwkwardMP.Log.LogError("PlayerIndex not changed");
                    }
                    break;
            }
        }

        private static void ChangePlayerIndex(int _oldIndex, int _newIndex)
        {
            AwkwardPlayer player = Players.SingleOrDefault(x => x.Index == _oldIndex);
            bool bSwitchIndexes = false;

            if (player != null)
            {
                AwkwardPlayer existingPlayer = Players.SingleOrDefault(x => x.Index == _newIndex);
                if (existingPlayer != null)
                {
                    existingPlayer.Index = _oldIndex;
                    UpdatePlayerName(_oldIndex, existingPlayer.Name);
                    bSwitchIndexes = true;
                }
                else
                {
                    UpdatePlayerName(_oldIndex, "", false);
                }

                player.Index = _newIndex;
                UpdatePlayerName(_newIndex, player.Name);
            }

            FixPlayerNames();

            object _params = new
            {
                roomId = RoomCode,
                oldIndex = _oldIndex,
                newIndex = _newIndex,
                switchIndex = bSwitchIndexes
            };

            SendMessage("H_ChangePlayerIndex", _params);
        }


        /************************************************************************************
         *                         Socket
         * **********************************************************************************/
        public static void SendMessage(string _type, object _params)
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
            if (wsClient == null)
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
            if (wsClient != null && wsClient.ReadyState == WebSocketState.Open)
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
            UpdateConnection();

            if (RoomCode != null)
            {
                ReConnectToRoom();
            }
        }

        private static void HandleOnClose(object sender, CloseEventArgs e)
        {
            AwkwardMP.Log.LogInfo("Disconnected");
            UpdateConnection();

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
            if (heartbeatThread != null)
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

                if (timeOutCount > 3)
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
                SendMessage("ping", new { });
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
                case "S_PlayerJoin":
                    {
                        AwkwardMP.Log.LogInfo($"PlayerJoin");
                        OnPlayerJoin((string)message["_params"]["playerName"], (string)message["_params"]["rndCode"]);
                    }
                    break;
                case "S_PlayerLeave":
                    {
                        AwkwardMP.Log.LogInfo($"PlayerLeave");
                        OnPlayerLeave((int)message["_params"]["playerIndex"]);
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
