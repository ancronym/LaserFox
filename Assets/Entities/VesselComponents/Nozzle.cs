using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nozzle : MonoBehaviour {

	GameObject thrustEffect;
    bool thrustOn;

	void Start(){
		
        thrustEffect = gameObject.transform.Find("ThrustEffect").gameObject;
        

        if (thrustEffect) {
            thrustEffect.SetActive(false);
            Debug.Log("thrust effect found: " + thrustEffect.name);
        }

	}

	public void EmitThrust(){      

        if (!thrustOn) {
            thrustOn = true;
            thrustEffect.SetActive(true);
        }
        
	}

    public void StopThrust()
    {
        if (thrustOn)
        {
            thrustOn = false;
           thrustEffect.SetActive(false);
        }
    }
}
