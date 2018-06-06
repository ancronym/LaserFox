using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nozzle : MonoBehaviour {

	ParticleSystem thruster;
	ParticleSystem.EmitParams emitParams;    

	void Start(){
		thruster = gameObject.GetComponent<ParticleSystem> ();        
		thruster.Emit (emitParams,1);
        
	}

	public void EmitThrust(){
		thruster.Emit (emitParams,1);
        
	}
}
