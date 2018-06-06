using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UnityEngine.UI;

public class VesselAI : MonoBehaviour {
    // ---------- ----------
    #region Debugging stuff    
    LineRenderer waypointLine;
    LineRenderer waypointLine2;
    public GameObject DebugLine2D;
    public TextMesh statusText;
    #endregion
    //-------------------------

    // -------------------------
    #region Ship and other gameObject stuff
    ShipController ship;
    LineRenderer velocityVector;
    

    #endregion
    // -------------------------

    // --------- AI parameters, use values from 0.0f to 1.0f
    float accuracy = 0.5f;
    float decisionLag = 0.3f;
    float agression = 0.5f;
    float wit = 0.5f;
    float repeatingCheckRate = 0.2f;
    // -----------------------------------------------

    /* -----------------------------------------------
     * The waypoint class instance stores a Vector2 coordinates, a float radius and a movingWp bool
     * Usually only use of 3 waypoints is planned, but a ten slot list has buffer for objectavoidance waypoints
     ----------------------------------------------- */
    public List<NTools.Waypoint> wpList = new List<NTools.Waypoint>(10);
    int wpIndex;


    // -------------------------------
    #region Movement and collision avoidance stuff
    float headingToWaypoint;    
    float desiredHeading;
    float desiredSpeed;

    // Between 0f and 0.3 f, will be added to final thrust command to ship
    float rightTCASThrust = 0f; float leftTCASThrust = 0f; float forwardTCASThrust = 0f; float backwardsTCASThrust = 0f;
    float lateralCorrectionThrust = 1f;   
    public float patrolSpeedMax = 5f, engagementSpeedMax = 10f, emergencySpeedMax = 1000f;
    float intendedThrust = 0f; float generalThrust;  float patrolThrust = 0.3f; float dangerThrust = 0.7f;
    
    
    public List<NTools.CollisionThreat> collisionThreatList = new List<NTools.CollisionThreat>(50);
    public List<NTools.CollisionThreat> TCASThreatList = new List<NTools.CollisionThreat>(10);

    Vector2 TCASthreatPosition; Vector2 TCASthreatVelocity; // in order to save creating new ones in TCASCheck
    Vector2 collisonThreatPosition; Vector2 collisionThreatVelocity; // in order to save creating new ones in collisionAvoidance

    public float minSeparation = 1.2f; // Taken into account in CollisionAvoidance()    

    CircleCollider2D visionCollider;
    CapsuleCollider2D tcas;
    LayerMask TCASLayerMask;
    public string[] TCASLayers;
    #endregion
    

    // -----------------------------------
    #region Communication with TeamManager    
    public TeamManager.TeamSide teamSide;
    public TeamManager teamManager;

    #endregion
    // -----------------------------------

    
    #region AI local states and parameters
    public enum StatusInFormation { lead, Position2, Position3, Position4};
    public StatusInFormation statusInFormation;

    public enum WingmanState { inFormation, followingOrder };
    public WingmanState wingmanState = WingmanState.inFormation;

    public enum FlightStatus { patrolling, intercepting, sentry, engaging, retreating};
    public FlightStatus flightStatus;
    bool flightStatusIsChanging; // used to check if resetting movement parameters etc is required

    // Loiter parameters
    float loiterTime = 3f;    float loiterStart;    bool loiterOngoing = false;

    #endregion

    // --------------------------------------
    #region Fighting Parameters and info
    public enum EngagementStatus { rushing, evadingFiring, evading };
    EngagementStatus engagementStatus;

    public enum EngagementPattern { headOn, bracket, flank };
    EngagementPattern engagementPattern;

    public List<RadarController.Bogie> formationBogies = new List<RadarController.Bogie>(50);

    #endregion
    

    // --------------------------------
    #region FORMATION STUFF 
    public GameObject formationLead;
    public GameObject objectToDefend;
    public GameObject[] wingmen = new GameObject[3];
    PIDController pIDControllerX;
    PIDController pIDControllerY;

    
    #endregion
    /* ------------------------------------------------------------------------------------------------------------------------------- */

    // Use this for initialization
    void Start () {        
        ship = gameObject.GetComponent<ShipController>();   
        
        velocityVector = gameObject.transform.Find("VelocityVector").GetComponent<LineRenderer>();
        waypointLine = gameObject.transform.Find("WaypointLine").GetComponent<LineRenderer>();
        waypointLine2 = gameObject.transform.Find("WaypointLine2").GetComponent<LineRenderer>();
        visionCollider = gameObject.transform.Find("VisionField").GetComponent<CircleCollider2D>();

        tcas = gameObject.transform.Find("TCAS").GetComponent<CapsuleCollider2D>();
        TCASLayerMask = LayerMask.GetMask(TCASLayers);

        pIDControllerX = new PIDController();
        pIDControllerY = new PIDController();
        pIDControllerX.pGain = 0.15f; pIDControllerX.iGain = 0.15f; pIDControllerX.dGain = 0.15f;
        pIDControllerY.pGain = 0.15f; pIDControllerY.iGain = 0.15f; pIDControllerY.dGain = 0.15f;
        
        ClearWaypoints();
        
        //Debug.Log("My name is: " + gameObject.name);
        
        desiredSpeed = patrolSpeedMax;
        generalThrust = patrolThrust;      

        // Calculating AI parameters based on the difficulty float which, ranges from 0 to 2.
        // maximum accuraccy is 1 and minimum 0
        accuracy = MissionMaker.difficulty / 2;                     // higher is more accurate, values: 1 - 0
        decisionLag = 0.6f - (MissionMaker.difficulty / 4);        // lower is better, values: 0,1f - 0,6f. Transaltes to seconds
        agression = MissionMaker.difficulty / 2;                  // lower is wussier, values: 1 - 0

        InvokeRepeating("RepeatingChecks", 0.5f, (repeatingCheckRate + decisionLag + UnityEngine.Random.Range(-0.1f,0.1f)));

        InvokeRepeating("TCASCollisionCheck", 0.4f, decisionLag + UnityEngine.Random.Range(-0.1f, 0.1f));      
        
    }

