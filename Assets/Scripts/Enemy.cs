using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField]
    EnemyWeapon enemyWeapon;

    Animator enemyAnimator;
    AnimatorStateInfo info;

    public int health;

    public bool LeftHit;
    public bool RightHit;
    public bool Dying;
    public bool isStunned;

    [SerializeField]
    GameObject body;

    [SerializeField]
    GameObject head;
    
    // Vihollisen colliderit
    MeshCollider bodyCollider;
    CapsuleCollider headCollider;

    // Vihollisen silmät
    [SerializeField] GameObject eyes;
    MeshRenderer eyeMesh;
    public Material eyeMaterial;
    public Material attackMaterial;

    FPS_Controller player;

    [SerializeField] AudioSource enemyAudioSource;
    [SerializeField] AudioClip deathAudio;
    [SerializeField] AudioClip enemyGunShotAudio; 

    public SkinnedMeshRenderer SkMesh;

    // Liikkumiseen tarvittavat muuttujat
    NavMeshAgent agent;
    public Transform playerTransform;
    public LayerMask whatIsGround, whatIsPlayer;

    // Patrolling muuttujat
    Vector3 home;                           // Alkupiste
    Vector3 walkPoint;                     // Piste johon liikutaan            
    Vector3 previousWalkPoint;            // Edellinen piste johon liikuttiin
    bool walkPointSet;                   // Onko pistejohon liikutaan löydetty
    public float walkPointRange = 10;    // Etäisyys jonka sisällä voidaan liikkua
    NavMeshPath path;
    bool canReach;
    float randomX, randomZ;
    bool goHome;
    bool isChasing;
    bool canShoot;
    bool isPlayerInFront;

    // Start is called before the first frame update
    void Start()
    {
        eyeMesh = eyes.GetComponent<MeshRenderer>();
        eyeMesh.material = eyeMaterial;

        home = transform.position;
        path = new NavMeshPath();
        previousWalkPoint = transform.position;
        goHome = false;

        agent = GetComponent<NavMeshAgent>();
        agent.speed = 5f;
        enemyAnimator = GetComponent<Animator>();
        bodyCollider = body.GetComponent<MeshCollider>();
        health = 100;
        isStunned = false;
        canShoot = true;

        // Pelaajan muuttujat
        player = FindObjectOfType<FPS_Controller>();
        playerTransform = player.transform;

        SkMesh = GetComponentInChildren<SkinnedMeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {  
        //Debug.Log(transform.position - walkPoint + " canReach: " + canReach);

        if (transform.position.y < 5)
            goHome = true;

        if (PlayerNear(25f) && !Dying)
        {
            eyeMesh.material = attackMaterial;  // Muuttaa silmät punaiseksi
            isChasing = true;
            agent.speed = 15f;
        }
        else if (!Dying)
        {
            eyeMesh.material = eyeMaterial;  // Muuttaa silmät vihreäksi
            isChasing = false;
            isPlayerInFront = false;
            agent.speed = 5f;
        }

        if (isChasing && !isStunned && !Dying)
        {     
            enemyAnimator.SetBool("Chasing", true);
            Chasing();
        }
        else if (goHome)    // Palautetaan vihollinen alkupisteeseen
        {          
            enemyAnimator.SetBool("Chasing", false);  
            isPlayerInFront = false;
            walkPointSet = true;
            agent.SetDestination(home);

            Vector3 toHome = transform.position - home;
            if (toHome.magnitude < 3f)
            { 
                goHome = false;
                walkPointSet = false;
                Patrolling();
            }
        }
        else
        {          
            enemyAnimator.SetBool("Chasing", false);
            isPlayerInFront = false;
            Patrolling();
        }

        // Animaatio kun damage tulee vasemmalta
        if (LeftHit && !isStunned)
        { 
            enemyAnimator.SetTrigger("LeftHit"); 
            LeftHit = false;            
        } 

        // Animaatio kun damage tulee oikealta
        if (RightHit && !isStunned)
        { 
            enemyAnimator.SetTrigger("RightHit"); 
            RightHit = false;            
        }

        if (Dying)
        { 
            // Pysäytetään liikkuminen
            agent.isStopped = true;

            // Käynnistetään kuoleman animaatio 
            enemyAnimator.SetTrigger("Dying");

            // Poistetaan colliderit käytöstä
            bodyCollider.enabled = false;
            Destroy(head);
        }

        if (player.cameraShake.isShaking && !isStunned && !Dying)
        {
            if (PlayerNear(15f))
                StartCoroutine(Stunned());
        }
            
    }

    // Odottaa hetken ennen kun voidaan ampua uudestaan
    private IEnumerator WaitToShoot()
    {
        canShoot = false;
        yield return new WaitForSeconds(0.7f);
        canShoot = true;  
    }

    private IEnumerator Stunned()
    { 
        // Pysäytetään liikkuminen
        agent.isStopped = true;

        isStunned = true;
        enemyAnimator.SetBool("Stunned", true);
        yield return new WaitForSeconds(5f);
        enemyAnimator.SetBool("Stunned", false);
        isStunned = false;

        // Sallitaan liikkuminen
        agent.isStopped = false;
    }

    // Tarkistaa onko matka pelaajasta viholliseen vähemmän kuin parametri d
    private bool PlayerNear(float d)
    {
        float dist = Vector3.Distance(player.transform.position, transform.position);
        
        if (dist < d)
            return true;
        else
            return false;
    }

    public void PlayDeathAudio()
    {
        enemyAudioSource.PlayOneShot(deathAudio, 1f);
    }

    // Partioidaan. Partio metodit: Patrolling() ja SearchWalkPoint()
    private void Patrolling()
    {
        if (!walkPointSet)
            SearchWalkPoint();

        if (walkPointSet)
            agent.SetDestination(walkPoint);

        float distanceToWalkPointX = transform.position.x - walkPoint.x;
        float distanceToWalkPointZ = transform.position.z - walkPoint.z;

        // Jos saavuttiin päämäärään
        if (distanceToWalkPointX < 2f && distanceToWalkPointZ < 2f)
            walkPointSet = false;
    }

    // Etsitään paikka johon vihollinen voi liikkua
    private void SearchWalkPoint()
    { 
        if (RandomPoint(transform.position, walkPointRange, out walkPoint))
        {
            canReach = agent.CalculatePath(walkPoint, path);
            if (path.status == NavMeshPathStatus.PathPartial)
                canReach = false;

            if (canReach)
                walkPointSet = true;
        }
    }

    bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * range;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }

    // Jahdetaan pelaajaa
    private void Chasing()
    {
        // Ammutaan pelaajaa kun se on lähellä
        if (PlayerNear(15f) && canShoot)
        { 
            enemyAudioSource.PlayOneShot(enemyGunShotAudio, 1f);
            enemyWeapon.StartCoroutine(enemyWeapon.EnemyShoot());
            enemyAnimator.SetTrigger("Shooting"); 
            StartCoroutine(WaitToShoot());
        } 

        // Käännytään pelaajaan päin
        if (!isPlayerInFront) 
            StartCoroutine(RotateEnemy());
        else       
            transform.LookAt(playerTransform.position + Vector3.down); 

        MoveToPlayer();
        goHome = false;
    }

    // Liikutaan pelaajan eteen
    private void MoveToPlayer()
    { 
        int randInt = Random.Range(-10, 10);
        Vector3 inFrontOfPlayer = playerTransform.position + playerTransform.forward * 5 + playerTransform.right * randInt;
        agent.SetDestination(inFrontOfPlayer);
    }

    IEnumerator RotateEnemy()
    { 
        Quaternion targetRotation = Quaternion.LookRotation(playerTransform.position - transform.position + Vector3.down);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 20); 
        yield return new WaitForSeconds(0.7f);
        isPlayerInFront = true;  
    }
}
