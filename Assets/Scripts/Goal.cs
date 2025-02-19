using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goal : MonoBehaviour
{
    public bool reachedGoal;

    void Start()
    {
        reachedGoal = false;
    }

    void OnTriggerEnter(Collider other)
    {
        // Pelaaja on saapunut maaliin
        if (other.tag == "Player")
        { 
            reachedGoal = true;
        }
    }
}
