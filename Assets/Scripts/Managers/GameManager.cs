using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections.Generic;
using Prototype.NetworkLobby;
[System.Serializable]
public class EndGameData
{
    public string title;
    public string msg;
    public string playerName;
    public Color playerColor;
    public string playerWinCount;
    public string ready;
    public List<EndGameData> playerData = new List<EndGameData>();
}

public class GameManager : NetworkBehaviour
{
    static public GameManager s_Instance;

    static public List<TankManager> m_Tanks = new List<TankManager>();             // A collection of managers for enabling and disabling different aspects of the tanks.

    public int m_NumRoundsToWin = 5;          // The number of rounds a single player has to win to win the game.
    public float m_StartDelay = 3f;           // The delay between the start of RoundStarting and RoundPlaying phases.
    public float m_EndDelay = 3f;             // The delay between the end of RoundPlaying and RoundEnding phases.
    public CameraControl m_CameraControl;     // Reference to the CameraControl script for control during different phases.
    public Text m_MessageText;                // Reference to the overlay Text to display winning text, etc.

    public Transform[] m_SpawnPoint;

    [HideInInspector]
    [SyncVar]
    public bool m_GameIsFinished = false;

    //Various UI references to hide the screen between rounds.
    [Space]
    [Header("UI")]
    public CanvasGroup m_FadingScreen;
    public CanvasGroup m_EndRoundScreen;


    private int m_RoundNumber;                  // Which round the game is currently on.
    private WaitForSeconds m_StartWait;         // Used to have a delay whilst the round starts.
    private WaitForSeconds m_EndWait;           // Used to have a delay whilst the round or game ends.
    private TankManager roundWinner;          // Reference to the winner of the current round.  Used to make an announcement of who won.
    private TankManager gameWinner;           // Reference to the winner of the game.  Used to make an announcement of who won.
    public int arenaIndex = 0;
    public GameObject[] arenas;
    public GameObject cratePrefab;
    [HideInInspector]
    public bool missileReady = false;
    [HideInInspector]
    public bool clickOnUI = false;
    [HideInInspector]
    public bool ARMode = false;// this var=true mean we are AR mode
    [Space]
    [Header("AR")]
    public Transform m_AREntity;
    public Transform m_NormalEntity;
    private Transform m_ActiveTerrain;
    private bool firstTimeDetect, firstTimeReady = false;
    public Transform playerHolder;
    public bool SetMissileReady()
    {
        missileReady = !missileReady;
        return missileReady;
    }

    void Awake()
    {
        s_Instance = this;
        LobbyManager.s_Singleton.missileDelegate = SetMissileReady;
        Screen.SetResolution(960, 540, true);
    }

    [ServerCallback]
    private void Start()
    {
        // Create the delays so they only have to be made once.
        m_StartWait = new WaitForSeconds(m_StartDelay);
        m_EndWait = new WaitForSeconds(m_EndDelay);
        //InitNormalGame();
        // Once the tanks have been created and the camera is using them as targets, start the game.
        StartCoroutine(GameLoop());
    }

    public void InitARGame()
    {
        m_AREntity.gameObject.SetActive(true);
        DatasetManager.ActivateDataSet("box");
        m_AREntity.GetChild(1).position = m_ActiveTerrain.position + new Vector3(50, 0, 50);
        m_AREntity.GetChild(1).gameObject.SetActive(true);
        m_AREntity.GetChild(1).GetComponent<Vuforia.ImageTargetBehaviour>().enabled = true;
        m_AREntity.GetChild(2).gameObject.SetActive(false);
        m_NormalEntity.gameObject.SetActive(false);
    }

    public void InitUDT()
    {
        m_AREntity.gameObject.SetActive(true);
        DatasetManager.DeActivateAllDatasets();
        m_AREntity.GetChild(1).gameObject.SetActive(false);
        m_AREntity.GetChild(2).gameObject.SetActive(true);
        m_AREntity.GetChild(2).GetChild(1).position = m_ActiveTerrain.position + new Vector3(50, 0, 50);
        m_AREntity.GetChild(1).GetComponent<Vuforia.ImageTargetBehaviour>().enabled = false;
        m_AREntity.GetChild(2).GetChild(1).gameObject.SetActive(true);
        m_NormalEntity.gameObject.SetActive(false);
    }

