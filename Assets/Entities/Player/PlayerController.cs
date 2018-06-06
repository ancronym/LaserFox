using UnityEngine;
using System.Collections;
using UnityEngine.UI;   

public class PlayerController : MonoBehaviour {

	// TODO find out why this is negative!
	public float rotationSpeed = 0.1f;
    
    string target = "";

    enum Weapon { plasma, railgun, missile};

    Weapon selectedWeapon = Weapon.plasma;

    ShipController ship;
	LineRenderer velocityVector;
    FiringController fc;
    WeaponTextScript weaponText;
    FuelText fuelText;
    UIRadarText radarText;
    RadarController.RadarState radarState;

    float desiredHeading =0f;
    public float zoomSpeed = 5f;
    public float initialZoom = 10f;   

    
    bool alive;

    // Target indication 
    bool targetIndicatorEnabled;
    public GameObject targetIndicatorPrefab;
    GameObject targetIndicator;
    public Camera camera;

    // ----------- UI STUFF ---------------
    public Slider fuelSlider;
    public Slider healthSlider; float initialHealth;
    GameObject menuCanvas;
    LevelManager levelManager;


    void Start () {
        // TODO figure out a better way to do this
        LevelManager.isPaused = false;
        alive = true;

        ship = gameObject.GetComponentInParent<ShipController>();
        targetIndicatorEnabled = false;
        velocityVector = gameObject.transform.Find("VelocityVector").GetComponent<LineRenderer>();
        fc = gameObject.GetComponent<FiringController>();
        ship.IFF = "green";

        menuCanvas = GameObject.Find("MenuCanvas");
        Debug.Log("Canvas found with name: " + menuCanvas.name);
        menuCanvas.SetActive(false);
        levelManager = GameObject.Find("LevelManager").GetComponent<LevelManager>();

        weaponText = GameObject.Find("UIWeaponText").GetComponent<WeaponTextScript>();
        radarText = GameObject.Find("UIRadarText").GetComponent<UIRadarText>();
        fuelSlider = GameObject.Find("FuelSlider").GetComponent<Slider>();
        healthSlider = GameObject.Find("HealthSlider").GetComponent<Slider>();
        initialHealth = ship.health;

        selectedWeapon = Weapon.plasma;
        weaponText.SetUIWeapontext("plasma");
        SetCursorGame();

        float distance = transform.position.z - Camera.main.transform.position.z;        
        camera.orthographicSize = initialZoom;

        InvokeRepeating("UpdateUI", 0.00001f, 0.5f);

    }
	
	// Update is called once per frame
	void Update () {

        if (Input.GetKey(KeyCode.Escape) && !LevelManager.isPaused) { levelManager.PauseGame(menuCanvas); }

        if (alive)
        {
            if (!LevelManager.isPaused)
            {
                desiredHeading += Input.GetAxis("Horizontal") * rotationSpeed;
                float deltaTime = Time.deltaTime;
                ship.Rotate(desiredHeading, deltaTime);

                camera.orthographicSize -= Input.GetAxis("MouseScrollWheel") * zoomSpeed;
                camera.orthographicSize = Mathf.Clamp(camera.orthographicSize, 5f, 50f);
                camera.transform.localPosition = new Vector3(
                    0,
                    camera.orthographicSize * 0.5f,
                    -10f
                    );                

                if (Input.GetKey(KeyCode.W)) { ship.ThrustForward(1f); }
                if (Input.GetKey(KeyCode.A)) { ship.ThrustLeft(1f); }
                if (Input.GetKey(KeyCode.S)) { ship.ThrustBackward(1f); }
                if (Input.GetKey(KeyCode.D)) { ship.ThrustRight(1f); }
                //if (Input.GetKey(KeyCode.T)) {                      fc.GetBoresightTarget(); }

                if (Input.GetKeyDown(KeyCode.Mouse0)) { FireWeapon(); }
                if (Input.GetKeyUp(KeyCode.Mouse0)) { ship.CancelFire(); }
                if (Input.GetKeyDown(KeyCode.Mouse1)) { SelectNextWeapon(); }

                if (Input.GetKeyDown(KeyCode.R)) { ship.ToggleRadar(); SetRadarText(); }
                if (Input.GetKeyDown(KeyCode.T)) { if (ship.radar.radarState != RadarController.RadarState.off) { ship.SetTarget(); } }
            }

            
            UpdateVelocityVector();
            UpdateTargetIndicator();
        }

	}

