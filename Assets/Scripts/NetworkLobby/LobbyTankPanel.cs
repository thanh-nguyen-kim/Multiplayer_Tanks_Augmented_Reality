using UnityEngine;
using UnityEngine.UI;
using Prototype.NetworkLobby;
public class LobbyTankPanel : MonoBehaviour
{
    private Transform previousTank, currentTank;
    public Transform previousSlot, currentSlot, nextSlot;
    private Transform[] m_Tanks;
    public int currentTankIndex = 0;
    public Button btnNextTank, btnPreviousTank;
    public Color currentColor = Color.green;
    private int focusMode;
    public Transform infoPanel;
    GameObject hangar;
    public Text m_Health, m_Damage, m_Speed;

    void OnEnable()
    {
        hangar = GameObject.Find("TankHangar") as GameObject;
        hangar.transform.GetChild(0).gameObject.SetActive(true);
        hangar.transform.GetChild(1).gameObject.SetActive(true);
        hangar.transform.GetChild(2).gameObject.SetActive(true);
        Transform tanks = hangar.transform.GetChild(0);
        m_Tanks = new Transform[tanks.childCount];
        for (int i = 0; i < tanks.childCount; i++)
        {
            m_Tanks[i] = tanks.GetChild(i);
        }
        previousTank = null;
        currentTank = null;
        currentTankIndex = LobbyManager.s_Singleton.localLobbyPlayer.tankType;
        currentColor = LobbyManager.s_Singleton.localLobbyPlayer.playerColor;
        ChangeColor();
    }

    void OnDisable()
    {
        hangar.transform.GetChild(0).gameObject.SetActive(false);
        hangar.transform.GetChild(1).gameObject.SetActive(false);
        hangar.transform.GetChild(2).gameObject.SetActive(false);
    }

    public void OnClickTankElement(int i)
    {
    }

    public void FinishChoosingTank()
    {
        //disable all tank lobby element
        //foreach (Transform tank in m_Tanks) tank.gameObject.SetActive(false);
        LobbyPlayer localOne = LobbyManager.s_Singleton.localLobbyPlayer;
        localOne.SetUpTankLobby(currentTankIndex, currentColor);
        LobbyManager.s_Singleton.ChangeTo(LobbyManager.s_Singleton.lobbyPanel);
    }

    private void ChangeColor()
    {
        //currentColor = NextColor();
        Renderer[] renderers = m_Tanks[currentTankIndex].GetChild(0).GetComponentsInChildren<Renderer>();
        foreach (Renderer ren in renderers)
        {
            ren.material.color = currentColor;
        }
    }

    public void NextColor()
    {
        int currentColorIndex = System.Array.IndexOf(Constants.Colors, currentColor);
        int nextColorIndex = currentColorIndex + 1;
        nextColorIndex = nextColorIndex >= Constants.Colors.Length ? 0 : nextColorIndex;
        currentColor = Constants.Colors[nextColorIndex];
        ChangeColor();
    }

    private void InitCurrentColor()
    {
        currentColor = currentTank.GetComponentsInChildren<Renderer>()[0].material.color;
    }

    public void Next()
    {
        int lastTankIndex = currentTankIndex;
        btnPreviousTank.interactable = true;
        if (currentTankIndex < m_Tanks.Length - 1)
        {
            currentTankIndex++;
        }
        if (currentTankIndex == m_Tanks.Length - 1)
        {
            btnNextTank.interactable = false;
        }
        ChangeToTank(lastTankIndex, currentTankIndex);
        //FocusOnTank(tanks[currentTankIndex], true);

        if (currentTankIndex == 0)
        {
            btnPreviousTank.interactable = false;
        }
    }

    public void Back()
    {
        btnNextTank.interactable = true;
        int lastTankIndex = currentTankIndex;
        if (currentTankIndex > 0)
        {
            currentTankIndex--;
        }
        if (currentTankIndex == m_Tanks.Length)
        {
            btnNextTank.interactable = false;
        }

        ChangeToTank(lastTankIndex, currentTankIndex);

        if (currentTankIndex == 0)
        {
            btnPreviousTank.interactable = false;
        }

    }

    private void ChangeToTank(int last, int next)
    {
        if (m_Tanks[last] != null)
        {
            m_Tanks[last].gameObject.SetActive(false);
        }
        m_Tanks[next].gameObject.SetActive(true);
        ChangeColor();
        UpdateTankInfoPanel();
    }

    public void UpdateTankInfoPanel()
    {
        TankData tankData = m_Tanks[currentTankIndex].GetComponent<TankData>();
        m_Health.text = tankData.health.ToString();
        m_Damage.text = tankData.damage.ToString();
        m_Speed.text = tankData.speed.ToString();
    }
}
