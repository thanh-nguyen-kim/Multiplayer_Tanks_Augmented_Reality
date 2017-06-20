using UnityEngine;
using System.Collections;
public class TankARMovement : MonoBehaviour
{
    private Vector3 cameraCenter = new Vector3(0.5f, 0.5f, 0);

    public int m_PlayerNumber = 1;                // Used to identify which tank belongs to which player.  This is set by this tank's manager.
    public int m_LocalID = 1;
    public float m_Speed = 12f;                   // How fast the tank moves forward and back.
    public float m_TurnSpeed = 180f;              // How fast the tank turns in degrees per second.
    public float m_PitchRange = 0.2f;             // The amount by which the pitch of the engine noises can vary.
    public AudioSource m_MovementAudio;           // Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
    public AudioClip m_EngineIdling;              // Audio to play when the tank isn't moving.
    public AudioClip m_EngineDriving;             // Audio to play when the tank is moving.
    public ParticleSystem m_LeftDustTrail;        // The particle system of dust that is kicked up from the left track.
    public ParticleSystem m_RightDustTrail;       // The particle system of dust that is kicked up from the rightt track.
    public Rigidbody m_Rigidbody;              // Reference used to move the tank.

    private string m_MovementAxis;              // The name of the input axis for moving forward and back.
    private string m_TurnAxis;                  // The name of the input axis for turning.
    private float m_MovementInput;              // The current value of the movement input.
    private float m_TurnInput;                  // The current value of the turn input.
    private float m_OriginalPitch;              // The pitch of the audio source at the start of the scene.

    private Ray ray;
    private RaycastHit hit;
    private NavMeshAgent navAgent;
    private int touchCount = 0;
    private TankShootingOffline tankShooting;
    public GameObject inGameClick, inGameEnemyClick;

    public bool m_IsAR = false;
    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        tankShooting = GetComponent<TankShootingOffline>();
        touchCount = 0;
    }
    //navmesh agent must be disable in order to move object free by setting its position otherwise it will cause error.

    void OnEnable()
    {
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.enabled = true;
        navAgent.destination = transform.position;
    }

    void OnDisable()
    {

        navAgent = GetComponent<NavMeshAgent>();
        navAgent.enabled = false;

    }
    private void Start()
    {
        // Store the original pitch of the audio source.
        m_OriginalPitch = m_MovementAudio.pitch;
    }

    private void Update()
    {
    }


    private void EngineAudio()
    {
        // If there is no input (the tank is stationary)...
        if (Mathf.Abs(m_MovementInput) < 0.1f && Mathf.Abs(m_TurnInput) < 0.1f)
        {
            // ... and if the audio source is currently playing the driving clip...
            if (m_MovementAudio.clip == m_EngineDriving)
            {
                // ... change the clip to idling and play it.
                m_MovementAudio.clip = m_EngineIdling;
                m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }
        else
        {
            // Otherwise if the tank is moving and the idling clip is currently playing...
            if (m_MovementAudio.clip == m_EngineIdling)
            {
                // ... change the clip to driving and playing.
                m_MovementAudio.clip = m_EngineDriving;
                m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }
    }


    private void FixedUpdate()
    {
    }


    public void Move()
    {
        ray = Camera.main.ViewportPointToRay(cameraCenter);
        Physics.Raycast(ray, out hit);
        if (hit.collider)
        {
            StartCoroutine(PlayClickAtPosition(hit.point));
            navAgent.SetDestination(hit.point);
            tankShooting.ResetTurretRoration();
            EngineAudio();
        }
    }

    public void Fire()
    {
        ray = Camera.main.ViewportPointToRay(cameraCenter);
        Physics.Raycast(ray, out hit);
        if (hit.collider)
        {
            if (hit.collider.tag == "AI")
                StartCoroutine(PlayClickAtEnemy(hit.collider.transform));
            tankShooting.Fire(hit.point);
        }
    }

    private void Turn()
    {
        // Determine the number of degrees to be turned based on the input, speed and time between frames.
        float turn = m_TurnInput * m_TurnSpeed * Time.deltaTime;

        // Make this into a rotation in the y axis.
        Quaternion inputRotation = Quaternion.Euler(0f, turn, 0f);

        // Apply this rotation to the rigidbody's rotation.
        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * inputRotation);
    }


    // This function is called at the start of each round to make sure each tank is set up correctly.
    public void SetDefaults()
    {
        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;

        m_MovementInput = 0f;
        m_TurnInput = 0f;

        m_LeftDustTrail.Clear();
        m_LeftDustTrail.Stop();

        m_RightDustTrail.Clear();
        m_RightDustTrail.Stop();
    }

    public void ReEnableParticles()
    {
        m_LeftDustTrail.Play();
        m_RightDustTrail.Play();
    }

    private IEnumerator PlayClickAtPosition(Vector3 pos)
    {
        inGameClick.SetActive(false);
        inGameClick.transform.position = pos + Vector3.up * 0.1f;
        inGameClick.gameObject.SetActive(true);
        Animator a_Click = inGameClick.GetComponent<Animator>();
        a_Click.Play("InGamePointerClick");
        yield return new WaitForSeconds(a_Click.GetCurrentAnimatorStateInfo(0).length);
        inGameClick.SetActive(false);
    }

    private IEnumerator PlayClickAtEnemy(Transform target)
    {
        inGameEnemyClick.SetActive(false);
        inGameEnemyClick.transform.position = target.position + Vector3.up;
        if (target.tag == "AI")
            inGameEnemyClick.transform.localScale = Vector3.one * 4;
        else
            if (target.tag == "Building")
            inGameEnemyClick.transform.localScale = Vector3.one * 8;
        inGameEnemyClick.gameObject.SetActive(true);
        Animator a_Click = inGameEnemyClick.GetComponent<Animator>();
        a_Click.Play("InGameEnemyClick");

        float timeCount = 0f;
        float timeThresHold = 2;
        while (timeCount < timeThresHold)
        {
            timeCount += Time.deltaTime;
            inGameEnemyClick.transform.position = target.position + Vector3.up * 0.1f;
            if (!transform.gameObject.activeInHierarchy) break;
            yield return null;
        }
        inGameEnemyClick.SetActive(false);
    }

    public void Boost()
    {
        navAgent.acceleration *= 2;
        navAgent.speed *= 2;
        navAgent.angularSpeed *= 2;
        GetComponent<TankShootingOffline>().turretAngularSpeed = 2;
        StartCoroutine(UnBoost());
    }

    private IEnumerator UnBoost()
    {
        yield return new WaitForSeconds(Constants.BOOST_TIME);
        navAgent.acceleration /= 2;
        navAgent.speed /= 2;
        navAgent.angularSpeed /= 2;
        GetComponent<TankShootingOffline>().turretAngularSpeed = 1;
    }
}