    public void OnImageTargetFound()
    {
        //Time.timeScale = 1;
        if (m_GameIsFinished) return;
        if (m_ActiveTerrain)
        {
            m_ActiveTerrain.gameObject.SetActive(true);
            m_ActiveTerrain.GetComponent<Terrain>().enabled = true;
            for (int i = 0; i < m_ActiveTerrain.childCount; i++)
            {
                m_ActiveTerrain.GetChild(i).gameObject.SetActive(true);
            }
        }
        for (int i = 0; i < playerHolder.childCount; i++)
        {
            playerHolder.GetChild(i).GetComponent<TankSetup>().HideTankRenderer(true);
        }
        LobbyManager.s_Singleton.ChangeTo(LobbyManager.s_Singleton.m_PlayARPanel);
        if (firstTimeDetect)
        {
            firstTimeDetect = false;
        }
    }

    public void OnImageTargetLost()
    {
        if (m_GameIsFinished) return;
        LobbyManager.s_Singleton.ChangeTo(LobbyManager.s_Singleton.m_DetectingTarget);
        if (m_ActiveTerrain)
        {
            m_ActiveTerrain.GetComponent<Terrain>().enabled = false;
            for (int i = 0; i < m_ActiveTerrain.childCount; i++)
            {
                m_ActiveTerrain.GetChild(i).gameObject.SetActive(false);
            }
        }
        for (int i = 0; i < playerHolder.childCount; i++)
        {
            playerHolder.GetChild(i).GetComponent<TankSetup>().HideTankRenderer(false);
        }
    }

    public void InitNormalGame()
    {
        m_AREntity.gameObject.SetActive(false);
        m_NormalEntity.gameObject.SetActive(true);
        //GetComponent<AudioSource>().Play();
    }

    public void BeginGame()
    {
        ArenaInit();
        LobbyManager.s_Singleton.arInit = InitARGame;
        LobbyManager.s_Singleton.normalInit = InitNormalGame;
        LobbyManager.s_Singleton.m_UDTInit = InitUDT;
        LobbyManager.s_Singleton.OnBeginMultiplay();
    }

    public void OnDestroy()
    {
        StopAllCoroutines();
    }

    void OnApplicationQuit()
    {
        StopAllCoroutines();
    }

    public void ArenaInit()
    {
        //Init arena correspond to arena choosed in lobby
        for (int i = 0; i < arenas.Length; i++)
            if (i != arenaIndex)
                arenas[i].SetActive(false);
        arenas[arenaIndex].SetActive(false);
        m_ActiveTerrain = arenas[arenaIndex].transform;
        //ResetAllTanks();
    }

    private void DisableAllArena()
    {
        for (int i = 0; i < arenas.Length; i++)
            arenas[i].SetActive(false);
    }

    public void SpawnGameItem()
    {
        GameObject go = Instantiate(cratePrefab, Vector3.zero, Quaternion.identity) as GameObject;
        //todo: spawn plane and after that the plane drop a crate. handle different terrain

        int index = Random.Range(0, 4);
        int posX = Random.Range(-5, 5);
        int posZ = Random.Range(-5, 5);
        Vector3 originSpawnPoint = m_SpawnPoint[index + 4 * arenaIndex].position;
        Vector3 crateSpawnPoint = new Vector3(originSpawnPoint.x + posX, 30, originSpawnPoint.z);
        go.transform.position = crateSpawnPoint;
        NetworkServer.Spawn(go);
    }
    /// <summary>
    /// Add a tank from the lobby hook
    /// </summary>
    /// <param name="tank">The actual GameObject instantiated by the lobby, which is a NetworkBehaviour</param>
    /// <param name="playerNum">The number of the player (based on their slot position in the lobby)</param>
    /// <param name="c">The color of the player, choosen in the lobby</param>
    /// <param name="name">The name of the Player, choosen in the lobby</param>
    /// <param name="localID">The localID. e.g. if 2 player are on the same machine this will be 1 & 2</param>
    static public void AddTank(GameObject tank, int playerNum, Color c, string name, int localID, int tankType)
    {
        TankManager tmp = new TankManager();
        tmp.instance = tank;
        tmp.playerNumber = playerNum;
        tmp.playerColor = c;
        tmp.playerName = name;
        tmp.localPlayerID = localID;
        tmp.tankType = tankType;
        tmp.Setup();

        m_Tanks.Add(tmp);
    }

    public void RemoveTank(GameObject tank)
    {
        TankManager toRemove = null;
        foreach (var tmp in m_Tanks)
        {
            if (tmp.instance == tank)
            {
                toRemove = tmp;
                break;
            }
        }

        if (toRemove != null)
            m_Tanks.Remove(toRemove);
    }

