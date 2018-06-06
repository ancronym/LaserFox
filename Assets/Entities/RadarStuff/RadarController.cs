using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadarController : MonoBehaviour {

    public float radarScanRate = 0.5f;
    public float narrowBeamMultiplier = 1.1f;
    public float wideBeamMultiplier = 2f;
    public float pingReach = 50f;
    public string target = "";
    
    public enum RadarState { off, narrow, wide};
    public RadarState radarState;

    public GameObject pingPrefab;
    public CapsuleCollider2D radarCollider;    

    public struct Bogie
    {
        public GameObject bogieObject;
        public float timeOfContact;
    }

    public List<Bogie> radarBogies;

    //--- Soon obsolete


    // Use this for initialization
    void Start () {        
        SetRadarOff();
        target = "";
    }

    void Update() {
        if (target != "" && target != null)
        {
            // Debug.Log("Target: " + target);
            if (!GameObject.Find(target))
            {
                target = "";
            }
            else { 
                if (Vector3.Distance(gameObject.transform.position, GameObject.Find(target).transform.position) > pingReach)
                {
                    target = "";
                }
            }
           
        }
       // Debug.Log(target);
    }
	
	public string GetNearestTarget()
    {
        ping();

        return target;
    }

    public string GetBoresightTarget()
    {
        string targetName = "";

        return targetName;
    }

    public void PingResult(string targetName, string targetTag) {
        if (targetTag == "Vessel")
        {
            target = targetName;
        }
    }

    void ping() {
        target = "";

        Vector3 forward = gameObject.transform.up.normalized;
        Vector3 position = gameObject.transform.position;
        Vector3 pingStart = new Vector3(position.x + 2*forward.x,position.y + 2*forward.y,0f);
        GameObject ping = Instantiate(pingPrefab, pingStart, Quaternion.identity) as GameObject;
        ping.GetComponent<PingScript>().parentRadar = gameObject;
        ping.GetComponent<PingScript>().pingReach = pingReach;
        ping.transform.parent = ping.transform;

        switch (radarState) {
            case RadarState.narrow:
                ping.GetComponent<PingScript>().beamWidth = narrowBeamMultiplier;
                break;

            case RadarState.wide:
                ping.GetComponent<PingScript>().beamWidth = wideBeamMultiplier;
                break;
        }
    }

    public void ToggleRadar() {
        switch (radarState) {
            case RadarState.off:
                SetRadarWide();
                break;
            case RadarState.narrow:
                SetRadarOff();
                break;
            case RadarState.wide:
                SetRadarOff();
                break;
        }
    }

    public void ToggleRadarWidth(){
        switch (radarState){
            case RadarState.off:
                SetRadarWide();
                break;
            case RadarState.narrow:
                SetRadarWide();
                break;
            case RadarState.wide:
                SetRadarNarrow();
                break;
        }
    }

    public void SetRadarNarrow(){
        radarState = RadarState.narrow;
    }

    public void SetRadarWide() {
        radarState = RadarState.wide;
    }

    public void SetRadarOff() {
        radarState = RadarState.off;
    }

    public void SetRadarOn(){
        radarState = RadarState.wide;
    }

}
