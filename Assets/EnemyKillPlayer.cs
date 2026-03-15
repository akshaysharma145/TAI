using UnityEngine;
using Unity.FPS.Game;

public class EnemyKillPlayer : MonoBehaviour
{
    public string EnemyTag = "Enemy";
    public Health playerHealth;

    void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag(EnemyTag))
        {
            KillPlayer(collision.gameObject);
            Debug.Log("Player Died");
        }
    }

    void KillPlayer(GameObject player)
    {
        if (playerHealth != null)
        {
            Debug.Log("function called");
            playerHealth.Kill();
        }
    }
}