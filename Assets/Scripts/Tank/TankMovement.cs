using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
public class TankMovement : NetworkBehaviour
{
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
    private float shotTime = 0;
    private bool hasShot;
    private TankShooting tankShooting;
    private GameObject inGameClick, inGameEnemyClick;
    private Vector3 cameraCenter = new Vector3(0.5f, 0.5f, 0);
    private bool m_IsAR = false;
    private IEnumerator currentCoroutine;
    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        tankShooting = GetComponent<TankShooting>();
        touchCount = 0;
    }
    //navmesh agent must be disable in order to move object free by setting its position otherwise it will cause error.

    void OnEnable()
    {
        navAgent = GetComponent<NavMeshAgent>();
        if (isLocalPlayer)
        {
            navAgent.enabled = true;
            navAgent.destination = transform.position;
            GameObject clickHolder = GameObject.Find("ClickIndicator") as GameObject;
            inGameClick = clickHolder.transform.GetChild(0).gameObject;
            inGameEnemyClick = clickHolder.transform.GetChild(1).gameObject;
            inGameClick.SetActive(false);
            inGameEnemyClick.SetActive(false);

            this.m_IsAR = Prototype.NetworkLobby.LobbyManager.s_Singleton.ARMode;
            if (m_IsAR)
            {
                Prototype.NetworkLobby.LobbyManager.s_Singleton.moveBtnDelegate = Move;
                Prototype.NetworkLobby.LobbyManager.s_Singleton.shotBtnDelegate = Fire;
            }
        }
        else
        {
            navAgent.enabled = false;
        }
    }

    void OnDisable()
    {
        if (isLocalPlayer)
        {
            navAgent = GetComponent<NavMeshAgent>();
            navAgent.enabled = false;
            if (inGameClick)
                inGameClick.SetActive(true);
            if (inGameEnemyClick)
                inGameEnemyClick.SetActive(true);
        }
    }

    private void Start()
    {
        // Store the original pitch of the audio source.
        m_OriginalPitch = m_MovementAudio.pitch;
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;
        if (hasShot)
        {
            shotTime += Time.deltaTime;
        }
        if (shotTime > 1)
        {
            hasShot = false;
            shotTime = 0;
        }
        if (!m_IsAR)
            if (Input.GetMouseButtonDown(0))
            {
                if (GameManager.s_Instance.clickOnUI) return;
                ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                //if(GraphicRaycaster.)
                Physics.Raycast(ray, out hit);
                m_MovementInput = Vector3.Distance(transform.position, hit.point);
                if (hit.collider.gameObject.tag == "Player")
                {
                    if (hit.collider.gameObject != gameObject)
                    {
                        touchCount++;
                        navAgent.SetDestination(transform.position);//stop the tank
                        tankShooting.RorateTurretToMouseDirection(hit.point);
                    }
                }
                else
                {
                    //#if UNITY_EDITOR_WIN
                    //                    if (EventSystem.current.IsPointerOverGameObject())
                    //                    {
                    //                        return;//if click on UI dont move the tank
                    //                    }
#if UNITY_ANDROID
                    if (EventSystem.current.IsPointerOverGameObject())
                        return;//if click on UI dont move the tank
                    if (Input.touchSupported)
                        if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                            return;
#else
                    if (EventSystem.current.IsPointerOverGameObject())
                    {
                        return;//if click on UI dont move the tank
                    }
#endif
                    if (currentCoroutine != null) StopCoroutine(currentCoroutine);
                    currentCoroutine = PlayClickAtPosition(hit.point);
                    StartCoroutine(currentCoroutine);
                    navAgent.SetDestination(hit.point);
                    tankShooting.ResetTurretRoration();
                    touchCount = 0;
                }
                if (touchCount > 1)
                {
                    StartCoroutine(PlayClickAtEnemy(hit.collider.transform));
                    tankShooting.RorateTurretToMouseDirection(hit.collider.transform.position);
                    tankShooting.Fire(hit.collider.transform);
                    touchCount = 0;
                }
                EngineAudio();

            }
    }

    public void Move()
    {
        ray = Camera.main.ViewportPointToRay(cameraCenter);
        Physics.Raycast(ray, out hit);
        if (hit.collider)
        {
            if (currentCoroutine != null) StopCoroutine(currentCoroutine);
            currentCoroutine = PlayClickAtPosition(hit.point);
            StartCoroutine(currentCoroutine);
            navAgent.SetDestination(hit.point);
            tankShooting.ResetTurretRoration();
            EngineAudio();
        }
    }

    public void Fire()
    {
        if (hasShot) return;
        else
        {
            hasShot = true;
            ray = Camera.main.ViewportPointToRay(cameraCenter);
            Physics.Raycast(ray, out hit);
            if (hit.collider)
            {
                tankShooting.RorateTurretToMouseDirection(hit.point);
                StartCoroutine(PlayClickAtEnemy(hit.collider.transform));
                if (hit.collider.tag == "Player")
                {
                    if (tankShooting.ARFireMissile(hit.collider.transform))
                    {
                        return;
                    }
                }
                tankShooting.Fire(hit.point);
            }
        }
    }

    private void EngineAudio()
    {
        // If there is no input (the tank is stationary)...
        if (SoundManager.Instance.Audio)
            if (navAgent.velocity.magnitude < 0.1f)
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
        if (target.tag == "Player")
            inGameEnemyClick.transform.localScale = Vector3.one * 4;
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

    //We freeze the rigibody when the control is disabled to avoid the tank drifting!
    protected RigidbodyConstraints m_OriginalConstrains;
    //void OnDisable()
    //{
    //    m_OriginalConstrains = m_Rigidbody.constraints;
    //    m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
    //}

    //void OnEnable()
    //{
    //    m_Rigidbody.constraints = m_OriginalConstrains;
    //}
}