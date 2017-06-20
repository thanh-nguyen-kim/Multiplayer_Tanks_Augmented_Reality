using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class Building : MonoBehaviour
{
    public float m_Range = 10;
    public int m_StartingHealth = 100;
    private float m_CurrentHealth = 0;
    public Transform m_Character, m_FireTransform;
    public float m_FiringRate = 5;
    private float timeCounter = 1;
    public GameObject m_BulletPrefab;
    public Slider m_Slider;
    public Image m_FillImage;
    [HideInInspector]
    public bool m_ZeroHealthHappened;
    public Color m_ZeroHealthColor, m_FullHealthColor;
    public GameObject m_BuildingRenderers;
    public GameObject m_HealthCanvas;
    private bool playerInRange;
    public ParticleSystem m_ExplosionParticles;
    private float signalInteval = 0f;
    private AudioSource audioSource;
    public AudioClip shotClip;
    void Start()
    {
        m_CurrentHealth = m_StartingHealth;
        SetHealthUI();
        audioSource = GetComponent<AudioSource>();
        m_Range += (int)GameManagerOffline.s_Instance.m_GameMode * 5;
    }

    void EnableComponent(bool state)
    {
        if (!state) StopAllCoroutines();
        enabled = state;
    }

    public void Update()
    {
        playerInRange = Vector3.Distance(m_Character.position, transform.position) < m_Range;
        if (playerInRange)
        {
            if (signalInteval > 0) signalInteval -= Time.deltaTime;
            else
            {
                signalInteval = 2;
                Collider[] comrades;

                comrades = Physics.OverlapSphere(transform.position, m_Range, LayerMask.GetMask("Players"));
                if (comrades.Length > 0)
                {
                    for (int i = 0; i < comrades.Length; i++)
                    {
                        if (comrades[i].gameObject.tag == "AI")
                        {
                            if (gameObject == comrades[i].gameObject) continue;
                            if (comrades[i].GetComponent<TankAI>())
                                comrades[i].GetComponent<TankAI>().State = AIState.Attack;
                        }
                    }
                }
            }

            timeCounter -= Time.deltaTime;
            if (timeCounter < 0)
                Attack();
            //shoot a bullet
        }
    }


    private void Attack()
    {
        timeCounter = m_FiringRate;
        StartCoroutine(_Attack());
    }

    private IEnumerator _Attack()
    {
        int count = (int)GameManagerOffline.s_Instance.m_GameMode + 2;
        do
        {
            GameObject go = Instantiate(m_BulletPrefab, m_FireTransform.position, Quaternion.identity) as GameObject;
            go.GetComponent<Bullet>().m_Target = m_Character.position;
            count--;
            if (SoundManager.Instance.Audio)
                audioSource.PlayOneShot(shotClip);
            yield return new WaitForSeconds(0.25f);
        }
        while (count > 0);

    }
    public bool IsDestroyed()
    {
        return m_ZeroHealthHappened;
    }

    private void SetHealthUI()
    {
        // Set the slider's value appropriately.
        m_Slider.value = m_CurrentHealth;

        // Interpolate the color of the bar between the choosen colours based on the current percentage of the starting health.
        m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, m_Range);
    }

    public void Damage(float amount)
    {
        // Reduce current health by the amount of damage done.
        float damageAmount = amount;

        m_CurrentHealth -= damageAmount;
        // If the current health is at or below zero and it has not yet been registered, call OnZeroHealth.
        if (m_CurrentHealth <= 0f && !m_ZeroHealthHappened)
        {
            OnZeroHealth();
        }
        SetHealthUI();
    }

    private void OnZeroHealth()
    {
        // Set the flag so that this function is only called once.
        m_ZeroHealthHappened = true;
        // Play the particle system of the tank exploding.
        m_ExplosionParticles.Play();

        SetActive(false);
    }

    private void SetActive(bool active)
    {
        GetComponent<BoxCollider>().enabled = active;
        if (m_BuildingRenderers != null)
            m_BuildingRenderers.SetActive(active);
        m_HealthCanvas.SetActive(active);
        EnableComponent(active);
    }
}

