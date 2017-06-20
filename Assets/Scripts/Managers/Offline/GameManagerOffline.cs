using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum OType
{
    NIL,//null
    KTA,//"Kill all enemy."
    SV,//Survive
    ESC,//Escape
    DB//Destroy enemy building
}
public enum GameMode
{
    Private = 0,
    Captain = 1,
    Veteran = 2
}
public class GameManagerOffline : MonoBehaviour
{
    static public GameManagerOffline s_Instance;

    public Transform m_Character;
    public List<TankAI> m_TankAIs = new List<TankAI>();
    public List<Building> m_EnemyBuildings = new List<Building>();
    public List<GameObject> m_SpawnedObject = new List<GameObject>();
    public int m_NumRoundsToWin = 5;          // The number of rounds a single player has to win to win the game.
    public float m_StartDelay = 3f;           // The delay between the start of RoundStarting and RoundPlaying phases.
    public float m_EndDelay = 3f;             // The delay between the end of RoundPlaying and RoundEnding phases.
    public CameraControl m_CameraControl;     // Reference to the CameraControl script for control during different phases.
    public Text m_MessageText;                // Reference to the overlay Text to display winning text, etc.

    public Transform[] m_SpawnPoint;

    //Various UI references to hide the screen between rounds.
    [Space]
    [Header("UI")]
    public CanvasGroup m_FadingScreen;
    public CanvasGroup m_EndRoundScreen;
    public Text m_MainObjective;
    public Text m_BonusObjective;
    public Transform m_CompletedObjective;
    public Sprite[] m_Skulls;

    private int m_RoundNumber;                  // Which round the game is currently on.
    private WaitForSeconds m_StartWait;         // Used to have a delay whilst the round starts.
    private WaitForSeconds m_EndWait;           // Used to have a delay whilst the round or game ends.
    private TankManagerOffline roundWinner;          // Reference to the winner of the current round.  Used to make an announcement of who won.
    private TankManagerOffline gameWinner;           // Reference to the winner of the game.  Used to make an announcement of who won.
    public GameObject m_CratePrefab;
    [HideInInspector]
    public bool missileReady = false;
    [HideInInspector]
    public bool clickOnUI = false;

    private int enemeyKilled = 0;
    [Space]
    [Header("Mission")]
    public float m_SurviveTime;
    [HideInInspector]
    public float inGameTime;
    public string m_MissionName;
    public OType main, bonus;
    public string m_MainMission, m_BonusMission;
    public GameMode m_GameMode;
    [HideInInspector]
    public bool gameEnd = false;
    [HideInInspector]
    public bool bonusMissionCompleted = false;
    public Transform m_ExtractedZone;
    public Transform[] m_EnemySpawnPos;
    [Space]
    [Header("ARSetting")]
    public bool ARMode = false;
    private bool firstTimeDetect;
    public Transform m_AREntity, m_NormalEntity, m_Terrain;

    public int EnemeyKilled
    {
        get
        {
            if (main != OType.SV)
            { 
                enemeyKilled = 0;
                foreach (TankAI ai in m_TankAIs)
                {
                    if (ai.GetComponent<TankHealthOffline>().m_ZeroHealthHappened) enemeyKilled++;
                }
            }
            return enemeyKilled;
        }

        set
        {
            this.enemeyKilled = value;
        }
    }

    public void SetMissileReady()
    {
        missileReady = true;
    }

    void Awake()
    {
        s_Instance = this;
        ARMode = MenuManager.Instance.ARMode;
    }

    void Start()
    {
        firstTimeDetect = true;
        if (ARMode)
        {
            InitARGame();
            Screen.SetResolution(960, 540, true);
        }
        else
        {
            Screen.SetResolution(1280, 720, true);
            InitNormalGame();
        }
    }

    private void InitARGame()
    {
        m_AREntity.gameObject.SetActive(true);
        m_NormalEntity.gameObject.SetActive(false);
    }

    public void OnImageTargetFound()
    {
        if (gameEnd) return;
        Time.timeScale = 1;
        m_Terrain.gameObject.SetActive(true);
        m_Terrain.GetComponent<Terrain>().enabled = true;
        for (int i = 0; i < m_Terrain.childCount; i++)
        {
            m_Terrain.GetChild(i).gameObject.SetActive(true);
        }
        MenuManager.Instance.ChangeToUI(MenuManager.Instance.m_PlayARPanel);
        if (firstTimeDetect)
        {
            firstTimeDetect = false;
            m_StartWait = new WaitForSeconds(m_StartDelay);
            m_EndWait = new WaitForSeconds(m_EndDelay);

            StartCoroutine(GameLoop());
            bonusMissionCompleted = false;
            m_GameMode = (GameMode)OfflineVariableManager.Instance.m_GameMode;
            //GetComponent<AudioSource>().Play();
        }
    }

