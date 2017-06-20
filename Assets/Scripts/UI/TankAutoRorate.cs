using UnityEngine;
public class TankAutoRorate : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(Vector3.up * Time.deltaTime * Constants.TANK_RORATE_LOBBY_SPEED);
    }
}
