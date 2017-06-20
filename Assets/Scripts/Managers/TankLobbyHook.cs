using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using Prototype.NetworkLobby;
public class TankLobbyHook : LobbyHook
{
    public override void OnLobbyServerSceneLoadedForPlayer(NetworkManager manager, GameObject lobbyPlayer, GameObject gamePlayer)
    {
        if (lobbyPlayer == null)
            return;

        Prototype.NetworkLobby.LobbyPlayer lp = lobbyPlayer.GetComponent<Prototype.NetworkLobby.LobbyPlayer>();

        if (lp != null)
        {
            GameManager.AddTank(gamePlayer, lp.slot, lp.playerColor, lp.nameInput.text, lp.playerControllerId,lp.tankType);
        }
    }

    public override void OnLobbyClientSceneChanged(int arenaIndex)
    {
        GameManager.s_Instance.arenaIndex = LobbyManager.s_Singleton.arenaIndex;
        GameManager.s_Instance.BeginGame();
    }
}
