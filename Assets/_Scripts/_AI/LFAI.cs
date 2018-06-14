using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LFAI : MonoBehaviour {

    public enum OrderType { patrol, intercept, moveToDefend, regroup }
    public enum TMProbType { flightBingo, flightInDanger, orderRequest, perimeter, objectiveAttack, objectiveDefence }

    public class TMProblem
    {
        public float timeOfDeclaration;
        public TMProbType ProbType;
        public float ProbPriority; // The higher the number, the higher the priority
        public LFAI.Formation problemFormation; // if the problem is associated with a flight, reference GameObject

        // important value, that should always be checked, else the problem could not be solved.
        // the value should be set according to the problem nature and used in repeating solution checking 
        public float solutionParameter; 

        public TMProblem(TMProbType probType, float priority, float solutionValue)
        {
            ProbType = probType;    ProbPriority = priority; solutionParameter = solutionValue;

            timeOfDeclaration = Time.timeSinceLevelLoad;
        }

        public TMProblem(TMProbType probType, float priority, float solutionValue, LFAI.Formation probFormation)
        {
            ProbType = probType; ProbPriority = priority;   problemFormation = probFormation; solutionParameter = solutionValue;

            timeOfDeclaration = Time.timeSinceLevelLoad;
        }
    }

    public struct FormationReport
    {
        public bool hasWp;
        public int nrHostiles;       
        public int formationSize;
        public bool fuelIsBingo;
        public float fuelPercentage;

        public FormationReport(bool wpHas, int hostiles, int flightSize, bool bingo, float fuelpercentage)
        {
            hasWp = wpHas; nrHostiles = hostiles; formationSize = flightSize; fuelIsBingo = bingo; fuelPercentage = fuelpercentage;
        }
    }

    public class Formation
    {
        public GameObject Lead;
        public int nrHostiles;
        public int formationSize;
        public bool fuelIsBingo;
        public float fuelPercentage;

        public Formation(GameObject Flead, int hostiles, int flightSize, bool bingo, float fuelpercentage)
        {
            Lead = Flead; nrHostiles = hostiles; formationSize = flightSize; fuelIsBingo = bingo; fuelPercentage = fuelpercentage;
        }

        public Formation(GameObject Flead)
        {
            Lead = Flead;
        }
    }

    public struct TMAssets
    {
        public int scoutAmount;
        public int fighterAmount;
        public int bomberAmount;
    }

    public class TMObjective
    {
        public GameObject objectiveObject;        
        public bool objectiveIsFriendly;
        public bool isAlive;
        public bool isMoving;

        public TMObjective(GameObject objective, bool friendly, bool alive, bool moving)
        {
            objectiveObject = objective; objectiveIsFriendly = friendly; isAlive = alive; isMoving = moving;
        }
    }
   
    public struct Order
    {     
        public OrderType orderType;
        public VesselAI.FlightStatus flightStatus;
        public List<NTools.Waypoint> waypoints;

        public Order(OrderType type, VesselAI.FlightStatus status, List<NTools.Waypoint> wpList)
        {
            orderType = type; flightStatus = status; waypoints = wpList;
        }
    }

    public class Perimeter
    {
        public Vector3 center; // in world space
        public Vector2 heading2D; public float headingFloat; // also in world space, heading2D carries distance info
        public GameObject perimeterAnchor;

        public bool isMoving = false;
        public bool upToDate = false;       

        public List<TMSector> Sectors = new List<TMSector>(13);

        public Perimeter(Vector2 centerCoords, float heading, bool isstatic)
        {
            center = centerCoords; headingFloat = heading;
            isMoving = !isstatic;
        }

        public Perimeter(Vector2 centerCoords, float heading, bool isstatic, GameObject anchor)
        {
            center = centerCoords; headingFloat = heading;
            isMoving = !isstatic;
            perimeterAnchor = anchor;
        }

        // Sector radius ranges
        float a1 = 0f, a2 = 25f;        // center
        float b1 = 25.01f, b2 = 60f;    // closer sectors
        float c1 = 60.01f, c2 = 100f;   // outer sectors

        void Start()
        {
            // Creating sector upon initialization of class
            // Center sector
            Sectors.Add(new TMSector("center", new Vector2(0f, 360f), new Vector2(a1, a2), 0));            

            // CCW, just as the headings go, closer sectors
            Sectors.Add(new TMSector("front", new Vector2(45f, 315f), new Vector2(b1, b2), 1));
            Sectors.Add(new TMSector("left", new Vector2(135f, 46f), new Vector2(b1, b2), 1));
            Sectors.Add(new TMSector("back", new Vector2(225f, 135f), new Vector2(b1, b2), 1));
            Sectors.Add(new TMSector("right", new Vector2(225f, 315f), new Vector2(b1, b2), 1));

            // CCW, farther sectors
            Sectors.Add(new TMSector("frontFar", new Vector2(22.5f, 337.5f),        new Vector2(c1, c2), 2));
            Sectors.Add(new TMSector("frontLeftFar", new Vector2(67.5f, 22.5f),     new Vector2(c1, c2), 2));
            Sectors.Add(new TMSector("leftFar", new Vector2(67.5f, 112.5f),         new Vector2(c1, c2), 2));
            Sectors.Add(new TMSector("backLeftFar", new Vector2(112.5f, 157.5f),    new Vector2(c1, c2), 2));
            Sectors.Add(new TMSector("backFar", new Vector2(157.5f, 202.5f),        new Vector2(c1, c2), 2));
            Sectors.Add(new TMSector("backRightFar", new Vector2(202.5f, 247.5f),   new Vector2(c1, c2), 2));
            Sectors.Add(new TMSector("rightFar", new Vector2(247.5f, 292.5f),       new Vector2(c1, c2), 2));
            Sectors.Add(new TMSector("frontRightFar", new Vector2(292.5f, 337.5f),  new Vector2(c1, c2), 2));  
        }

        void Update()
        {
            if(perimeterAnchor && !isMoving)
            {
                center = perimeterAnchor.transform.position;
            }
        }
    }

    public class TMSector
    {
        public int layerFromCenter;
        public Vector2 headingRange;
        public Vector2 distanceRange;
        public string sectorName = "";
        public int greenCount = 0;
        public int redCount = 0;
        public float importance = 0f;
        public float importanceMet = 0f;

        public TMSector(string name, Vector2 headings, Vector2 distances, int layer)
        {
            headingRange = headings; distanceRange = distances; sectorName = name; layerFromCenter = layer;
        }
    }

    // Not the neatest approach ... by far
    public class TMSectorImportancePresets
    {
        // each float array in a list corresponds to the enum TeamMood number
        // 0 - noContact, 1 - cautious, 2 - attacking, 3 - defending

        public List<float[]> scouting = new List<float[]>(4);
        public List<float[]> attacking = new List<float[]>(4);
        public List<float[]> defending = new List<float[]>(4);
        public List<float[]> regrouping = new List<float[]>(4);

        
        void Start() { 
            // Filling priorities               C   F       L   B       R   FF      FLF LF      BLF BF      BRF RF      FRF        
            float[] scouting1 = new float[] { 0.2f, 0.5f, 0.1f, 0.1f, 0.4f, 0.5f, 0.5f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.5f };            
            float[] scouting2 = new float[] { 0.4f, 0.5f, 0.2f, 0.1f, 0.2f, 0.7f, 0.6f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.6f };
            float[] scouting3 = new float[] { 0.6f, 0.5f, 0.4f, 0.1f, 0.4f, 0.8f, 0.8f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.8f };
            float[] scouting4 = new float[] { 0.8f, 0.8f, 0.7f, 0.3f, 0.7f, 0.2f, 0.2f, 0.2f, 0.2f, 0.1f, 0.2f, 0.2f, 0.2f };
            scouting.Add(scouting1); scouting.Add(scouting2); scouting.Add(scouting3); scouting.Add(scouting4);

            float[] attacking1 = new float[] { 0.2f, 0.5f, 0.1f, 0.1f, 0.4f, 0.5f, 0.5f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.5f };
            float[] attacking2 = new float[] { 0.4f, 0.5f, 0.2f, 0.1f, 0.2f, 0.7f, 0.6f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.6f };
            float[] attacking3 = new float[] { 0.6f, 0.5f, 0.4f, 0.1f, 0.4f, 0.8f, 0.8f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.8f };
            float[] attacking4 = new float[] { 0.8f, 0.8f, 0.7f, 0.3f, 0.7f, 0.2f, 0.2f, 0.2f, 0.2f, 0.1f, 0.2f, 0.2f, 0.2f };
            attacking.Add(attacking1); attacking.Add(attacking2); attacking.Add(attacking3); attacking.Add(attacking4);

            float[] defending1 = new float[] { 0.5f, 0.5f, 0.4f, 0.1f, 0.4f, 0.7f, 0.7f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.7f };
            float[] defending2 = new float[] { 0.7f, 0.7f, 0.5f, 0.5f, 0.8f, 0.5f, 0.5f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.5f };
            float[] defending3 = new float[] { 0.3f, 0.5f, 0.4f, 0.1f, 0.4f, 0.7f, 0.7f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.7f };
            float[] defending4 = new float[] { 1.0f, 0.8f, 0.8f, 0.1f, 0.8f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
            defending.Add(defending1); defending.Add(defending2); defending.Add(defending3); defending.Add(defending4);

            float[] regroup1 = new float[] { 0.5f, 0.5f, 0.4f, 0.1f, 0.4f, 0.7f, 0.7f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.7f };
            float[] regroup2 = new float[] { 0.5f, 0.5f, 0.4f, 0.1f, 0.4f, 0.7f, 0.7f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.7f };
            float[] regroup3 = new float[] { 0.5f, 0.5f, 0.4f, 0.1f, 0.4f, 0.7f, 0.7f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.7f };
            float[] regroup4 = new float[] { 1.0f, 0.8f, 0.8f, 0.1f, 0.8f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
            regrouping.Add(regroup1); regrouping.Add(regroup2); regrouping.Add(regroup3); regrouping.Add(regroup4);

        }
    }

}
