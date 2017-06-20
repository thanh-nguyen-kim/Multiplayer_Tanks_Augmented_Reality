using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using System.Collections;

namespace Prototype.NetworkLobby
{
    public class LobbyManager : NetworkLobbyManager
    {
        static short MsgKicked = MsgType.Highest + 1;
        static short MsgArena = MsgType.Highest + 2;
        static public LobbyManager s_Singleton;


        [Header("Unity UI Lobby")]
        [Tooltip("Time in second between all players ready & match start")]
        public float prematchCountdown = 5.0f;

        [Space]
        [Header("UI Reference")]
        public LobbyTopPanel topPanel;

        public RectTransform mainMenuPanel, arenaPanel, tankPanel, m_StartPanel, m_LoadingPanel, m_PausePanel, m_PlayPanel, m_SummaryPanel;
        public RectTransform lobbyPanel;

        public LobbyInfoPanel infoPanel;

        protected RectTransform currentPanel;

        public Button backButton, m_MissileReadyBtn, m_MissileARReadyBtn;

        public Text statusInfo;
        public Text hostInfo;

        public Sprite[] m_MissileStates;
        //Client numPlayers from NetworkManager is always 0, so we count (throught connect/destroy in LobbyPlayer) the number
        //of players, so that even client know how many player there is.
        public int _playerNumber = 0;

        //used to disconnect a client properly when exiting the matchmaker
        [HideInInspector]
        public bool _isMatchmaking = false;

        protected bool _disconnectServer = false;

        protected ulong _currentMatchID;

        protected LobbyHook _lobbyHooks;
        public int arenaIndex = 0;
        public LobbyPlayer localLobbyPlayer;

        public delegate void BackGroundSound(int i);
        [HideInInspector]
        public BackGroundSound bgSound;
        private void PlayBgSound(int i)
        {
            bgSound(i);
        }

        [HideInInspector]
        public bool ARMode = false;
        [Space]
        [Header("AR")]
        public RectTransform m_PlayARPanel;
        public RectTransform m_ReadyPanel, m_DetectingTarget;
        [HideInInspector]
        public bool isReady = false;
        private const string ON_DETECTED_TARGET = "Detected game board. Now click ready to start.";
        private const string ON_LOST_TARGET = "Point your phone camera to target.";
        #region AR
        public delegate void ARBuildNewTargetDelegate();
        [HideInInspector]
        public ARBuildNewTargetDelegate targetBuilder;
        public void BuildNewTarget()
        {
            targetBuilder();
        }
        public delegate void UDTInitDelegate();
        [HideInInspector]
        public UDTInitDelegate m_UDTInit;
        public void UDTInit()
        {
            m_UDTInit();
        }
        public delegate void ARInitDelegate();
        [HideInInspector]
        public ARInitDelegate arInit;
        public void ARInit()
        {
            arInit();
        }
        public delegate void NormalInitDelegate();
        [HideInInspector]
        public NormalInitDelegate normalInit;
        public void NormalInit()
        {
            normalInit();
        }

        public delegate void ARMoveBtnDelegate();
        [HideInInspector]
        public ARMoveBtnDelegate moveBtnDelegate;
        public void ARMove()
        {
            moveBtnDelegate();
        }

        public delegate void ARShotBtnDelegate();

        [HideInInspector]
        public ARShotBtnDelegate shotBtnDelegate;
        public void ARShot()
        {
            shotBtnDelegate();
        }

        public void OnToggleARModeChange(Toggle ARToggle)
        {
            m_ReadyPanel.transform.GetChild(0).gameObject.SetActive(true);
            this.ARMode = ARToggle.isOn;
            if (ARToggle.isOn)
            {
                m_ReadyPanel.GetComponent<Image>().color = new Color(1, 1, 1, 0);
                StopCoroutine("AutoReady");
                ARInit();
                m_ReadyPanel.transform.GetChild(1).gameObject.SetActive(false);
                m_ReadyPanel.transform.GetChild(2).gameObject.SetActive(true);
                m_ReadyPanel.GetChild(2).GetChild(0).GetComponent<Toggle>().isOn = false;
            }
            else
            {
                m_ReadyPanel.GetComponent<Image>().color = Color.gray;
                StopCoroutine("AutoReady");
                StartCoroutine("AutoReady");
                NormalInit();
                m_ReadyPanel.GetChild(3).gameObject.SetActive(false);
                m_ReadyPanel.GetChild(1).gameObject.SetActive(true);
                m_ReadyPanel.GetChild(2).gameObject.SetActive(false);
                m_ReadyPanel.GetChild(4).gameObject.SetActive(true);
            }
        }

