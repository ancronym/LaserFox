using UnityEngine;
using System.Collections;

public class ShipController : MonoBehaviour {

    public enum ShipType { missile, scout, fighter, bomber, capital }
    public ShipType shipType;

	public float mainThrust = 1000f;
	public float secondaryThrust = 300f;
	public float health = 30f;
	public float rotateThrust = 15f;
    public float fuel = 1000000f, initialFuel, fuelPercentage; public bool fuelIsBingo;
    public float radarRange = 100f;
    public string IFF = "";

    public GameObject missilePrefab;
	public GameObject boltPrefab; bool leftSide = false; float firingSide; float dispersion; public float plasmaDispersion = 0.1f;
    public GameObject railPrefab;

    PIDController pidController;

    public ParticleSystem deathBlast;

	public AudioClip plasmaAudio;
    public AudioClip railgunAudio;
    public AudioClip missileLaunchAudio;
	public AudioClip playerDeathAudio;
    public AudioClip thrustAudio;
    public AudioSource audioSource;

    // Thruster controllers:
	Nozzle mainThruster;	Nozzle frontThruster1;	Nozzle frontThruster2;	Nozzle frontLeftThruster;
    Nozzle frontRightThruster;    Nozzle backLeftThruster;	Nozzle backRightThruster;

	LineRenderer headingLine;

    // Weapon parameters:
    public float plasmaSpeed = 10f;    public float railSlugSpeed = 20f;
    public float plasmaROF = 0.1f;    public float railROF = 1f;    public float missileROF = 2f;
    float railReload = 0f;    float missileReload = 0f;

    public RadarController radar;


    float desiredHeading = 0f;

    // Use this for initialization
    void Start () {

        // mainThruster = gameObject.transform.Find ("Thrusters/MainThruster").GetComponent<Nozzle> ();
        mainThruster = gameObject.transform.Find("Thrusters/MainThruster").GetComponent<Nozzle>();
        frontLeftThruster = gameObject.transform.Find ("Thrusters/FrontLeftThruster").GetComponent<Nozzle> ();
		frontRightThruster = gameObject.transform.Find ("Thrusters/FrontRightThruster").GetComponent<Nozzle> ();
		backLeftThruster = gameObject.transform.Find ("Thrusters/BackLeftThruster").GetComponent<Nozzle> ();
		backRightThruster = gameObject.transform.Find ("Thrusters/BackRightThruster").GetComponent<Nozzle> ();
		frontThruster1 = gameObject.transform.Find ("Thrusters/FrontThruster1").GetComponent<Nozzle> ();
		frontThruster2 = gameObject.transform.Find ("Thrusters/FrontThruster2").GetComponent<Nozzle> ();

        radar = gameObject.transform.Find("Radar").GetComponent<RadarController>();
        radar.pingReach = radarRange;
        radar.SetRadarOff();
        initialFuel = fuel; fuelIsBingo = false;

		pidController = gameObject.GetComponent<PIDController> ();
        if (gameObject.tag == "Vessel")
        {
            StartCoroutine(ClearSpaceAroundShip(false, 0.1f));
        }
        
    }

    IEnumerator ClearSpaceAroundShip(bool status, float delaytime) {

        yield return new WaitForSeconds(delaytime);
        SpaceClearer.ClearScenery(gameObject.transform.position, 20f);
    }

	
	// Update is called once per frame
	void Update () {
        railReload -= Time.deltaTime;
        missileReload -= Time.deltaTime;
        fuelPercentage = fuel / initialFuel * 100f;
        if(fuelPercentage < 20) { fuelIsBingo = true; }
	}

	void OnCollisionEnter2D(Collision2D collision){
		ProjectileController projectile = collision.gameObject.GetComponent<ProjectileController> ();
		if (projectile && projectile.gameObject.tag == "Projectile") {
			health -= projectile.projectileDamage;
            projectile.Hit();
        }
        // the impulse is taken from health for damage
        if (    collision.gameObject.tag == "Projectile"
            ||  collision.gameObject.tag == "Vessel"
            ||  collision.gameObject.tag == "Scenery")        {
            health -= collision.relativeVelocity.magnitude * collision.gameObject.GetComponent<Rigidbody2D>().mass / 20;
        }

		if (health <= 0f) {
			Die();
		}
        
	}

