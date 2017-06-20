using UnityEngine;
    public class InvisibleCloakItemOffline:MonoBehaviour
    {
    private int invisibleTime = 5;

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            //disable this player mesh on all client except the player who collider for 5 seconds, destroy this gameobject
            other.gameObject.GetComponent<TankSetupOffline>().Invisible();
            Destroy(gameObject);
        }
    }
}

