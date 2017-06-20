using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
//Purpose of that class is syncing data between server - client
public class TankSetup : NetworkBehaviour
{
    [Header("UI")]
    public Text m_NameText;
    public GameObject m_Crown;

    [Header("Network")]
    [Space]
    [SyncVar]
    public Color m_Color;

    [SyncVar]
    public string m_PlayerName;

    //this is the player number in all of the players
    [SyncVar]
    public int m_PlayerNumber;

    //This is the local ID when more than 1 player per client
    [SyncVar]
    public int m_LocalID;

    [SyncVar]
    public bool m_IsReady = false;

    [SyncVar]
    public int tankType = 0;
    //This allow to know if the crown must be displayed or not
    protected bool m_isLeader = false;

    [SyncVar(hook = "OnInvisibleChange")]
    public bool isInvisible = false;
    public GameObject m_TankRenderers;
    public GameObject minimapIndicator, minimapCamera;
    public GameObject[] tankRendererPrefabs;

    public Material semiTransparentMat;
    private Material primeMat;

    void Start()
    {
        if (isLocalPlayer)
        {
                Debug.Log("has authority "+m_PlayerName);
                minimapCamera.SetActive(true);
                Prototype.NetworkLobby.LobbyManager.s_Singleton.readyDelegate = SetReady;
        }
        else
        {
            minimapCamera.SetActive(false);
        }

        transform.SetParent(GameManager.s_Instance.playerHolder);
        HideTankRenderer(false);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!isServer)
        {
            GameManager.AddTank(gameObject, m_PlayerNumber, m_Color, m_PlayerName, m_LocalID, this.tankType);

        }
        GameObject _tankRenderer = Instantiate(tankRendererPrefabs[tankType], Vector3.zero, Quaternion.identity) as GameObject;
        _tankRenderer.transform.SetParent(transform);
        _tankRenderer.transform.SetAsFirstSibling();
        _tankRenderer.transform.localPosition = Vector3.zero;
        _tankRenderer.transform.localScale = Vector3.one;
        _tankRenderer.transform.localRotation = Quaternion.identity;
        GetComponent<TankHealth>().m_TankRenderers = _tankRenderer;
        GameObject tankTurret = _tankRenderer.transform.Find("TankTurret").gameObject;
        TankShooting tankShooting = GetComponent<TankShooting>();
        tankShooting.TankTurret = tankTurret;
        tankShooting.m_FireTransform = tankTurret.transform.Find("FireTransform");
        tankShooting.CmdSetTankDamage(Constants.TANK_MULTI_DAMAGE[tankType]);
        GetComponent<NetworkTransformChild>().target = tankTurret.transform;
        GetComponent<NavMeshAgent>().speed = Constants.TANK_MULTI_SPEED[tankType];
        m_TankRenderers = _tankRenderer;

        // Get all of the renderers of the tank.
        Renderer[] renderers = m_TankRenderers.GetComponentsInChildren<Renderer>();
        primeMat = renderers[0].material;
        // Go through all the renderers...
        for (int i = 0; i < renderers.Length; i++)
        {
            // ... set their material color to the color specific to this tank.
            renderers[i].material.color = m_Color;
        }

        if (m_TankRenderers)
            m_TankRenderers.SetActive(false);

        m_NameText.text = "<color=#" + ColorUtility.ToHtmlStringRGB(m_Color) + ">" + m_PlayerName + "</color>";
        m_Crown.SetActive(false);

        //Init tank color in minimap
        minimapIndicator.GetComponent<MeshRenderer>().material.color = m_Color;

    }

    public void HideTankRenderer(bool state)
    {
        transform.GetComponent<TankMovement>().enabled = state;
        transform.GetComponent<TankShooting>().enabled = state;
        for (int i = 0; i < transform.childCount-2; i++)
        {
            transform.GetChild(i).gameObject.SetActive(state);
        }
    }

    public void SetLeader(bool leader)
    {
        RpcSetLeader(leader);
    }

    [ClientRpc]
    public void RpcSetLeader(bool leader)
    {
        m_isLeader = leader;
    }

    public void SetReady()
    {
        CmdSetReady();
    }

    [Command]
    public void CmdSetReady()
    {
        m_IsReady = true;
    }

    [Command]
    public void CmdUnReady()
    {
        m_IsReady = false;
    }

    public void ActivateCrown(bool active)
    {//if we try to show (not hide) the crown, we only show it we are the current leader
        m_Crown.SetActive(active ? m_isLeader : false);
        m_NameText.gameObject.SetActive(active);
    }
    public void Invisible()
    {
        this.isInvisible = true;
        StartCoroutine(FinishedInvisible());
    }

    IEnumerator FinishedInvisible()
    {
        yield return new WaitForSeconds(Constants.INVISIBLE_TIME);
        this.isInvisible = false;
    }
    public void OnInvisibleChange(bool invisibleState)
    {
        if (isLocalPlayer)
        {
            semiTransparentMat.color = new Color(m_Color.r, m_Color.g, m_Color.b, 0.5f);
            // Get all of the renderers of the tank.
            Renderer[] renderers = m_TankRenderers.GetComponentsInChildren<Renderer>();

            // Go through all the renderers...
            for (int i = 0; i < renderers.Length; i++)
            {
                // ... set their material color to the color specific to this tank.
                if (invisibleState)
                    renderers[i].material = semiTransparentMat;
                else
                    renderers[i].material = primeMat;
            }

        }
        else
        {
            //m_TankRenderers.SetActive(!invisibleState);
            GetComponent<TankHealth>().MakeTankInvisible(!invisibleState);
        }

    }

    public override void OnNetworkDestroy()
    {
        GameManager.s_Instance.RemoveTank(gameObject);
    }
}
