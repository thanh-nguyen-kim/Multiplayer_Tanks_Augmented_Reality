using UnityEngine.UI;
using UnityEngine;
public class ToastSystem : MonoBehaviour
{
    private static ToastSystem instance = null;
    public Text messText;
    public static ToastSystem Instance
    {
        get
        {
            return instance;
        }

        private set
        {
            instance = value;
        }
    }

    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
            Destroy(gameObject);
    }

    public void ShowToast(string message)
    {
        messText.text = message;
        gameObject.GetComponent<Animator>().enabled = true;
        gameObject.GetComponent<Animator>().Play("pop_up");
    }

}
