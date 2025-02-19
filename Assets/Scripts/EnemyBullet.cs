using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    FPS_Controller player;

    // Start is called before the first frame update
    void Start()
    {
        player = FindObjectOfType<FPS_Controller>();
        StartCoroutine(DestroyBullet());
    }

    // Ammus tuhotaan kolmen sekunnin kuluttua
    private IEnumerator DestroyBullet()
    {
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

    void OnTriggerExit(Collider other)
    {
        
        Destroy(gameObject);

        // Poistetaan pelaajalta 5 health pistettä
        if (other.tag == "Player")
        { 
            player.Health -= 5;
            player.StompAudioSource.PlayOneShot(player.GotHitAudio, 0.2f);
            Debug.Log("player health" + player.Health);

            if (player.Health <= 0)
                player.StartCoroutine(player.GameOver());
        }
    }
}
