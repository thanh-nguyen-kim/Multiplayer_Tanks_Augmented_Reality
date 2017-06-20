using UnityEngine;
public class PlaneBladeRorate : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(Vector3.right * Time.deltaTime * 200);
    }
}
