using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamManager : MonoBehaviour {

    public enum TeamSide        {       green,          red    }    
    public enum TeamMood        {       noContact,      defensive,  cautious,           offensive,      }   // translates to danger tolerance often
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

    public List<LFAI.TMProblem> newProblems = new List<LFAI.TMProblem>(25);
    public List<LFAI.TMProblem> problemsBeingSolved = new List<LFAI.TMProblem>(25);

    public Vector3 enemyGeneralPosition;
    public List<RadarController.Bogie> bogies = new List<RadarController.Bogie>(40); public float bogieTimeout = 20f;
    private LFAI.Perimeter perimeter = new LFAI.Perimeter(new Vector2(0,0),0,false);
    private LFAI.TMSectorImportancePresets sectorImportancePresets;    
    

    float updateRepeat;

    void Start () {
        perimeter.upToDate = false;

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

        // Problem solving repeating calls
        InvokeRepeating("SolveTopProblem", 3f, 0.5f + UnityEngine.Random.Range(-0.1f, 0.1f));
        InvokeRepeating("SortNewProblemList", 10f, 5f + UnityEngine.Random.Range(-2f, 2f));
        InvokeRepeating("CheckProblemsBeingSolved", 12f, 10f + UnityEngine.Random.Range(-2f, 2f));

        
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

        GetAndProcessReports();
    }

    void CheckPerimeter()
    {
        if (!perimeter.upToDate) { perimeter.upToDate = ResetPerimeter(); }

        // determine enemy heading range etc i.e disposition
        List<GameObject> bogieGOs = new List<GameObject>(bogies.Count);

        for (int i = 0; i < bogies.Count; i++)
        {
            bogieGOs[i] = bogies[i].bogieObject;
        }

        NTools.Slice enemyDisposition = NTools.AnalyseGroupDisposition(
            perimeter.center,
            perimeter.headingFloat,
            bogieGOs
            );

        // Adjust perimeter heading based on enemy positions center of weight

        // Determine green and nongreen population of sectors

        // Compare population to sector importance, compute importanceMet value
               // if needed, add problem of type perimeter
               // if there exists a problem with said sector, but is not relevant anymore, remove problem





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


        // Adds an importance to each sector based on tabel in LFAI
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

    //------------------------is this even relevant ...-----------------------------------------------------------------------------------
    #region TeamState flow // is this even relevant ...
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


    void SortNewProblemList()
    {
        if (newProblems.Count == 0) { return; }

    }

    void CheckProblemsBeingSolved()
    {
        if (problemsBeingSolved.Count == 0) { return; }

    }

    void SolveTopProblem()
    {        
        if(newProblems.Count == 0) { return; }

        LFAI.TMProblem problem = newProblems[newProblems.Count - 1];
        float solutionParameter;

        switch (problem.ProbType)
        {
            case LFAI.TMProbType.flightBingo:
                solutionParameter = SolveFlightBingo(problem);
                break;
            case LFAI.TMProbType.flightInDanger:
                solutionParameter = SolveFlightDanger(problem);
                break;
            case LFAI.TMProbType.orderRequest:
                solutionParameter = SolveOrderRequest(problem);
                break;
            case LFAI.TMProbType.perimeter:
                solutionParameter = SolvePerimeter(problem);
                break;

            case LFAI.TMProbType.objectiveAttack:
                solutionParameter = SolveObjectiveAttack(problem);
                break;
            case LFAI.TMProbType.objectiveDefence:
                solutionParameter = SolveObjectiveDefence(problem);
                break;
        }


        problemsBeingSolved.Add(problem);
        newProblems.RemoveAt(newProblems.Count - 1);

    }

    float SolveFlightBingo(LFAI.TMProblem problem) {
        float solutionParameter = 0f;






        return solutionParameter;
    }

    float SolveFlightDanger(LFAI.TMProblem problem)
    {
        float solutionParameter = 0f;






        return solutionParameter;
    }

    float SolveOrderRequest(LFAI.TMProblem problem)
    {
        float solutionParameter = 0f;






        return solutionParameter;
    }
    
    float SolvePerimeter(LFAI.TMProblem problem)
    {
        float solutionParameter = 0f;






        return solutionParameter;
    }

    float SolveObjectiveAttack(LFAI.TMProblem problem)
    {
        float solutionParameter = 0f;






        return solutionParameter;
    }

    float SolveObjectiveDefence(LFAI.TMProblem problem)
    {
        float solutionParameter = 0f;






        return solutionParameter;
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
    public void NewFormationLead(GameObject previousLead, GameObject newLead)
    {

    }


    // untested, This handles all incoming reports from flights
    private void GetAndProcessReports()
    {
        LFAI.FormationReport report;

        // problem prioritization parameters, computed every time for being up to date
        float bingoPriority = 0.5f;
        float dangerTolerance = (float)teamMood / 3;

        foreach(LFAI.Formation formation in formations)
        {
            report = formation.Lead.GetComponent<VesselAI>().ReportRequest();
            float danger = EvaluateDanger(report.formationSize, report.nrHostiles);

            if (report.fuelIsBingo)
            {
                if (!CheckNewProblemDupe(LFAI.TMProbType.flightBingo, formation))
                {
                    newProblems.Add(new LFAI.TMProblem(
                        LFAI.TMProbType.flightBingo,
                        bingoPriority,
                        90,              // if fuel is again over 90% average for flight, problem is solved
                        formation
                        ));
                }
            }            

            if (danger > dangerTolerance)
            {
                if (!CheckNewProblemDupe(LFAI.TMProbType.flightInDanger, formation))
                {
                    newProblems.Add(new LFAI.TMProblem(
                        LFAI.TMProbType.flightInDanger,
                        1f - dangerTolerance,       // the higher the threat tolerance, the lower the priority and vice versa
                        dangerTolerance,            // problem will be solved, if the threat level is lower than tolerance
                        formation
                        ));
                }
            }

        }
    }

    // untested, returns true, if problem already exists
    bool CheckNewProblemDupe(LFAI.TMProbType newType, LFAI.Formation formation)
    {
        bool dupe = false;

        foreach(LFAI.TMProblem problem in newProblems)
        {
            if(problem.problemFormation == formation && problem.ProbType == newType)
            {
                dupe = true;
            }
        }

        return dupe;
    }

    // untested, returns danger between 0 and 1. 0 being no danger, 1 being three times or more baddies as friendlies.
    private float EvaluateDanger(int formationSize, int nrHostiles)
    {
        float danger = 0f;

        danger = ((float)nrHostiles / (float)formationSize) * 0.33f; 
        // if nrs are even, then danger is 0.33
        // max danger is 1, when there are three times as many hostiles than friendlies
       
        return Mathf.Clamp(danger,0f,1f);
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
