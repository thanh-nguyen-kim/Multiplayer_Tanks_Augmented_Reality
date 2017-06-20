using UnityEngine;

    public class ElectroShieldOffline:MonoBehaviour
    {
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            //todo: enable a shield to protect tank, play a sound, then destroy it
            TankHealthOffline tankHealth = other.gameObject.GetComponent<TankHealthOffline>();
            tankHealth.ActiveShield();
            Destroy(gameObject);
        }
    }
}