    // This is called from start and will run each phase of the game one after another. ONLY ON SERVER (as Start is only called on server)
    private IEnumerator GameLoop()
    {
        float waitTime = 0f;
        while (m_Tanks.Count < 2 && waitTime < Constants.START_GAME_WAIT_TIME)
        {
            waitTime += Time.deltaTime;
            yield return null;
        }
        // this two lines may cause sytem loop after a tank quit unexpected. now we need to sure if there is only a tank so we can break.
        int tankAliveCount = 0;
        for (int i = 0; i < m_Tanks.Count; i++)
        {
            if (m_Tanks[i].instance != null)
                tankAliveCount++;
        }
        if (!firstTimeReady)
        {
            bool allPlayerReady = true;
            do
            {
                allPlayerReady = true;
                foreach (var tmp in m_Tanks)
                {
                    allPlayerReady &= tmp.IsReady();
                }
                yield return new WaitForSeconds(1);
            }
            while (!allPlayerReady);
            firstTimeReady = true;
        }
        if (tankAliveCount == 1)
        {
            LobbyManager.s_Singleton.ServerReturnToLobby();
        }
        else
        {
            //wait to be sure that all are ready to start
            yield return new WaitForSeconds(1.0f);
            m_ActiveTerrain.gameObject.SetActive(true);
            // Start off by running the 'RoundStarting' coroutine but don't return until it's finished.
            yield return StartCoroutine(RoundStarting());

            // Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished.
            yield return StartCoroutine(RoundPlaying());

            // Once execution has returned here, run the 'RoundEnding' coroutine.
            yield return StartCoroutine(RoundEnding());

            // This code is not run until 'RoundEnding' has finished.  At which point, check if there is a winner of the game.
            if (gameWinner != null)
            {// If there is a game winner, wait for certain amount or all player confirmed to start a game again
                m_GameIsFinished = true;
                float leftWaitTime = 15.0f;
                bool allAreReady = false;
                int flooredWaitTime = 15;

                while (leftWaitTime > 0.0f && !allAreReady)
                {
                    yield return null;

                    allAreReady = true;
                    foreach (var tmp in m_Tanks)
                    {
                        allAreReady &= tmp.IsReady();
                    }

                    leftWaitTime -= 0.01f;

                    int newFlooredWaitTime = Mathf.FloorToInt(leftWaitTime);

                    if (newFlooredWaitTime != flooredWaitTime)
                    {
                        flooredWaitTime = newFlooredWaitTime;
                        //EndMessage(flooredWaitTime);
                        GameEndMessage(flooredWaitTime);
                    }
                }
                LobbyManager.s_Singleton.isReady = false;
                LobbyManager.s_Singleton.ServerReturnToLobby();
            }
            else
            {
                // If there isn't a winner yet, restart this coroutine so the loop continues.
                StartCoroutine(GameLoop());
            }
        }
    }

    private IEnumerator RoundStarting()
    {
        //we notify all clients that the round is starting
        RpcRoundStarting();

        // Wait for the specified length of time until yielding control back to the game loop.
        yield return m_StartWait;
    }

    [ClientRpc]
    void RpcRoundStarting()
    {
        // As soon as the round starts reset the tanks and make sure they can't move.
        m_ActiveTerrain.gameObject.SetActive(true);
        for (int i = 0; i < playerHolder.childCount; i++)
        {
            playerHolder.GetChild(i).GetComponent<TankSetup>().HideTankRenderer(true);
        }

        ResetAllTanks();
        DisableTankControl();
        Time.timeScale = 1;
        if (LobbyManager.s_Singleton.ARMode)
        {
            LobbyManager.s_Singleton.ChangeTo(LobbyManager.s_Singleton.m_PlayARPanel);
            if (firstTimeDetect)
            {
                firstTimeDetect = false;
            }
        }
        else
        {
            LobbyManager.s_Singleton.ChangeTo(LobbyManager.s_Singleton.m_PlayPanel);
        }

        // Snap the camera's zoom and position to something appropriate for the reset tanks.
        m_CameraControl.SetAppropriatePositionAndSize();

        // Increment the round number and display text showing the players what round it is.
        m_RoundNumber++;
        m_MessageText.gameObject.SetActive(true);
        m_MessageText.text = "ROUND " + m_RoundNumber;


        StartCoroutine(ClientRoundStartingFade());
    }

