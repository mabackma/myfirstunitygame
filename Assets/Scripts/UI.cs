using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI : MonoBehaviour
{
    Image crossHair;
    Image hitMarker;
    Image singleShot;
    Image automatic;
    Image points;
    Image healthPoints;
    Image stomp;

    [SerializeField]
    Weapon weapon;

    [SerializeField]
    private Goal playerGoal;

    [SerializeField]
    private TMP_Text aliensLeftText;

    [SerializeField]
    private TMP_Text playerBulletsText;

    [SerializeField]
    private GameObject playerPanel;

    [SerializeField]
    private TMP_Text playerControls;

    [SerializeField]
    private GameObject switchPanel;

    [SerializeField]
    private TMP_Text switchPrompt;

    [SerializeField]
    private GameObject jumpingPanel;

    [SerializeField]
    private TMP_Text jumpingPrompt;

    [SerializeField]
    private GameObject gamePanel;

    [SerializeField]
    private TMP_Text gamePrompt;

    bool showControls;
    bool canFlash; 

    FPS_Controller player;
    AcrossSwitch triggerToShoot;
    Notification notification;

    // Start is called before the first frame update
    void Start()
    {
        player = FindObjectOfType<FPS_Controller>();
        triggerToShoot = FindObjectOfType<AcrossSwitch>();
        notification = FindObjectOfType<Notification>();

        singleShot = GameObject.Find("SingleShot").GetComponent<Image> ();
        singleShot.enabled = true;

        automatic = GameObject.Find("Automatic").GetComponent<Image> ();
        automatic.enabled = false;

        crossHair = GameObject.Find("Crosshair").GetComponent<Image> ();
        crossHair.enabled = true;

        hitMarker = GameObject.Find("HitMarker").GetComponent<Image> ();
        hitMarker.enabled = false;

        points = GameObject.Find("Points").GetComponent<Image> ();
        points.fillAmount = 0f;

        healthPoints = GameObject.Find("HealthPoints").GetComponent<Image> ();
        points.fillAmount = 100f;

        stomp = GameObject.Find("Ready").GetComponent<Image> ();
        stomp.enabled = false;
        
        aliensLeftText.text = "Aliens Left: " + weapon.AliensLeft;
        playerBulletsText.text = DisplayBullets() + "\n/ " + weapon.GetBulletsLeft().ToString();

        playerPanel.gameObject.SetActive(true);
        showControls = true;
        canFlash = true;
 
        switchPanel.gameObject.SetActive(true);
        jumpingPanel.gameObject.SetActive(false);
        gamePanel.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // Näyttää pelaajan pisteet
        points.fillAmount = player.Points / 300f;

        // Näyttää pelaajan pisteet
        healthPoints.fillAmount = player.Health / 100f;
        if (player.Health <= 0)
        {
            gamePanel.gameObject.SetActive(true);
            gamePrompt.text = "YOU DIED!";
        }

        // Näyttää montako vihollista on jäljellä
        aliensLeftText.text = "Aliens Left: " + weapon.AliensLeft;  
        if (weapon.AliensLeft <= 0 && playerGoal.reachedGoal)
        {
            gamePanel.gameObject.SetActive(true);
            gamePrompt.text = "CONGRATULATIONS!\nYOU HAVE SAVED THE PLANET";
            player.GameOver();
        }

        // Ilmoittaa kun pelaaja voi suorittaa stompin
        if (player.Points >= 300 && canFlash)
            StartCoroutine(FlashStomp());

        if (Input.GetKeyDown(KeyCode.LeftAlt))
            showControls = !showControls;

        if (showControls)
        {
            playerPanel.gameObject.SetActive(true);
            playerControls.enabled = true;
        }
        else
        {
            playerPanel.gameObject.SetActive(false);
            playerControls.enabled = false;
        }

        DisplayFireMode();

        // Jos vihollinen kuolee tai tulee headshot, niin muutetaan tähtäin punaiseksi
        if (weapon.headShotOrKill)
            StartCoroutine(ShowHitMarker());
        
        playerBulletsText.text = DisplayBullets() + "\n/ " + weapon.GetBulletsLeft().ToString();
        
        if (notification.tellToJump)
            StartCoroutine(TellJumpControls()); 

        if (triggerToShoot.NearSwitch)
        {
            switchPanel.gameObject.SetActive(true);
            switchPrompt.enabled = true;
        }
        else
        {
            switchPanel.gameObject.SetActive(false);
            switchPrompt.enabled = false;
        }
    }

    // Näyttää kuvan onko sarjatuli vai yksittäinen laukaisu
    private void DisplayFireMode()
    {
        if (weapon.autoFire)
        {
            automatic.enabled = true;
            singleShot.enabled = false;
        }
        else
        {
            automatic.enabled = false;
            singleShot.enabled = true;
        }
    }

    // Tulostaa montako luotia on jäljellä
    private string DisplayBullets()
    {
        string bulletString = "";
        int bullets = weapon.GetCurrentBullets();

        for (int i=0; i<bullets; i++)
            bulletString += "|";
        
        return bulletString;
    }

    // Muuttaa tähtäimen hetkeksi punaiseksi
    private IEnumerator ShowHitMarker()
    {
        crossHair.enabled = false;
        hitMarker.enabled = true;

        yield return new WaitForSeconds(0.4f);
        
        hitMarker.enabled = false;
        yield return new WaitForSeconds(0.1f);
        crossHair.enabled = true;

        weapon.headShotOrKill = false;
    }

    // Vilkuttaa merkkiä kun pelaaja saa tehdä stompin.
    private IEnumerator FlashStomp()
    {
        canFlash = false;
        player.StompAudioSource.PlayOneShot(player.activateAudio, 1f);

        while (true)
        {
            stomp.enabled = true;
            yield return new WaitForSeconds(0.5f); 
            stomp.enabled = false;
            yield return new WaitForSeconds(0.5f);
            if (player.Points < 300)
                break;
        }

        canFlash = true;
    }

    // Kertoo pelaajalle miten voi tehdä pidempiä hyppyjä.
    private IEnumerator TellJumpControls()
    {
        jumpingPanel.gameObject.SetActive(true);
        jumpingPrompt.enabled = true;
        yield return new WaitForSeconds(5f); 
        jumpingPanel.gameObject.SetActive(false);
        jumpingPrompt.enabled = false;
        notification.tellToJump = false;
    }
}
