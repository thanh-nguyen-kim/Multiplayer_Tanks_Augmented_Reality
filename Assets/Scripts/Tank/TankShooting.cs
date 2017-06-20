using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
public class TankShooting : NetworkBehaviour
{
    public int m_PlayerNumber = 1;            // Used to identify the different players.
    public Rigidbody m_Shell;                 // Prefab of the shell.

    public Transform m_FireTransform;         // A child of the tank where the shells are spawned.
    public Slider m_AimSlider;                // A child of the tank that displays the current launch force.
    public AudioSource m_ShootingAudio;       // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
    public AudioClip m_ChargingClip;          // Audio that plays when each shot is charging up.
    public AudioClip m_FireClip;              // Audio that plays when each shot is fired.
    public float m_MinLaunchForce = 15f;      // The force given to the shell if the fire button is not held.
    public float m_MaxLaunchForce = 30f;      // The force given to the shell if the fire button is held for the max charge time.
    public float m_MaxChargeTime = 0.75f;     // How long the shell can charge for before it is fired at max force.

    [SyncVar]
    public int m_localID;

    private string m_FireButton;            // The input axis that is used for launching shells.
    private Rigidbody m_Rigidbody;          // Reference to the rigidbody component.
    [SyncVar]
    private float m_CurrentLaunchForce;     // The force that will be given to the shell when the fire button is released.
    [SyncVar]
    private float m_ChargeSpeed;            // How fast the launch force increases, based on the max charge time.
    private bool m_Fired;                   // Whether or not the shell has been launched with this button press.
    [HideInInspector]
    [SyncVar(hook = "OnHasMissileChange")]
    public bool hasMissile = false;

    public GameObject missilePrefab;

    private GameObject tankTurret;
    private bool turning = false;
    [SyncVar]
    private bool lockedTurret = false;
    private Transform tankTarget;
    private Vector3 tankTargetPos;
    //private NavMeshAgent navAgent;
    Quaternion rootTurretRoration;
    [HideInInspector]
    [SyncVar]
    public int tankDamage = 50;
    [Command]
    public void CmdSetTankDamage(int damage)
    {
        this.tankDamage = damage;
    }
    private void OnHasMissileChange(bool state)
    {
        if (hasAuthority)
        {
            Prototype.NetworkLobby.LobbyManager.s_Singleton.MissileReadyButtonState(state);
            hasMissile = state;
        }
    }

    public GameObject TankTurret
    {
        get
        {
            return tankTurret;
        }

        set
        {
            this.tankTurret = value;
            rootTurretRoration = TankTurret.transform.localRotation;
        }
    }

    private void Awake()
    {
        // Set up the references.
        m_Rigidbody = GetComponent<Rigidbody>();
        // navAgent = GetComponent<NavMeshAgent>();
        tankTarget = null;
    }


    private void Start()
    {
        // The fire axis is based on the player number.
        m_FireButton = "Fire" + (m_localID + 1);

        // The rate that the launch force charges up is the range of possible forces by the max charge time.
        m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;

    }

    public void OnDestroy()
    {
        StopAllCoroutines();
    }

    public void OnApplicationQuit()
    {
        StopAllCoroutines();
    }

    void Update()
    {
        if (!isLocalPlayer || !hasAuthority) return;
        if (!lockedTurret)
        {
            if (turning)
            {
                Vector3 direction;
                if (tankTarget == null)
                    direction = tankTargetPos - transform.position;
                else
                    direction = tankTarget.position - transform.position;
                direction = new Vector3(direction.x, 0, direction.z);
                Quaternion targetRoration = Quaternion.LookRotation(direction);
                TankTurret.transform.rotation = Quaternion.Slerp(TankTurret.transform.rotation, targetRoration, 0.06f * Constants.TANK_TURRET_RORATE_SPEED);
                if (Quaternion.Angle(TankTurret.transform.rotation, targetRoration) < 2)
                    CmdLockedTurret(true);
            }
            else
            {
                if (Quaternion.Angle(TankTurret.transform.localRotation, rootTurretRoration) < 5)
                    CmdLockedTurret(true);
                TankTurret.transform.localRotation = Quaternion.Slerp(TankTurret.transform.localRotation, rootTurretRoration, Time.deltaTime * Constants.TANK_TURRET_RORATE_SPEED);
            }
        }
    }

    private float CalculateLauchForce(Vector3 target)
    {
        float alpha = m_FireTransform.localRotation.eulerAngles.x * Mathf.PI / 180f;

        float xDistance = Vector3.Distance(target, m_FireTransform.position);
        float deltaY = target.y - m_FireTransform.position.y;
        float velocity = xDistance / Mathf.Cos(alpha) * Mathf.Sqrt(Constants.GRAVITY_ACCELERATION / Mathf.Abs(xDistance * Mathf.Tan(alpha) + deltaY) / 2);
        if (velocity > m_MaxLaunchForce) velocity = m_MaxLaunchForce;
        return velocity;
    }

