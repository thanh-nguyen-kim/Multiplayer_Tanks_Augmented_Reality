using UnityEngine;
public class HealthItemOffline : MonoBehaviour
{
    public ParticleSystem m_PickUpParticle;
    void OnDestroy()
    {
        GameManagerOffline.s_Instance.m_SpawnedObject.Remove(gameObject);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            TankHealthOffline tankHealth = other.gameObject.GetComponent<TankHealthOffline>();
            tankHealth.Heal(Constants.HEALTH_RESTORE_AMMOUNT);
            SoundManager.Instance.PlayPickItemAudio();
            m_PickUpParticle.Play();
            transform.GetChild(0).gameObject.SetActive(false);
            Destroy(gameObject,m_PickUpParticle.duration);
        }
    }
}