    void UpdateUI() {        
        SetFuelText();
    }

    void SelectNextWeapon() {
        switch (selectedWeapon)
        {
            case Weapon.plasma:
                selectedWeapon = Weapon.railgun;
                Debug.Log("railgun");
                weaponText.SetUIWeapontext("railgun");
                break;
            case Weapon.missile:
                Debug.Log("plasma");
                selectedWeapon = Weapon.plasma;
                weaponText.SetUIWeapontext("plasma");
                break;
            case Weapon.railgun:
                Debug.Log("missile");
                weaponText.SetUIWeapontext("missile");
                selectedWeapon = Weapon.missile;
                break;
            default:
                selectedWeapon = Weapon.plasma;
                break;
        }


    }

    void FireWeapon() {

        switch (selectedWeapon) {
            case Weapon.plasma:
                ship.InvokePlasma();
                break;
            case Weapon.railgun:
                ship.FireRailgun();
                break;

            case Weapon.missile:
                ship.FireMissile();
                break;
            default:

                break;
        }
    }

    /* 
     * ----------------------   UI STUFF -------------------------------------------- 
     */

	void SetCursorGame(){
        Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

	}
	void SetCursorUI(){
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
	}

    void UpdateTargetIndicator() {        
        if (ship.radar.target != "" && ship.radar.target != null)
        {
            if (targetIndicatorEnabled == false)
            {
                targetIndicator = Instantiate(
                    targetIndicatorPrefab,
                    GameObject.Find(ship.radar.target).transform.position,
                    Quaternion.identity
                    ) as GameObject;
                targetIndicatorEnabled = true;
            }else if (GameObject.Find(ship.radar.target) && targetIndicator)
            {
                targetIndicator.transform.parent = GameObject.Find(ship.radar.target).transform;
            }


            // Debug.Log("TI pos: " + targetIndicator.transform.parent + "Tgt Pos: " + GameObject.Find(ship.radar.target).transform.position); 
        }
        else
        {
            if (targetIndicatorEnabled == true)
            {
                Destroy(targetIndicator);
                targetIndicatorEnabled = false;
            }
        }        
        
    }

    void SetRadarText() {
        radarState = ship.GetRadarState();
        switch (radarState)
        {
            case RadarController.RadarState.off:
                radarText.SetUIRadarText("off");
                break;

            case RadarController.RadarState.wide:
                radarText.SetUIRadarText("wide");
                break;

            case RadarController.RadarState.narrow:
                radarText.SetUIRadarText("narrow");
                break;

        }
    }

    void SetFuelText()    {
        fuelSlider.value = ship.fuel / ship.initialFuel * 100f;
        healthSlider.value = ship.health / initialHealth * 100f;
    }


    public void Death(){
		SetCursorUI();
        
        gameObject.GetComponent<PolygonCollider2D>().enabled = false;
        // gameObject.GetComponent<SpriteRenderer>().enabled = false;
        gameObject.transform.Find("VelocityVector").GetComponent<LineRenderer>().enabled = false;

        alive = false;

        
		// FindObjectOfType<LevelManager> ().LoadLevel ("Win");

	}    



    void UpdateVelocityVector(){
		Vector2 velocity = gameObject.GetComponent<Rigidbody2D> ().velocity;
		Vector3 position = gameObject.transform.position;
		Vector3 lineStart = new Vector3 (position.x +velocity.normalized.x, position.y + velocity.normalized.y, position.z);
		Vector3 lineEnd = new Vector3 (position.x + velocity.x * 2,position.y+ velocity.y * 2, position.z);
			
		velocityVector.SetPosition (0, lineStart);
		velocityVector.SetPosition (1, lineEnd);
	}
}


