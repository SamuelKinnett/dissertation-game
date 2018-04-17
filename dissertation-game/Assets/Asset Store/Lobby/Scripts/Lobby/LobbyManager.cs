using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using Assets.Scripts.Player.Enums;
using Assets.Scripts.Environment.Enums;

namespace Prototype.NetworkLobby
{
    public class LobbyManager : NetworkLobbyManager
    {
        static short MsgKicked = MsgType.Highest + 1;

        static public LobbyManager s_Singleton;

        [Header("Unity UI Lobby")]
        [Tooltip("Time in second between all players ready & match start")]
        public float prematchCountdown = 5.0f;

        [Tooltip("A reference to the DatabaseManager prefab")]
        public GameObject DatabaseManagerPrefab;

        [Tooltip("A reference to the GameInstanceData prefab")]
        public GameObject GameInstanceDataPrefab;

        [HideInInspector]
        public GameType gameType;

        [Space]
        [Header("UI Reference")]
        public LobbyTopPanel topPanel;

        public GameObject DedicatedControlButton;
        public Text DedicatedControlButtonText;

        public RectTransform mainMenuPanel;
        public RectTransform lobbyPanel;

        public RectTransform CreditsPanel;

        public LobbyInfoPanel infoPanel;
        public LobbyCountdownPanel countdownPanel;
        public GameObject addPlayerButton;

        protected RectTransform currentPanel;

        public Button backButton;
        public Button DedicatedServerControlButton;
        public Button DedicatedServerProceduralButton;

        public Text statusInfo;
        public Text hostInfo;

        private DatabaseManager databaseManager;
        private GameInstanceData gameInstanceData;

        //Client numPlayers from NetworkManager is always 0, so we count (throught connect/destroy in LobbyPlayer) the number
        //of players, so that even client know how many player there is.
        [HideInInspector]
        public int _playerNumber = 0;

        //used to disconnect a client properly when exiting the matchmaker
        [HideInInspector]
        public bool _isMatchmaking = false;

        protected bool _disconnectServer = false;

        protected ulong _currentMatchID;

        protected LobbyHook _lobbyHooks;

        void Start()
        {
            s_Singleton = this;
            _lobbyHooks = GetComponent<Prototype.NetworkLobby.LobbyHook>();
            currentPanel = mainMenuPanel;

            if (DatabaseManager.DoRequiredDatabasesExist() && DatabaseManager.TestConnections())
            {
                DedicatedServerControlButton.interactable = true;
                DedicatedServerProceduralButton.interactable = true;
            }
            else
            {
                DedicatedServerControlButton.interactable = false;
                DedicatedServerProceduralButton.interactable = false;
            }

            backButton.gameObject.SetActive(false);
            GetComponent<Canvas>().enabled = true;

            DontDestroyOnLoad(gameObject);

            SetServerInfo("Offline", "None");
        }

        public override void OnLobbyClientSceneChanged(NetworkConnection conn)
        {
            if (SceneManager.GetSceneAt(0).name == lobbyScene)
            {
                if (topPanel.isInGame)
                {
                    ChangeTo(lobbyPanel);
                    if (_isMatchmaking)
                    {
                        if (conn.playerControllers[0].unetView.isServer)
                        {
                            backDelegate = StopHostClbk;
                        }
                        else
                        {
                            backDelegate = StopClientClbk;
                        }
                    }
                    else
                    {
                        if (conn.playerControllers[0].unetView.isClient)
                        {
                            backDelegate = StopHostClbk;
                        }
                        else
                        {
                            backDelegate = StopClientClbk;
                        }
                    }
                }
                else
                {
                    ChangeTo(mainMenuPanel);
                }

                topPanel.ToggleVisibility(true);
                topPanel.isInGame = false;
            }
            else
            {
                ChangeTo(null);

                Destroy(GameObject.Find("MainMenuUI(Clone)"));

                //backDelegate = StopGameClbk;
                topPanel.isInGame = true;
                topPanel.ToggleVisibility(false);
            }
        }

