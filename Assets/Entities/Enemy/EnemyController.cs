using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour {
	public float MaxHealth = 10f;
	public float boltSpeed = 2f;
	public float firePause = 3;
	public float shotProbability = 0.2f;

	public AudioClip laser;
	public AudioClip death;

	float nextFire;

	public GameObject laserPrefab;
	ShipController ship;

	float Health;

	void Start(){
		ship = GetComponent<ShipController> ();
		Health = MaxHealth;
		nextFire = Time.timeSinceLevelLoad + Random.Range(2f,5f);
	}


	/*void OnTriggerEnter2D(Collider2D collider){
		BoltController bolt = collider.GetComponentInParent<BoltController>();
		if (bolt && bolt.gameObject.tag == "Player") {
			Health -= bolt.boltDamage;
		}
		if (Health <= 0f) {
			Destroy (gameObject);

		}
		bolt.Hit ();
	}*/

	void Update(){
		/* My version of the firing mechanism
		if (nextFire <= Time.timeSinceLevelLoad) {
			Fire();
		}
		*/

		//Instructors version:
		float probability = Time.deltaTime * shotProbability;
		if (Random.value < probability) {
			Fire();
		}

	
	}

	public void Death(){
		FindObjectOfType<ScoreKeeper>().addScore();
		AudioSource.PlayClipAtPoint(death, transform.position, 0.9f);
		Destroy (gameObject);
	}

	void Fire(){
		ship.FireWeaponOne ();

		firePause = Random.Range(2f,4f);
		nextFire = Time.timeSinceLevelLoad + firePause;
		AudioSource.PlayClipAtPoint (laser, transform.position, 0.4f);
	}

}
