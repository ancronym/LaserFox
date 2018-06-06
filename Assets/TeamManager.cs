using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamManager : MonoBehaviour {

    public enum TeamSide        {       green,          red    }    
    public enum TeamMood        {       noContact,      cautious,   offensive,          defensive,      }
    public enum TeamState       {       Scouting,       Attacking,  MaintainPerimeter,  Regrouping      }
    public enum MissionState    {       ongoing,        won,        lost    }    

    public TeamSide side;
    public MissionState missionState;
    bool changingState = true;

    // ---------------- Decision and flow parameters
    private TeamMood teamMood;
    private TeamState teamState;
    private float balanceROC; // negative means, things are going down hill and vice versa
    private float perimeterIntegrity;
    MissionPlanner.MissionType missionType = MissionPlanner.missionType;


    public LFAI.TMAssets teamAssets;
    public LFAI.TMObjective objective;

    public List<GameObject> capitalShips = new List<GameObject>(5);
    //public List<GameObject> FormationLeads = new List<GameObject>(20);
    public List<LFAI.Formation> formations = new List<LFAI.Formation>(20);

    public List<LFAI.TMProblem> problems = new List<LFAI.TMProblem>(25);

    public Vector3 enemyGeneralPosition;
    public List<RadarController.Bogie> bogies = new List<RadarController.Bogie>(40); public float bogieTimeout = 20f;
    private LFAI.Perimeter perimeter;
    private LFAI.TMSectorImportancePresets sectorImportancePresets;    
    

    float updateRepeat;

    void Start () {
        missionState = MissionState.ongoing;
        
        teamMood = TeamMood.noContact;
        
        if(side == TeamSide.green)
        {
            updateRepeat = MissionMaker.difficulty / 2;
        }else if ( side == TeamSide.red)
        {
            updateRepeat = MissionMaker.difficulty / 2;
        }        

        updateRepeat = Mathf.Clamp(updateRepeat, 0.5f, 2f) + UnityEngine.Random.Range(-0.2f,0.2f);

        InvokeRepeating("UpdateTeam", 2f, updateRepeat);
        InvokeRepeating("RepeatingChecks", 1f, updateRepeat * 0.81f);
        InvokeRepeating("BogieListRemoveOld", 5f, 10f + UnityEngine.Random.Range(0f,5f));
    }
	
	void Update () {
        
	}

    //------------------------------------------------------------------------------------------------------------------------------------
    #region Repeating Checks
    void RepeatingChecks()
    {
        CheckPerimeter();
        CheckProgress();
        CheckObjective();
    }

    void CheckPerimeter()
    {
        if (!perimeter.upToDate) { perimeter.upToDate = ResetPerimeter(); }


        // determine enemy heading range, 
        // min distance
        // adjust perimeter heading
    }

    private bool ResetPerimeter()
    {
        if(objective.objectiveIsFriendly && objective.objectiveObject)
        {
            perimeter.center = objective.objectiveObject.transform.position;

            perimeter.heading2D = new Vector2(
                enemyGeneralPosition.x - perimeter.center.x,
                enemyGeneralPosition.y - perimeter.center.y
                );
            perimeter.headingFloat = NTools.HeadingFromVector(perimeter.heading2D);

            perimeter.isMoving = objective.isMoving;

        } else if(!objective.objectiveIsFriendly && objective.objectiveObject)
        {
            if(capitalShips.Count != 0)
            {
                perimeter.center = NTools.GetCenterOfObjects3D(capitalShips);
                perimeter.center.z = 0f;

                perimeter.heading2D = new Vector2(
                enemyGeneralPosition.x - perimeter.center.x,
                enemyGeneralPosition.y - perimeter.center.y
                );
                perimeter.headingFloat = NTools.HeadingFromVector(perimeter.heading2D);
                perimeter.isMoving = objective.isMoving;

            } else
            {
                //She-fucking-nanigans
                List<GameObject> leads = new List<GameObject>(formations.Count);

                for (int i = 0; i <formations.Count; i++)
                {
                    leads[i] = formations[i].Lead;
                }

                perimeter.center = NTools.GetCenterOfObjects3D(leads);
                perimeter.center.z = 0f;

                perimeter.heading2D = new Vector2(
                enemyGeneralPosition.x - perimeter.center.x,
                enemyGeneralPosition.y - perimeter.center.y
                );
                perimeter.headingFloat = NTools.HeadingFromVector(perimeter.heading2D);
            }
        }

        int mood = (int)teamMood;

        switch (teamState)
        {
            case TeamState.Scouting:
                return AssignSectorPriorities(sectorImportancePresets.scouting[mood]);                
            case TeamState.Attacking:
                return AssignSectorPriorities(sectorImportancePresets.attacking[mood]);                
            case TeamState.MaintainPerimeter:
                return AssignSectorPriorities(sectorImportancePresets.defending[mood]);                
            case TeamState.Regrouping:
                return AssignSectorPriorities(sectorImportancePresets.regrouping[mood]);
            default:
                return false;
        }    
    }

    bool AssignSectorPriorities(float[] priorities)
    {
        if(priorities.Length != perimeter.Sectors.Count) { return false; }

        int i = 0;

        foreach(LFAI.TMSector sector in perimeter.Sectors)
        {
            sector.importance = Mathf.Clamp(priorities[i] + UnityEngine.Random.Range(-0.2f,0.2f),0f,1f);

            i++;
        }

        return true;
    }

    void CheckProgress()
    {

    }

    void CheckObjective()
    {

    }
    #endregion

    //------------------------------------------------------------------------------------------------------------------------------------
    #region TeamState flow
    void UpdateTeam()    {
        switch (teamState)
        {
            case TeamState.Scouting:
                if (changingState)
                {
                    perimeter.upToDate = false;
                    changingState = false;
                }
                Scout();
                break;
            case TeamState.Attacking:
                if (changingState)
                {
                    perimeter.upToDate = false;
                    changingState = false;
                }
                Attack();
                break;
            case TeamState.MaintainPerimeter:
                if (changingState)
                {
                    perimeter.upToDate = false;
                    changingState = false;
                }
                MaintainPerimeter();
                break;
            case TeamState.Regrouping:
                if (changingState)
                {
                    perimeter.upToDate = false;
                    changingState = false;
                }
                Regroup();
                break;
        }
    }

   

    void Scout()
    {

    }

    void Attack()
    {

    }

    void MaintainPerimeter()
    {

    }

    void Regroup()
    {

    }

    #endregion

    //------------------------------------------------------------------------------------------------------------------------------------
    #region Problem Solving
    void SolveProblems()
    {
        foreach(LFAI.TMProblem problem in problems)
        {
            switch (problem.ProbType)
            {
                case LFAI.TMProbType.flightBingo:
                    if(capitalShips.Count == 0)
                    {

                    }
                    break;
                case LFAI.TMProbType.orderRequest:

                    break;
                case LFAI.TMProbType.perimeter:

                    break;

                case LFAI.TMProbType.objective:

                    break;
            }

        }
    }


    #endregion

    #region Helper and Tool methods

    // untested
    // determine sector by world coordinates, returning 401 means out of perimeter, 404 generic error
    private int DetermineSector(GameObject plot)    {

        Vector3 plotBearing3D = plot.gameObject.transform.position - perimeter.center;

        float sqrToPlot = Vector3.SqrMagnitude(plotBearing3D);
        if (sqrToPlot > (perimeter.Sectors[12].distanceRange.y * perimeter.Sectors[12].distanceRange.y))
        { return 404; } // if the dbogie*dbogie is farther than an outer sector farther distance*distance, return 404, i.e out of perimeter
        
        float headingToPlot = NTools.HeadingFromVector(new Vector2(plotBearing3D.x, plotBearing3D.y));
        float bearing = NTools.GetBearingFromHeadings(perimeter.headingFloat,headingToPlot);         
        
        if(bearing > 360 || bearing < 0) { return 404; }            // Sanity check

        // 13 different sectors exist, loop through all, store the one where it is
        for (int i = 0;  i<=12; i++)
        {            
            if (sqrToPlot < Mathf.Pow(perimeter.Sectors[i].headingRange.y, 2f) && sqrToPlot > Mathf.Pow(perimeter.Sectors[i].headingRange.x, 2f))
            {
                if (perimeter.Sectors[i].headingRange.x > perimeter.Sectors[i].headingRange.y)
                {
                    if (bearing < perimeter.Sectors[i].headingRange.x && bearing > perimeter.Sectors[i].headingRange.y)
                    {
                        return i; // i represents the sector index from the sectors array
                    }
                } else if (perimeter.Sectors[i].headingRange.x < perimeter.Sectors[i].headingRange.y)
                {
                    if (bearing < perimeter.Sectors[i].headingRange.x || bearing > perimeter.Sectors[i].headingRange.y)
                    {
                        return i; // i represents the sector index from the sectors array
                    }
                }

            }
            
        }

        Debug.Log("Determine sector: 404");
        return 404;
    }

    private void ComputePerimeterIntegrity()
    {
        // Go through formations, 
            // determine sector 
            // get formation ship count, add to sector

        // go through bogies
            // determine sector
            // add +1 to sector red count

    }


    #endregion

    //------------------------------------------------------------------------------------------------------------------------------------
    #region Communication with other objects and other maintenance
    public void NewFormationLead(GameObject newLead)
    {

    }

    public void FormationReport(LFAI.FormationReport report)
    {

    }

    // If bogie is new, add to TMs bogie list, else replace preexisting with new data
    // Upon first spot, if the spotted vessel is capital ship, the team goes into defence    
    public void BogieSpotted(RadarController.Bogie bogie) {
        if(teamMood == TeamMood.noContact) {
            if (bogie.bogieObject.GetComponent<ShipController>().shipType == ShipController.ShipType.capital)
            {
                teamMood = TeamMood.defensive;
            }
            else
            {
                teamMood = TeamMood.cautious;
            }
        }

        // Check if the gameObject is already described as a plot, if not, add to list of known contacts
        bool addToList = true;

        for(int i = bogies.Count -1; i<=0; i--)
        {
            if(bogie.bogieObject == bogies[i].bogieObject)
            {
                bogies[i] = bogie;
                addToList = false;
            }
        }

        if (addToList) { bogies.Add(bogie); }
    }
    
    // Removes bogies when they have haven't been spotted in a while
    void BogieListRemoveOld()
    {
        if(bogies.Count == 0) { return; }

        float currentTime = Time.timeSinceLevelLoad;

        for (int i = (bogies.Count-1); i <= 0; i--)
        {
            if(bogies[i].timeOfContact < currentTime - bogieTimeout)
            {
                bogies.RemoveAt(i);
            }
        }

    }

    public void OrderRequest(GameObject requester, LFAI.FormationReport report)
    {

    }
    #endregion
}
