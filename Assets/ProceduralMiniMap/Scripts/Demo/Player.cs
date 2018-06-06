using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

	public float speed,rotateSpeed;


	void Update () {
			transform.position += transform.forward * Input.GetAxis("Vertical")*speed*Time.deltaTime;
		transform.Rotate (0,Input.GetAxis("Horizontal")*rotateSpeed*Time.deltaTime,0);
	}


}
