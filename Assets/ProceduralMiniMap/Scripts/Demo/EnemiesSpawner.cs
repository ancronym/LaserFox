using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemiesSpawner : MonoBehaviour {

	// Use this for initialization
	public GameObject EnemiePrefab;
	public float speed;
	public Slider SpawnLimit, SpawnRadius;
	List<GameObject> enemies;
	GameObject player;
	void Start () {
		player = GameObject.FindGameObjectWithTag ("Player").gameObject;
		enemies = new List<GameObject> ();

	}
	
	// Update is called once per frame
	void FixedUpdate () {

		if (GameObject.FindGameObjectsWithTag ("enemie").Length+GameObject.FindGameObjectsWithTag ("bigenemie").Length< SpawnLimit.value) {
			GameObject newEnemie = Instantiate (EnemiePrefab) as GameObject;
			if (Random.Range (0, 2) == 0) {
				newEnemie.tag = "enemie";
				newEnemie.transform.localScale = Vector3.one;
			} else {
				newEnemie.tag = "bigenemie";
				newEnemie.transform.localScale = Vector3.one*3;
			}
			newEnemie.transform.position =  new Vector3 ((Random.Range(-6,6))*SpawnRadius.value+1,newEnemie.transform.lossyScale.y/2,(Random.Range(-6,6)+1)*SpawnRadius.value) + player.transform.position;
			enemies.Add (newEnemie);
		}

		foreach (GameObject r in enemies) {
			if (r != null) {
				r.transform.position = Vector3.MoveTowards (r.transform.position, player.transform.position, speed * Time.deltaTime);
				r.transform.LookAt (player.transform.position);
			}
		}

	}


}