        public void ChangeTo(RectTransform newPanel)
        {
            if (currentPanel != null)
            {
                currentPanel.gameObject.SetActive(false);
            }

            if (newPanel != null)
            {
                newPanel.gameObject.SetActive(true);
            }

            currentPanel = newPanel;

            if (currentPanel != mainMenuPanel)
            {
                backButton.gameObject.SetActive(true);
            }
            else
            {
                backButton.gameObject.SetActive(false);
                SetServerInfo("Offline", "None");
                _isMatchmaking = false;
                DedicatedServerControlButton.interactable =
                    DedicatedServerProceduralButton.interactable =
                    DatabaseManager.DoRequiredDatabasesExist();
            }
        }

        public void DisplayIsConnecting()
        {
            var _this = this;
            infoPanel.Display("Connecting...", "Cancel", () => { _this.backDelegate(); });
        }

        public void SetServerInfo(string status, string host)
        {
            statusInfo.text = status;
            hostInfo.text = host;
        }

        public void ChangeGameType()
        {
            if (gameType == GameType.Control)
            {
                gameType = GameType.Procedural;
                DedicatedControlButtonText.text = "Procedural";
                SetServerInfo("Dedicated Server (Procedural)", networkAddress);
            }
            else
            {
                gameType = GameType.Control;
                DedicatedControlButtonText.text = "Control";
                SetServerInfo("Dedicated Server (Control)", networkAddress);
            }
        }


        public delegate void BackButtonDelegate();
        public BackButtonDelegate backDelegate;
        public void GoBackButton()
        {
            backDelegate();
            topPanel.isInGame = false;
        }

        public void SetCreditsVisible(bool visible)
        {
            CreditsPanel.gameObject.SetActive(visible);
        }

        // ----------------- Server management

        public void AddLocalPlayer()
        {
            TryToAddPlayer();
        }

        public void RemovePlayer(LobbyPlayer player)
        {
            player.RemovePlayer();
        }

        public void SimpleBackClbk()
        {
            ChangeTo(mainMenuPanel);
        }

        public void StopHostClbk()
        {
            if (_isMatchmaking)
            {
                matchMaker.DestroyMatch((NetworkID)_currentMatchID, 0, OnDestroyMatch);
                _disconnectServer = true;
            }
            else
            {
                StopHost();
            }


            ChangeTo(mainMenuPanel);
        }

        public void StopClientClbk()
        {
            StopClient();

            if (_isMatchmaking)
            {
                StopMatchMaker();
            }

            ChangeTo(mainMenuPanel);
        }

        public void StopServerClbk()
        {
            StopServer();
            ChangeTo(mainMenuPanel);
        }

        class KickMsg : MessageBase { }
        public void KickPlayer(NetworkConnection conn)
        {
            conn.Send(MsgKicked, new KickMsg());
        }




        public void KickedMessageHandler(NetworkMessage netMsg)
        {
            infoPanel.Display("Kicked by Server", "Close", null);
            netMsg.conn.Disconnect();
        }

        //===================

        public override void OnStartHost()
        {
            base.OnStartHost();

            ChangeTo(lobbyPanel);
            backDelegate = StopHostClbk;
            SetServerInfo("Hosting", networkAddress);
        }

        public override void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
        {
            base.OnMatchCreate(success, extendedInfo, matchInfo);
            _currentMatchID = (System.UInt64)matchInfo.networkId;
        }

        public override void OnDestroyMatch(bool success, string extendedInfo)
        {
            base.OnDestroyMatch(success, extendedInfo);
            if (_disconnectServer)
            {
                StopMatchMaker();
                StopHost();
            }
        }

        //allow to handle the (+) button to add/remove player
        public void OnPlayersNumberModified(int count)
        {
            _playerNumber += count;

            int localPlayerCount = 0;
            foreach (PlayerController p in ClientScene.localPlayers)
                localPlayerCount += (p == null || p.playerControllerId == -1) ? 0 : 1;

            addPlayerButton.SetActive(localPlayerCount < maxPlayersPerConnection && _playerNumber < maxPlayers);
        }

