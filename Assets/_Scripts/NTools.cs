using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NTools : MonoBehaviour {

    // Instance of this class maintains the Vector2 coordinates and float radius of one waypoint
    public class Waypoint {

        public Vector2 wpCoordinates;
        public float wpRadius;
        public bool movingWP;
        public bool temporary;

        public Waypoint(Vector2 coord, float radius, bool movingwp, bool temp) {
            wpCoordinates = coord;
            wpRadius = radius;
            movingWP = movingwp;
            temporary = temp;
        }
    }

    public class CollisionThreat
    {
        public Vector2 threatCoordinates;
        public Vector2 threatCoordinates2;
        public Vector2 threatVelocity;

        // ------------- Floats for sorting threats:----------------
        
        // Negative for left, positive for right, should be calculated by crossproduct so it also gives a heading basically
        public float leftRightOfHeading = 0;
        public float sqrDistance = 0;

        public CollisionThreat(Vector2 threatCoords, Vector2 threatCoords2, Vector2 threatVel, float leftOrRight, float distanceSQR)
        {
            threatCoordinates = threatCoords;
            threatCoordinates2 = threatCoords2;
            threatVelocity = threatVel;
            leftRightOfHeading = leftOrRight;
            sqrDistance = distanceSQR;
        }
    }

    public static Vector2 RotateVector2(Vector2 v, float degrees)
    {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

     
        return new Vector2((cos * v.x) - (sin * v.y), (sin * v.x) + (cos * v.y));
    }

    public class CollisionThreatsSorted
    {
        public bool realThreatsPresent = false;
        
        public List<NTools.CollisionThreat> realThreatsLeft = new List<NTools.CollisionThreat>(20);
        public List<NTools.CollisionThreat> realThreatsRight = new List<NTools.CollisionThreat>(20);

        public CollisionThreatsSorted(
            bool bthreats, 
            List<NTools.CollisionThreat> leftRealThreats, 
            List<NTools.CollisionThreat> rightRealThreats
            )
        {
            realThreatsPresent = bthreats;           
            realThreatsLeft = leftRealThreats;
            realThreatsRight = rightRealThreats;

        }
    }

    public static float HeadingFromPositions(Vector2 position1, Vector2 position2)
    {
        return HeadingFromVector(new Vector2(
            position2.x-position1.x,
            position2.y -position1.y
            ));        
    }

    public static float HeadingFromVector(Vector2 vector)
    {
        float heading = (float)((Mathf.Atan2(vector.x,vector.y)/Mathf.PI)*180f);
        if (heading < 0) { heading += 360f; }
        return heading = 360 - heading;     
    }

    public static Vector3 GetCenterOfObjects3D(List<GameObject> objectList)
    {
        Vector3 center = new Vector3(0,0,0);
        float xSum = 0f, ySum = 0f, zSum = 0f;

        for (int i = 0; i < objectList.Count; i++)
        {
            xSum += objectList[i].transform.position.x;
            ySum += objectList[i].transform.position.y;
            zSum += objectList[i].transform.position.z;
        }

        center.x = xSum / objectList.Count;
        center.y = ySum / objectList.Count;
        center.z = zSum / objectList.Count;

        return center;
    }

    // works CCW, does it work the other way around? Yes, it does!!!
    public static float GetBearingFromHeadings(float ahead, float headingToObject)
    {
        float bearing = 0f;

        if (ahead < headingToObject && (headingToObject - ahead) > 180f)
        {
            bearing = headingToObject - 360 - ahead;

        }
        else if (ahead < headingToObject && (headingToObject - ahead) < 180f)
        {
            bearing = headingToObject - ahead;
        }
        else if (headingToObject < ahead && (ahead - headingToObject) > 180f)
        {
            bearing = headingToObject + 360 - ahead;
        }
        else if (headingToObject < ahead && (ahead - headingToObject) < 180f)
        {
            bearing = headingToObject - ahead;
        }

        return bearing;
    }

}
