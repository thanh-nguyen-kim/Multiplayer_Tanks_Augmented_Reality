using UnityEngine;
public class ShieldLockedRorationToMainCam : MonoBehaviour
{
    Quaternion primeRoration;
    void OnEnable()
    {
        Quaternion parentQuaternion = transform.parent.rotation;
        Quaternion thisLocalQuaternion = transform.localRotation;
        transform.localRotation=Quaternion.Euler(thisLocalQuaternion.x,thisLocalQuaternion.eulerAngles.y - parentQuaternion.eulerAngles.y,thisLocalQuaternion.z);
        primeRoration = transform.localRotation;
    }

    void LateUpdate()
    {
        transform.localRotation = primeRoration;
    }
}

