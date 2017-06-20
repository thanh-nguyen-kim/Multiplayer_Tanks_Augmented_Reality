using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
public class ARButton : MonoBehaviour {

public void Exit()
    {
        SceneManager.LoadScene(0);
    }
}