    // Update is called once per frame
    void Update() {
        switch (statusInFormation) {
            case StatusInFormation.lead:
                ExecuteBehaviour(flightStatus);
                break;

            case StatusInFormation.Position2:
                if (wingmanState == WingmanState.inFormation)
                {

                    //statusText.text = "In Formation";                    
                    MaintainFormation(statusInFormation.ToString());
                }
                else if (wingmanState == WingmanState.followingOrder) {
                    ExecuteBehaviour(flightStatus);
                }
                break;
            case StatusInFormation.Position3:
                if (wingmanState == WingmanState.inFormation)
                {
                    
                    //statusText.text = "In Formation";
                    MaintainFormation(statusInFormation.ToString());
                }
                else if (wingmanState == WingmanState.followingOrder)
                {                    
                    ExecuteBehaviour(flightStatus);
                }
                break;
            case StatusInFormation.Position4:
                if (wingmanState == WingmanState.inFormation)
                {
                    
                    //statusText.text = "In Formation";
                    MaintainFormation(statusInFormation.ToString());
                }
                else if (wingmanState == WingmanState.followingOrder)
                {
                    ExecuteBehaviour(flightStatus);
                }
                break;
        }

        // statusText.text = NTools.HeadingFromVector(gameObject.GetComponent<Rigidbody2D>().velocity).ToString();

        UpdateVelocityVectorAndTCAS();
        UpdateWaypointLine();        
    }

    public LFAI.FormationReport ReportRequest()
    {
        bool hasWP = (wpList.Count != 0);        
        int flightSize = 1;
        for(int i = 0; i <=2; i++)
        {
            if (wingmen[i])
            {
                flightSize++;
            }
        }

        return new LFAI.FormationReport(
            hasWP,
            formationBogies.Count,
            flightSize,
            ship.fuelIsBingo,
            ship.fuelPercentage
            );
    }

    public bool OrderReception(LFAI.Order order)
    {
        switch (order.orderType)
        {
            case LFAI.OrderType.patrol:
                if(ship.radar.radarBogies.Count == 0 && !ship.fuelIsBingo)
                {
                    flightStatus = FlightStatus.patrolling;
                } else {
                    return false;
                }
                break;
            case LFAI.OrderType.intercept:
                if (ship.radar.radarBogies.Count == 0 && !ship.fuelIsBingo)
                {
                    flightStatus = FlightStatus.intercepting;
                }
                else
                {
                    return false;
                }

                break;
            case LFAI.OrderType.moveToDefend:
                if (ship.radar.radarBogies.Count == 0)
                {
                    flightStatus = FlightStatus.sentry;
                    
                }
                else
                {
                    return false;
                }

                break;
            case LFAI.OrderType.regroup:
                if (flightStatus != FlightStatus.retreating)
                {
                    flightStatusIsChanging = true;
                    flightStatus = FlightStatus.retreating;
                    
                }
                break;
        }
        
        return true;
    }

   
    // Works quite well
    void Move() {
        desiredHeading = headingToWaypoint;

        ship.Rotate(desiredHeading, Time.deltaTime);

        // Lateral correction neccessity determination
        Vector3 crossProduct = Vector3.Cross(
            gameObject.transform.up,
            new Vector3(gameObject.GetComponent<Rigidbody2D>().velocity.x, gameObject.GetComponent<Rigidbody2D>().velocity.y, 0f)
            );

        // Check for TCAS, if not significant, correct left or right drift
        if (rightTCASThrust < 0.05f && leftTCASThrust < 0.05f)
        {
            if (crossProduct.z > 0.1f)
            {
                ship.ThrustRight(lateralCorrectionThrust);
            }
            else if (crossProduct.z < -0.1f)
            {
                ship.ThrustLeft(lateralCorrectionThrust);
            }           
        } else if ( rightTCASThrust > leftTCASThrust)        {
            ship.ThrustRight(rightTCASThrust);
        } else if (leftTCASThrust > rightTCASThrust)        {
            ship.ThrustLeft(leftTCASThrust);
        }

        // Speed correction check - should I speed up or slow down
        if (Mathf.Abs(desiredHeading - gameObject.transform.eulerAngles.z) < 5f)
        {
            // check if the ship is not travelling backwards
            if (transform.InverseTransformVector(gameObject.GetComponent<Rigidbody2D>().velocity).y > 0)
            {
                if (gameObject.GetComponent<Rigidbody2D>().velocity.magnitude < desiredSpeed)
                {
                    intendedThrust = generalThrust;
                }
                else if (gameObject.GetComponent<Rigidbody2D>().velocity.magnitude > (desiredSpeed + 1f))
                {
                    intendedThrust = -generalThrust;
                }
                else { intendedThrust = 0f; }
            } else {
                intendedThrust = generalThrust; // the ship is travelling backwards, correct
            }
        }

        // Check for TCAS, if not significant, (de)accelerate to intended cruising speed
        if (forwardTCASThrust < 0.05f && backwardsTCASThrust < 0.05f)
        {
            if (intendedThrust < 0f)
            {
                intendedThrust = Mathf.Abs(intendedThrust);
                ship.ThrustBackward(intendedThrust);
            }
            else if (intendedThrust > 0f)
            {
                intendedThrust = Mathf.Abs(intendedThrust);
                ship.ThrustForward(intendedThrust);
            }
        } else if (forwardTCASThrust > backwardsTCASThrust)
        {
            ship.ThrustForward(forwardTCASThrust);
        } else if (backwardsTCASThrust > forwardTCASThrust)
        {
            ship.ThrustBackward(backwardsTCASThrust);
        } else { }

        if (SettingsStatic.debugEnabled) { statusText.text = "Fuel: " + ship.fuelPercentage; }
        
    }

