using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class TankShootingOffline : MonoBehaviour
{
    public int m_PlayerNumber = 1;            // Used to identify the different players.
    public Rigidbody m_Shell;                 // Prefab of the shell.

    public Transform m_FireTransform;         // A child of the tank where the shells are spawned.
    public Slider m_AimSlider;                // A child of the tank that displays the current launch force.
    public AudioClip m_ChargingClip;          // Audio that plays when each shot is charging up.
    public AudioSource m_ShootingAudio;
    public AudioClip m_FireClip;              // Audio that plays when each shot is fired.
    public float m_MinLaunchForce = 15f;      // The force given to the shell if the fire button is not held.
    public float m_MaxLaunchForce = 30f;      // The force given to the shell if the fire button is held for the max charge time.
    public float m_MaxChargeTime = 0.75f;     // How long the shell can charge for before it is fired at max force.

    public int m_localID;

    private string m_FireButton;            // The input axis that is used for launching shells.
    private Rigidbody m_Rigidbody;          // Reference to the rigidbody component.
    private float m_CurrentLaunchForce;     // The force that will be given to the shell when the fire button is released.
    private float m_ChargeSpeed;            // How fast the launch force increases, based on the max charge time.
    private bool m_Fired;                   // Whether or not the shell has been launched with this button press.
    private bool hasMissile = false;

    public GameObject missilePrefab;

    public GameObject m_TankTurret;
    private bool turning = false;
    private bool lockedTurret = false;
    private Transform tankTarget;
    private Vector3 tankTargetPos;
    private NavMeshAgent navAgent;
    public Quaternion rootTurretRoration;
    [HideInInspector]
    public float turretAngularSpeed = 1f;
    [HideInInspector]
    public int tankDamage = 0;
    public GameObject TankTurret
    {
        get
        {
            return m_TankTurret;
        }

        set
        {
            this.m_TankTurret = value;
            rootTurretRoration = TankTurret.transform.localRotation;
        }
    }

    public bool HasMissile
    {
        get
        {
            return hasMissile;
        }

        set
        {
            this.hasMissile = value;
            MenuManager.Instance.MissileReadyButtonState(hasMissile);
        }
    }

    private void Awake()
    {
        // Set up the references.
        m_Rigidbody = GetComponent<Rigidbody>();
        navAgent = GetComponent<NavMeshAgent>();
        tankTarget = null;
        turretAngularSpeed = 1f;
    }


    private void Start()
    {
        // The fire axis is based on the player number.
        m_FireButton = "Fire" + (m_localID + 1);

        // The rate that the launch force charges up is the range of possible forces by the max charge time.
        m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        if (TankTurret != null) rootTurretRoration = TankTurret.transform.localRotation;

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
                TankTurret.transform.rotation = Quaternion.Slerp(TankTurret.transform.rotation, targetRoration, Time.deltaTime * Constants.TANK_TURRET_RORATE_SPEED * turretAngularSpeed);
                if (Quaternion.Angle(TankTurret.transform.rotation, targetRoration) < 2)
                    LockedTurret(true);
            }
            else
            {
                if (Quaternion.Angle(TankTurret.transform.localRotation, rootTurretRoration) < 1)
                    LockedTurret(true);
                TankTurret.transform.localRotation = Quaternion.Slerp(TankTurret.transform.localRotation, rootTurretRoration, Time.deltaTime * Constants.TANK_TURRET_RORATE_SPEED * turretAngularSpeed);
            }
        }
    }

    public float CalculateMaxFireDistance()
    {
        float alpha = m_FireTransform.localRotation.eulerAngles.x * Mathf.PI / 180f;
        float maxDistance = Mathf.Abs(2 * m_MaxLaunchForce * m_MaxLaunchForce * Mathf.Cos(alpha) * Mathf.Sin(alpha) / Constants.GRAVITY_ACCELERATION);
        return maxDistance * 0.9f;
    }

    public float CalculateLauchForce(Vector3 target)
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
        if (HasMissile && GameManagerOffline.s_Instance.missileReady)
        {
            GameManagerOffline.s_Instance.missileReady = false;
            FireMissile(target);
        }
        else
            Fire(target.position);

        // Reset the launch force.  This is a precaution in case of missing button events.
        m_CurrentLaunchForce = m_MinLaunchForce;
    }

    public bool ARFireMissile(Transform target)
    {
        m_Fired = true;

        //m_CurrentLaunchForce = CalculateLauchForce(target);
        if (HasMissile && GameManagerOffline.s_Instance.missileReady)
        {
            GameManagerOffline.s_Instance.missileReady = false;
            FireMissile(target);
            return true;
        }
        return false;
    }

    private void FireMissile(Transform target)
    {
        if (transform == null) return;//todo: do not consume this player missile
        else
        {
            HasMissile = false;
            RorateTurret(target);
            StartCoroutine(_FireMissile(target));
        }
    }

    private IEnumerator _FireMissile(Transform target)
    {
        //turning = true;
        lockedTurret = false;
        float timeCount = 0f;
        while (!lockedTurret)
        {
            timeCount += Time.deltaTime;
            if (timeCount > 2) yield break;// safely check to exit coroutine if it run for too long
            yield return null;
        }

        //todo: spawn a missile and play a sound
        GameObject homingMissile = Instantiate(missilePrefab, m_FireTransform.position, m_FireTransform.rotation) as GameObject;
        homingMissile.GetComponent<HomingMissileOffline>().target = target;
        Rigidbody homingMissileRig = homingMissile.GetComponent<Rigidbody>();
        homingMissileRig.velocity = m_MaxLaunchForce * m_FireTransform.forward;//make missile lauch velocity a little bigger than current tank velocity to ignore collider
        homingMissileRig.AddForce((Constants.HOMING_MISSILE_LAUCH_FORCE) * homingMissileRig.velocity.normalized);
        lockedTurret = false;
    }

    public void Fire(Vector3 target)
    {
        RorateTurret(target);
        StartCoroutine(_Fire(target));
    }

    public IEnumerator _Fire(Vector3 target)
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
        shellInstance.GetComponent<ShellExplosionOffline>().m_MaxDamage = this.tankDamage;
        float launchForce = CalculateLauchForce(target);
        // Create a velocity that is the tank's velocity and the launch force in the fire position's forward direction.
        Vector3 velocity = launchForce * m_FireTransform.forward;

        // Set the shell's velocity to this velocity.
        shellInstance.velocity = velocity;

        //turning = true;
        lockedTurret = false;
        PlayShootAudio();
    }

    private void PlayShootAudio()
    {
        // Change the clip to the firing clip and play it.
        if (SoundManager.Instance.Audio)
        {
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();
            //AudioSource.PlayClipAtPoint(m_FireClip, transform.position,1);
        }
    }

    public void RorateTurretToMouseDirection(Vector3 mousePos)
    {
        //turning = true;
        RorateTurret(mousePos);
    }
    private void LockedTurret(bool val)
    {
        lockedTurret = val;
    }

    private void RorateTurret(Vector3 targetTankPos)
    {
        //lockedTurret = false;
        LockedTurret(false);
        this.tankTarget = null;
        this.tankTargetPos = targetTankPos;
        turning = true;
    }

    public void ResetTurretRoration()
    {
        LockedTurret(false);
        turning = false;
    }

    private void RorateTurret(Transform targetTank)
    {
        //lockedTurret = false;
        LockedTurret(false);
        this.tankTarget = targetTank;
        turning = true;
    }
    // This is used by the game manager to reset the tank.
    public void SetDefaults()
    {
        m_CurrentLaunchForce = m_MinLaunchForce;
        m_AimSlider.value = m_MinLaunchForce;
    }

    public void EnableComponent(bool state)
    {
        if(!state) StopAllCoroutines();
        enabled = state;
    }
}

