using UnityEngine;
public class BoltOffline : MonoBehaviour
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
            TankMovementOffline tankMov = other.gameObject.GetComponent<TankMovementOffline>();
            tankMov.Boost();
            SoundManager.Instance.PlayPickItemAudio();
            transform.GetChild(0).gameObject.SetActive(false);
            m_PickUpParticle.Play();
            Destroy(gameObject,m_PickUpParticle.duration);
        }
    }
}

