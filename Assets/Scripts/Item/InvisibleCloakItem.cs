using UnityEngine;
using UnityEngine.Networking;

public class InvisibleCloakItem : NetworkBehaviour
{
    private int invisibleTime = 5;
    public ParticleSystem m_InvisibleParticle;
    public ParticleSystem m_PickUpParticles;
    [ServerCallback]
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            //disable this player mesh on all client except the player who collider for 5 seconds, destroy this gameobject
            other.gameObject.GetComponent<TankSetup>().Invisible();
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
