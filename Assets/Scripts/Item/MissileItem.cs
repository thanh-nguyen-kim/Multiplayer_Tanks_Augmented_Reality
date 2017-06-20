using UnityEngine;
using UnityEngine.Networking;
public class MissileItem : NetworkBehaviour
{
    public ParticleSystem m_PickUpParticles;
    [ServerCallback]
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            TankShooting tankShooting = other.gameObject.GetComponent<TankShooting>();
            tankShooting.hasMissile = true;
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