        public void OnImageTargetDetected()
        {
            Debug.Log("Detect");
            m_ReadyPanel.GetChild(2).GetChild(2).GetComponent<Text>().text = ON_DETECTED_TARGET;
            m_ReadyPanel.GetChild(4).gameObject.SetActive(true);
        }


        public void OnImageTargetLost()
        {
            Debug.Log("Lost");
            m_ReadyPanel.GetChild(2).GetChild(2).GetComponent<Text>().text = ON_LOST_TARGET;
            m_ReadyPanel.GetChild(4).gameObject.SetActive(false);
        }

        public void OnToggleUDTChange(Toggle UDTToggle)
        {
            if (UDTToggle.isOn)
            {
                m_ReadyPanel.GetChild(3).gameObject.SetActive(true);
                m_ReadyPanel.GetChild(2).GetChild(1).GetChild(0).gameObject.SetActive(true);
                UDTInit();
            }
            else
            {
                ARInit();
                m_ReadyPanel.GetChild(3).gameObject.SetActive(false);
                m_ReadyPanel.GetChild(2).GetChild(1).GetChild(0).gameObject.SetActive(false);
            }
        }

        public void Ready()
        {
            isReady = true;
            SetReady();
            ResetReadyPanel();
            m_ReadyPanel.GetChild(5).gameObject.SetActive(true);
        }

        private void ResetReadyPanel()
        {
            m_ReadyPanel.GetComponent<Image>().color = Color.gray;
            for (int i = 0; i < m_ReadyPanel.childCount; i++)
                m_ReadyPanel.GetChild(i).gameObject.SetActive(false);
        }


        IEnumerator AutoReady()
        {
            Text countDountText = m_ReadyPanel.GetChild(1).GetChild(1).GetComponent<Text>();
            float time = 16f;
            while (time > 0)
            {
                yield return new WaitForSeconds(1);
                time--;
                countDountText.text = time.ToString();
            }
            Ready();
        }

        public void OnBeginMultiplay()
        {
            isReady = false;
            ChangeTo(m_ReadyPanel);
            ResetReadyPanel();
            m_ReadyPanel.transform.GetChild(0).gameObject.SetActive(true);
            m_ReadyPanel.GetChild(0).GetComponent<Toggle>().isOn = false;
            m_ReadyPanel.transform.GetChild(1).gameObject.SetActive(true);
            m_ReadyPanel.transform.GetChild(4).gameObject.SetActive(true);
            StartCoroutine("AutoReady");
        }


        #endregion
        void Start()
        {
            s_Singleton = this;
            _lobbyHooks = GetComponent<Prototype.NetworkLobby.LobbyHook>();
            currentPanel = m_StartPanel;

            backButton.gameObject.SetActive(false);
            GetComponent<Canvas>().enabled = true;

            DontDestroyOnLoad(gameObject);
            Screen.SetResolution(1280, 720, true);
            SetServerInfo("Offline", "None");
        }

