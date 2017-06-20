using System;
using UnityEngine;

[Serializable]
public class TankManager
{
    // This class is to manage various settings on a tank.
    // It works with the GameManager class to control how the tanks behave
    // and whether or not players have control of their tank in the 
    // different phases of the game.

    public Color playerColor;               // This is the color this tank will be tinted.
    public Transform spawnPoint;            // The position and direction the tank will have when it spawns.
    [HideInInspector]
    public int playerNumber;                // This specifies which player this the manager for.
    [HideInInspector]
    public GameObject instance;             // A reference to the instance of the tank when it is created.
    [HideInInspector]
    public GameObject tankRenderers;        // The transform that is a parent of all the tank's renderers.  This is deactivated when the tank is dead.
    [HideInInspector]
    public int wins;                        // The number of wins this player has so far.
    [HideInInspector]
    public string playerName;                    // The player name set in the lobby
    [HideInInspector]
    public int localPlayerID;                    // The player localID (if there is more than 1 player on the same machine)
    public int tankType;
    public TankMovement m_Movement;        // References to various objects for control during the different game phases.
    public TankShooting m_Shooting;
    public TankHealth m_Health;
    public TankSetup m_Setup;


    public void Setup()
    {
        // Get references to the components.
        m_Movement = instance.GetComponent<TankMovement>();
        m_Shooting = instance.GetComponent<TankShooting>();
        m_Health = instance.GetComponent<TankHealth>();
        m_Setup = instance.GetComponent<TankSetup>();
        // Get references to the child objects.
        //tankRenderers = m_Health.m_TankRenderers;

        //Set a reference to that amanger in the health script, to disable control when dying
        m_Health.m_Manager = this;

        // Set the player numbers to be consistent across the scripts.
        m_Movement.m_PlayerNumber = playerNumber;
        m_Movement.m_LocalID = localPlayerID;

        m_Shooting.m_PlayerNumber = playerNumber;
        m_Shooting.m_localID = localPlayerID;

        //setup is use for diverse Network Related sync
        m_Setup.m_Color = playerColor;
        m_Setup.m_PlayerName = playerName;
        m_Setup.m_PlayerNumber = playerNumber;
        m_Setup.m_LocalID = localPlayerID;
        m_Setup.tankType = tankType;
    }


    // Used during the phases of the game where the player shouldn't be able to control their tank.
    public void DisableControl()
    {
        m_Movement.enabled = false;
        m_Shooting.enabled = false;
        m_Movement.m_MovementAudio.enabled = false;
    }


    // Used during the phases of the game where the player should be able to control their tank.
    public void EnableControl()
    {
        m_Movement.enabled = true;
        m_Shooting.enabled = true;
        m_Movement.m_MovementAudio.enabled = SoundManager.Instance.Audio;
        m_Movement.ReEnableParticles();
    }

    public string GetName()
    {
        return m_Setup.m_PlayerName;
    }

    public void SetLeader(bool leader)
    {
        m_Setup.SetLeader(leader);
    }

    public bool IsReady()
    {
        return m_Setup.m_IsReady;
    }

    // Used at the start of each round to put the tank into it's default state.
    public void Reset()
    {
        m_Movement.SetDefaults();
        m_Shooting.SetDefaults();
        m_Health.SetDefaults();
        if (m_Setup.hasAuthority)
        {
            m_Setup.CmdUnReady();
        }
        if (m_Movement.hasAuthority)
        {
            m_Movement.transform.position = this.spawnPoint.position;
            m_Movement.transform.rotation = this.spawnPoint.rotation;
        }
    }

    public bool CheckRendererActive()
    {
        if (tankRenderers == null)
            tankRenderers = m_Health.m_TankRenderers;
        if (tankRenderers == null) return false;
        return tankRenderers.activeSelf;
    }

    public bool CheckTankAlive()
    {
        return m_Health == null || !m_Health.m_ZeroHealthHappened;
    }
}