        // ----------------- Server callbacks ------------------

        //we want to disable the button JOIN if we don't have enough player
        //But OnLobbyClientConnect isn't called on hosting player. So we override the lobbyPlayer creation
        public override GameObject OnLobbyServerCreateLobbyPlayer(NetworkConnection conn, short playerControllerId)
        {
            GameObject obj = Instantiate(lobbyPlayerPrefab.gameObject) as GameObject;

            LobbyPlayer newPlayer = obj.GetComponent<LobbyPlayer>();
            newPlayer.ToggleJoinButton(numPlayers + 1 >= minPlayers);


            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

                if (p != null)
                {
                    p.RpcUpdateRemoveButton();
                    p.ToggleJoinButton(numPlayers + 1 >= minPlayers);
                }
            }

            return obj;
        }

        public override void OnLobbyServerPlayerRemoved(NetworkConnection conn, short playerControllerId)
        {
            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

                if (p != null)
                {
                    p.RpcUpdateRemoveButton();
                    p.ToggleJoinButton(numPlayers + 1 >= minPlayers);
                }
            }
        }

        public override void OnLobbyServerDisconnect(NetworkConnection conn)
        {
            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

                if (p != null)
                {
                    p.RpcUpdateRemoveButton();
                    p.ToggleJoinButton(numPlayers >= minPlayers);
                }
            }

        }

        public override bool OnLobbyServerSceneLoadedForPlayer(GameObject lobbyPlayer, GameObject gamePlayer)
        {
            //This hook allows you to apply state data from the lobby-player to the game-player
            //just subclass "LobbyHook" and add it to the lobby object.

            if (_lobbyHooks)
                _lobbyHooks.OnLobbyServerSceneLoadedForPlayer(this, lobbyPlayer, gamePlayer);

            return true;
        }

        // --- Countdown management

        public override void OnLobbyServerPlayersReady()
        {
            bool allready = true;
            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                if (lobbySlots[i] != null)
                    allready &= lobbySlots[i].readyToBegin;
            }

            if (allready)
            {
                DatabaseManager.Instance.StartNewGame(gameType);
                GameInstanceData.Instance.GameType = gameType;

                var redTeamId = DatabaseManager.Instance.AddTeam();
                var blueTeamId = DatabaseManager.Instance.AddTeam();

                GameInstanceData.Instance.RedTeamId = redTeamId;
                GameInstanceData.Instance.BlueTeamId = blueTeamId;

                ResolvePlayers(redTeamId, blueTeamId);
                StartCoroutine(ServerCountdownCoroutine());
            }
        }

        public IEnumerator ServerCountdownCoroutine()
        {
            float remainingTime = prematchCountdown;
            int floorTime = Mathf.FloorToInt(remainingTime);

            while (remainingTime > 0)
            {
                yield return null;

                remainingTime -= Time.deltaTime;
                int newFloorTime = Mathf.FloorToInt(remainingTime);

                if (newFloorTime != floorTime)
                {//to avoid flooding the network of message, we only send a notice to client when the number of plain seconds change.
                    floorTime = newFloorTime;

                    for (int i = 0; i < lobbySlots.Length; ++i)
                    {
                        if (lobbySlots[i] != null)
                        {//there is maxPlayer slots, so some could be == null, need to test it before accessing!
                            (lobbySlots[i] as LobbyPlayer).RpcUpdateCountdown(floorTime);
                        }
                    }
                }
            }

            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                if (lobbySlots[i] != null)
                {
                    (lobbySlots[i] as LobbyPlayer).RpcUpdateCountdown(0);
                }
            }

            ServerChangeScene(playScene);
        }

        public override void OnLobbyStartServer()
        {
            base.OnLobbyStartServer();

            DedicatedControlButton.SetActive(true);
            DedicatedControlButtonText.text = gameType == GameType.Control ? "Control" : "Procedural";

            databaseManager = Instantiate(DatabaseManagerPrefab).GetComponent<DatabaseManager>();
            DontDestroyOnLoad(databaseManager.gameObject);

            gameInstanceData = Instantiate(GameInstanceDataPrefab).GetComponent<GameInstanceData>();
            DontDestroyOnLoad(gameInstanceData.gameObject);

            DatabaseManager.Instance.InitialiseDatabases();
            // DatabaseManager.Instance.StartNewSession();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();

            DedicatedControlButton.SetActive(false);

            Destroy(databaseManager.gameObject);
            Destroy(gameInstanceData.gameObject);
        }

        // ----------------- Client callbacks ------------------

        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);

            infoPanel.gameObject.SetActive(false);

            conn.RegisterHandler(MsgKicked, KickedMessageHandler);

            if (!NetworkServer.active)
            {//only to do on pure client (not self hosting client)
                ChangeTo(lobbyPanel);
                backDelegate = StopClientClbk;
                SetServerInfo("Client", networkAddress);
            }
        }


        public override void OnClientDisconnect(NetworkConnection conn)
        {
            base.OnClientDisconnect(conn);
            ChangeTo(mainMenuPanel);
        }

        public override void OnClientError(NetworkConnection conn, int errorCode)
        {
            ChangeTo(mainMenuPanel);
            infoPanel.Display("Cient error : " + (errorCode == 6 ? "timeout" : errorCode.ToString()), "Close", null);
        }

        private void ResolvePlayers(int redTeamId, int blueTeamId)
        {
            int redCount = 0;
            int blueCount = 0;
            int playerCount = 0;
            List<int> unplacedIndices = new List<int>();

            // Iterate through the current players and find the number on each
            // team and the indices of unplaced (random) players.
            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                if (lobbySlots[i] != null)
                {
                    ++playerCount;

                    switch ((lobbySlots[i] as LobbyPlayer).PlayerTeam)
                    {
                        case Team.Random:
                            unplacedIndices.Add(i);
                            break;

                        case Team.Red:
                            DatabaseManager.Instance.AddPlayerToTeam((lobbySlots[i] as LobbyPlayer).PlayerId, redTeamId);
                            (lobbySlots[i] as LobbyPlayer).PlayerTeamId = redTeamId;
                            ++redCount;
                            break;

                        case Team.Blue:
                            DatabaseManager.Instance.AddPlayerToTeam((lobbySlots[i] as LobbyPlayer).PlayerId, blueTeamId);
                            (lobbySlots[i] as LobbyPlayer).PlayerTeamId = blueTeamId;
                            ++blueCount;
                            break;
                    }
                }
            }

            // Calculate how many red or blue players are needed to have a 50
            // 50 split.
            int remainingRed = Mathf.Abs(playerCount / 2 - redCount);
            int remainingBlue = Mathf.Abs(playerCount / 2 - blueCount);

            // Place the random players into teams
            foreach (var unplacedIndex in unplacedIndices)
            {
                var newTeam = remainingRed > 0
                    ? remainingBlue > 0
                        ? (Team)Random.Range((int)Team.Red, (int)Team.Blue)
                        : Team.Red
                    : Team.Blue;

                if (newTeam == Team.Red)
                {
                    DatabaseManager.Instance.AddPlayerToTeam((lobbySlots[unplacedIndex] as LobbyPlayer).PlayerId, redTeamId);
                    (lobbySlots[unplacedIndex] as LobbyPlayer).PlayerTeamId = redTeamId;
                    --remainingRed;
                }
                else
                {
                    DatabaseManager.Instance.AddPlayerToTeam((lobbySlots[unplacedIndex] as LobbyPlayer).PlayerId, blueTeamId);
                    (lobbySlots[unplacedIndex] as LobbyPlayer).PlayerTeamId = blueTeamId;
                    --remainingBlue;
                }

                (lobbySlots[unplacedIndex] as LobbyPlayer).PlayerTeam = newTeam;
            }
        }
    }
}
