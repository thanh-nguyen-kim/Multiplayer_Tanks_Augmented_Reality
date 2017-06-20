using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class TankSetupOffline : MonoBehaviour
{
    [Header("UI")]
    public Text m_NameText;
    public GameObject m_Crown;

    [Header("Network")]
    [Space]
    public Color m_Color;

    public string m_PlayerName;

    //this is the player number in all of the players
    public int m_PlayerNumber;

    //This is the local ID when more than 1 player per client
    public int m_LocalID;

    public bool m_IsReady = false;

    public int tankType = 0;
    //This allow to know if the crown must be displayed or not
    protected bool m_isLeader = false;

    public bool isInvisible = false;
    public GameObject m_TankRenderers;
    public GameObject minimapIndicator, minimapCamera;
    public GameObject[] tankRendererPrefabs;

    public Material semiTransparentMat;
    private Material primeMat;

    void Start()
    {
        tankType = MenuManager.Instance.equippedTank;
        GameObject _tankRenderer = Instantiate(tankRendererPrefabs[tankType], transform.position, Quaternion.identity) as GameObject;
        _tankRenderer.transform.SetParent(transform);
        _tankRenderer.transform.SetAsFirstSibling();
        _tankRenderer.transform.localPosition = Vector3.zero;
        _tankRenderer.transform.localScale = Vector3.one;
        _tankRenderer.transform.localRotation = Quaternion.identity;
        GetComponent<TankHealthOffline>().m_TankRenderers = _tankRenderer;
        GetComponent<NavMeshAgent>().speed = Constants.TANK_SPEED[tankType];
        GameObject tankTurret = _tankRenderer.transform.Find("TankTurret").gameObject;
        TankShootingOffline tankShooting = GetComponent<TankShootingOffline>();
        tankShooting.tankDamage = Constants.TANK_DAMAGE[tankType];
        tankShooting.TankTurret = tankTurret;
        tankShooting.m_FireTransform = tankTurret.transform.Find("FireTransform");
        m_TankRenderers = _tankRenderer;

        m_NameText.text = "<color=#" + ColorUtility.ToHtmlStringRGB(m_Color) + ">" + m_PlayerName + "</color>";
        GetComponent<AudioSource>().enabled = SoundManager.Instance.Audio;
    }

    public void SetLeader(bool leader)
    {
        m_isLeader = leader;
    }



    public void SetReady()
    {
        m_IsReady = true;
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

    public bool IsAlive()
    {
        return true;
    }

    public void OnInvisibleChange(bool invisibleState)
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

    public void SimulateExplosionForce(float force,Vector3 center,float radius)
    {
        //GetComponent<NavMeshAgent>().Stop();
        //modify center of explode
        Vector3 newCenter = new Vector3(center.x, transform.position.y - 3, center.z);

        GetComponent<NavMeshAgent>().enabled = false;
        //GetComponent<Rigidbody>().isKinematic = false;
        GetComponent<Rigidbody>().AddExplosionForce(force, newCenter, radius);
        StartCoroutine(_FinishSimulation());
    }

    private IEnumerator _FinishSimulation()
    {
        yield return new WaitForSeconds(0.5f);
        Rigidbody tankRig = GetComponent<Rigidbody>();
        while (tankRig.velocity.magnitude > 1)
            yield return null;
        //tankRig.isKinematic = true;
        GetComponent<NavMeshAgent>().enabled = true;
        GetComponent<NavMeshAgent>().Resume();
    }
}
