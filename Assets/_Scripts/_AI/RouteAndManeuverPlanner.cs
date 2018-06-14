using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RouteAndManeuverPlanner
{

    // NOT USED : struct for storing three waypoint patrol routes in Vector2 format
    public struct PatrolRoute {
        public Vector2 waypoint1;
        public Vector2 waypoint2;
        public Vector2 waypoint3;

        public PatrolRoute(Vector2 w1, Vector2 w2, Vector2 w3){
            waypoint1 = w1;
            waypoint2 = w2;
            waypoint3 = w3;
        }

    }

    public static float GetInterceptHeading(Transform target, Transform seeker, float velocity) {
        float interceptHeading = 0f;

        Vector2 directVector = new Vector2(target.position.x - seeker.position.x, target.position.y - seeker.position.y);
        float staticDistance = directVector.magnitude;

        float staticTime = staticDistance / velocity;

        Vector2 targetVelocity = target.GetComponent<Rigidbody2D>().velocity;
        Vector2 dynamicTargetPosition = new Vector2(target.position.x + targetVelocity.x * staticTime, target.position.y + targetVelocity.y * staticTime);

        interceptHeading = HeadingFromPositions(new Vector2(seeker.position.x, seeker.position.y), dynamicTargetPosition);
      
        return interceptHeading;
    }

    // The first member of the return vector is the heading for burn and second member dV required
    public static Vector2 GetHitBurn(Transform target, Transform seeker, float fuel, float mass) {
        Vector2 hitBurn = new Vector2(0f,0f);

        float potentialdV = fuel / mass;

        Vector2 targetVector = target.GetComponent<Rigidbody2D>().velocity;
        Vector2 seekerVector = target.GetComponent<Rigidbody2D>().velocity;
        Vector2 relativeSpeedVector = seekerVector - targetVector;
        Vector2 directVector = new Vector2(target.position.x - seeker.position.x, target.position.y - seeker.position.y);

        float relativeSpeed = relativeSpeedVector.magnitude;


        // If the missile has not enough fuel to overcome the relative velocity already present, it will not accelerate or turn
        if (relativeSpeed >= potentialdV) {
            return new Vector2(seeker.eulerAngles.z, 0f);
        }

        // the divider 2 basically dictates that for this maneuver we use only half the fuel available AFTER correcting relative speed
        float dVforBurn = (potentialdV - relativeSpeed) / 10f + relativeSpeed;
        float staticTime = directVector.magnitude / ((potentialdV - relativeSpeed) / 2);

        Vector2 targetEndpoint = new Vector2(target.position.x + targetVector.x * staticTime, target.position.y + targetVector.y * staticTime);
        Vector2 seekerEndpoint = new Vector2(seeker.position.x + seekerVector.x * staticTime, seeker.position.y + seekerVector.y * staticTime);
        Vector2 hitVectorNormal = new Vector2(targetEndpoint.x - seekerEndpoint.x, targetEndpoint.y - seekerEndpoint.y).normalized;

        hitBurn = new Vector2(HeadingFromPositions(seekerEndpoint, targetEndpoint), dVforBurn);
        
        return hitBurn;

    }

    public static List<NTools.Waypoint> PlanPatrol(Vector3 shipPosition) {
        List<NTools.Waypoint> route = new List<NTools.Waypoint>(10);

        

        float patrolWpRadius = 10f;

        // Placeholder for compileability

        route.Add(new NTools.Waypoint(new Vector2(30 + Random.Range(-20f, 20f), 30 + Random.Range(-20f, 20f)), patrolWpRadius, false, false));
        route.Add(new NTools.Waypoint(new Vector2(60f + Random.Range(-20f, 20f), -20f + Random.Range(-20f, 20f)), patrolWpRadius, false, false));
        route.Add(new NTools.Waypoint(new Vector2(-60f + Random.Range(-20f, 20f), 40f + Random.Range(-20f, 20f)), patrolWpRadius, false, false));

        //Debug.Log("Route assembled!");

        

        
        return route;
    }

    // returns the counterclockwise 0-360 heading from position1 towards position2
    public static float HeadingFromPositions(Vector2 position1, Vector2 position2) {
        

        float hypotenuse = Mathf.Abs(Vector2.Distance(position1, position2));


        // float interceptHeading = Mathf.Atan2(dynamicTargetPosition.y - seeker.position.y, dynamicTargetPosition.x - seeker.position.x) * 180 / Mathf.PI;

        // First Quadrant i.e NE
        if (position2.x > position1.x && position2.y > position1.y)
        {
            float deltaX = position2.x - position1.x;
            return 270 + Mathf.Acos(deltaX / hypotenuse) * Mathf.Rad2Deg;
        }

        // Second Quadrant i.e SE
        else if (position2.x > position1.x && position2.y < position1.y)
        {
            float deltaX = position2.x - position1.x;
            return 270 - Mathf.Acos(deltaX / hypotenuse) * Mathf.Rad2Deg;
        }

        // Third Quadrant i.e SW
        else if (position2.x < position1.x && position2.y < position1.y)
        {
            float deltaX = position1.x - position2.x;
            return 90 + Mathf.Acos(deltaX / hypotenuse) * Mathf.Rad2Deg;
        }

        // Fourth Quadrant i.e NW
        else if (position2.x < position1.x && position2.y > position1.y)
        {
            float deltaX = position1.x - position2.x;
            return 90 - Mathf.Acos(deltaX / hypotenuse) * Mathf.Rad2Deg;
        }
        else {
            return 0f;
        }       
    }

    // This checks if two objects are threatened to collide in the future
    public static bool WillCollide(Vector3 object1, Vector2 velocity1, Vector3 object2, Vector2 velocity2)    {
        bool willCollide = false;



        return willCollide;
    }
}