    // Works pretty well
    void MaintainFormation(string pos)
    {
        if (!formationLead) { return; }
               

        bool movingWP = true;
        bool temporary = false;
        float wpRadius = 1.5f;

        GameObject positionObject = formationLead.transform.Find("Formation4(Clone)/" + pos).gameObject;
        Vector3 posCoords = positionObject.transform.position;


        Vector2 wpCoordinates = new Vector2(posCoords.x, posCoords.y);

        NTools.Waypoint formationWP = new NTools.Waypoint(wpCoordinates, wpRadius, movingWP, temporary);
        if (wpList.Count == 0)
        {
            wpList.Add(formationWP);
        } else if (wpList.Count > 1)
        {
            ClearWaypoints();
            wpList.Add(formationWP);
        }
        else
        {
            wpList[0].wpCoordinates = formationWP.wpCoordinates;
        }

        // Checks if wingman is on position
        // if so, applies position keeping lateral thrust and maintains heading of lead
        // TCAS override implemented
        //float distanceToPos = Mathf.Sqrt(sqrDistanceToPos);

        Vector3 wpLocalCoords = gameObject.transform.InverseTransformPoint(posCoords);
        float sqrDistanceToPos = Vector2.SqrMagnitude(wpLocalCoords);

        if (wpLocalCoords.x < wpList[wpIndex].wpRadius*3f && wpLocalCoords.y < wpList[wpIndex].wpRadius*3f)
        {            
            FormationPositionCorrection(wpLocalCoords);


        } /*else if (wpLocalCoords.y > 0f && wpLocalCoords.y < 4f && Mathf.Abs(wpCoordinates.x) > wpList[wpIndex].wpRadius)
        {
            FormationPositionCorrection(wpLocalCoords);
        }*/
        else
        {
            // ------ Speed adjustment, based on distance from position in formation
            if (sqrDistanceToPos < 121f) { desiredSpeed = patrolSpeedMax + 0.5f; }
            if (sqrDistanceToPos > 121f && sqrDistanceToPos < 400f) { desiredSpeed = patrolSpeedMax + 1f; }
            if (sqrDistanceToPos > 400f) { desiredSpeed = patrolSpeedMax + 3f; }

            desiredHeading = headingToWaypoint;
            Move(); // If the Wingman is off position, regular move is used.
        }

        
    }

    // Quite ok, needs PID tuning
    void FormationPositionCorrection(Vector3 wpLocalCoords)
    {
        desiredHeading = formationLead.transform.eulerAngles.z;
        ship.Rotate(desiredHeading, Time.deltaTime);

        float xCorrection = pIDControllerX.Correction(0f, wpLocalCoords.x, Time.deltaTime);
        float yCorrection = pIDControllerY.Correction(0f, wpLocalCoords.y, Time.deltaTime);

        if (xCorrection > 0.02f && rightTCASThrust < 0.01f) { ship.ThrustLeft(Mathf.Abs(xCorrection)); }
        else { ship.ThrustRight(rightTCASThrust); }

        if (xCorrection < -0.02f && leftTCASThrust < 0.01f) { ship.ThrustRight(Mathf.Abs(xCorrection)); }
        else { ship.ThrustLeft(leftTCASThrust); }

        if (yCorrection > 0.02f && forwardTCASThrust < 0.01f) { ship.ThrustBackward(Mathf.Abs(yCorrection)); }
        else { ship.ThrustBackward(backwardsTCASThrust); }

        if (yCorrection < -0.02f && backwardsTCASThrust < 0.01f) { ship.ThrustForward(Mathf.Abs(yCorrection)); }
        else { ship.ThrustForward(forwardTCASThrust); }
    }

    void Stop() {
        // Lateral correction neccessity determination
        Vector3 crossProduct = Vector3.Cross(
            gameObject.transform.up,
            new Vector3(gameObject.GetComponent<Rigidbody2D>().velocity.x, gameObject.GetComponent<Rigidbody2D>().velocity.y, 0f)
            );

        // Check for TCAS, if not significant, correct left or right drift
        if (rightTCASThrust < 0.05f && leftTCASThrust < 0.05f)
        {
            if (crossProduct.z > 0.1f)
            {
                ship.ThrustRight(lateralCorrectionThrust);
            }
            else if (crossProduct.z < -0.1f)
            {
                ship.ThrustLeft(lateralCorrectionThrust);
            }
        }
        else if (rightTCASThrust > leftTCASThrust)
        {
            ship.ThrustRight(rightTCASThrust);
        }
        else if (leftTCASThrust > rightTCASThrust)
        {
            ship.ThrustLeft(leftTCASThrust);
        }

        // Speed correction check - should I speed up or slow down
        
            // check if the ship is not travelling backwards
            if (transform.InverseTransformVector(gameObject.GetComponent<Rigidbody2D>().velocity).y > 0)
            {
                if (gameObject.GetComponent<Rigidbody2D>().velocity.sqrMagnitude < 1f)
                {
                    intendedThrust = 0f;
                }
                else if (gameObject.GetComponent<Rigidbody2D>().velocity.sqrMagnitude > 1f)
                {
                    intendedThrust = -generalThrust;
                }
                else { intendedThrust = 0f; }
            }
            else
            {
                intendedThrust = generalThrust; // the ship is travelling backwards, correct
            }
        

        // Check for TCAS, if not significant, (de)accelerate to intended cruising speed
        if (forwardTCASThrust < 0.05f && backwardsTCASThrust < 0.05f)
        {
            if (intendedThrust < 0f)
            {
                intendedThrust = Mathf.Abs(intendedThrust);
                ship.ThrustBackward(intendedThrust);
            }
            else if (intendedThrust > 0f)
            {
                intendedThrust = Mathf.Abs(intendedThrust);
                ship.ThrustForward(intendedThrust);
            }
        }
        else if (forwardTCASThrust > backwardsTCASThrust)
        {
            ship.ThrustForward(forwardTCASThrust);
        }
        else if (backwardsTCASThrust > forwardTCASThrust)
        {
            ship.ThrustBackward(backwardsTCASThrust);
        }
        else { }

    }

