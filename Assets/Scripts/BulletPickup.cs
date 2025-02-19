using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletPickup : MonoBehaviour
{
    [SerializeField] AudioSource pickupAudioSource;
    [SerializeField] AudioClip pickupAudio;

    int amount = 30;
    bool soundPlayed;

    void Start()
    {
        pickupAudioSource = gameObject.GetComponent<AudioSource>();
        soundPlayed = false;
    }

    void OnTriggerEnter(Collider other)
    {
        // Lisätään pelaajalle 30 luotia talteen
        if (other.tag == "Player")
        {
            Weapon weapon = FindObjectOfType<Weapon>();

            // Soittaa äänen vain kerran
            if(!soundPlayed)
                pickupAudioSource.Play();
            soundPlayed = true;

            weapon.bulletsLeft += amount;
            amount = 0;

            // Laittaa laatikon näkymättömäksi ja tuhoaa sen
            MeshRenderer meshRend = GetComponentInChildren<MeshRenderer>();
            meshRend.enabled = false;
            Destroy(gameObject, 1f);
        }
    }
}

