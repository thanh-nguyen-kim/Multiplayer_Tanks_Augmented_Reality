using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
public class MenuManager : MonoBehaviour
{
    private static MenuManager instance = null;
    public static MenuManager Instance
    {
        get { return instance; }
    }
    [Header("UI")]
    [Space]
    public RectTransform m_MissionInfoPanel;
    public RectTransform m_PausePanel, m_TopPanel, m_PlayPanel, m_MenuPanel, m_SummaryDialog, m_AlertBanner, m_LoadingPanel;
    public RectTransform m_DetectPanel, m_PlayARPanel, m_TankPanel;
    public Button m_MissileReadyBtn, m_ARMissileReadyBtn;
    public Sprite[] m_Skulls, m_MissileStates;
    public Text m_Coin, m_Skull;
    public Transform m_Glow;
    public Transform m_MissionZero;
    public Transform[] m_GameModeBtn;
    private Transform lastMissionBtn;
    private Transform lastGameModeBtn;
    [HideInInspector]
    public string m_CurrentMission;
    [HideInInspector]
    public int m_GameMode = 0;
    [HideInInspector]
    public RectTransform currentPanel;
    public delegate void BackBtnDelegate();
    [HideInInspector]
    public BackBtnDelegate backDelegate;

    private bool[] itemToBuy = new bool[3];
    public int[] m_ItemCosts;
    public bool ARMode = false;

    [Header("TankPanel")]
    [Space]
    public Transform[] m_Tanks;
    private int activeTank;
    public int equippedTank;
    public Toggle m_EquippedToggle;
    public Button m_BuyButton;
    private GameObject m_Hangar;
    public Text m_Health, m_Damage, m_Speed;
    void Start()
    {
        if (!instance)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        equippedTank = PlayerPrefs.GetInt("EquippedTank", 0);
        ChangeToMission(m_MissionZero);
        SetGameMode(0);
        UpdateUI();
        ChangeToUI(m_MenuPanel);
        backDelegate = ReturnToLobby;//set back button to quit to lobby
        ResetBuyList();
    }

    public void DisplayMissionInfo(MissionInfo info)
    {
        m_MissionInfoPanel.transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = info.m_MissionName;
        m_MissionInfoPanel.transform.GetChild(0).GetChild(2).GetChild(0).GetComponent<Text>().text = info.m_MainObjective;
        m_MissionInfoPanel.transform.GetChild(0).GetChild(3).GetChild(0).GetComponent<Text>().text = info.m_bonusObjective;
        m_CurrentMission = info.m_CurrentMission;
        OfflineVariableManager.Instance.m_MissionIndex = info.m_MissionIndex;
    }

    public void SetGameMode(int mode)
    {
        //deactive old button
        m_GameModeBtn[m_GameMode].GetChild(0).gameObject.SetActive(false);
        m_GameModeBtn[m_GameMode].localScale = Vector3.one;
        //active new button
        m_GameModeBtn[mode].GetChild(0).gameObject.SetActive(true);
        m_GameModeBtn[mode].localScale = Vector3.one * 1.1f;
        m_GameMode = mode;
    }

    public void StartGame()
    {
        OfflineVariableManager.Instance.m_GameMode = m_GameMode;
        StartCoroutine(LoadMissionScene());
    }

    public IEnumerator LoadMissionScene()
    {
        ChangeToUI(m_LoadingPanel);
        yield return SceneManager.LoadSceneAsync(m_CurrentMission, LoadSceneMode.Single);
        if (!GameManagerOffline.s_Instance.ARMode)
            ChangeToUI(m_PlayPanel);
        else
            ChangeToUI(m_DetectPanel);
        SoundManager.Instance.PlayBackGroundAudio(1);
    }

    public void AbortGame()
    {
        GameManagerOffline.s_Instance.gameEnd = true;
        SceneManager.LoadScene("OfflineMenuScene");
        ChangeToUI(m_MenuPanel);
        backDelegate = ReturnToLobby;
        SoundManager.Instance.PlayBackGroundAudio(0);
        Time.timeScale = 1;
    }

    public void ChangeToMission(Transform mission)
    {
        m_Glow.position = mission.position;
        if (lastMissionBtn)
        {
            lastMissionBtn.GetComponent<Animator>().enabled = false;
            lastMissionBtn.localScale = Vector3.one;
        }
        mission.GetComponent<Animator>().enabled = true;
        lastMissionBtn = mission;
        DisplayMissionInfo(mission.GetComponent<MissionInfo>());
    }

