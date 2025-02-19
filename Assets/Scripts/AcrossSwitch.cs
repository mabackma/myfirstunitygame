using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcrossSwitch : MonoBehaviour
{
    GameObject switchAcross;
    public bool NearSwitch;

    // Start is called before the first frame update
    void Start()
    {
        switchAcross = GameObject.Find("Switch");
        NearSwitch = false;
    }

    void Update()
    {
        if (switchAcross == null) 
            NearSwitch = false; 
    }

    // Muutetaan NearSwitch boolean trueksi kun pelaaja seisoo hissien edessä.
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && switchAcross != null)     
            NearSwitch = true;
    }

    // Muutetaan NearSwitch boolean falseksi kun pelaaja ei seiso hissien edessä.
    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")     
            NearSwitch = false;
    }
    
}
