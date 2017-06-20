using UnityEngine;
using System.Collections;

class HomingMissileOffline : MonoBehaviour
{
    public Transform target;
    private float lifeTime = 0f;
    private int missileVel = 10;
    private int rorationDelta = 10;
    //private float fuseDelay = 0.1f;
    private Rigidbody rig;
    private Quaternion targetRorate;
    public ParticleSystem m_ExplosionParticles;
    public AudioSource m_ExplosionAudio, m_LaunchAudio;
    private bool destroyed = false;
    void Start()
    {
        //todo: play sound when lauching
        rig = GetComponent<Rigidbody>();
        if (SoundManager.Instance.Audio)
            m_LaunchAudio.Play();
    }

    void FixedUpdate()
    {
        if (target == null || destroyed) return;
        rig.velocity = transform.forward * rig.velocity.magnitude;
        targetRorate = Quaternion.LookRotation(target.position - transform.position);
        rig.MoveRotation(Quaternion.Slerp(transform.rotation, targetRorate, rorationDelta));
        if (Quaternion.Angle(transform.rotation, targetRorate) < 1)
            rig.velocity += rig.velocity.normalized * missileVel * Time.fixedDeltaTime;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (destroyed) return;
        if (other.gameObject.tag == "AI")
        {
            destroyed = true;
            TankHealthOffline tankHealth = other.gameObject.GetComponent<TankHealthOffline>();
            tankHealth.Damage(Constants.HOMING_MISSILE_DAMAGE);
        }
        if (other.gameObject.tag == "Building")
        {
            destroyed = true;
            Building building = other.gameObject.GetComponent<Building>();
            building.Damage(Constants.HOMING_MISSILE_DAMAGE);
        }
        if (destroyed)
        {
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(false);
            m_ExplosionParticles.Play();
            if (SoundManager.Instance.Audio)
                m_ExplosionAudio.Play();
            Destroy(gameObject, m_ExplosionParticles.duration);
        }
    }

}