    public void UpdateUI()
    {
        StartCoroutine(NumberIncreaseEffect(m_Coin, OfflineVariableManager.Instance.Coin, 1));
        m_Skull.text = OfflineVariableManager.Instance.GetTotalSkull().ToString();
    }

    public void BackButton()
    {
        backDelegate();
    }

    public void PauseButton()
    {
        GameManagerOffline.s_Instance.DisableControl();//disable all game object
        ChangeToUI(m_PausePanel);
        UpdateUI();
        backDelegate = delegate ()
        {
            if (!ARMode)
                ChangeToUI(m_PlayPanel);
            else
                ChangeToUI(m_PlayARPanel);
            GameManagerOffline.s_Instance.EnableControl();
        };
    }

    void ReturnToLobby()
    {
        SceneManager.LoadScene("LobbyScene");
        Destroy(gameObject);
    }

    public void ChangeToUI(RectTransform newPanel)
    {
        if (currentPanel != null)
        {
            currentPanel.gameObject.SetActive(false);
        }

        if (newPanel != null)
        {
            newPanel.gameObject.SetActive(true);
            bool topActive = (newPanel != m_PlayPanel) && (newPanel != m_SummaryDialog)
                && (newPanel != m_DetectPanel) && (newPanel != m_PlayARPanel)
                && (newPanel != m_TankPanel);
            m_TopPanel.gameObject.SetActive(topActive);//top panel not enable in play mode or summary mode
            if (topActive) UpdateUI();
        }
        currentPanel = newPanel;
    }

    private IEnumerator DisplaySkull()
    {
        //display skull
        int active = 1;
        for (int i = 0; i < m_SummaryDialog.GetChild(0).childCount; i++)
        {
            if (i > (int)m_GameMode) active = 0;
            yield return new WaitForSeconds(0.5f);
            m_SummaryDialog.GetChild(0).GetChild(i).GetComponent<Image>().sprite = m_Skulls[active];
        }
    }

    private IEnumerator NumberIncreaseEffect(Text t, float score, float delay)
    {
        yield return delay;
        float count = 0;
        while (count < score)
        {
            count = Mathf.Min(count + score / 30, score);
            t.text = (int)count + "";
            yield return null;
        }
    }

    private string InGameTimeCalc(float time)
    {
        float minute = time / 60;
        int seconds = ((int)time) % 60;
        if (minute >= 1)
            return (int)minute + " min " + seconds + " seconds";
        return seconds + " seconds";
    }

