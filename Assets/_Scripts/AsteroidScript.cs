using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidScript : MonoBehaviour {

    public float health;
    public int rubbleAmount;
    public float rubbleSpeed;
    public float rubbleSpeedVariance;
    public float spawnSpeed;
    public float spawnRotation;
    public bool hasLifetime;
    public float lifetime;

    public ParticleSystem dustCloud;
    public GameObject[] smallRubble;    
    public GameObject[] mediumRubble;

	// Use this for initialization
	void Start () {
        float speed = Random.Range(-spawnSpeed, spawnSpeed);	
        Vector2 speedVector = new Vector2(Random.Range(-1f,1f),Random.Range(-1f,1f));
        gameObject.GetComponent<Rigidbody2D>().velocity = speedVector * speed;

        float mass = gameObject.GetComponent<Rigidbody2D>().mass;
        gameObject.GetComponent<Rigidbody2D>().AddTorque(Random.Range(- spawnRotation * mass, spawnRotation * mass));
        if (hasLifetime)
        {
            Destroy(gameObject, lifetime);
        }
	}


    void OnCollisionEnter2D(Collision2D collision) {
        if(collision.gameObject.tag == "Projectile" 
            || collision.gameObject.tag == "Vessel"
            || collision.gameObject.tag == "Scenery")
        {
            if (collision.gameObject.tag == "Projectile"){
                collision.gameObject.GetComponent<ProjectileController>().Hit(1f, "scenery");
                health -= collision.gameObject.GetComponent<ProjectileController>().projectileDamage;
            }            
            health -= collision.relativeVelocity.magnitude * collision.gameObject.GetComponent<Rigidbody2D>().mass;
        }
        

        // the impulse is taken from health for damage
        
        if (health < 0) {
            Vector3 dustPos = new Vector3(
                gameObject.transform.position.x,
                gameObject.transform.position.y,
                -1f
                );
            Instantiate(dustCloud, dustPos, Quaternion.identity);
            if (rubbleAmount != 0)
            {
                gameObject.GetComponent<PolygonCollider2D>().enabled = false;
                SpawnRubble();
            }
            Die();
            
        }
    }

    void SpawnRubble() {
        float probability = Random.Range(0f, 1f);
        

        if (mediumRubble.Length > 0) {
            for (int i = 1; i <= rubbleAmount; i++) {
                Vector3 spawnDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized;
                float spawnDistance = Random.Range(1f, 2f);
                Vector3 spawnPosition = gameObject.transform.position + spawnDirection * spawnDistance;

                GameObject fragment = Instantiate(
                    mediumRubble[Random.Range(0,mediumRubble.Length-1)], 
                    spawnPosition, 
                    Quaternion.identity) as GameObject;

                fragment.GetComponent<Rigidbody2D>().velocity = 
                    new Vector2(spawnDirection.x * rubbleSpeed, spawnDirection.y * rubbleSpeed);
                fragment.transform.parent = gameObject.transform.parent;
                    
            }

        }
        if (smallRubble.Length > 0) {
            for (int i = 1; i <= rubbleAmount; i++)
            {
                Vector3 spawnDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized;
                float spawnDistance = Random.Range(1f, 2f);
                Vector3 spawnPosition = gameObject.transform.position + spawnDirection * spawnDistance;

                GameObject fragment = Instantiate(
                    smallRubble[Random.Range(0, smallRubble.Length - 1)],
                    spawnPosition,
                    Quaternion.identity) as GameObject;

                fragment.GetComponent<Rigidbody2D>().velocity =
                    new Vector2(spawnDirection.x * rubbleSpeed * 30f, spawnDirection.y * rubbleSpeed * 30f);
                fragment.transform.parent = gameObject.transform.parent;

            }

        }

    }
    public void Die() {
        Destroy(gameObject);
    }




}
