using UnityEngine;
using UnityEngine.Networking;
public class HealthItem : NetworkBehaviour
{
    public ParticleSystem m_PickUpParticles;
    [ServerCallback]
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            //increase health on collider
            TankHealth tankHealth = other.gameObject.GetComponent<TankHealth>();
            tankHealth.Heal(Constants.HEALTH_RESTORE_AMMOUNT);
            NetworkServer.Destroy(gameObject);
        }
    }
    public override void OnNetworkDestroy()
    {
        base.OnNetworkDestroy();
        m_PickUpParticles.transform.SetParent(null);
        m_PickUpParticles.Play();
        SoundManager.Instance.PlayPickItemAudio();
        transform.GetChild(0).gameObject.SetActive(false);
        Destroy(m_PickUpParticles.gameObject, m_PickUpParticles.duration);
    }
}
