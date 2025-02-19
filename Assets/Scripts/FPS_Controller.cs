using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPS_Controller : MonoBehaviour
{
    // Kertoo onko peli toiminnassa
    bool gamePlaying;

    public int Points;
    public int Health;

    [SerializeField] bool isGrounded = true;
    float distToGround = 0f;
    bool runFromGround;

    [SerializeField] Transform playerCamera = null;     // Pelaajan kameran Transform muuttuja
    [SerializeField] Light flashLight;                  // Pelaajan taskulamppu
    [SerializeField] float mouseSensitivity = 3.5f;     // Hiiren herkkyys
    [SerializeField] float walkSpeed = 10f;             // Pelaajan kävelynopeus
    [SerializeField] float runSpeed = 20f;              // Pelaajan juoksunopeus
    public float gravity = -15f;                        // Painovoima
    [SerializeField] float jumpSpeed = 7.5f;            // Pelaajan hyppynopeus 

   // Aika jonka kuluttua pelaajan liike pysähtyy
    [SerializeField][Range(0f, 0.5f)] float moveSmoothTime = 0.3f;

    // Aika jonka kuluttua pelaajan kameran liike pysähtyy
    [SerializeField][Range(0f, 0.5f)] float mouseSmoothTime = 0.03f;

    // Dash
    bool canDash = true;
    bool isDashing;
    float dashingPower = 80f;
    float dashingTime = 0.12f;
    float dashingCooldown = 0.2f;

    // Dash äänet
    [SerializeField] AudioSource dashAudioSource;    
    [SerializeField] AudioClip dashAudio;

    // Stomp
    bool isStomping;
    float stompingWaitTime = 0.5f;

    // Stomp äänet
    public AudioSource StompAudioSource;   
    [SerializeField] AudioClip startStompAudio; 
    [SerializeField] AudioClip endStompAudio; 
    [SerializeField] AudioClip stompAudio;
    public AudioClip activateAudio;

    // Osuma ääni
    public AudioClip GotHitAudio;

    float velocityY = 0f;   // Tippumis/hyppy nopeus
    int doubleJump = 0; 	// Muuttuja joka pitää kirjaa tuplahypystä

    CharacterController m_playerController;
    float cameraPitch = 0f;
    float cameraYaw = 0f;

    // Camera shake
    public CameraShake cameraShake;
    float duration = 4f;
    float magnitude = 0.15f;

    Vector2 currentDir = Vector2.zero;
    Vector2 currentVelocity = Vector2.zero;

    Vector2 currentMouseDelta = Vector2.zero;
    Vector2 currentMouseVelocity = Vector2.zero;

    Vector2 targetDir = Vector2.zero;

    // Start is called before the first frame update
    void Start()
    {
        gamePlaying = true;

        Points = 0;
        Health = 100;

        // Pelaajan kontrolleri
        m_playerController = GetComponent<CharacterController>();

        // Kameran tärinä
        cameraShake = FindObjectOfType<CameraShake>();
        flashLight.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    { 
        // Lopettaa pelin
        if (Input.GetKey("escape"))
            Application.Quit();
        
        if (gamePlaying)
        {       
            UpdateMouseLook();
            UpdateMovement();
        }
    }

    // Kääntää pelaajan kameraa
    void UpdateMouseLook()
    {
        // Target arvo joka luetaan hiiren liikkeestä
        Vector2 targetMouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        
        // Muuttaa vektoria vähitellen kohti target arvoa
        currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, targetMouseDelta, ref currentMouseVelocity, mouseSmoothTime);

        // Asetetaan kameran pystysuora kulma -90 ja 90 asteen välille
        cameraPitch -= currentMouseDelta.y * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);

        cameraYaw += mouseSensitivity * currentMouseDelta.x;

        if (Input.GetAxisRaw("Horizontal") != 0)
            Tilt(cameraYaw, true);
        else
            Tilt(cameraYaw, false);
        
        playerCamera.localEulerAngles = Vector3.right * cameraPitch;

        // Kääntää pelaajaa y-akselin ympäri (kamera kääntyy mukana)
        transform.Rotate(Vector3.up * mouseSensitivity * currentMouseDelta.x);
    }

    // Kallistaa kameraa
    private void Tilt(float camYaw, bool isTilt)
    {
        Vector3 targetVector;
        float tiltSmooth;
        float tiltAmount = 5f; 

        // Kallistuskulma
        float rotZ = -Input.GetAxis("Horizontal") * tiltAmount; 

        // Vektori jonka suuntaan kamera käännetään
        if (isTilt)
        {
            tiltSmooth = 0.04f;
            targetVector = Vector3.forward * rotZ + Vector3.up * camYaw;
        }
        else
        {
            tiltSmooth = 0.01f;
            targetVector = Vector3.up * camYaw;
        }

        Quaternion finalRot = Quaternion.Euler(targetVector);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, finalRot, tiltSmooth);  
    }

    // Liikuttaa pelaajaa
    void UpdateMovement()
    { 
        // m_playerController.isGrounded ei aina toimi, niin tarkistetaan raycastillä itse isGrounded
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast (transform.position, -Vector3.up, out hit)) 
            distToGround = hit.distance;

        if (distToGround < 1.1f || m_playerController.isGrounded)
            isGrounded = true;
        else
            isGrounded = false;

        // Tarkastetaan että mitään Coroutine tapahtumaa ei ole käynnissä
        if (isDashing || isStomping || cameraShake.isShaking)
            return;

        // Target arvo joka luetaan W A S D näppäimistä
        targetDir.Set(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // Normalisoi vektorin (antaa pituudeksi 1)
        targetDir.Normalize();

        // Muuttaa vektoria vähitellen kohti target arvoa
        currentDir = Vector2.SmoothDamp(currentDir, targetDir, ref currentVelocity, moveSmoothTime);

        // Pelaaja on maassa
        if (isGrounded)
            doubleJump = 0;    
        else 
        {
            // Pelaaja voi hypätä vain kerran ilmasta, myös tippuessaan
            if (doubleJump == 0)
                doubleJump++;
        }

        // Pelaaja hyppää
        if (Input.GetButtonDown("Jump") && doubleJump < 2)
        {
            doubleJump++;
            velocityY = jumpSpeed;
        }

        // Pelaaja siirtyy nopeasti vähän matkaa eteenpäin (Dash)
        if ((Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.Mouse3)) && canDash)
        {
            // Dashiä voi suorittaa useammin maassa
            if (isGrounded)
                dashingCooldown = 0.2f;
            else
                dashingCooldown = 1f;

            StartCoroutine(Dash());
        }

        // Pelaaja tippuu maahan suurella voimalla (Stomp)
        if (Input.GetKey(KeyCode.Mouse2) && !isGrounded && Points >= 300) 
            StartCoroutine(Stomp());    

        velocityY += gravity * Time.deltaTime;

        if ((Input.GetKeyDown("left shift") || Input.GetKey("left shift")) && isGrounded)       
            runFromGround = true;
        
        if (Input.GetKeyUp("left shift"))
            runFromGround = false;

        // Lasketaan 3D vektori liikkeelle ja liikutetaan pelaajaa
        if (Input.GetKey("left shift") && isGrounded)
        {
            // Pelaaja juoksee
            Vector3 velocity = (transform.forward * currentDir.y + transform.right * currentDir.x) * runSpeed + Vector3.up * velocityY;
            m_playerController.Move(velocity * Time.deltaTime); 
        }
        else if (Input.GetKey("left shift") && runFromGround && !isGrounded)
        {
            // Pelaaja juoksee
            Vector3 velocity = (transform.forward * currentDir.y + transform.right * currentDir.x) * runSpeed + Vector3.up * velocityY;
            m_playerController.Move(velocity * Time.deltaTime); 
        }
        else
        {
            // Pelaaja kävelee
            Vector3 velocity = (transform.forward * currentDir.y + transform.right * currentDir.x) * walkSpeed + Vector3.up * velocityY;
            m_playerController.Move(velocity * Time.deltaTime); 
        }

        // Taskulamppu päälle/pois päältä
        if (Input.GetKeyDown(KeyCode.V))
            flashLight.enabled = !flashLight.enabled;
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        gravity = 0;

        dashAudioSource.PlayOneShot(dashAudio, 0.5f);

        // Lasketaan liikkumiseen tarvittavat vektorit samalla tavalla kuin UpdateMovement metodissa
        Vector2 targetDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); 
        targetDir.Normalize(); 
        currentDir = Vector2.SmoothDamp(currentDir, targetDir, ref currentVelocity, moveSmoothTime);

        // Jätetään pois painovoima liikkeestä
        Vector3 velocity = (transform.forward * currentDir.y + transform.right * currentDir.x);
        velocity.y = 0;

        // Liikutetaan pelaajaa suurella voimalla (dashingPower)
        float startTime = Time.time;
        while (Time.time < startTime + dashingTime)
        {
            m_playerController.Move(velocity * Time.deltaTime * dashingPower); 
            yield return null;
        }
    
        isDashing = false;
        gravity = -15f;

        yield return new WaitForSeconds(dashingCooldown); 
        canDash = true;
    }

    private IEnumerator Stomp()
    { 
        isStomping = true; 
        Points = 0;

        // Lisätään health pisteitä
        Health += 20;
        if (Health > 100)
            Health = 100;

        StompAudioSource.PlayOneShot(startStompAudio, 1f);

        float jumpUpTime = Time.time;
        while (Time.time < jumpUpTime + 0.05f)
        {
            m_playerController.Move(Vector3.up * jumpSpeed * 10 * Time.deltaTime); 
            yield return null;
        }

        // Pysäyttää pelaajan vähäksi aikaa
        float startTime = Time.time;
        while (Time.time < startTime + stompingWaitTime)
        {
            m_playerController.Move(Vector3.zero);
            yield return null;
        }

        // Tiputtaa pelaajan voimalla alas
        while (!isGrounded)
        {
            gravity = -160f;
            m_playerController.Move(new Vector3(0, gravity * Time.deltaTime, 0));
            yield return null;
        }

        gravity = -15f;

        StompAudioSource.PlayOneShot(endStompAudio, 1f);
        StompAudioSource.PlayOneShot(stompAudio, 0.5f);

        // Pysäyttää pelaajan vähäksi aikaa ja tärisyttää kameraa
        StartCoroutine(cameraShake.Shake(duration*2, magnitude));
        StartCoroutine(SpinLight(duration*1.5f));
        isStomping = false;
    }

    // Pyöritetään valoa
    private IEnumerator SpinLight(float spinTime)
    {
        bool wasFlashLightOn = false;

        if (!flashLight.enabled)
            flashLight.enabled = true;
        else
            wasFlashLightOn = true;

        float elapsed = 0f;

        flashLight.color = Color.cyan;
        while (elapsed < spinTime)
        {
            flashLight.transform.Rotate(0f, 15f, 0f, Space.Self);
            elapsed += Time.deltaTime * 5; 
            yield return null;
        }
        flashLight.color = Color.white;

        flashLight.transform.rotation = playerCamera.rotation;

        if (wasFlashLightOn)
            flashLight.enabled = true;
        else
            flashLight.enabled = false;
    }

    // Peli loppuu
    public IEnumerator GameOver()
    {
        gamePlaying = false;
        yield return new WaitForSeconds(5f);
        Application.Quit();
    }
}
