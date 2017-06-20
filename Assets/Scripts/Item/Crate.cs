using UnityEngine;
using UnityEngine.Networking;
//this class handle spawn special item like heart,shield..
public class Crate : NetworkBehaviour
{
    public GameObject[] items;
    void Start()
    {

    }

    void Update()
    {
        transform.Rotate(Vector3.up * Time.deltaTime * Constants.PARACHUTE_RORATE_SPEED);

    }

    [ServerCallback]
    public void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Terrain")
        {
            GameObject item = Instantiate(items[Random.Range(0, items.Length)], transform.position, Quaternion.identity) as GameObject;
            NetworkServer.Spawn(item);
            NetworkServer.Destroy(gameObject);//destroy when it collision with earth then spawn a random item
        }
    }

    public override void OnNetworkDestroy()
    {
        base.OnNetworkDestroy();
        SoundManager.Instance.PlayDropItem();
    }
}
