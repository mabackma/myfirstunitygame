using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Notification : MonoBehaviour
{
    public bool tellToJump;

    // Start is called before the first frame update
    void Start()
    {
        tellToJump = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")     
            tellToJump = true;
    }
}