    public void FireRailgun() {
        if (railReload <= 0)
        {
            railReload = railROF;
            GameObject slug = Instantiate(railPrefab, gameObject.transform.position, Quaternion.identity) as GameObject;

            float newZ = gameObject.transform.eulerAngles.z;
            slug.transform.eulerAngles = new Vector3(0f, 0f, newZ);

            Vector3 forwardUnit = gameObject.transform.up.normalized;
            slug.transform.parent = gameObject.transform;
            slug.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            slug.transform.parent = slug.transform;

            if (slug)
            {
                float slugX = gameObject.GetComponent<Rigidbody2D>().velocity.x + forwardUnit.x * railSlugSpeed;
                float slugY = gameObject.GetComponent<Rigidbody2D>().velocity.y + forwardUnit.y * railSlugSpeed;
                slug.GetComponent<Rigidbody2D>().velocity = new Vector2(slugX, slugY);

                AudioSource.PlayClipAtPoint(railgunAudio, transform.position, 3.0f);
                // Debug.Log("Fire!");
            }
        }

    }

    public void FireMissile() {
        if (missileReload <= 0)
        {
            AudioSource.PlayClipAtPoint(missileLaunchAudio, gameObject.transform.position, 0.5f);
            missileReload = missileROF;
            GameObject firedMissile = Instantiate(missilePrefab, gameObject.transform.position, Quaternion.identity) as GameObject;
            firedMissile.GetComponent<MissileController>().parentShip = gameObject.transform;

            float newZ = gameObject.transform.eulerAngles.z;
            firedMissile.transform.eulerAngles = new Vector3(0f, 0f, newZ);
            Vector2 forwardVector = new Vector2(gameObject.transform.up.x, gameObject.transform.up.y);

            Vector2 shipVelocity = gameObject.GetComponent<Rigidbody2D>().velocity;
            firedMissile.GetComponent<Rigidbody2D>().velocity = shipVelocity + forwardVector;
            if (radar.target != "" || radar.target != null)
            {
                firedMissile.transform.Find("Radar").GetComponent<RadarController>().target = radar.target;
                // Debug.Log("Target at launch: " + radar.target);
            }
            
            firedMissile.GetComponent<ShipController>().IFF = IFF;
        }
    }

	public void FirePlasma(){
        //Siwtching firing side:
        if (leftSide) {
            firingSide = 0.3f;
            leftSide = false;
        } else
        {
            firingSide = -0.3f;
            leftSide = true;
        }

        // setting shot dispersion:
        dispersion = Random.Range(-plasmaDispersion, plasmaDispersion);

        GameObject bolt = Instantiate(boltPrefab, gameObject.transform.position,Quaternion.identity) as GameObject;

		float newZ = this.gameObject.transform.eulerAngles.z;
		bolt.transform.eulerAngles = new Vector3 (0f, 0f, newZ);

		//The Green vector
		Vector3 forwardUnit = gameObject.transform.up.normalized;
        bolt.transform.parent = gameObject.transform;        
		bolt.transform.localPosition = new Vector3 (firingSide, 1.5f, 0f);
		bolt.transform.parent = bolt.transform;

		if (bolt) {
			float boltX = gameObject.GetComponent<Rigidbody2D> ().velocity.x + forwardUnit.x * plasmaSpeed;
			float boltY = gameObject.GetComponent<Rigidbody2D> ().velocity.y + forwardUnit.y * plasmaSpeed;
			bolt.GetComponent<Rigidbody2D> ().velocity = new Vector2 (boltX + dispersion, boltY + dispersion);
		
			AudioSource.PlayClipAtPoint (plasmaAudio, transform.position, 0.4f);
			// Debug.Log ("Fire!");
		}
	}

	public void InvokePlasma(){
        InvokeRepeating("FirePlasma", 0.00000001f, plasmaROF);
    }
	public void CancelFire(){
		CancelInvoke("FirePlasma");
	}