    private IEnumerator ClientRoundStartingFade()
    {
        float elapsedTime = 0.0f, lastSyncTime = 0f;
        float wait = m_StartDelay - 0.5f;

        yield return null;

        while (elapsedTime < wait)
        {
            if (m_RoundNumber == 1)
                m_FadingScreen.alpha = 1.0f - (elapsedTime / wait);
            else
                m_EndRoundScreen.alpha = 0.75f - (elapsedTime / wait);

            elapsedTime += Time.deltaTime;

            //sometime, synchronization lag behind because of packet drop, so we make sure our tank are reseted
            if (elapsedTime / wait < 0.5f)
            {
                if (elapsedTime - lastSyncTime > 0.25f)
                {
                    lastSyncTime = elapsedTime;
                    ResetAllTanks();
                }
            }

            yield return null;
        }
        m_FadingScreen.gameObject.SetActive(false);
        m_EndRoundScreen.gameObject.SetActive(false);
    }

    private IEnumerator RoundPlaying()
    {
        //notify clients that the round is now started, they should allow player to move.
        RpcRoundPlaying();

        // While there is not one tank left...
        float timeCount = 0f;
        while (!OneTankLeft())
        {
            timeCount += 0.5f;
            if (timeCount > Constants.SPAWN_ITEM_INTERVAL)
            {
                SpawnGameItem();
                timeCount = 0;
            }
            //todo: spawn a crate every 30s
            // ... return on the next frame.
            yield return new WaitForSeconds(0.5f);
        }
    }

    [ClientRpc]
    void RpcRoundPlaying()
    {
        // As soon as the round begins playing let the players control the tanks.
        EnableTankControl();

        // Clear the text from the screen.
        m_MessageText.text = string.Empty;
        m_MessageText.gameObject.SetActive(false);
    }

    private IEnumerator RoundEnding()
    {
        // Clear the winner from the previous round.
        roundWinner = null;

        // See if there is a winner now the round is over.
        roundWinner = GetRoundWinner();

        // If there is a winner, increment their score.
        if (roundWinner != null)
            roundWinner.wins++;

        // Now the winner's score has been incremented, see if someone has one the game.
        gameWinner = GetGameWinner();
        RpcUpdateMessage(RoundEndMessage());

        //notify client they should disable tank control
        RpcRoundEnding();

        // Wait for the specified length of time until yielding control back to the game loop.
        yield return m_EndWait;
    }

    [ClientRpc]
    private void RpcRoundEnding()
    {
        DisableTankControl();
        StartCoroutine(ClientRoundEndingFade());
    }

    [ClientRpc]
    private void RpcUpdateMessage(string msg)
    {
        m_MessageText.gameObject.SetActive(true);
        m_MessageText.text = msg;
    }

