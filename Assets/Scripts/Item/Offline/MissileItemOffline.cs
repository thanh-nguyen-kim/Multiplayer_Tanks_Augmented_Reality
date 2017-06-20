using UnityEngine;
public class MissileItemOffline: MonoBehaviour
{
    public ParticleSystem m_PickUpParticle;
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            TankShootingOffline tankShooting = other.gameObject.GetComponent<TankShootingOffline>();
            tankShooting.HasMissile = true;
            SoundManager.Instance.PlayPickItemAudio();
            transform.GetChild(0).gameObject.SetActive(false);
            m_PickUpParticle.Play();
            Destroy(gameObject,m_PickUpParticle.duration);
        }
    }

    void OnDestroy()
    {
        GameManagerOffline.s_Instance.m_SpawnedObject.Remove(gameObject);
    }

}
