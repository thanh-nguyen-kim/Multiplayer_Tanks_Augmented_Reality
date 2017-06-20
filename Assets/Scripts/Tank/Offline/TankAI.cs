using UnityEngine;
using System.Collections;
public enum AIState
{
    Patrol = 0,
    Attack = 1
}
public class TankAI : MonoBehaviour
{
    public Transform target;
    public int m_DetectRange = 5;
    public int m_ViewFustum = 135;
    private TankShootingOffline tankShooting;
    //[HideInInspector]
    public AIState state = AIState.Patrol;
    public Transform[] m_PatrolPoints;
    private Vector3[] runtimePatrolPoint;
    private int currentPatrolPoint = 0;
    private bool steering = false, lockPosition = false;
    private NavMeshAgent navAgent = null;
    public float pursudeTimeThreshold = 1f, shootTimeThresHold = 1;
    private float maxShellFlyDistance = 0f;

    public AIState State
    {
        get
        {
            return state;
        }

        set
        {
            this.state = value;
        }
    }

    void Start()
    {
        tankShooting = GetComponent<TankShootingOffline>();
        navAgent = GetComponent<NavMeshAgent>();
        runtimePatrolPoint = new Vector3[m_PatrolPoints.Length + 1];
        runtimePatrolPoint[0] = transform.position;//init point
        for (int i = 1; i < m_PatrolPoints.Length + 1; i++)
        {
            runtimePatrolPoint[i] = m_PatrolPoints[i - 1].position;
        }
        GetComponent<TankShootingOffline>().tankDamage = 50;
        steering = false;
        lockPosition = false;
        currentPatrolPoint = 0;
        maxShellFlyDistance = tankShooting.CalculateMaxFireDistance();
        GetComponent<AudioSource>().enabled = SoundManager.Instance.Audio;
        m_DetectRange += (int)GameManagerOffline.s_Instance.m_GameMode * 5;
        StartCoroutine(SignalAttack());
    }

    public void EnableComponent(bool state)
    {
        if (!state) StopAllCoroutines();
        enabled = state;
    }

    void Update()
    {
        Vector3 lookDirection = target.position - transform.position;
        float distanceToPlayer = lookDirection.magnitude;
        if (State == AIState.Patrol)
        {
            if (distanceToPlayer < m_DetectRange)
            {
                //detect if player in view fustum
                float angle = Quaternion.Angle(Quaternion.LookRotation(lookDirection), tankShooting.TankTurret.transform.rotation);
                if (angle < m_ViewFustum / 2)
                {
                    State = AIState.Attack;
                    pursudeTimeThreshold = 0;
                }//detected player
            }
            Patrol();
        }

        if (State == AIState.Attack)
        {
            if (pursudeTimeThreshold > -1f)
                pursudeTimeThreshold -= Time.deltaTime;
            if (distanceToPlayer > maxShellFlyDistance)
            {
                if (pursudeTimeThreshold < 0)
                {
                    pursudeTimeThreshold = 5;
                    Pursude();//target move out of view so we have to recalculate routine to player
                }
            }
            else
            {
                shootTimeThresHold -= Time.deltaTime;
                if (shootTimeThresHold < 0)
                {
                    if (Random.Range(0, 10) > 2)//we have 70 percents for shooting a shell
                    {
                        if (distanceToPlayer < maxShellFlyDistance)
                        {
                            StartCoroutine(ShotShell());
                        }
                    }
                    else
                    {
                        shootTimeThresHold = Random.Range(2, 10);
                        Pursude();//move to other location
                    }
                }
            }
        }
    }

    public IEnumerator SignalAttack()
    {
        yield return new WaitForSeconds(1);
        if (State == AIState.Attack)
        {
            Collider[] comrades;
            comrades = Physics.OverlapSphere(transform.position, m_DetectRange, LayerMask.GetMask("Players"));
            if (comrades.Length > 0)
            {
                for (int i = 0; i < comrades.Length; i++)
                {
                    if (comrades[i].tag == "AI") comrades[i].GetComponent<TankAI>().State = AIState.Attack;
                }
            }
        }
    }



    private IEnumerator ShotShell()
    {
        shootTimeThresHold = 10;
        navAgent.Stop();//we arrived at the shootable postion. so shoot a shell
        //our target still moving so we have to predict its future pos
        float distance = (target.position - transform.position).magnitude;
        float shellFlyTime = distance / tankShooting.CalculateLauchForce(target.position);
        Vector3 futurePos = target.position + target.GetComponent<NavMeshAgent>().velocity * shellFlyTime;
        tankShooting.RorateTurretToMouseDirection(futurePos);
        yield return tankShooting._Fire(futurePos);
        shootTimeThresHold = Random.Range(1, 10);
        Pursude();//after shoot we need to move to a new location
    }

    private void Pursude()
    {
        Vector3 direction = transform.position - target.position;
        float saltX = Random.Range(0.01f, 500f) * direction.x;
        float saltZ = Random.Range(0.01f, 500f) * direction.z;
        Vector3 saltPos = (new Vector3(saltX, 0, saltZ)).normalized * tankShooting.CalculateMaxFireDistance();
        Vector3 moveToPos = target.position + saltPos * Random.Range(0.5f, 1f);
        navAgent.Resume();
        tankShooting.ResetTurretRoration();
        shootTimeThresHold = (moveToPos - transform.position).magnitude / navAgent.speed + 0.5f;
        navAgent.SetDestination(moveToPos);
    }


    private void Patrol()
    {
        //Debug.Log(Vector3.Distance(transform.position, runtimePatrolPoint[currentPatrolPoint]));
        if (Vector3.Distance(transform.position, runtimePatrolPoint[currentPatrolPoint]) < 0.5f)
        {
            navAgent.SetDestination(transform.position);
            if (!steering)
            {
                if (Random.Range(0, 10) > 6 && !lockPosition) StartCoroutine(_Steering());
                else
                {
                    int temp = currentPatrolPoint;
                    do
                    {
                        temp = Random.Range(0, m_PatrolPoints.Length);
                    }
                    while (temp == currentPatrolPoint);
                    currentPatrolPoint = temp;
                    tankShooting.ResetTurretRoration();
                    navAgent.Resume();
                    navAgent.SetDestination(runtimePatrolPoint[currentPatrolPoint]);
                    navAgent.speed = Constants.AI_PATROL_SPEED;
                    lockPosition = false;
                }
            }
        }
    }

    private IEnumerator _Steering()
    {
        steering = true;
        do
        {
            int x = Random.Range(0, 10);
            int z = Random.Range(0, 10);
            Vector3 steerTarget = new Vector3(x, 0, z);
            tankShooting.RorateTurretToMouseDirection(transform.position + steerTarget);
            yield return new WaitForSeconds(Random.Range(0.5f, 3f));
        }
        while (Random.Range(0, 10) > 8);
        steering = false;
        lockPosition = true;
    }

    //for debug purpose
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, m_DetectRange);
    }

    public bool IsDead()
    {
        return false;
    }
}