    private IEnumerator ClientRoundEndingFade()
    {
        float elapsedTime = 0.0f;
        float wait = m_EndDelay;
        while (elapsedTime < wait)
        {
            m_EndRoundScreen.alpha = (elapsedTime / wait);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    // This is used to check if there is one or fewer tanks remaining and thus the round should end.
    private bool OneTankLeft()
    {
        // Start the count of tanks left at zero.
        int numTanksLeft = 0;

        // Go through all the tanks...
        for (int i = 0; i < m_Tanks.Count; i++)
        {
            // ... and if they are active, increment the counter.
            if (m_Tanks[i].CheckTankAlive())
                numTanksLeft++;
        }

        // If there are one or fewer tanks remaining return true, otherwise return false.
        return numTanksLeft <= 1;
    }


    // This function is to find out if there is a winner of the round.
    // This function is called with the assumption that 1 or fewer tanks are currently active.
    private TankManager GetRoundWinner()
    {
        // Go through all the tanks...
        for (int i = 0; i < m_Tanks.Count; i++)
        {
            // ... and if one of them is active, it is the winner so return it.
            if (/*m_Tanks[i].tankRenderers.activeSelf*/m_Tanks[i].CheckTankAlive())
                return m_Tanks[i];
        }

        // If none of the tanks are active it is a draw so return null.
        return null;
    }


    // This function is to find out if there is a winner of the game.
    private TankManager GetGameWinner()
    {
        int maxScore = 0;

        // Go through all the tanks...
        for (int i = 0; i < m_Tanks.Count; i++)
        {
            if (m_Tanks[i].wins > maxScore)
            {
                maxScore = m_Tanks[i].wins;
            }

            // ... and if one of them has enough rounds to win the game, return it.
            if (m_Tanks[i].wins == m_NumRoundsToWin)
                return m_Tanks[i];
        }

        //go throught a second time to enable/disable the crown on tanks
        //(note : we don't enter it if the maxScore is 0, as no one is current leader yet!)
        for (int i = 0; i < m_Tanks.Count && maxScore > 0; i++)
        {
            m_Tanks[i].SetLeader(maxScore == m_Tanks[i].wins);
        }

        // If no tanks have enough rounds to win, return null.
        return null;
    }


    // Returns a string of each player's score in their tank's color.
    private string RoundEndMessage()
    {
        // By default, there is no winner of the round so it's a draw.
        string message = "DRAW!";

        if (roundWinner != null)
            message = "<color=#" + ColorUtility.ToHtmlStringRGB(roundWinner.playerColor) + ">" + roundWinner.playerName + "</color> WINS THE ROUND!";

        return message;
    }


    // Returns a string of each player's score in their tank's color.
    private void GameEndMessage(int waitTime)
    {
        EndGameData endGameData = new EndGameData();

        // By default, there is no winner of the round so it's a draw.
        string message = "DRAW!";


        // If there is a game winner set the message to say which player has won the game.
        if (gameWinner != null)
            message = "<color=#" + ColorUtility.ToHtmlStringRGB(gameWinner.playerColor) + ">" + gameWinner.playerName + "</color> WINS THE GAME!";
        // If there is a winner, change the message to display 'PLAYER #' in their color and a winning message.
        else if (roundWinner != null)
            message = "<color=#" + ColorUtility.ToHtmlStringRGB(roundWinner.playerColor) + ">" + roundWinner.playerName + "</color> WINS THE ROUND!";


        endGameData.title = message;

        // Go through all the tanks and display their scores with their 'PLAYER #' in their color.

        for (int i = 0; i < m_Tanks.Count; i++)
        {
            EndGameData tmp = new EndGameData();
            tmp.playerColor = m_Tanks[i].playerColor;
            tmp.playerName = m_Tanks[i].playerName;
            tmp.playerWinCount = m_Tanks[i].wins + " ROUNDS. ";
            tmp.ready = (m_Tanks[i].IsReady() ? "READY" : "NOT READY");
            endGameData.playerData.Add(tmp);
        }

        if (gameWinner != null)
        {
            message = "Return to lobby in " + waitTime + ".Press \"Replay\" button to get ready";
            endGameData.msg = message;
        }
        string msg = JsonUtility.ToJson(endGameData);
        RpcDisplayEndgame(msg);
        return;
    }

    [ClientRpc]
    private void RpcDisplayEndgame(string json)
    {
        m_MessageText.gameObject.SetActive(false);
        LobbyManager.s_Singleton.ChangeTo(LobbyManager.s_Singleton.m_SummaryPanel);
        EndGameData data = JsonUtility.FromJson<EndGameData>(json);
        Transform summaryPanel = LobbyManager.s_Singleton.m_SummaryPanel.GetChild(1);
        summaryPanel.GetChild(0).GetComponent<Text>().text = data.title;

        for (int i = summaryPanel.childCount - 2; i > 0; i--)
        {
            summaryPanel.GetChild(i).gameObject.SetActive(false);
        }

        for (int i = 0; i < m_Tanks.Count; i++)
        {
            EndGameData player = data.playerData[i];
            summaryPanel.GetChild(i + 1).gameObject.SetActive(true);
            summaryPanel.GetChild(i + 1).GetComponent<Image>().color = player.playerColor;
            summaryPanel.GetChild(i + 1).GetChild(0).GetComponent<Text>().text = player.playerName;
            summaryPanel.GetChild(i + 1).GetChild(1).GetComponent<Text>().text = player.playerWinCount;
            summaryPanel.GetChild(i + 1).GetChild(2).GetComponent<Text>().text = player.ready;
        }
        summaryPanel.GetChild(summaryPanel.childCount - 1).GetComponent<Text>().text = data.msg;
    }


    // This function is used to turn all the tanks back on and reset their positions and properties.
    private void ResetAllTanks()
    {
        for (int i = 0; i < m_Tanks.Count; i++)
        {
            m_Tanks[i].spawnPoint = m_SpawnPoint[m_Tanks[i].m_Setup.m_PlayerNumber + 4 * arenaIndex];
            m_Tanks[i].Reset();
        }
    }


    private void EnableTankControl()
    {
        for (int i = 0; i < m_Tanks.Count; i++)
        {
            m_Tanks[i].EnableControl();
        }
    }


    private void DisableTankControl()
    {
        for (int i = 0; i < m_Tanks.Count; i++)
        {
            m_Tanks[i].DisableControl();
        }
    }

    public Transform GetTankTransformByNumber(int index)
    {
        for (int i = 0; i < m_Tanks.Count; i++)
        {
            if (index == m_Tanks[i].playerNumber)
                return m_Tanks[i].m_Setup.transform;
        }
        return null;
    }

    public void HandlePointerOnUIElement(bool state)
    {
        clickOnUI = state;
    }


}