        public override void OnLobbyClientSceneChanged(NetworkConnection conn)
        {
            if (SceneManager.GetSceneAt(0).name == lobbyScene)
            {
                PlayBgSound(0);
                if (topPanel.isInGame)
                {
                    ChangeTo(lobbyPanel);
                    topPanel.gameObject.SetActive(true);
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
                Destroy(GameObject.Find("MainMenuUI(Clone)"));
                _lobbyHooks.OnLobbyClientSceneChanged(this.arenaIndex);
                topPanel.isInGame = true;
                topPanel.ToggleVisibility(false);
                PlayBgSound(2);
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

            if (currentPanel == m_StartPanel || currentPanel == m_LoadingPanel
                || currentPanel == m_PlayPanel || currentPanel == m_SummaryPanel 
                || currentPanel == tankPanel|| currentPanel == m_PlayARPanel)
                topPanel.gameObject.SetActive(false);
            else
                topPanel.gameObject.SetActive(true);
            if (currentPanel != tankPanel && currentPanel != m_StartPanel && currentPanel != m_PlayPanel)
            {
                if (currentPanel == mainMenuPanel || currentPanel == m_PausePanel)
                {
                    topPanel.gameObject.SetActive(true);
                    topPanel.ToggleVisibility(true);
                }
                backButton.interactable = true;
            }
            else
            {
                backButton.interactable = false;
                SetServerInfo("Offline", "None");
                _isMatchmaking = false;
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


        public delegate void BackButtonDelegate();
        public BackButtonDelegate backDelegate;
        public void GoBackButton()
        {
            backDelegate();
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

        class ArenaMsg : MessageBase
        {
            public int arenaIndex = 0;
            public ArenaMsg() { }
            public ArenaMsg(int index)
            {
                this.arenaIndex = index;
            }
        }
        public void SetArena(NetworkConnection conn)
        {
            conn.Send(MsgArena, new ArenaMsg(arenaIndex));
        }

        public void ArenaMessageHandler(NetworkMessage netMsg)
        {
            this.arenaIndex = netMsg.ReadMessage<ArenaMsg>().arenaIndex;
            lobbyPanel.GetComponent<LobbyPlayerList>().SetArena();
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
        }

        // ----------------- Server callbacks ------------------

        //we want to disable the button JOIN if we don't have enough player
        //But OnLobbyClientConnect isn't called on hosting player. So we override the lobbyPlayer creation
        public override GameObject OnLobbyServerCreateLobbyPlayer(NetworkConnection conn, short playerControllerId)
        {
            GameObject obj = Instantiate(lobbyPlayerPrefab.gameObject) as GameObject;

            LobbyPlayer newPlayer = obj.GetComponent<LobbyPlayer>();
            newPlayer.ToggleJoinButton(numPlayers + 1 >= minPlayers);
            //set arena index on new player
            SetArena(conn);

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
                StartCoroutine(ServerCountdownCoroutine());
        }

        public IEnumerator ServerCountdownCoroutine()
        {
            float remainingTime = prematchCountdown + 1;
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

            ServerChangeScene(playScene);
            while (SceneManager.GetActiveScene().buildIndex == 0) yield return null;
            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                if (lobbySlots[i] != null)
                {
                    (lobbySlots[i] as LobbyPlayer).RpcUpdateCountdown(-1);
                }
            }
        }

        // ----------------- Client callbacks ------------------

        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);

            infoPanel.gameObject.SetActive(false);

            conn.RegisterHandler(MsgKicked, KickedMessageHandler);
            conn.RegisterHandler(MsgArena, ArenaMessageHandler);
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

        public void StartOfflineGame()
        {
            StartCoroutine(LoadAsync());

        }

        public IEnumerator LoadAsync()
        {
            ChangeTo(m_LoadingPanel);
            yield return SceneManager.LoadSceneAsync("OfflineMenuScene");
            yield return new WaitForSeconds(m_LoadingPanel.GetChild(0).GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length);
            Destroy(gameObject);
        }

        public void StartMultiPlayerGame()
        {
            ChangeTo(mainMenuPanel);
        }

        public void ARScene()
        {
            SceneManager.LoadScene("AR");
            Destroy(gameObject);
        }

        public void ReadyCountDown(int time)
        {
            if (time < 0)
            {
                lobbyPanel.GetChild(lobbyPanel.childCount - 1).gameObject.SetActive(false);
                return;
            }
            GameObject countDown = lobbyPanel.GetChild(lobbyPanel.childCount - 1).gameObject;
            countDown.SetActive(true);
            countDown.GetComponent<Text>().text = time.ToString();
        }

        public void Pause()
        {
            ChangeTo(m_PausePanel);
            backDelegate = Resume;
        }

        public void Resume()
        {
            if (!ARMode)
                ChangeTo(m_PlayPanel);
            else
                ChangeTo(m_PlayARPanel);
        }

        public delegate void ReadyButtonDelegate();
        public ReadyButtonDelegate readyDelegate;
        public void SetReady()
        {
            readyDelegate();
        }

        public delegate bool MissileNuttonDelegate();
        public MissileNuttonDelegate missileDelegate;
        public void SetMissileReady()
        {
            bool state = missileDelegate();
            Sprite tmp;
            if (!state)
            {
                tmp = m_MissileStates[0];
            }
            else
                tmp = m_MissileStates[1];
            if (!ARMode)
                m_MissileReadyBtn.GetComponent<Image>().sprite = tmp;
            else
                m_MissileARReadyBtn.GetComponent<Image>().sprite = tmp;
        }

        public void MissileReadyButtonState(bool state)
        {
            if (!ARMode)
            {
                m_MissileReadyBtn.interactable = state;
                if (state) { m_MissileReadyBtn.GetComponent<Image>().sprite = m_MissileStates[0]; }
            }
            else
            {
                m_MissileARReadyBtn.interactable = state;
                if (state) { m_MissileARReadyBtn.GetComponent<Image>().sprite = m_MissileStates[0]; }
            }
        }
    }
}