    public void OnImageTargetLost()
    {
        if (gameEnd) return;
        MenuManager.Instance.ChangeToUI(MenuManager.Instance.m_DetectPanel);
        if (m_Terrain)
        {
            m_Terrain.GetComponent<Terrain>().enabled = false;
            for (int i = 0; i < m_Terrain.childCount; i++)
            {
                m_Terrain.GetChild(i).gameObject.SetActive(false);
            }
        }
        Time.timeScale = 0;
    }

    private void InitNormalGame()
    {
        m_AREntity.gameObject.SetActive(false);
        m_NormalEntity.gameObject.SetActive(true);
        m_StartWait = new WaitForSeconds(m_StartDelay);
        m_EndWait = new WaitForSeconds(m_EndDelay);
        if (m_Terrain)
            m_Terrain.gameObject.SetActive(true);
        StartCoroutine(GameLoop());
        bonusMissionCompleted = false;
        m_GameMode = (GameMode)OfflineVariableManager.Instance.m_GameMode;
        //if (SoundManager.Instance.Audio)
        //    GetComponent<AudioSource>().Play();
    }


    private void OnEnable()
    {
        m_SpawnedObject.Clear();
    }

    public void OnDestroy()
    {
        for (int i = m_SpawnedObject.Count; i > 0; i--)
        {
            Destroy(m_SpawnedObject[i - 1]);
        }
        StopAllCoroutines();
    }

    void OnApplicationQuit()
    {
        StopAllCoroutines();
    }

    public void SpawnGameItem()
    {
        GameObject go = Instantiate(m_CratePrefab, Vector3.zero, Quaternion.identity) as GameObject;

        int index = Random.Range(0, 4);
        int posX = Random.Range(-5, 5);
        int posZ = Random.Range(-5, 5);
    }

    // This is called from start and will run each phase of the game one after another. ONLY ON SERVER (as Start is only called on server)
    private IEnumerator GameLoop()
    {

        // Start off by running the 'RoundStarting' coroutine but don't return until it's finished.
        yield return StartCoroutine(RoundStarting());

        // Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished.
        yield return StartCoroutine(RoundPlaying());

        // Once execution has returned here, run the 'RoundEnding' coroutine.
        yield return StartCoroutine(RoundEnding());
    }

    private IEnumerator RoundStarting()
    {
        // As soon as the round starts reset the tanks and make sure they can't move.
        ResetTank();
        DisableControl();
        yield return StartCoroutine(ClientRoundStartingFade());
        //yield return new WaitForSeconds(1f);//wait for all thing init
        _RoundStarting();

        // Wait for the specified length of time until yielding control back to the game loop.
        //yield return m_StartWait;
        yield return StartCoroutine(DisplayMissionInfo());
    }

    void _RoundStarting()
    {
       
        // Increment the round number and display text showing the players what round it is.
        m_RoundNumber++;
        m_MessageText.gameObject.SetActive(true);
        m_MessageText.text = m_MissionName;
        //StartCoroutine(ClientRoundStartingFade());
    }

    private void ResetTank()
    {
        m_Character.GetComponent<TankMovementOffline>().SetDefaults();
        m_Character.GetComponent<TankShootingOffline>().SetDefaults();
        m_Character.GetComponent<TankHealthOffline>().SetDefaults();
    }

