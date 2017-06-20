using UnityEngine;
using UnityEngine.Networking;
public class ElectroShield : NetworkBehaviour
{
    public ParticleSystem m_PickUpParticles;
    [ServerCallback]
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            TankHealth tankHealth = other.gameObject.GetComponent<TankHealth>();
            tankHealth.ActiveShield();
            NetworkServer.Destroy(gameObject);
        }
    }
    public override void OnNetworkDestroy()
    {
        base.OnNetworkDestroy();
        m_PickUpParticles.transform.SetParent(null);
        m_PickUpParticles.Play();
        transform.GetChild(0).gameObject.SetActive(false);
        SoundManager.Instance.PlayPickItemAudio();
        Destroy(m_PickUpParticles.gameObject, m_PickUpParticles.duration);
    }
}