	public void Rotate(float desiredHeading,float deltaTime){
        if (fuel <= 0f) { return; }        

        Rigidbody2D ship = gameObject.GetComponent<Rigidbody2D> ();
		float currentHeading = gameObject.transform.eulerAngles.z;

        // If the correction is too small, exit method
        if (Mathf.Abs(currentHeading - desiredHeading) < 0.01f) { return; }

		float DV = 0f;
		float CV = 0f;

		// Fixes The desired heading between 0 and 360 degrees
		if (desiredHeading < 0) {				desiredHeading = desiredHeading + 360;		}
		if (desiredHeading >= 360) {		desiredHeading = desiredHeading - 360;		}


		if (desiredHeading > currentHeading && (desiredHeading - currentHeading) > 180) {
			// right turn
			DV = -(currentHeading + (360 - desiredHeading));
		} else if (desiredHeading < currentHeading && (currentHeading - desiredHeading) > 180) {
			// left turn
			DV = (desiredHeading + (360 - currentHeading));
		}else if(desiredHeading > currentHeading){
			// left turn
			DV = desiredHeading - currentHeading;
		}else if(desiredHeading <currentHeading){
			// right turn
			DV = desiredHeading - currentHeading;
		}

		float correction = pidController.Correction(DV,CV,deltaTime);
		// Debug.Log ("Correction: " + correction);
		// Debug.Log("S.Desired: " + desiredHeading + "S.Current: " +currentHeading);

		if (correction > 0.5f) {
			backLeftThruster.EmitThrust ();
			frontRightThruster.EmitThrust ();
		} else if (correction < -0.5f) {
			backRightThruster.EmitThrust ();
			frontLeftThruster.EmitThrust ();
		}

		float thrust = Mathf.Clamp (correction, -rotateThrust, rotateThrust);
			ship.AddTorque (thrust);
        fuel -= Mathf.Abs(thrust);
	}


    // Throttle should be 0 to 1, but get's clamped anyway!
	public void ThrustForward(float throttle){        
        if (fuel <= 0f) { return; }
        throttle = Mathf.Clamp(throttle, 0f, 1f);
        float speedRatio = Time.deltaTime * mainThrust* throttle;
        this.GetComponent<Rigidbody2D>().AddRelativeForce(new Vector2 (0f,speedRatio));
        //mainThruster.EmitThrust ();
        
        fuel -= speedRatio;

        this.GetComponent<Rigidbody2D>().mass -= (speedRatio/100000);        
    }
    
	public void ThrustBackward(float throttle){
        if (fuel <= 0f) { return; }
        throttle = Mathf.Clamp(throttle, 0f, 1f);
        float speedRatio = Time.deltaTime * secondaryThrust * throttle;
		this.GetComponent<Rigidbody2D>().AddRelativeForce(new Vector2 (0f,-speedRatio));
		frontThruster1.EmitThrust ();
		frontThruster2.EmitThrust ();
        fuel -= speedRatio;

        this.GetComponent<Rigidbody2D>().mass -= (speedRatio / 100000);
    }

    public void ThrustLeft(float throttle){
        if (fuel <= 0f) { return; }
        throttle = Mathf.Clamp(throttle, 0f, 1f);

        float speedRatio = Time.deltaTime * secondaryThrust * throttle;
		this.GetComponent<Rigidbody2D>().AddRelativeForce (new Vector2 (-speedRatio,0f));
		backRightThruster.EmitThrust ();
		frontRightThruster.EmitThrust ();
        fuel -= speedRatio;

        this.GetComponent<Rigidbody2D>().mass -= (speedRatio / 100000);
    }

    public void ThrustRight(float throttle){
        if (fuel <= 0f) { return; }

        throttle = Mathf.Clamp(throttle, 0f, 1f);
        float speedRatio = Time.deltaTime * secondaryThrust * throttle;
		this.GetComponent<Rigidbody2D>().AddRelativeForce(new Vector2(speedRatio,0f));
		backLeftThruster.EmitThrust ();
		frontLeftThruster.EmitThrust ();
        fuel -= speedRatio;

        this.GetComponent<Rigidbody2D>().mass -= (speedRatio / 100000);
    }

    void Die(){
		AudioSource.PlayClipAtPoint(playerDeathAudio,transform.position,0.8f);
		Instantiate (deathBlast, gameObject.transform.position, Quaternion.identity);
        // Destroy(gameObject);

        if (gameObject.GetComponent<PlayerController>())
        {
            gameObject.GetComponent<PlayerController>().Death();
        }
        else if (gameObject.GetComponent<VesselAI>())
        {
            gameObject.GetComponent<VesselAI>().Death();
            Destroy(gameObject, 0.2f);
        }        
        else {
            Destroy(gameObject, 0.1f);
        }
	}

    public void SelfDestruct() {
        Die();
    }

    public void ToggleRadar() {
        radar.ToggleRadar();
    }

    public void SetTarget() {
        radar.GetNearestTarget();
    }

    public RadarController.RadarState GetRadarState() {
        return radar.radarState;
    }
}
