
using UnityEngine;
public class CameraFollowOffline : MonoBehaviour
{
    public Transform m_Character;
    public float m_DampTime;
    private Vector3 m_Offset=Vector3.zero;
    private Vector3 targetPos;
    private Vector3 currentVel;
    void Start()
    {
        m_Offset = transform.position - m_Character.position;
    }
    void Update()
    {
        targetPos = m_Character.position+m_Offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVel, m_DampTime);
    }
}

