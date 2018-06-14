using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour {

    public GameObject projectilePrefab;

    public string weaponName;    
    public bool repeatingWeapon; public float ROF = 0.1f; public float cooldown = 1f; public float fireTime;
    public float projectileSpeed;
    public float weaponDispersion = 0f;

    public enum Barrels { single, dual }
    public Barrels barrels;
    public float barrelOffset = 0f;
    bool leftSide; float firingSide = 0f;

    public AudioClip weaponAudio; public float volume;  

    ShipController owningShip;
    GameObject owningObject;

	void Start () {
        owningShip = gameObject.transform.parent.GetComponent<ShipController>();
        owningObject = gameObject.transform.parent.gameObject;
        if (owningShip) { Debug.Log("owning ship found"); }
        if (owningObject) { Debug.Log("owner found " + owningObject.name); }

        fireTime = Time.timeSinceLevelLoad;
    }

    void OnDrawGizmos()
    {
            Gizmos.DrawWireSphere(transform.position, 0.1f);
    }


    void Update () {
		
	}

    public void Fire(bool firing)
    {
        if (repeatingWeapon)
        {
            if (firing)
            {
                InvokeRepeating("FireWeapon", 0.0001f, ROF);
            }
            else
            {
                CancelInvoke("FireWeapon");
            }
        } else if(fireTime < Time.timeSinceLevelLoad - cooldown)
        {
            FireWeapon();
            fireTime = Time.timeSinceLevelLoad;
        }
    }

    void FireWeapon()
    {
        //Siwtching firing side:
        if (barrels == Barrels.dual)
        {
            if (leftSide)
            {
                firingSide = barrelOffset;
                leftSide = false;
            }
            else
            {
                firingSide = -barrelOffset;
                leftSide = true;
            }
        } 

        // setting shot dispersion:
        float dispersion = Random.Range(-weaponDispersion, weaponDispersion);

        GameObject bolt = Instantiate(projectilePrefab, gameObject.transform.position, Quaternion.identity) as GameObject;

        float newZ = gameObject.transform.parent.transform.eulerAngles.z;
        bolt.transform.eulerAngles = new Vector3(0f, 0f, newZ);

        //The Green vector
        Vector3 forwardUnit = gameObject.transform.up.normalized;
        bolt.transform.parent = gameObject.transform;
        bolt.transform.localPosition = new Vector3(firingSide, 0f, 0f);
        bolt.transform.parent = bolt.transform;

        if (bolt)
        {
            float boltX = gameObject.GetComponentInParent<Rigidbody2D>().velocity.x + forwardUnit.x * projectileSpeed;
            float boltY = gameObject.GetComponentInParent<Rigidbody2D>().velocity.y + forwardUnit.y * projectileSpeed;
            bolt.GetComponent<Rigidbody2D>().velocity = new Vector2(boltX + dispersion, boltY + dispersion);

            AudioSource.PlayClipAtPoint(weaponAudio, gameObject.transform.position, volume);
            // Debug.Log ("Fire!");
        }
    }

}

