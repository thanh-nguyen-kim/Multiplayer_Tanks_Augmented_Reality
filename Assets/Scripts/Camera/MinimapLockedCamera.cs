using UnityEngine;
class MinimapLockedCamera : MonoBehaviour
{
    private Quaternion fixedQuaternion;
    void Start()
    {
        transform.rotation = transform.localRotation;
        fixedQuaternion = transform.rotation;
    }
    void Update()
    {
        transform.rotation = fixedQuaternion;
    }
}

