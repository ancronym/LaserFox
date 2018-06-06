using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionMaker : MonoBehaviour {     
    
    /* 
    Clamped between -1 and 1 later on, there is a check before instantiating, if probability
    is less than asteroid density, then asteroid is spawned and vice versa
     */
    public float asteroidDensity = -0.3f;
    public int asteroidGap = 10;
    public float randomness = 3;
    public int asteroidCount = 0;
    public int normalFormationCount = 3;

    public static float difficulty;

    // Asteroid prefabs
    public GameObject[] largeAsteroids;
    public GameObject[] mediumAsteroids;
    public GameObject[] smallAsteroids;
    public GameObject asteroidField;
    GameObject field;

    // objective and other asset prefabs:
    public GameObject playerPrefab;
    public GameObject enemyStationPrefab;
    public GameObject friendlyStationPrefab;
    public GameObject formation4Prefab;
    public GameObject civilianEscortablePrefab;
    public GameObject militaryEscortablePrefab;
    public GameObject friendlyFighterPrefab;
    public GameObject enemyFighterPrefab;

    public TeamManager[] teamManagers;

    Vector3 greenPos3D, redPos3D;
     

    void Start () {
        
        difficulty = SettingsManager.GetDifficulty();

        greenPos3D = new Vector3(MissionPlanner.greenPosition.x, MissionPlanner.greenPosition.y, 0f);
        redPos3D = new Vector3(MissionPlanner.redPosition.x, MissionPlanner.redPosition.y, 0f);        

        if (MissionPlanner.asteroidDistribution != MissionPlanner.AsteroidDistribution.none) {
            field = Instantiate(asteroidField, transform.position, Quaternion.identity) as GameObject;
            PlaceAsteroids();
        }

        PlaceFriendlyFormations();

        PlaceHostileFormations();

        switch (MissionPlanner.missionType)
        {
            case MissionPlanner.MissionType.attackBase:
                PlaceEnemyBase();
                
                break;
            case MissionPlanner.MissionType.defendBase:
                PlaceFriendlyBase();

                break;
            case MissionPlanner.MissionType.destroyFighters:

                break;
            case MissionPlanner.MissionType.escortCivilian:
                PlaceFriendlyCivilian();

                break;
            case MissionPlanner.MissionType.escortMilitary:
                PlaceFriendlyMilitary();

                break;
        }    
        
        asteroidDensity = Mathf.Clamp(asteroidDensity, -1f, 1f);

        Debug.Log("Asteroid Cound: " + asteroidCount);

	}

    
      // Pretty solid
    void PlaceAsteroids() {
        int mapRadiusInt = (int)MissionPlanner.mapRadius;        

        Vector3 asteroidPosition;
        float existence;
        float xGradientDirection = Random.Range(0f, 1f);
        float yGradientDirection = Random.Range(0f, 1f);
        float xGradientScale = Random.Range(1f, 4f);
        float yGradientScale = Random.Range(1f, 4f);
        float xP;
        float yP;        
        
        // TODO get gradient working
        for (int xPos = -mapRadiusInt; xPos < mapRadiusInt; xPos = xPos + asteroidGap)
        {
            for (int yPos = -mapRadiusInt; yPos < mapRadiusInt; yPos = yPos + asteroidGap)
            {
                switch (MissionPlanner.asteroidDistribution)
                {
                    case MissionPlanner.AsteroidDistribution.gradient:


                        // Maximum value for xP and yP is 1, minimum value -1
                        if (xGradientDirection > 0.5f)                        {
                            xP = Random.Range(-1f, 1f) + (xPos / mapRadiusInt) * xGradientScale;                            
                        } else {
                            xP = Random.Range(-1f, 1f) -(xPos / mapRadiusInt) * xGradientScale ;                            
                        }

                        if (yGradientDirection > 0.5f)                        {
                            yP = Random.Range(-1f, 1f) + (yPos / mapRadiusInt) * yGradientScale;
                        } else {
                            yP = Random.Range(-1f, 1f) -(yPos / mapRadiusInt) * yGradientScale;
                        }

                        if (yP < asteroidDensity && xP < asteroidDensity)
                        {
                            asteroidPosition = new Vector3(xPos, yPos, 0f);
                            InstantiateAsteroid(asteroidPosition);
                        }
                        break;

                    case MissionPlanner.AsteroidDistribution.uniform:
                        existence = Random.Range(-1f, 1f);
                        if (existence < asteroidDensity)
                        {
                            asteroidPosition = new Vector3(xPos, yPos, 0f);
                            InstantiateAsteroid(asteroidPosition);
                        }
                        break;

                }
            }
        }
    }

    // Pretty solid
    void InstantiateAsteroid(Vector3 position) {
        asteroidCount++;

        int size = Random.Range(1,4);
        int selection;
        Vector3 dispersion = new Vector3(Random.Range(-randomness, randomness), Random.Range(-randomness, randomness), 0f);
        position += dispersion;

        switch (size) {
            case 1:
                // They are annoying!

                /* selection = Random.Range(0, smallAsteroids.Length - 1);
                GameObject asteroid = Instantiate(smallAsteroids[selection], position, Quaternion.identity) as GameObject;
                asteroid.transform.parent = field.transform; */
                break;
            case 2:
                selection = Random.Range(0, mediumAsteroids.Length - 1);
                GameObject asteroid2 = Instantiate(mediumAsteroids[selection], position, Quaternion.identity) as GameObject;
                asteroid2.transform.parent = field.transform;
                break;
            case 3:
                selection = Random.Range(0, largeAsteroids.Length - 1);
                GameObject asteroid3 = Instantiate(largeAsteroids[selection], position, Quaternion.identity) as GameObject;
                asteroid3.transform.parent = field.transform;
                break;
        }


    }

    // Some tuning required
    void PlaceFriendlyFormations()
    {
        Vector3 playerPos = greenPos3D + greenPos3D.normalized * 10;
        GameObject player = Instantiate(playerPrefab, playerPos , Quaternion.identity);

        int nrFormations = normalFormationCount - (int)difficulty;
        Vector3[] fighterPositions = GetFormationPositionOffsets(TeamManager.TeamSide.green, nrFormations);              

        for (int i = 0; i < fighterPositions.Length; i++)
        {
            InstantiateFightersInFormation(friendlyFighterPrefab, fighterPositions, i, 0);
        }

    }

    // Some tuning required
    void PlaceHostileFormations()
    {
        int nrFormations = normalFormationCount + (int)difficulty;
        Vector3[] formationPositions = GetFormationPositionOffsets(TeamManager.TeamSide.red, nrFormations);

        for (int i = 0; i < formationPositions.Length; i++)
        {
            InstantiateFightersInFormation(enemyFighterPrefab, formationPositions, i, 1);
        }
    }

    // Some tuning required
    void InstantiateFightersInFormation(GameObject fighterPrefab, Vector3[] formationPositions, int i, int managerNr)
    {
        GameObject formationLead = null;

        GameObject formation = Instantiate(formation4Prefab, formationPositions[i], Quaternion.identity);
        formationLead = null;
        bool leadVessel = true;
        int wingmanNr = 0;

        foreach (Transform fPosition in formation.transform)
        {
            GameObject vessel = Instantiate(fighterPrefab, fPosition.position, Quaternion.identity) as GameObject;
            VesselAI vesselAI = vessel.GetComponent<VesselAI>();

            if (leadVessel)
            {
                vesselAI.statusInFormation = VesselAI.StatusInFormation.lead;
                vesselAI.flightStatus = VesselAI.FlightStatus.sentry;

                formationLead = vessel;
                teamManagers[managerNr].formations.Add(new LFAI.Formation(vessel));
                teamManagers[managerNr].teamAssets.fighterAmount++;
                leadVessel = false;
            }
            else
            {
                vesselAI.statusInFormation = (VesselAI.StatusInFormation)(wingmanNr + 1);
                vesselAI.formationLead = formationLead;
                vesselAI.wingmanState = VesselAI.WingmanState.inFormation;
                formationLead.GetComponent<VesselAI>().wingmen[wingmanNr] = vessel; wingmanNr++;
                teamManagers[managerNr].teamAssets.fighterAmount++;
            }
            
            vesselAI.teamManager = teamManagers[managerNr];
            vesselAI.teamSide = (TeamManager.TeamSide)managerNr;
        }

        if (formationLead)
        {
            formation.transform.parent = formationLead.transform;
            for (int j = 0; j <= 2; j++) {
                // formationLead.GetComponent<VesselAI>().wingmen[j] = formation.transform.Find("Position" + (j + 2).ToString()).gameObject;
            }
        }
    }

    // Untested
    void PlaceEnemyBase()
    {
        GameObject enemyBase = Instantiate(enemyStationPrefab, redPos3D, Quaternion.identity);
        UpdateObjectiveInTeamManagers(enemyBase, false, false);
        
    }

    // Untested
    void PlaceFriendlyBase()
    {
        GameObject friendlyBase = Instantiate(friendlyStationPrefab, greenPos3D, Quaternion.identity);
        UpdateObjectiveInTeamManagers(friendlyBase, true, false);
        
    }
    
    // Untested
    void PlaceFriendlyMilitary()
    {
        GameObject militaryObjective = Instantiate(militaryEscortablePrefab, greenPos3D, Quaternion.identity);
        UpdateObjectiveInTeamManagers(militaryObjective, true, true);
    }

    // Untested
    void PlaceFriendlyCivilian()
    {
        GameObject civilianObjective = Instantiate(civilianEscortablePrefab, greenPos3D, Quaternion.identity);
        UpdateObjectiveInTeamManagers(civilianObjective, true, true);
    }

    // Untested
    void UpdateObjectiveInTeamManagers(GameObject objectiveObject, bool isGreen, bool moving)
    {
        teamManagers[0].objective = new LFAI.TMObjective(
            objectiveObject,
            isGreen,
            true,
            moving
            );

        teamManagers[1].objective = new LFAI.TMObjective(
            objectiveObject, 
            !isGreen,
            true,
            moving
            );
    }

    // Pretty solid
    Vector3[] GetFormationPositionOffsets(TeamManager.TeamSide side, int nrFormations)
    {
        Vector3[] positions = new Vector3[nrFormations];
        Vector3 startPos = new Vector3(0f,0f,0f);

        MissionPlanner.StartLocation startLocation = MissionPlanner.StartLocation.E;

        switch (side)
        {
            case TeamManager.TeamSide.green:
                startLocation = MissionPlanner.greenStart;
                startPos = greenPos3D;
                break;
            case TeamManager.TeamSide.red:
                startPos = redPos3D;
                startLocation = MissionPlanner.redStart;
                break;
        }

        switch (startLocation)
        {
            case MissionPlanner.StartLocation.N:
                for (int i = 0; i < nrFormations; i++)
                {
                    positions[i] = startPos + new Vector3(
                        20f * (float)(i-2),
                        -15f - UnityEngine.Random.Range(-3f, 3f),
                        0f
                        );
                }
                break;
            case MissionPlanner.StartLocation.NE:
                for (int i = 0; i < nrFormations; i++)
                {
                    positions[i] = startPos + new Vector3(
                        -20f + (float)i * 20f,
                        -(float)i * 20f,
                        0f
                        );
                }

                break;
            case MissionPlanner.StartLocation.E:
                for (int i = 0; i < nrFormations; i++)
                {
                    positions[i] = startPos + new Vector3(                        
                        -15f - UnityEngine.Random.Range(-3f, 3f),
                        20f * (float)(i - 2),
                        0f
                        );
                }
                break;
            case MissionPlanner.StartLocation.SE:
                for (int i = 0; i < nrFormations; i++)
                {
                    positions[i] = startPos + new Vector3(
                        -20f + (float)i * 20f,
                        (float)i * 20f,
                        0f
                        );
                }
                break;
            case MissionPlanner.StartLocation.S:
                for (int i = 0; i < nrFormations; i++)
                {
                    positions[i] = startPos + new Vector3(
                        20f * (float)(i - 2),
                        15f - UnityEngine.Random.Range(-3f, 3f),
                        0f
                        );
                }
                break;
            case MissionPlanner.StartLocation.SW:
                for (int i = 0; i < nrFormations; i++)
                {
                    positions[i] = startPos + new Vector3(
                        20f - (float)i * 20f,
                        (float)i * 15f,
                        0f
                        );
                }
                break;
            case MissionPlanner.StartLocation.W:
                for (int i = 0; i < nrFormations; i++)
                {
                    positions[i] = startPos + new Vector3(
                        15f - UnityEngine.Random.Range(-3f, 3f),
                        20f * (float)(i - 2),
                        0f
                        );
                }
                break;
            case MissionPlanner.StartLocation.NW:
                for (int i = 0; i < nrFormations; i++)
                {
                    positions[i] = startPos + new Vector3(
                        20f - (float)i * 20f,
                        -(float)i * 15f,
                        0f
                        );
                }

                break;

        }


        //switch(MissionPlanner.)


        return positions;
    }
}
