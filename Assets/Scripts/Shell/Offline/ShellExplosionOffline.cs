using UnityEngine;
using System.Collections;
public class ShellExplosionOffline : MonoBehaviour
{
    public ParticleSystem m_ExplosionParticles;         // Reference to the particles that will play on explosion.
    public AudioSource m_ExplosionAudio;                // Reference to the audio that will play on explosion.
    public float m_MaxDamage = 100f;                    // The amount of damage done if the explosion is centred on a tank.
    public float m_ExplosionForce = 5f;              // The amount of force added to a tank at the centre of the explosion.
    public float m_MaxLifeTime = 2f;                    // The time in seconds before the shell is removed.
    public float m_ExplosionRadius = 5f;                // The maximum distance away from the explosion tanks can be and are still affected.
    private Rigidbody shellRig = null;

    private int m_TankMask;                             // A layer mask so that only the tanks are affected by the explosion.

    private void Start()
    {

        // If it isn't destroyed by then, destroy the shell after it's lifetime.
        Destroy(gameObject, m_MaxLifeTime);
        GetComponent<Collider>().enabled = false;
        StartCoroutine(EnableCollision());
        shellRig = GetComponent<Rigidbody>();

        // Set the value of the layer mask based solely on the Players layer.
        m_TankMask = LayerMask.GetMask("Players");
    }

    void Update()
    {
        shellRig.rotation = Quaternion.LookRotation(shellRig.velocity);
    }

    //allow to delay a bit the activation of the collider so that it don't collide when spawn close to the canon
    IEnumerator EnableCollision()
    {
        yield return new WaitForSeconds(0.1f);
        GetComponent<Collider>().enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Collect all the colliders in a sphere from the shell's current position to a radius of the explosion radius.
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);

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

            // If there is no Building script attached to the gameobject, go on to the next collider.
            Building building = targetRigidbody.GetComponent<Building>();
            if (!targetHealth && !building)
                continue;

            // Create a vector from the shell to the target.
            Vector3 explosionToTarget = targetRigidbody.position - transform.position;

            // Calculate the distance from the shell to the target.
            float explosionDistance = explosionToTarget.magnitude;

            // Calculate the proportion of the maximum distance (the explosionRadius) the target is away.
            float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;

            // Calculate damage as this proportion of the maximum possible damage.
            float damage = relativeDistance * m_MaxDamage;

            damage = Mathf.Max(0f, damage);
            if (targetHealth)
            {
                // Deal this damage to the tank.
                targetHealth.Damage(damage);
            }
            else
            if (building)
                building.Damage(damage);//deal damage to building
        }

        DestroyShell();
    }

    public void DestroyShell()
    {

        //we spawn the explosion particle
        ExplodeShell();
        GetComponent<MeshRenderer>().enabled = false;
        //set the particle to be destroyed at the end of their lifetime
        Destroy(m_ExplosionParticles.gameObject, m_ExplosionParticles.duration);
    }

    void ExplodeShell()
    {
        // Unparent the particles from the shell.
        m_ExplosionParticles.transform.parent = null;

        // Play the particle system.
        m_ExplosionParticles.Play();

        // Play the explosion sound effect.
        if (SoundManager.Instance.Audio)
            m_ExplosionAudio.Play();

    }
}

