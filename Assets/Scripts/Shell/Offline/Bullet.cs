
using UnityEngine;
public class Bullet : MonoBehaviour
{
    [HideInInspector]
    public Vector3 m_Target;
    public int m_Speed;
    public int m_Damage = 0;
    public int m_ExplosionForce = 5;
    public int m_ExplosionRadius = 3;
    private float speed = 0;
    public ParticleSystem m_ExplodeParticle;
    public AudioSource m_ExplodeAudioSource;
    public bool destroyed = false;
    void Start()
    {
        speed = m_Speed;
    }
    void Update()
    {
        if (speed < m_Speed * 3)
        {
            speed += Time.deltaTime;
        }
        if (!destroyed)
        {
            transform.Translate((m_Target - transform.position).normalized * Time.deltaTime * speed);
            if (Vector3.Distance(transform.position, m_Target) < 0.5f)
            {
                destroyed = true;
                Explode();
            }
        }
    }

    private void Explode()
    {
        // Collect all the colliders in a sphere from the shell's current position to a radius of the explosion radius.
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, LayerMask.GetMask("Players"));

        // Go through all the colliders...
        for (int i = 0; i < colliders.Length; i++)
        {
            // ... and find their rigidbody.
            Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();

            // If they don't have a rigidbody, go on to the next collider.
            if (!targetRigidbody)
                continue;

            // Find the TankHealth script associated with the rigidbody.
            TankHealthOffline targetHealth = targetRigidbody.GetComponent<TankHealthOffline>();

            // If there is no TankHealth script attached to the gameobject, go on to the next collider.
            if (!targetHealth)
                continue;

            // Create a vector from the shell to the target.
            Vector3 explosionToTarget = targetRigidbody.position - transform.position;

            // Calculate the distance from the shell to the target.
            float explosionDistance = explosionToTarget.magnitude;

            // Calculate the proportion of the maximum distance (the explosionRadius) the target is away.
            float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;

            // Calculate damage as this proportion of the maximum possible damage.
            float damage = relativeDistance * m_Damage;

            // Make sure that the minimum damage is always 0.
            damage = Mathf.Max(0f, damage);

            // Deal this damage to the tank.
            targetHealth.Damage(damage);
        }
        GetComponent<MeshRenderer>().enabled = false;
        m_ExplodeParticle.Play();
        if (SoundManager.Instance.Audio)
            m_ExplodeAudioSource.Play();
        Destroy(gameObject, m_ExplodeParticle.duration);
    }

    void PhysicForces()
    {
        // Collect all the colliders in a sphere from the shell's current position to a radius of the explosion radius.
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, LayerMask.GetMask("Players"));

        // Go through all the colliders...
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].gameObject.GetComponent<TankSetupOffline>().SimulateExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);
        }
    }
}