    private IEnumerator ClientRoundStartingFade()
    {
        float elapsedTime = 0.0f;
        float wait = m_StartDelay - 0.5f;

        yield return null;

        while (elapsedTime < wait)
        {
            if (m_RoundNumber == 1)
            {
                m_FadingScreen.alpha = 1.0f - (elapsedTime / wait);
            }
            else
            {
                m_EndRoundScreen.alpha = 0.75f - (elapsedTime / wait);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        m_FadingScreen.gameObject.SetActive(false);
        m_EndRoundScreen.gameObject.SetActive(false);
    }

    private IEnumerator RoundPlaying()
    {
        //notify clients that the round is now started, they should allow player to move.
        _RoundPlaying();

        // While tank alive and objective not complete
        while (!MainObjectiveComplete())
        {
            inGameTime += 1;
            // ... return on the next second.
            if (bonus != OType.NIL)
                if (!bonusMissionCompleted)
                    if (ObjectiveComplete(bonus))//check if bonus objective complete
                    {
                        bonusMissionCompleted = true;
                        StartCoroutine(DisplayBonusMissionComplete());
                    }
            //check if player is dead so we can display end game
            if (m_Character.GetComponent<TankHealthOffline>().m_ZeroHealthHappened)
                yield break;
            yield return new WaitForSeconds(1);
        }
    }

    private bool MainObjectiveComplete()
    {
        return ObjectiveComplete(main);
    }

    private bool ObjectiveComplete(OType type)
    {
        switch (type)
        {
            case OType.DB:
                return CheckDestroyAllBuilding();
            case OType.ESC:
                return CheckArrivedExtractedZone();
            case OType.KTA:
                return CheckDestroyAllEnemy();
            case OType.SV:
                return CheckSurviveEnough();
            default:
                return true;
        }
    }

    private bool CheckDestroyAllBuilding()
    {
        bool result = true;
        for (int i = m_EnemyBuildings.Count - 1; i > -1; i--)
        {
            if (!m_EnemyBuildings[i].GetComponent<Building>().m_ZeroHealthHappened)
            {
                result = false;
                break;
            }
        }
        return result;
    }

    private bool CheckSurviveEnough()
    {
        if (inGameTime > m_SurviveTime)
        {
            return true;
        }
        else
        {
            SpawnEnemyTank();
            return false;
        }
    }

    private bool CheckArrivedExtractedZone()
    {
        if (Vector3.Distance(m_Character.position, m_ExtractedZone.position) < 3)
            return true;
        return false;
    }

    private bool CheckDestroyAllEnemy()
    {
        bool complete = true;
        foreach (TankAI tank in m_TankAIs)
        {
            if (!tank.GetComponent<TankHealthOffline>().m_ZeroHealthHappened)
            {
                complete = false;
                break;
            }
        }
        return complete;
    }

    private void SpawnEnemyTank()
    {
        int i = 0;
        for (i = m_TankAIs.Count - 1; i > -1; i--)
        {
            if (m_TankAIs[i].GetComponent<TankHealthOffline>().m_ZeroHealthHappened)
            {
                enemeyKilled++;
                m_TankAIs[i].transform.position = m_EnemySpawnPos[Random.Range(0, m_EnemySpawnPos.Length)].position;
                m_TankAIs[i].GetComponent<TankHealthOffline>().SetDefaults();
                m_TankAIs[i].GetComponent<TankAI>().State = AIState.Attack;
                break;
            }
        }
        
    }
    void _RoundPlaying()
    {
        // As soon as the round begins playing let the players control the tanks.
        EnableControl();

        // Clear the text from the screen.
        m_MessageText.text = string.Empty;
        m_MessageText.gameObject.SetActive(false);
    }

    private IEnumerator RoundEnding()
    {
        gameEnd = true;
        // check If player is dead or alive
        if (!m_Character.GetComponent<TankHealthOffline>().m_ZeroHealthHappened)
        {

            yield return StartCoroutine(DisplayMainMissionComplete());
            if (bonus != OType.NIL)
                if (!bonusMissionCompleted)
                    if (ObjectiveComplete(bonus))//check if bonus objective complete
                    {
                        bonusMissionCompleted = true;
                        yield return StartCoroutine(DisplayBonusMissionComplete());
                    }
            SummaryDialogUpdate(true);
        }
        else
        {
            SummaryDialogUpdate(false);
            //level false 
        }

        _RoundEnding();

        // Wait for the specified length of time until yielding control back to the game loop.
        yield return m_EndWait;
    }

    private void _RoundEnding()
    {
        DisableControl();
        StartCoroutine(ClientRoundEndingFade());
    }

    private void SummaryDialogUpdate(bool win)
    {
        StartCoroutine(MenuManager.Instance.SummaryDialogUpdate(win));
    }

    private void _UpdateMessage(string msg)
    {
        m_MessageText.text = msg;
    }

    private IEnumerator ClientRoundEndingFade()
    {
        float elapsedTime = 0.0f;
        float wait = m_EndDelay;
        m_EndRoundScreen.gameObject.SetActive(true);
        while (elapsedTime < wait)
        {
            m_EndRoundScreen.alpha = (elapsedTime / wait) < 0.75f ? (elapsedTime / wait) : 0.75f;

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    public void EnableControl()
    {
        m_Character.GetComponent<TankMovementOffline>().enabled = true;
        m_Character.GetComponent<TankMovementOffline>().ReEnableParticles();
        m_Character.GetComponent<TankShootingOffline>().enabled = true;
        for (int i = 0; i < m_TankAIs.Count; i++)
        {
            m_TankAIs[i].GetComponent<TankAI>().enabled=!m_TankAIs[i].GetComponent<TankHealthOffline>().m_ZeroHealthHappened;
        }
        for (int i = 0; i < m_EnemyBuildings.Count; i++)
        {
            Building building = m_EnemyBuildings[i].GetComponent<Building>();
            building.enabled = !building.m_ZeroHealthHappened;
        }
    }

    public void DisableControl()
    {
        m_Character.GetComponent<TankMovementOffline>().enabled = false;
        m_Character.GetComponent<TankShootingOffline>().enabled = false;

        for (int i = 0; i < m_TankAIs.Count; i++)
        {
            m_TankAIs[i].GetComponent<TankAI>().enabled = false;
        }
        for (int i = 0; i < m_EnemyBuildings.Count; i++)
        {
            m_EnemyBuildings[i].GetComponent<Building>().enabled = false;
        }
    }

    public void HandlePointerOnUIElement(bool state)
    {
        clickOnUI = state;
    }

    private IEnumerator DisplayBonusMissionComplete()
    {
        m_CompletedObjective.GetChild(2).GetComponent<Text>().text = "Bonus Objective Completed";
        m_CompletedObjective.GetChild(3).GetComponentInChildren<Text>().text = m_BonusMission;
        m_CompletedObjective.gameObject.SetActive(true);
        m_CompletedObjective.GetComponent<Animator>().Play("Mission_Message");
        yield return new WaitForSeconds(3);
        m_CompletedObjective.GetComponent<Animator>().Play("Mission_Message_Reverse");
        yield return new WaitForSeconds(1);
        m_CompletedObjective.gameObject.SetActive(false);
    }

    private IEnumerator DisplayMainMissionComplete()
    {
        m_CompletedObjective.GetChild(2).GetComponent<Text>().text = "Main Objective Completed";
        m_CompletedObjective.GetChild(3).GetComponentInChildren<Text>().text = m_MainMission;
        m_CompletedObjective.gameObject.SetActive(true);
        m_CompletedObjective.GetComponent<Animator>().Play("Mission_Message");
        yield return new WaitForSeconds(3);
        m_CompletedObjective.GetComponent<Animator>().Play("Mission_Message_Reverse");
        yield return new WaitForSeconds(1);
        m_CompletedObjective.gameObject.SetActive(false);
    }

    private IEnumerator DisplayMissionInfo()
    {
        Animator a_Main = m_MainObjective.GetComponent<Animator>();
        Animator a_Bonus = m_BonusObjective.GetComponent<Animator>();
        m_MainObjective.GetComponent<Text>().text = "Main Objective";
        m_MainObjective.transform.GetChild(0).GetComponentInChildren<Text>().text = m_MainMission;
        if (bonus != OType.NIL)
        {
            m_BonusObjective.GetComponent<Text>().text = "Bonus Objective";
            m_BonusObjective.transform.GetChild(0).GetComponentInChildren<Text>().text = m_BonusMission;
        }
        m_MainObjective.gameObject.SetActive(true);
        a_Main.Play("Mission_Message");
        if (bonus != OType.NIL)
        {
            yield return new WaitForSeconds(1);
            m_BonusObjective.gameObject.SetActive(true);
            a_Bonus.Play("Mission_Message");
        }
        yield return new WaitForSeconds(3);
        a_Main.Play("Mission_Message_Reverse");
        yield return new WaitForSeconds(1);
        a_Bonus.Play("Mission_Message_Reverse");
        yield return new WaitForSeconds(1);
        m_MainObjective.gameObject.SetActive(false);
        m_BonusObjective.gameObject.SetActive(false);
    }

    public void ReturnToOfflineMenu()
    {
        MenuManager.Instance.AbortGame();
    }

    public void SpawnCrate(bool[] itemToSpawn)
    {
        GameObject go = Instantiate(m_CratePrefab, m_Character.transform.position + Vector3.up * 20, Quaternion.identity) as GameObject;
        go.GetComponent<CrateOffline>().itemToSpawn = itemToSpawn;
        m_SpawnedObject.Add(go);
    }

}

