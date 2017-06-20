using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
public class HomingMissile : NetworkBehaviour
{
    public Transform target;
    public Transform owner;
    private float lifeTime = 0f;
    private int damage = 0;
    private int missileVel = 10;
    private int rorationDelta = 10;
    private float fuseDelay = 0.1f;
    private Rigidbody rig;
    private Quaternion targetRorate;
    public ParticleSystem m_ExplosionParticles;
    public AudioSource m_ExplosionAudio, m_LaunchAudio;

    void Start()
    {
        //todo: play sound when lauching
        rig = GetComponent<Rigidbody>();
        if (isLocalPlayer)
            if (SoundManager.Instance.Audio)
                m_LaunchAudio.Play();
    }

    [ServerCallback]
    void FixedUpdate()
    {
        //todo: change the way our missile fly. smoothly change the vel and roration of missile
        if (target == null) return;
        rig.velocity = transform.forward * rig.velocity.magnitude;
        targetRorate = Quaternion.LookRotation(target.position - transform.position);
        rig.MoveRotation(Quaternion.Slerp(transform.rotation, targetRorate, rorationDelta));
        if (Quaternion.Angle(transform.rotation, targetRorate) < 1)
            rig.velocity += rig.velocity.normalized * missileVel * Time.fixedDeltaTime;
    }

    [ServerCallback]
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            if (other.transform == owner) return;
            TankHealth tankHealth = other.gameObject.GetComponent<TankHealth>();
            tankHealth.Damage(Constants.HOMING_MISSILE_DAMAGE);
            NetworkServer.Destroy(gameObject);
        }
    }

    public override void OnNetworkDestroy()
    {
        base.OnNetworkDestroy();
        transform.GetChild(0).gameObject.SetActive(false);
        transform.GetChild(1).gameObject.SetActive(false);
        m_ExplosionParticles.transform.SetParent(null);
        m_ExplosionParticles.Play();
        if (SoundManager.Instance.Audio)
            m_ExplosionAudio.Play();
        Destroy(m_ExplosionParticles.gameObject, m_ExplosionParticles.duration);
    }

}

