using UnityEngine.UI;
using UnityEngine;
using Prototype.NetworkLobby;
public class LobbyArenaPanel : MonoBehaviour
{
    public LobbyManager lobbyManager;
    public Text terrainDescription;
    //public RectTransform contentPanel;
    public Image m_ArenaScreenShot;
    public Sprite[] m_ArenaSprites;
    private int tmpArena = -1, elementOffset = 0;
    private Vector3 targetPos;
    private bool needFocus;
    private void OnEnable()
    {
        tmpArena = -1;
        //elementOffset = (int)contentPanel.rect.width / contentPanel.childCount;
        needFocus = true;
        //targetPos= new Vector3(0, contentPanel.localPosition.y, contentPanel.localPosition.z);
        lobbyManager.backDelegate=delegate () { lobbyManager.ChangeTo(lobbyManager.mainMenuPanel); };
    }
    public void DisplayChoosingArena(int i)
    {
        if (tmpArena != i)
        {
            tmpArena = i;
            //targetPos = new Vector3(-elementOffset * i, contentPanel.localPosition.y, contentPanel.localPosition.z);
            //needFocus = true;
            //ToastSystem.Instance.ShowToast("Click again to select this arena");
            m_ArenaScreenShot.sprite = m_ArenaSprites[i];
            m_ArenaScreenShot.gameObject.SetActive(true);
            m_ArenaScreenShot.GetComponent<Animator>().Play("ArenaScreenShotPopout");
            if (tmpArena >= 0 && tmpArena < Strings.ARENA_DESCRIPTION.Length)
                terrainDescription.text = Strings.ARENA_DESCRIPTION[tmpArena];
        }
    }

    public void Finish()
    {
        lobbyManager.arenaIndex = tmpArena;
        OnClickHost();
    }

    private void OnClickHost()
    {
        lobbyManager.StartHost();
    }
}