    public IEnumerator SummaryDialogUpdate(bool win)
    {
        ChangeToUI(m_SummaryDialog);//close all UI
        for (int i = 0; i < m_SummaryDialog.GetChild(0).childCount; i++)
        {
            m_SummaryDialog.GetChild(0).GetChild(i).GetComponent<Image>().sprite = m_Skulls[0];
        }
        //reset all text value
        for (int i = m_SummaryDialog.GetChild(1).childCount - 1; i > 0; i--)
        {
            m_SummaryDialog.GetChild(1).GetChild(i).GetChild(1).GetComponent<Text>().text = "0";
        }

        Animator a_Summary = m_SummaryDialog.GetComponent<Animator>();
        m_SummaryDialog.GetChild(2).GetComponent<Button>().interactable = false;
        a_Summary.Play("Summary_Slide_Up");
        yield return new WaitForSeconds(a_Summary.GetCurrentAnimatorStateInfo(0).length);

        if (win)
            StartCoroutine(DisplaySkull());

        int count = GameManagerOffline.s_Instance.EnemeyKilled;
        if (win)

            m_SummaryDialog.GetChild(1).GetChild(0).GetComponent<Text>().text = "Mission Accomplished";
        else
            m_SummaryDialog.GetChild(1).GetChild(0).GetComponent<Text>().text = "Mission Failed";
        m_SummaryDialog.GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = count.ToString();

        count = 0;
        for (int i = 0; i < GameManagerOffline.s_Instance.m_EnemyBuildings.Count; i++)
        {
            if (GameManagerOffline.s_Instance.m_EnemyBuildings[i].IsDestroyed()) count++;
        }

        m_SummaryDialog.GetChild(1).GetChild(2).GetChild(1).GetComponent<Text>().text = count.ToString();

        m_SummaryDialog.GetChild(1).GetChild(3).GetChild(1).GetComponent<Text>().text = InGameTimeCalc(GameManagerOffline.s_Instance.inGameTime);

        for (int i = 1; i < m_SummaryDialog.GetChild(1).childCount; i++)
        {
            m_SummaryDialog.GetChild(1).GetChild(i).gameObject.SetActive(true);
        }

        if (win)
            StartCoroutine(NumberIncreaseEffect(m_SummaryDialog.GetChild(1).GetChild(4).GetChild(1).GetComponent<Text>(), Constants.MAIN_OBJECT_SCORE, 0));
        else
            m_SummaryDialog.GetChild(1).GetChild(4).GetChild(1).GetComponent<Text>().text = "0";
        if (GameManagerOffline.s_Instance.bonus != OType.NIL && GameManagerOffline.s_Instance.bonusMissionCompleted && win)
            StartCoroutine(NumberIncreaseEffect(m_SummaryDialog.GetChild(1).GetChild(5).GetChild(1).GetComponent<Text>(), Constants.BONUS_OBJECT_SCORE, 0));
        else
            m_SummaryDialog.GetChild(1).GetChild(5).GetChild(1).GetComponent<Text>().text = "0";
        count = 0;
        if (win)
            count = Constants.MAIN_OBJECT_SCORE;

        if (GameManagerOffline.s_Instance.bonus != OType.NIL && GameManagerOffline.s_Instance.bonusMissionCompleted && win) count += Constants.BONUS_OBJECT_SCORE;
        if (win)
        {
            yield return StartCoroutine(NumberIncreaseEffect(m_SummaryDialog.GetChild(1).GetChild(6).GetChild(1).GetComponent<Text>(), count, 0));
            OfflineVariableManager.Instance.Coin += count;
            OfflineVariableManager.Instance.SetCurrentMissionSkull((int)m_GameMode+1);
        }
        else
            m_SummaryDialog.GetChild(1).GetChild(6).GetChild(1).GetComponent<Text>().text = "0";
        m_SummaryDialog.GetChild(2).GetComponent<Button>().interactable = true;


    }

    public void AddToBuyList(int i)
    {
        itemToBuy[i] = true;
        UpdateTotalCost();
    }

    public void RemoveFromBuyList(int i)
    {
        itemToBuy[i] = false;
        UpdateTotalCost();
    }

    private void ResetBuyList()
    {
        for (int i = 0; i < itemToBuy.Length; i++)
        {
            itemToBuy[i] = false;
        }
        UpdateTotalCost();
    }

    private void UpdateTotalCost()
    {
        m_PausePanel.GetChild(2).GetChild(1).GetComponent<Text>().text = GetTotalCost().ToString();
    }

    private float GetTotalCost()
    {
        float total = 0;
        for (int i = 0; i < itemToBuy.Length; i++)
        {
            if (itemToBuy[i])
            {
                total += m_ItemCosts[i];
            }
        }
        return total;
    }

    public void Buy()
    {
        //grant them their item and decrease their money
        if (OfflineVariableManager.Instance.Coin >= GetTotalCost())
        {
            OfflineVariableManager.Instance.Coin -= GetTotalCost();
            GameManagerOffline.s_Instance.SpawnCrate(itemToBuy);
            backDelegate();
        }
        else
        {
            ShowAlertBanner(Strings.LOW_COIN_ALERT);
        }
        //dont have enough money
    }

    public void ShowItemInfo(ItemInfo i)
    {
        m_PausePanel.GetChild(5).gameObject.SetActive(true);
        m_PausePanel.GetChild(5).GetComponent<Animator>().Play("ItemInfoPopUp");
        m_PausePanel.GetChild(5).GetChild(1).GetChild(0).GetComponent<Image>().sprite = i.m_ItemIcon;
        m_PausePanel.GetChild(5).GetChild(2).GetComponent<Text>().text = i.m_Title;
        m_PausePanel.GetChild(5).GetChild(3).GetComponent<Text>().text = i.m_Description;
    }

    public void ShowAlertBanner(string alertMessage)
    {
        m_AlertBanner.gameObject.SetActive(true);
        m_AlertBanner.GetChild(0).GetComponent<Animator>().Play("ItemInfoPopUp");
        m_AlertBanner.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = alertMessage;
    }