    //----------------------------------------
    #region Behaviours, behaviours, behaviours

    void ExecuteBehaviour(FlightStatus behaviour) {
        switch (behaviour)
        {
            case FlightStatus.patrolling:
                if (flightStatusIsChanging)
                {

                    flightStatusIsChanging = false;
                }
                else
                {
                    Patrol();
                }
                break;
            case FlightStatus.intercepting:
                if (flightStatusIsChanging)
                {

                    flightStatusIsChanging = false;
                }
                else
                {
                    Intercept();
                }
                break;
            case FlightStatus.sentry:
                if (flightStatusIsChanging)
                {

                    flightStatusIsChanging = false;
                }
                else
                {
                    Sentry();
                }
                break;
            case FlightStatus.engaging:
                if (flightStatusIsChanging)
                {

                    flightStatusIsChanging = false;
                }
                else
                {
                    Engage();
                }          
                break;
            case FlightStatus.retreating:
                if (flightStatusIsChanging)
                {

                    flightStatusIsChanging = false;
                }
                else
                {
                    Flee();
                }
                break;      
        }
    }

    
    void Patrol() {
        // Debug.Log("Patrolling");
        if (wpList.Count == 0 || wpIndex < 0) {           
            ClearWaypoints();  
            Debug.Log("No WP");
            flightStatus = FlightStatus.sentry;
        }

        // string wp = wpList[wpIndex].wpCoordinates.ToString();
        // Debug.Log("Index: " + wpIndex + " wp: " + wp);

        Move();
    }

    void Intercept() {
        Move();
    }

    // Simply clears waypoints and stops the vessel for predefined loiter time
    void Sentry() {
        // Simple control for exiting and entering Loiter
        if (!loiterOngoing)        {
            loiterOngoing = true;
            statusText.text = "loitering";
            loiterStart = Time.timeSinceLevelLoad;
            ClearWaypoints();
        }
        if (Time.timeSinceLevelLoad - loiterTime > loiterStart)        {
            flightStatus = FlightStatus.patrolling;
            wpList = RouteAndManeuverPlanner.PlanPatrol(gameObject.transform.position);
            wpIndex = wpList.Count - 1;
            desiredSpeed = patrolSpeedMax;
            generalThrust = patrolThrust;
            loiterOngoing = false;
            statusText.text = "";
            return;
        }

        Stop();
    }

    void Engage() {

    }

    void Flee() {
        Move();
    }

    void Defend() {

    }
    #endregion
    // -------------------------------------

    void RepeatingChecks() {
        bool collisionThreatDetected = false;
        if (wpList.Count != 0)
        {
            WaypointCheck();

            int threatCount = CollisionAvoidancePrepare();

            if (threatCount > 1)
            {
                
                collisionThreatDetected = CollisionAvoidance();
            }
        }

        HostilePresenceCheck();        

        FuelCheck();


        
    }

    // --------------------------------------
    #region Collision Avoidance - both TCAS and MTCD
    void TCASCollisionCheck() {
        // Debug.Log("Check called");

        TCASThreatList.Clear();

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(TCASLayerMask);   
        Collider2D[] contacts = new Collider2D[10]; 

        int tcasObjects = tcas.OverlapCollider(filter, contacts);
        

        if(tcasObjects <= 1) {
            rightTCASThrust = 0f;
            leftTCASThrust = 0f;
            forwardTCASThrust = 0f;
            backwardsTCASThrust = 0f;
            return;
        }
        // Debug.Log("TCAS contacts " + (tcasObjects - 1) + " " + contacts.Length);
        // Put all valid threats into CollisionThreat list

        for (int i = 0; i < tcasObjects; i++){
            if (contacts[i] && contacts[i].gameObject.name != gameObject.name) {
                // Debug.Log("TCAS contact: " + contacts[i].name);
                TCASthreatPosition.x = gameObject.transform.InverseTransformPoint(contacts[i].transform.position).x;
                TCASthreatPosition.y = gameObject.transform.InverseTransformPoint(contacts[i].transform.position).y;
                // threatVelocity.x = gameObject.transform.InverseTransformDirection(contacts[i].GetComponent<Rigidbody2D>().velocity).x;
                // threatVelocity.y = gameObject.transform.InverseTransformDirection(contacts[i].GetComponent<Rigidbody2D>().velocity).y;
                TCASThreatList.Add(new NTools.CollisionThreat(TCASthreatPosition, TCASthreatPosition, TCASthreatVelocity, 0f,0f));
            }
        }
        // Work through CollisonThreat list and produce TCASThrusts
        if (TCASThreatList.Count != 0)
        {
            // zeroing the previous avoiding values
            rightTCASThrust = 0f;
            leftTCASThrust = 0f;
            forwardTCASThrust = 0f;
            backwardsTCASThrust = 0f;

            for (int i = 0; i < TCASThreatList.Count; i++)
            {
                // Debug.Log("In the TCAS final loop");
                // get inline avoidance thrust
                if (TCASThreatList[i].threatCoordinates.y > 0f)
                {
                    backwardsTCASThrust += (5 - TCASThreatList[i].threatCoordinates.y) / 3f;
                }
                else if (TCASThreatList[i].threatCoordinates.y < 0f)
                {
                    forwardTCASThrust += (5 + TCASThreatList[i].threatCoordinates.y) / 4f;
                }

                // get lateral avoidance thrust
                if (TCASThreatList[i].threatCoordinates.x > 0f)
                {
                    leftTCASThrust += (5 - TCASThreatList[i].threatCoordinates.x) / 2f;
                }
                else if (TCASThreatList[i].threatCoordinates.x < 0f)
                {
                    rightTCASThrust += (5 + TCASThreatList[i].threatCoordinates.x) / 2f;
                }

            }
        }        
    }