    public void Fire(Transform target)
    {
        // Set the fired flag so only Fire is only called once.
        m_Fired = true;

        //m_CurrentLaunchForce = CalculateLauchForce(target);
        if (hasMissile && GameManager.s_Instance.missileReady)
        {
            GameManager.s_Instance.missileReady = false;
            int targetNumber = target.GetComponent<TankSetup>().m_PlayerNumber;
            CmdFireMissile(targetNumber);
        }
        else
            CmdFire(target.position);
        // Reset the launch force.  This is a precaution in case of missing button events.
        m_CurrentLaunchForce = m_MinLaunchForce;
    }

    public bool ARFireMissile(Transform target)
    {
        m_Fired = true;
        if (hasMissile && GameManager.s_Instance.missileReady)
        {
            GameManager.s_Instance.missileReady = false;
            int targetNumber = target.GetComponent<TankSetup>().m_PlayerNumber;
            CmdFireMissile(targetNumber);
            return true;
        }
        return false;
    }

    public void Fire(Vector3 position)
    {
        m_Fired = true;
        CmdFire(position);
    }


    [Command]
    private void CmdFireMissile(int target)
    {
        Transform targetTransform = GameManager.s_Instance.GetTankTransformByNumber(target);
        if (transform == null) return;
        else
        {
            hasMissile = false;
            StartCoroutine(_FireMissile(targetTransform));
        }
    }

    private IEnumerator _FireMissile(Transform target)
    {
        lockedTurret = false;
        float timeCount = 0f;
        while (!lockedTurret)
        {
            timeCount += 0.1f;
            if (timeCount > 2) yield break;// safely check to exit coroutine if it run for too long
            yield return new WaitForSeconds(0.1f);
        }

        GameObject homingMissile = Instantiate(missilePrefab, m_FireTransform.position, m_FireTransform.rotation) as GameObject;
        homingMissile.GetComponent<HomingMissile>().target = target;
        homingMissile.GetComponent<HomingMissile>().owner = transform;
        Rigidbody homingMissileRig = homingMissile.GetComponent<Rigidbody>();
        homingMissileRig.velocity = m_MaxLaunchForce * m_FireTransform.forward;//make missile lauch velocity a little bigger than current tank velocity to ignore collider
        homingMissileRig.AddForce((Constants.HOMING_MISSILE_LAUCH_FORCE) * homingMissileRig.velocity.normalized);
        NetworkServer.Spawn(homingMissile);
        lockedTurret = false;
    }

    [Command]
    private void CmdFire(Vector3 target)
    {
        //RorateTurret(target);
        StartCoroutine(_Fire(target));
    }

    private IEnumerator _Fire(Vector3 target)
    {
        //bool turning = true;
        lockedTurret = false;
        float timeCount = 0f;
        while (!lockedTurret)
        {
            timeCount += Time.deltaTime;
            if (timeCount > 2) yield break;
            yield return null;
        }

        // Create an instance of the shell and store a reference to it's rigidbody.
        Rigidbody shellInstance =
             Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
        shellInstance.GetComponent<ShellExplosion>().m_MaxDamage = this.tankDamage;
        float launchForce = CalculateLauchForce(target);
        // Create a velocity that is the tank's velocity and the launch force in the fire position's forward direction.
        Vector3 velocity = launchForce * m_FireTransform.forward;

        // Set the shell's velocity to this velocity.
        shellInstance.velocity = velocity;

        NetworkServer.Spawn(shellInstance.gameObject);
        lockedTurret = false;
        RpcPlayShootAudio();
    }

    [ClientRpc]
    private void RpcPlayShootAudio()
    {
        if (SoundManager.Instance.Audio)
        {
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();
        }
    }

    public void RorateTurretToMouseDirection(Vector3 mousePos)
    {
        RorateTurret(mousePos);
    }
    [Command]
    private void CmdLockedTurret(bool val)
    {
        lockedTurret = val;
    }

    private void RorateTurret(Vector3 targetTankPos)
    {
        CmdLockedTurret(false);
        this.tankTarget = null;
        this.tankTargetPos = targetTankPos;
        turning = true;
    }

    public void ResetTurretRoration()
    {
        CmdLockedTurret(false);
        turning = false;
    }

    private void RorateTurret(Transform targetTank)
    {
        //lockedTurret = false;
        CmdLockedTurret(false);
        this.tankTarget = targetTank;
        turning = true;
    }
    // This is used by the game manager to reset the tank.
    public void SetDefaults()
    {
        m_CurrentLaunchForce = m_MinLaunchForce;
        m_AimSlider.value = m_MinLaunchForce;
        if (hasAuthority)
        {
            CmdResetMissile();
            ResetTurretRoration();
        }

    }
    [Command]
    private void CmdResetMissile()
    {
        hasMissile = false;
    }
}