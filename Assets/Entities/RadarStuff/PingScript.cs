using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PingScript : MonoBehaviour {

    float currentPosition;
    float newPosition;
    float distanceTravelled;
    string targetName;
    string targetTag;

    public GameObject parentRadar;
    public float pingSpeed = 100f;

    //This comes all the way from the ship controller
    public float pingReach;
    public float beamWidth = 1f;
    LineRenderer pingLine;
    bool drawPing = true;

    // Use this for initialization
    void Start () {
        float newZ = parentRadar.transform.eulerAngles.z;
        gameObject.transform.eulerAngles = new Vector3(0f, 0f, newZ);

        Vector3 forwardUnitVector = gameObject.transform.up.normalized;
        float newX = forwardUnitVector.x * pingSpeed;
        float newY = forwardUnitVector.y * pingSpeed;
                
        gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(newX, newY);
        // Debug.Log("Ping parent: " + parentRadar.name);
        pingLine = gameObject.GetComponent<LineRenderer>();

        if (drawPing){
            pingLine.enabled = true;
        }
        else {
            pingLine.enabled = false;
        }

        targetName = "";
        targetTag = "";
	}

    private void Update()
    {
        if (!parentRadar) { Destroy(gameObject); }
        distanceTravelled = Vector2.Distance(parentRadar.transform.position, gameObject.transform.position);
        gameObject.transform.localScale = new Vector3(distanceTravelled * beamWidth, 0.5f, 1f);
        
        if (drawPing)
        {
            DrawPing();
        }

        if ( distanceTravelled > pingReach) {
            // If the ping times out and does not hit anything it will return the "" values for target name and tag
            parentRadar.GetComponent<RadarController>().PingResult(targetName, targetTag);
            Destroy(gameObject);
        }
    }

    // This will message the parent radar and pass the hit results GO name and tag
    void OnTriggerEnter2D(Collider2D collision)
    {
        targetName = collision.gameObject.name;
        // Debug.Log("Ping result: " + collision.name);
        targetTag = collision.gameObject.tag;
        parentRadar.GetComponent<RadarController>().PingResult(targetName, targetTag);
        Destroy(gameObject);
        
    }

    void DrawPing() {
        Vector3 rightVector = gameObject.transform.right.normalized;
        Vector3 position = gameObject.transform.position;
        Vector3 lineStart = new Vector3(position.x + rightVector.x, position.y + rightVector.y, 0f);
        Vector3 lineEnd = new Vector3(position.x - rightVector.x, position.y - rightVector.y, 0f); ;
            
        pingLine.SetPosition(0,lineStart);
        pingLine.SetPosition(1,lineEnd);
    }
}
