using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
public class TankHealth : NetworkBehaviour
{
    public float m_StartingHealth = 100f;             // The amount of health each tank starts with.
    public Slider m_Slider;                           // The slider to represent how much health the tank currently has.
    public Image m_FillImage;                         // The image component of the slider.
    public Color m_FullHealthColor = Color.green;     // The color the health bar will be when on full health.
    public Color m_ZeroHealthColor = Color.red;       // The color the health bar will be when on no health.
    public AudioClip m_TankExplosion;                 // The clip to play when the tank explodes.
    public ParticleSystem m_ExplosionParticles;       // The particle system the will play when the tank is destroyed.
    [HideInInspector]
    public GameObject m_TankRenderers = null;                // References to all the gameobjects that need to be disabled when the tank is dead.
    public GameObject m_HealthCanvas;
    public GameObject m_AimCanvas;
    public GameObject m_LeftDustTrail;
    public GameObject m_RightDustTrail;
    public TankSetup m_Setup;
    [HideInInspector]
    public TankManager m_Manager;                   //Associated manager, to disable control when dying.

    [SyncVar(hook = "OnCurrentHealthChanged")]
    private float m_CurrentHealth;                  // How much health the tank currently has.*
    [HideInInspector]
    [SyncVar]
    public bool m_ZeroHealthHappened;              // Has the tank been reduced beyond zero health yet?

    [SyncVar(hook = "OnShieldStateChange")] //has the shield turn on, sync it between player.
    private bool m_Shielded = false;
    private BoxCollider m_Collider;                 // Used so that the tank doesn't collide with anything when it's dead.
    public GameObject m_Shield;

    private void Awake()
    {
        m_Collider = GetComponent<BoxCollider>();
        //SetHealthUI();
    }

    private void OnDestroy()
    {
        GameManager.m_Tanks.Remove(m_Manager);//remove this tank manager from game manager to prevent looping
    }

    private void OnApplicationQuit()
    {
        GameManager.m_Tanks.Remove(m_Manager);//remove this tank manager from game manager to prevent looping
    }
    //this is called when a tank collider with an health restore object
    public void Heal(int amount)
    {
        m_CurrentHealth += amount;
        if (m_CurrentHealth > m_StartingHealth) m_CurrentHealth = m_StartingHealth;
    }

    // This is called whenever the tank takes damage.
    public void Damage(float amount)
    {
        // Reduce current health by the amount of damage done.
        float damageAmount = amount;
        if (m_Shielded)
        {
            //todo: display tank has dodge the attack.
            int decision = Random.Range(0, 100);
            if (decision > 50) return;//all damage have been negate
            else
                damageAmount = damageAmount * 0.5f;

        }
        m_CurrentHealth -= damageAmount;

        // If the current health is at or below zero and it has not yet been registered, call OnZeroHealth.
        if (m_CurrentHealth <= 0f && !m_ZeroHealthHappened)
        {
            OnZeroHealth();
        }
    }


    private void SetHealthUI()
    {
        // Set the slider's value appropriately.
        m_Slider.value = m_CurrentHealth;

        // Interpolate the color of the bar between the choosen colours based on the current percentage of the starting health.
        m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
    }


    void OnCurrentHealthChanged(float value)
    {
        m_CurrentHealth = value;
        // Change the UI elements appropriately.
        SetHealthUI();

    }

    private void OnZeroHealth()
    {
        // Set the flag so that this function is only called once.
        m_ZeroHealthHappened = true;

        RpcOnZeroHealth();
    }

    private void InternalOnZeroHealth()
    {
        // Disable the collider and all the appropriate child gameobjects so the tank doesn't interact or show up when it's dead.
        SetTankActive(false);
    }

    [ClientRpc]
    private void RpcOnZeroHealth()
    {
        // Play the particle system of the tank exploding.
        m_ExplosionParticles.Play();

        // Create a gameobject that will play the tank explosion sound effect and then destroy itself.
        if (SoundManager.Instance.Audio)
            AudioSource.PlayClipAtPoint(m_TankExplosion, transform.position);

        InternalOnZeroHealth();
    }

    private void SetTankActive(bool active)
    {
        m_Collider.enabled = active;
        if (m_TankRenderers != null)
            m_TankRenderers.SetActive(active);
        m_HealthCanvas.SetActive(active);
        m_AimCanvas.SetActive(active);
        m_LeftDustTrail.SetActive(active);
        m_RightDustTrail.SetActive(active);

        if (active) m_Manager.EnableControl();
        else m_Manager.DisableControl();

        m_Setup.ActivateCrown(active);
    }

    public void MakeTankInvisible(bool active)
    {
        m_Collider.enabled = active;
        if (m_TankRenderers != null)
            m_TankRenderers.SetActive(active);
        m_HealthCanvas.SetActive(active);
        m_AimCanvas.SetActive(active);
        m_LeftDustTrail.SetActive(active);
        m_RightDustTrail.SetActive(active);
        m_Shield.SetActive(m_Shielded && active);
        m_Setup.ActivateCrown(active);
    }

    // This function is called at the start of each round to make sure each tank is set up correctly.
    public void SetDefaults()
    {
        m_CurrentHealth = m_StartingHealth;
        m_ZeroHealthHappened = false;
        SetHealthUI();
        SetTankActive(true);

    }

    public void ActiveShield()
    {
        m_Shielded = true;
        //to do: enable shield icon above tank and dis activate after 15 seconds.
        StartCoroutine(DelayTurnOffShield());

    }

    private IEnumerator DelayTurnOffShield()
    {
        yield return new WaitForSeconds(Constants.SHIELD_LIFE_TIME);
        m_Shielded = false;
    }

    protected void OnShieldStateChange(bool state)
    {
        m_Shield.SetActive(state);
    }
}
