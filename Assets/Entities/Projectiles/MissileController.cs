using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileController : MonoBehaviour {

    
    GameObject targetObject;
    public float armingDistance = 2f;
    bool armed;
    float distanceToOwner = 0f;
    public float initialSpeed = 4f;
    public float initialFuel = 1000f;
    public float constantAcceleration = 1f;

    // missile guidance parameters
    float desiredHeading;
    float mass;
    Vector2 desiredVector;


    enum MissileState { locked, roaming};
    MissileState missileState;

    ShipController missile;

    // this is set by the launching vessel:
    public Transform parentShip;

    public AudioClip missileArmed;
    bool invokeSet = false;

	// Use this for initialization
	void Start () {
        armed = false;
        desiredHeading = parentShip.transform.eulerAngles.z;
        mass = gameObject.GetComponent<Rigidbody2D>().mass;
        gameObject.GetComponent<BoxCollider2D>().enabled = false;
        missile = GetComponent<ShipController>();
        missile.fuel = initialFuel;
        if (missile.radar.target == "") { missileState = MissileState.roaming; }
        else if (missile.radar.target != "") { targetObject = GameObject.Find(missile.radar.target); }

        // Debug.Log("Parent: " + parentShip.name);
        // Debug.Log("targeT: " + targetObject.name);
    }
	
	// Update is called once per frame
	void Update () {
        // -------------------------- DISARMED ------------------------- //
        if (!armed) {
            if (parentShip) {
                distanceToOwner = Vector2.Distance(gameObject.transform.position, parentShip.position);
                if (distanceToOwner > armingDistance)
                {
                    armed = true;

                    // Debug.Log("Missile Armed.");
                    AudioSource.PlayClipAtPoint(missileArmed, gameObject.transform.position, 0.8f);

                                                         
                    gameObject.GetComponent<BoxCollider2D>().enabled = true;
                    if (!targetObject)
                    {
                        ExecuteThrust(initialSpeed);
                        missileState = MissileState.roaming;                        
                    }
                    else {
                       // InitialLockedManeuver();
                        missileState = MissileState.locked;                        
                    }                    
                }
            } else { armed = true; }
            
        // -------------------------- ARMED ------------------------- //

        } else if (armed) {
         
            missile.Rotate(desiredHeading, Time.deltaTime);
            LateralCorrection();

            // Debug.Log(" Desired heading: " + desiredHeading);

            if (!invokeSet)
            {
                switch (missileState)
                {
                    case MissileState.locked:
                        InvokeRepeating("Pursue", 0.0000f, 0.2f);
                        
                        invokeSet = true;
                        break;

                    case MissileState.roaming:
                        InvokeRepeating("Roam", 0.0000f, 1f);
                        missile.radar.SetRadarWide();
                        invokeSet = true;
                        break;
                }
            }

        }
        // Debug.Log("Missile fuel: " + missile.fuel);

        if (missile.fuel < 0) {
            Invoke("missile.SelfDestruct",1f);
        }

        missile.ThrustForward(constantAcceleration);
    }

    void InitialLockedManeuver() {
        Transform targetTransform = targetObject.transform;
        desiredHeading = RouteAndManeuverPlanner.GetInterceptHeading(targetTransform, gameObject.transform, initialSpeed);

        // ExecuteThrust(initialSpeed);        
    }

    void LateralCorrection() {
        Vector3 velocity = new Vector3(gameObject.GetComponent<Rigidbody2D>().velocity.x, gameObject.GetComponent<Rigidbody2D>().velocity.y, 0f);
        Vector3 crossProduct = Vector3.Cross(gameObject.transform.up, velocity);
        // Debug.Log("Cross: " + crossProduct.z);

        
        if (crossProduct.z > 0.1f)
        {
            missile.ThrustRight(1f);
        }
        else if (crossProduct.z < -0.1f)
        {
            missile.ThrustLeft(1f);
        }
        
    
    }

    void Roam() {        
        missile.radar.GetNearestTarget();

        if (missile.radar.target != "" && missile.radar.target != null) {
            targetObject = GameObject.Find(missile.radar.target);
            if (targetObject.GetComponent<ShipController>()) {
                if (targetObject.GetComponent<ShipController>().IFF != missile.IFF) {
                    missileState = MissileState.locked;
                    CancelInvoke("Roam");
                    invokeSet = false;
                }
                
            }
            
        }
    }

    void Pursue(){
        if (targetObject)
        {
            float distanceToTarget = Vector2.Distance(new Vector2(gameObject.transform.position.x, gameObject.transform.position.y), new Vector2(targetObject.transform.position.x, targetObject.transform.position.y));

            // Switch to Roaming mode if target is out of radar range
            if (distanceToTarget > missile.radar.pingReach)
            {
                missileState = MissileState.roaming;
                CancelInvoke("Pursue");
                invokeSet = false;
                return;
            }

            Vector2 killBurn = RouteAndManeuverPlanner.GetHitBurn(targetObject.transform, gameObject.transform, missile.fuel, mass);
            desiredHeading = killBurn.x;
            if (Mathf.Abs(desiredHeading - missile.transform.eulerAngles.z) < 0.5f)
            {
                // ExecuteThrust(killBurn.y);
            }
        }
        
    }

    void ExecuteThrust(float dV) {
        float targetFuel = missile.fuel - dV * gameObject.GetComponent<Rigidbody2D>().mass;

        do{            
            missile.ThrustForward(0.1f);
            if (Input.GetKeyDown(KeyCode.Escape)) { return; }
        } while (missile.fuel > targetFuel);

    }

    void CorrectionManeuver(){


    }

    

}