    // Works, spits out how many collision threats it sees (one is own vessel), populates appropriate list if more than one
    int CollisionAvoidancePrepare()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(TCASLayerMask);
        Collider2D[] contacts = new Collider2D[50];

        int threatCount = visionCollider.OverlapCollider(filter, contacts);
        if( threatCount <= 1) { return threatCount; }

        collisionThreatList.Clear();
        for (int i = 0; i < threatCount; i++) {
            if (contacts[i].gameObject.name != gameObject.name && contacts[i].gameObject.GetComponent<Rigidbody2D>())
            {
                collisonThreatPosition.x = contacts[i].gameObject.transform.position.x;
                collisonThreatPosition.y = contacts[i].gameObject.transform.position.y;
                collisionThreatVelocity = gameObject.transform.InverseTransformVector(contacts[i].gameObject.GetComponent<Rigidbody2D>().velocity);
                // The position is added twice, just to fill the slot, in later processing it will be upgraded to projected position
                collisionThreatList.Add(new NTools.CollisionThreat(collisonThreatPosition, collisonThreatPosition, collisionThreatVelocity, 0f,0f));
            }
            else { }
        }
        return threatCount;
        
    }


    // returns TRUE when CA has determined a threat and sucessfully added a WP to solve conflict
    // False is returned when either CA failed or no threat was found
    bool CollisionAvoidance()
    {
        
        Vector2 shipProjectedPos = gameObject.GetComponent<Rigidbody2D>().position 
            + (wpList[wpIndex].wpCoordinates - gameObject.GetComponent<Rigidbody2D>().position).normalized * desiredSpeed;
        Vector2 shipPos = new Vector2(gameObject.GetComponent<Rigidbody2D>().position.x, gameObject.GetComponent<Rigidbody2D>().position.y);
        Vector2 headingToWP = wpList[wpIndex].wpCoordinates - shipPos;
        float distanceToWP = Mathf.Sqrt(Vector2.SqrMagnitude(wpList[wpIndex].wpCoordinates - gameObject.GetComponent<Rigidbody2D>().position));

        Vector2 closestThreat2D = new Vector2(0f,0f);
        Vector2 closestThreatVelocity2D = new Vector2(0f, 0f);
        Vector2 newHeadingVector = new Vector2(0f, 0f);

        // Variables for use in loops, to avoid repeditive declarations
        float timeToInterWP = 50f / desiredSpeed;
        float distanceToNearestThreat = 0f;
        float newHeadingLeft = 0f;
        float newHeadingRight = 0f;
        bool leftHeading = true;        
        bool solutionFound = false;
        

        // Setting the travel time, by which we set the threat movement amount        
        if (distanceToWP >= 50f)       {            timeToInterWP = 50f / desiredSpeed;                            }
        else                             {         timeToInterWP = distanceToWP/desiredSpeed;      }

        // Sorting the threats, which also fills in missing bits in the threat list
        SortCollisionThreats((wpList[wpIndex].wpCoordinates - gameObject.GetComponent<Rigidbody2D>().position), timeToInterWP);  
        // parsing the POTENTIAL threats for REAL threats, the method also returns a BOOL if there are REAL threats at all
        NTools.CollisionThreatsSorted parsedThreats = CheckHeadingClear(shipProjectedPos, wpList[wpIndex].wpCoordinates);

        // If no threats are found, exit CA, else clear temporary wps from the wp list and start meaty bit of CA
        if (!parsedThreats.realThreatsPresent) {
            return false;
        }  

        // Determine distance to closest threat and its coordinates -.-- Why do I do it?
        else if (parsedThreats.realThreatsLeft.Count != 0 && parsedThreats.realThreatsRight.Count != 0)
        {
            ClearTemporaryWaypoints();

            if (parsedThreats.realThreatsLeft[0].sqrDistance < parsedThreats.realThreatsRight[0].sqrDistance)
            {
                distanceToNearestThreat = Vector2.Distance(
                    shipPos, 
                    parsedThreats.realThreatsLeft[0].threatCoordinates
                    );
                closestThreat2D = parsedThreats.realThreatsLeft[0].threatCoordinates;
                closestThreatVelocity2D = parsedThreats.realThreatsLeft[0].threatVelocity;
            }
            else
            {
                distanceToNearestThreat = Vector2.Distance(
                       shipPos,
                       parsedThreats.realThreatsRight[0].threatCoordinates
                       );
                closestThreat2D = parsedThreats.realThreatsRight[0].threatCoordinates;
                closestThreatVelocity2D = parsedThreats.realThreatsRight[0].threatVelocity;
            }
        }
        else if (parsedThreats.realThreatsLeft.Count == 0 && parsedThreats.realThreatsRight.Count != 0)
        {
            ClearTemporaryWaypoints();

            distanceToNearestThreat = Vector2.Distance(
                       shipPos,
                       parsedThreats.realThreatsRight[0].threatCoordinates
                       );
            closestThreat2D = parsedThreats.realThreatsRight[0].threatCoordinates;
            closestThreatVelocity2D = parsedThreats.realThreatsRight[0].threatVelocity;
        }
        else if (parsedThreats.realThreatsLeft.Count != 0 && parsedThreats.realThreatsRight.Count == 0)
        {
            ClearTemporaryWaypoints();

            distanceToNearestThreat = Vector2.Distance(
                    shipPos,
                    parsedThreats.realThreatsLeft[0].threatCoordinates
                    );
            closestThreat2D = parsedThreats.realThreatsLeft[0].threatCoordinates;
            closestThreatVelocity2D = parsedThreats.realThreatsLeft[0].threatVelocity;
        }

        Vector2 vectorToThreat = closestThreat2D - shipPos;
        // statusText.text = vectorToThreat.ToString();

        // Ceck if the WP is closer than the threat, in that case, return and stop CA, return false
        if(distanceToWP < distanceToNearestThreat) { return false; }

        headingToWP = headingToWP.normalized * distanceToNearestThreat;             

        // parse intermittently left and right for a clear initial passage    
        int iterations = 0;
        do
        {
            switch (leftHeading)
            {
                case true:
                    newHeadingLeft -= 1;

                    parsedThreats = CheckHeadingClear(shipPos, shipPos + NTools.RotateVector2(headingToWP, newHeadingLeft));
                    if (SettingsStatic.debugEnabled) { DrawDebugLine(shipPos, shipPos + NTools.RotateVector2(headingToWP, newHeadingLeft)); }

                    leftHeading = false;
                    iterations++;
                    break;
                case false:
                    newHeadingRight += 1;

                    parsedThreats = CheckHeadingClear(shipPos, shipPos + NTools.RotateVector2(headingToWP, newHeadingRight));
                    if(SettingsStatic.debugEnabled) { DrawDebugLine(shipPos, shipPos + NTools.RotateVector2(headingToWP, newHeadingLeft).normalized);}

                    leftHeading = true;
                    iterations++;
                    break;
                    
            }
            // exit failsafes:
            if(newHeadingLeft < -90 || newHeadingRight > 90)
            {
                solutionFound = false;
                break;                
            }


        } while (parsedThreats.realThreatsPresent);

        if (newHeadingLeft > -90 && newHeadingRight < 90)
        {
            float newHeading;
            if (leftHeading) { newHeading = newHeadingRight; }
            else { newHeading = newHeadingLeft; }
            solutionFound = true;
            //Debug.Log("Clear relative heading found: " + newHeading.ToString());
        }

        if (!solutionFound) { Debug.Log("Heading not found in " + iterations + " iterations."); return false; }
        

        // Determine new direction from projected ship position
        if (leftHeading)    { newHeadingVector = NTools.RotateVector2(headingToWP, newHeadingRight); /*Debug.Log("New Heading: " + newHeadingRight);   */}
        else                { newHeadingVector = NTools.RotateVector2(headingToWP, newHeadingLeft); /*Debug.Log("New Heading: " + newHeadingLeft);    */}

        float newWPDistance = 1f;
        do
        {
            newWPDistance += 0.1f;
            parsedThreats = CheckHeadingClear(shipPos + newHeadingVector * newWPDistance, wpList[wpIndex].wpCoordinates);
            // check to limit the possible distance of the intermittent WP to the distance of the original WP
            if(newWPDistance * newHeadingVector.magnitude > distanceToWP) {
                Debug.Log("WP solution not found.");                
                break;
            }
        } while (parsedThreats.realThreatsPresent);

        if(newWPDistance < 10f) { solutionFound = true; }         


        // This happens, when the collisionAvoidance has done its job and can add a new intermediate waypoint
        // also entering this loop will return true - the method has parsed successfully and added a wp        
        if (solutionFound)
        {
            NTools.Waypoint newInbetweenWP = new NTools.Waypoint(shipPos + newHeadingVector * (newWPDistance - 0.2f), 4f, false, true);
            wpList.Add(newInbetweenWP);
            wpIndex = wpList.Count - 1;
            return true;
        }
        return false;
    }

    // This method sorts the global List of collision threats and adds missing values
    void SortCollisionThreats(Vector2 headingVector, float timeToInterWP)
    {
        Vector2 shipPos = gameObject.GetComponent<Rigidbody2D>().position;

        // sort lefties and righties and add projected point
        for (int i = 0; i < collisionThreatList.Count; i++)
        {
            // Calculating the endpos for threat with the time it takes the ship to fly to WP
            collisionThreatList[i].threatCoordinates2 = collisionThreatList[i].threatCoordinates + collisionThreatList[i].threatVelocity * timeToInterWP;

            // Adds the sqr distance to the appropriate contact in the list
            collisionThreatList[i].sqrDistance =
                    Vector2.SqrMagnitude(collisionThreatList[i].threatCoordinates - gameObject.GetComponent<Rigidbody2D>().position);

            //determining if threat is left or right from current heading by normalized crossproduct Z value
            collisionThreatList[i].leftRightOfHeading = Vector3.Cross(
                new Vector3(headingVector.x, headingVector.y, 0f).normalized,
                new Vector3(collisionThreatList[i].threatCoordinates.x - shipPos.x, collisionThreatList[i].threatCoordinates.y - shipPos.y, 0f).normalized
                ).z;
        }
    }

    // Determines real threats and sorts them by distance 
    NTools.CollisionThreatsSorted CheckHeadingClear(Vector2 initialPosition, Vector2 aimPoint)
    {
        
        //The ship Projected velocity is from initial position to aimpoint, NOT the real velocity vector!!!
        Vector3 shipProjectedVel = new Vector3((aimPoint - initialPosition).normalized.x, (aimPoint - initialPosition).normalized.y,0f) * desiredSpeed;
        Vector2 shipPos = gameObject.GetComponent<Rigidbody2D>().position;

        Vector3 threatLocalPos;
        
        List<NTools.CollisionThreat> realThreatsLeft = new List<NTools.CollisionThreat>(20);
        List<NTools.CollisionThreat> realThreatsRight = new List<NTools.CollisionThreat>(20);        

        NTools.CollisionThreatsSorted ctSorted = new NTools.CollisionThreatsSorted(false, realThreatsLeft, realThreatsRight);

        float separation = 0f; float separation2 = 0f;       


        // Parse for real threats and add them to ctSorted
        for (int i = 0; i < collisionThreatList.Count; i++)
        {
            threatLocalPos = gameObject.transform.InverseTransformPoint(new Vector3(
            collisionThreatList[i].threatCoordinates.x,
            collisionThreatList[i].threatCoordinates.y,
            0f
            ));

            if(threatLocalPos.y < 0) { continue; }

            // Debug.Log("Threat pos" + gameObject.transform.InverseTransformPoint(collisionThreatList[i].threatCoordinates.x, collisionThreatList[i].threatCoordinates.y,0f));

            separation = Vector3.Cross(
                new Vector3(collisionThreatList[i].threatCoordinates.x - shipPos.x, collisionThreatList[i].threatCoordinates.y - shipPos.y, 0f),
                shipProjectedVel
                ).magnitude / shipProjectedVel.magnitude;
            // Debug.Log("Sep1: " + separation);

            separation2 = Vector3.Cross(
                new Vector3(collisionThreatList[i].threatCoordinates2.x - shipPos.x, collisionThreatList[i].threatCoordinates2.y - shipPos.y, 0f),
                shipProjectedVel
                ).magnitude / shipProjectedVel.magnitude;

            // Debug.Log("Sep2: " + separation2);

            // if threat is valid push to appropriate list for side, also sorts so, that the nearest threat has lowest index
            if (separation < minSeparation || separation2 < minSeparation)
            {
                ctSorted.realThreatsPresent = true;

                if (collisionThreatList[i].leftRightOfHeading < 0f)
                {
                    if (realThreatsLeft.Count == 0)
                    {
                        realThreatsLeft.Add(collisionThreatList[i]);                        
                    }
                    else if (realThreatsLeft[realThreatsLeft.Count - 1].sqrDistance < collisionThreatList[i].sqrDistance)
                    {
                        realThreatsLeft.Add(collisionThreatList[i]);
                    }
                    else if (realThreatsLeft[realThreatsLeft.Count - 1].sqrDistance > collisionThreatList[i].sqrDistance)
                    {
                        NTools.CollisionThreat tempFromLefts = realThreatsLeft[realThreatsLeft.Count - 1];
                        realThreatsLeft.RemoveAt(realThreatsLeft.Count - 1);
                        realThreatsLeft.Add(collisionThreatList[i]);
                        realThreatsLeft.Add(tempFromLefts);
                    }
                }
                else
                {
                    if (realThreatsRight.Count == 0)
                    {
                        realThreatsRight.Add(collisionThreatList[i]);
                    }
                    else if (realThreatsRight[realThreatsRight.Count - 1].sqrDistance < collisionThreatList[i].sqrDistance)
                    {
                        realThreatsRight.Add(collisionThreatList[i]);
                    }
                    else if (realThreatsRight[realThreatsRight.Count - 1].sqrDistance > collisionThreatList[i].sqrDistance)
                    {
                        NTools.CollisionThreat tempFromLefts = realThreatsRight[realThreatsRight.Count - 1];
                        realThreatsRight.RemoveAt(realThreatsRight.Count - 1);
                        realThreatsRight.Add(collisionThreatList[i]);
                        realThreatsRight.Add(tempFromLefts);
                    }                    
                }
            }
        }       

        return ctSorted;
    }
    #endregion
    // --------------------------------------

    void HostilePresenceCheck()
    {

    }

    void FuelCheck()
    {


    }

    // passes lead to next wingman, if any available
    public void Death()
    {
        if (statusInFormation == StatusInFormation.lead)
        {
            GameObject formation4 = gameObject.transform.Find("Formation4(Clone)").gameObject;

            bool leadAssigned = false;
            int pos = 0;

            for (int i = 0; i <= 2; i++)
            {
                // Set up the new formation lead before death, rawr
                if (!leadAssigned && wingmen[i])
                {
                    wingmen[i].GetComponent<VesselAI>().statusInFormation = StatusInFormation.lead;
                    wingmen[i].GetComponent<VesselAI>().flightStatus = FlightStatus.sentry;
                    wingmen[i].GetComponent<VesselAI>().teamManager = teamManager;
                    wingmen[i].GetComponent<VesselAI>().formationBogies.Clear();
                    wingmen[i].GetComponent<VesselAI>().formationBogies = formationBogies;
                    teamManager.NewFormationLead(wingmen[i]);

                    formation4.transform.parent = wingmen[i].transform;
                    formation4.transform.localPosition = new Vector3(0,0,0);
                    formation4.transform.localRotation.SetEulerAngles(wingmen[i].transform.rotation.eulerAngles);

                    // Reassign wingmen to new lead
                    for (int j = i + 1; j <= 2; j++)
                    {
                        if (wingmen[j])
                        {
                            wingmen[i].GetComponent<VesselAI>().wingmen[pos] = wingmen[j];

                            VesselAI wmAI = wingmen[i].GetComponent<VesselAI>().wingmen[pos].GetComponent<VesselAI>();

                            wmAI.formationLead = wingmen[i];
                            wmAI.statusInFormation = (VesselAI.StatusInFormation)(pos + 1);
                            wmAI.wingmanState = WingmanState.inFormation;
                            wmAI.statusText.text = ("newpos: " + pos);

                            pos++;
                        }
                    }

                    leadAssigned = true;
                }
            }
        }
    }
   

    //--------------------------------------
    #region Waypoint methods
    void WaypointCheck() {
        wpIndex = wpList.Count - 1;

        if (statusInFormation == StatusInFormation.lead){          
            
            if (wpList.Count == 0) { return; } // sanity check

            // Check if the Vessel is close enough to the waypoint and if, then remove it
            Vector2 vectorToWP = wpList[wpIndex].wpCoordinates - gameObject.GetComponent<Rigidbody2D>().position;
            if (Vector2.SqrMagnitude(vectorToWP) < wpList[wpIndex].wpRadius * wpList[wpIndex].wpRadius) 
            {
                wpList.RemoveAt(wpIndex);
                wpIndex = wpList.Count - 1;

            }

            if (wpList[wpIndex].temporary)
            {
                TempWPSanityCheck();
            }
        }

        headingToWaypoint = RouteAndManeuverPlanner.HeadingFromPositions(
                new Vector2(gameObject.transform.position.x, gameObject.transform.position.y),
                wpList[wpIndex].wpCoordinates
                );
    }

    void TempWPSanityCheck()
    {
        Vector2 shipProjectedPos = gameObject.GetComponent<Rigidbody2D>().position
            + (wpList[wpIndex].wpCoordinates - gameObject.GetComponent<Rigidbody2D>().position).normalized * desiredSpeed;

        int nonTempWPindex = wpList.Count - 1;        

        // This loop starts from the top of the list i.e closest wp until it finds a non Temp WP, then sets the index and breaks the loop
        while (wpList[nonTempWPindex].temporary)
        {            
            nonTempWPindex--;
        }

        Vector2 nonTempWP2D = wpList[nonTempWPindex].wpCoordinates;

        if(!CheckHeadingClear(shipProjectedPos, nonTempWP2D).realThreatsPresent)
        {
            ClearTemporaryWaypoints();
        }
    }

    

    void ClearWaypoints()    {
        wpIndex = 0;
        wpList.Clear();
        wpList.Capacity = 10;
        
    }
    
    void ClearTemporaryWaypoints()
    {
        for (int i = 0; i < wpList.Count; i++)
        {
            if (wpList[i].temporary)
            {
                wpList.RemoveAt(i);
            }
        }

        wpIndex = wpList.Count - 1;
    }
    #endregion
    //------------------------------------------

    // Works in principle
    void UpdateVelocityVectorAndTCAS()
    {
        Vector2 velocity2D = gameObject.GetComponent<Rigidbody2D>().velocity;
        Vector3 localVelocity3D = gameObject.transform.InverseTransformVector(new Vector3(velocity2D.x, velocity2D.y, 0f));

        Vector3 position = gameObject.transform.position;
        Vector3 lineStart = new Vector3(position.x + velocity2D.normalized.x, position.y + velocity2D.normalized.y, position.z);
        Vector3 lineEnd = new Vector3(position.x + velocity2D.x * 2, position.y + velocity2D.y * 2, position.z);

        velocityVector.SetPosition(0, lineStart);
        velocityVector.SetPosition(1, lineEnd);

        float tcasXOffset = Mathf.Clamp(localVelocity3D.x, -1f, 1f);
        float tcasYOffset = Mathf.Clamp(localVelocity3D.y, -3f, 20f);

        tcas.offset = new Vector2(tcasXOffset, tcasYOffset);
    }


    #region Debugging
    void UpdateWaypointLine()
    {
        if (SettingsStatic.debugEnabled)
        {
            wpIndex = wpList.Count - 1;
            if (wpList.Count == 0) { return; }
            Vector3 position = gameObject.transform.position;

            waypointLine.SetPosition(0, gameObject.transform.position);
            waypointLine.SetPosition(1, new Vector3(wpList[wpIndex].wpCoordinates.x, wpList[wpIndex].wpCoordinates.y, 0f));

            if (wpList.Count > 1)
            {
                waypointLine2.SetPosition(0, new Vector3(wpList[wpIndex].wpCoordinates.x, wpList[wpIndex].wpCoordinates.y, 0f));
                waypointLine2.SetPosition(1, new Vector3(wpList[wpIndex - 1].wpCoordinates.x, wpList[wpIndex - 1].wpCoordinates.y, 0f));

            }
            else
            {
                waypointLine2.SetPosition(0, new Vector3(0f, 0f, 0f));
                waypointLine2.SetPosition(1, new Vector3(0f, 0f, 0f));
            }
        }         

    }

    void DrawDebugLine(Vector2 start, Vector2 end)
    {
        Vector3 startGlobal = new Vector3(start.x, start.y, 0f);
        Vector3 endGlobal = new Vector3(end.x, end.y, 0f);
        GameObject debugLine = Instantiate(DebugLine2D, new Vector3(0f, 0f, 0f), Quaternion.identity) as GameObject;
        LineRenderer debugLine2D = debugLine.GetComponent<LineRenderer>();

        debugLine2D.SetPosition(0, startGlobal);
        debugLine2D.SetPosition(1, endGlobal);

        Destroy(debugLine, 3f);
    }
    #endregion
}
