using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    // Pitää kirjaa montako vihollista on jäljellä
    public int AliensLeft;

    Animator gunAnimator;
    AnimatorStateInfo info;

    [SerializeField] 
    Transform shootPoint;

    float range = 300f;
    int bulletsPerMag = 30;     // Montako luotia lippaassa on
    public int bulletsLeft;     // Montako luotia on varastossa
    int currentBullets;         // Montako luotia on ladattuna

    bool canShoot;
    public bool autoFire;
    float fireRate = 0.1f;
    float fireTimer;

    // Jos pelaaja ampuu headshotin tai vihollinen kuolee
    public bool headShotOrKill = false;

    // Mistä suunnasta viholliseen nähden luoti tulee
    bool damageFromLeft;

    [SerializeField] GameObject bulletImpact;
    
    public AudioSource gunAudioSource;
    [SerializeField] AudioClip gunShotAudio; 
    [SerializeField] AudioClip headShotAudio;
    [SerializeField] AudioClip fireModeAudio;
    [SerializeField] AudioClip reloadAudio;
    [SerializeField] AudioClip killAudio;
    [SerializeField] AudioClip killAudio2;

    FPS_Controller player;
    AcrossSwitch shootFromHere;

    // Start is called before the first frame update
    void Start()
    {
        AliensLeft = 50;
        player = FindObjectOfType<FPS_Controller>();
        gunAnimator = GetComponent<Animator>();
        canShoot = true;
        autoFire = false;
        bulletsLeft = bulletsPerMag * 2;     // Aloitetaan peli kahdella lippaalla 
        currentBullets = 30;                 // Pelaajalla on myös yksi täysi lipas
        shootFromHere = FindObjectOfType<AcrossSwitch>();
    }

    // Update is called once per frame
    void Update()
    {
        // Vaihtaa sarjatulen päälle/pois päältä
        if (Input.GetKeyDown(KeyCode.X))
        {
            gunAudioSource.PlayOneShot(fireModeAudio, 1f);
            autoFire = !autoFire;
        }

        // Lataa aseen
        if (Input.GetKeyDown(KeyCode.R) && currentBullets < bulletsPerMag && bulletsLeft > 0 && canShoot)
            StartCoroutine(Reload());

        // Ampuu sarjaa
        if (Input.GetMouseButton(0) && autoFire && canShoot && currentBullets > 0)
        {
            gunAnimator.SetBool("ShootAuto", true);
            Fire();
        }

        // Pysäyttää sarjatulianimaation
        info = gunAnimator.GetCurrentAnimatorStateInfo(0);
        if (info.IsName("ShootingAuto"))
            gunAnimator.SetBool("ShootAuto", false);

        // Ampuu kerran
        if (Input.GetMouseButtonDown(0) && !autoFire && canShoot && currentBullets > 0)
            StartCoroutine(FireSingle());

        // Odotetaan kunnes saa ampua seuraavan kerran sarjaa (fireRate arvo)
        if (fireTimer < fireRate)
            fireTimer += Time.deltaTime;
    }

    private void Fire()
    {
        if (fireTimer < fireRate) return;

        gunAudioSource.PlayOneShot(gunShotAudio, 0.6f);
        currentBullets--;

        RaycastHit hitPoint;
        if (Physics.Raycast(shootPoint.position, shootPoint.transform.forward, out hitPoint, range))
        {
            Vector3 toOther = hitPoint.collider.transform.right;
            float angle = Vector3.Dot(shootPoint.transform.forward, toOther); 
            
            if (angle > 0)
                damageFromLeft = true;
            else
                damageFromLeft = false;

            GameObject hitTarget = hitPoint.collider.gameObject;  
            
            // Katsotaan mihin luoti osui
            if (hitPoint.collider.tag.Equals("enemyBody"))
                DealDamage(hitTarget, 10);
            else if (hitPoint.collider.tag.Equals("enemyAntenna"))
                DealDamage(hitTarget, 20);
            else if (hitPoint.collider.tag.Equals("enemyHead"))
            {
                gunAudioSource.PlayOneShot(headShotAudio, 0.5f);
                headShotOrKill = true;
                DealDamage(hitTarget, 40); 
            }
            else if (hitTarget.name.Equals("Switch") && shootFromHere.NearSwitch)   
                Destroy(hitTarget);
            else 
            {
                // Luodin tekemä reikä
                GameObject bulletHole = Instantiate(bulletImpact, hitPoint.point, Quaternion.FromToRotation(Vector3.forward, hitPoint.normal));

                // Tuhotaan reiät kahden sekunnin kuluttua
                Destroy(bulletHole, 2f);
            }

        }

        fireTimer = 0f;
    }

    // Vihollinen menettää healthiä
    private void DealDamage(GameObject target, int amount)
    {
        Enemy enemy = target.GetComponentInParent<Enemy>();
        player.Points += amount;
        Debug.Log(player.Points);

        // Kun luoti osuu vasemmalta päin
        if (damageFromLeft)
            enemy.LeftHit = true;
        
        // Kun luoti osuu oikealta päin
        if (!damageFromLeft)
            enemy.RightHit = true;

        enemy.health -= amount;

        if (enemy.health <= 0)
        {
            AliensLeft--;
            enemy.PlayDeathAudio(); 
            enemy.Dying = true;
            headShotOrKill = true;
            if (enemy.isStunned)
            {
                Transform gun = enemy.transform.Find("Box001");
                Destroy(gun.gameObject);
                enemy.SkMesh.enabled = false;
                StartCoroutine(DestroyEnemy(enemy));
            }
            else
                StartCoroutine(DestroyEnemy(enemy));

            StartCoroutine(PlayRandomKillAudio());
        }
    }

    IEnumerator PlayRandomKillAudio()
    {
        float rnd = Random.Range(0f, 15.0f);

        yield return new WaitForSeconds(0.7f);
        if (rnd < 5)
            gunAudioSource.PlayOneShot(killAudio, 0.6f);
        else if (rnd < 10)
            gunAudioSource.PlayOneShot(killAudio2, 0.7f);
    }

    // Tuhoaa vihollisen 3.9 sekunnin päästä
    private IEnumerator DestroyEnemy(Enemy e)
    {
        yield return new WaitForSeconds(3.7f); 
        Destroy(e.gameObject);
    }

    // Lataa aseen
    IEnumerator Reload()
    {
        canShoot = false;

        gunAnimator.SetTrigger("Reload");
        gunAudioSource.PlayOneShot(reloadAudio, 0.2f);

        int bulletsToLoad = bulletsPerMag - currentBullets;
        int bulletsToDeduct = (bulletsLeft >= bulletsToLoad) ? bulletsToLoad : bulletsLeft;

        bulletsLeft -= bulletsToDeduct;
        currentBullets += bulletsToDeduct;

        yield return new WaitForSeconds(1f);
        canShoot = true;
    }

    // Ampuu vain kerran ja odottaa 0,2 sekunttia
    IEnumerator FireSingle()
    {
        canShoot = false;
        gunAnimator.SetTrigger("Shoot");
        Fire(); 

        yield return new WaitForSeconds(0.2f);
        canShoot = true;
    }

    public int GetCurrentBullets()
    {
        return currentBullets;
    }

    public int GetBulletsLeft()
    {
        return bulletsLeft;
    }
}