    public void SetMissileReady()
    {
        bool state = GameManagerOffline.s_Instance.missileReady;
        GameManagerOffline.s_Instance.missileReady = !state;
        Sprite tmp;
        if (state)
        {
            tmp = m_MissileStates[0];
        }
        else
            tmp = m_MissileStates[1];
        if (!ARMode)
            m_MissileReadyBtn.GetComponent<Image>().sprite = tmp;
        else
            m_ARMissileReadyBtn.GetComponent<Image>().sprite = tmp;
    }

    public void MissileReadyButtonState(bool b)
    {
        if (!ARMode)
        {
            m_MissileReadyBtn.interactable = b;
            if (b) { m_MissileReadyBtn.GetComponent<Image>().sprite = m_MissileStates[0]; }
        }
        else
        {
            m_ARMissileReadyBtn.interactable = b;
            if (b) { m_ARMissileReadyBtn.GetComponent<Image>().sprite = m_MissileStates[0]; }
        }
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

    public void ARModeToggle(Toggle t)
    {
        this.ARMode = t.isOn;
    }

    public void PlayButtonClick()
    {
        SoundManager.Instance.PlayClickButton();
    }


    #region TankPanel

    public void OpenTankPanel()
    {
        equippedTank = PlayerPrefs.GetInt("EquippedTank", 0);
        ChangeToUI(m_TankPanel);
        activeTank = equippedTank;
        m_Hangar = GameObject.Find("TankHangar").gameObject;
        for(int i = 0; i < m_Hangar.transform.childCount; i++)
        {
            m_Hangar.transform.GetChild(i).gameObject.SetActive(true);
        }
        int count=m_Hangar.transform.GetChild(0).childCount;
        m_Tanks = new Transform[count];
        for(int i = 0; i < count; i++)
        {
            m_Tanks[i] = m_Hangar.transform.GetChild(0).GetChild(i);
            m_Tanks[i].gameObject.SetActive(false);
        }
        m_Hangar.transform.GetChild(0).GetChild(equippedTank).gameObject.SetActive(true);
        ResetTankUI();
    }
    public void NextTank()
    {
        m_Tanks[activeTank].gameObject.SetActive(false);
        activeTank = (activeTank + 1) % m_Tanks.Length;
        m_Tanks[activeTank].gameObject.SetActive(true);
        ResetTankUI();
    }
    public void PreviousTank()
    {
        m_Tanks[activeTank].gameObject.SetActive(false);
        activeTank = (activeTank - 1) < 0 ? m_Tanks.Length - 1 : activeTank - 1;
        m_Tanks[activeTank].gameObject.SetActive(true);
        ResetTankUI();
    }
    public void BuyTank()
    {
        OfflineVariableManager.Instance.SavePurchaseTank(activeTank, Constants.TANK_COSTS[activeTank]);
        ResetTankUI();
    }

    private void ResetTankUI()
    {
        TankData data=m_Tanks[activeTank].GetComponent<TankData>();
        m_Damage.text = data.damage.ToString();
        m_Health.text = data.health.ToString();
        m_Speed.text = data.speed.ToString();
        bool purchased = OfflineVariableManager.Instance.CheckPurchasedTank(activeTank);
        
        m_BuyButton.gameObject.SetActive(!purchased);
        if (!purchased)
        {
            bool canBuy = (OfflineVariableManager.Instance.GetTotalSkull() - Constants.TANK_COSTS[activeTank]) > -1 ? true : false;
            m_BuyButton.interactable = canBuy;
            m_BuyButton.transform.GetChild(1).GetComponent<Text>().text = Constants.TANK_COSTS[activeTank].ToString();
        }
        else
        {
            m_EquippedToggle.isOn = (activeTank == equippedTank);
        }

        if (activeTank > 0)
            m_EquippedToggle.gameObject.SetActive(purchased);
        else
            m_EquippedToggle.gameObject.SetActive(equippedTank != 0);
    }

    public void OnEquippedToggleChanged()
    {
        if (m_EquippedToggle.isOn)
            equippedTank = activeTank;
        else
            if (activeTank == equippedTank)
            equippedTank = 0;
        ResetTankUI();
        PlayerPrefs.SetInt("EquippedTank", equippedTank);
    }

    public void ReturnToMainMenu()
    {
        PlayerPrefs.SetInt("EquippedTank", equippedTank);
        ChangeToUI(m_MenuPanel);
        for (int i = 0; i < m_Hangar.transform.childCount; i++)
        {
            m_Hangar.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
    #endregion

}
