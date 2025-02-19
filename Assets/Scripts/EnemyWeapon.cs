using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    public Transform shootPoint;
    public EnemyBullet bulletPrefab;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    { 
    }

    // Ampuminen
    public IEnumerator EnemyShoot()
    {
        // Haetaan paikka josta ammus lähtee lentoon
        Vector3 launchPosition = shootPoint.position;
        yield return new WaitForSeconds(0.1f);
        launchPosition.y = shootPoint.position.y;

        // Luodaan uusi ammus
        EnemyBullet bullet = Instantiate(bulletPrefab); 
        bullet.transform.position = launchPosition;

        // Ammuksen alkuvauhti on nolla
        Rigidbody bulletPhysics = bullet.GetComponent<Rigidbody>();
        bulletPhysics.velocity = Vector3.zero;

        // Ammutaan ammus
        Enemy parentObject = transform.parent.GetComponent<Enemy>();
        bulletPhysics.AddForce(parentObject.transform.forward * 2000f);
    }
}
