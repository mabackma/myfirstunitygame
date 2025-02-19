using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    GameObject switch1;
    Collider collider;
    bool playerOnElevator;
    float speed;

    FPS_Controller player;

    // Start is called before the first frame update
    void Start()
    {
        switch1 = GameObject.Find("Switch");
        GetComponent<Animator>().enabled = false;
        player = FindObjectOfType<FPS_Controller>();

        collider = GetComponent<Collider>();
        playerOnElevator = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (playerOnElevator)
            player.gravity = 0f;
        else
            player.gravity = -15f;
            
        if (switch1 == null)
            GetComponent<Animator>().enabled = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player") 
        {   
            playerOnElevator = true;
            Debug.Log("ON ELEVATOR!");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player") 
        {   
            playerOnElevator = false; 
            Debug.Log("OFF!");
        }
    }
}
