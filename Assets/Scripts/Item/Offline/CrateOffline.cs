using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class CrateOffline:MonoBehaviour
    {
    public GameObject[] m_ItemPrefabs;
    public bool[] itemToSpawn = new bool[3];//indicate which item can spawn after buy at shop

    void Update()
    {
        transform.Rotate(Vector3.up * Time.deltaTime * Constants.PARACHUTE_RORATE_SPEED);

    }

    void OnDestroy()
    {
        GameManagerOffline.s_Instance.m_SpawnedObject.Remove(gameObject);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Terrain")
        {
            int count = 0;
            for (int i = 0; i < itemToSpawn.Length; i++)
            {
                if (!itemToSpawn[i]) continue;
                GameObject item = Instantiate(m_ItemPrefabs[i], transform.position+new Vector3(count,0,count), Quaternion.identity) as GameObject;
                count++;
                GameManagerOffline.s_Instance.m_SpawnedObject.Add(item);
                Destroy(gameObject);
            }
            SoundManager.Instance.PlayDropItem();
        }
    }
}

