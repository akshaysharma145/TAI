using UnityEngine;
using UnityEngine.SceneManagement;

public class WinTrigger : MonoBehaviour
{
    public string PlayerTag = "Player";
    public string WinSceneName = "WinScene"; // type your win scene name here

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PlayerTag))
        {
            Debug.Log("Player Won!");
            SceneManager.LoadScene(WinSceneName);
        }
    }
}