using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMaster : MonoBehaviour {
    public static GameMaster instance;

    public GameObject northEndzoneTrigger;
    public GameObject southEndzoneTrigger;
    public GameObject snapFormationPrefab;
    public GameObject firstDownLinePrefab;
    public Ball ballPrefab;
    private GameObject snapFormation;
    private GameObject firstDownLine;

    public static int[] slots = new int[12] { 1, 7, 6, 12, 5, 11, 2, 8, 3, 9, 4, 10 };
    private static bool[] slotTaken = new bool[12]; // default bool value is false

    // INT: clientId
    // private static Dictionary<int, PlayerInfo> players;
    private Dictionary<int, Unit> unitMap;
    private Grid pathfindingGrid;

    private GameState GS;
    private Stats S;
    private AutoPrevention AP;

    private bool endOfQuarterSignaled = false;
    private bool killGameSignaled = false;
    private bool justIntercepted = false;

    private void Awake() {
        if (instance != null) {
            Debug.LogError("More than one GameMaster instance in scene!");
            return;
        }
        instance = this;
    }

    private void Start() {
        unitMap = new Dictionary<int, Unit>();

        GS = GameState.instance;
        S = Stats.instance;
        AP = AutoPrevention.instance;

        pathfindingGrid = GameObject.Find("A*").GetComponent<Grid>();
    }

    void Update() {
        if (killGameSignaled) {
            killGameSignaled = false;
            KillGame();
        }
    }

    #region Controlling the Game
    public void SignalGameStart() {
        endOfQuarterSignaled = false;
        killGameSignaled = false;
        justIntercepted = false;

        ServerSend.GameStarting();
        GS.StartGame();

        // give the client some time to change scenes, load up, etc.
        Invoke("StartGame", 5f);
    }
    
    void StartGame() {
        CoinflipAtStart();
        GS.InitializeGameState();
        S.InitPlayers();
        SetInitialFieldLocations();
        CreateSnapFormation();
    }

    void CoinflipAtStart() {
        if (Server.lobby.Count == 1) { // for testing purposes, give red the ball if there's only one
            GS.SetPossession(GameState.Team.Red);
            return;
        }

        GS.coinflipWinner = CoinflipForPossession();
        GS.SetPossession(GS.coinflipWinner);
    }

    public static GameState.Team CoinflipForPossession() {
        return (GameState.Team)UnityEngine.Random.Range((int)GameState.Team.Red, (int)GameState.Team.Green + 1);
    }

    void SetInitialFieldLocations() {
        if (GS.possession == GameState.Team.Red) {
            GS.SetFreshFieldPosition(30f);
        } else {
            GS.SetFreshFieldPosition(-30f);
        }
    }

    void CreateSnapFormation() {
        if (snapFormation != null) DestroySnapFormation();

        pathfindingGrid.ResetGrid();

        // Before continuing, check to see if GameState let us know that the quarter ended.
        if (endOfQuarterSignaled) {
            Event_EndOfQuarter();
            return;
        }

        GS.hiked = false;
        snapFormation = Instantiate(snapFormationPrefab, new Vector3(0f, 0f, GS.lineOfScrimmageLocation), Quaternion.identity);
        firstDownLine = Instantiate(firstDownLinePrefab, new Vector3(0f, -1f, GS.firstDownLocation), Quaternion.identity);

        ServerSend.NewSnapFormation(GS.lineOfScrimmageLocation, GS.firstDownLocation, (int)GS.possession);

        // Populate the unit map with the snap formation we just instantiated
        unitMap.Clear();
        Unit[] units = snapFormation.GetComponentsInChildren<Unit>();
        if (units == null) {
            Debug.LogWarning("Units were null when trying set the unit map on snap formation.");
        } else {
            for (int i = 0; i < units.Length; i++) {
                unitMap.Add(units[i].unitId, units[i]);

                if (!HasPossession(units[i])) {
                    // we set the member directly because we dont want to send a packet before the client has set up the snap formation
                    units[i].canThrow = false;
                }
            }
        }

        ServerSend.NewSnapFormation(GS.lineOfScrimmageLocation, GS.firstDownLocation, (int)GS.possession);
    }

    void DestroySnapFormation() {
        if (firstDownLine != null) Destroy(firstDownLine);

        foreach (Unit unit in unitMap.Values) {
            if (unit != null) {
                unit.Destroy();
            }
        }

        Destroy(snapFormation);
    }

    public void GameState_EndOfQuarter() {
        endOfQuarterSignaled = true;
    }
    #endregion

    #region Events
    public void Event_EndOfQuarter() {
        ServerSend.QuarterEnding();

        switch (GS.quarter) {
            case 1:
            case 3:
                GS.SetQuarter(GS.quarter + 1);
                GS.SetTimeLeftInQuarter(Constants.TIME_PER_QUARTER);
                endOfQuarterSignaled = false;
                Invoke("CreateSnapFormation", 2f);
                break;
            case 2:
                Event_Halftime();
                break;
            case 4:
                if (GS.GetScore(GameState.Team.Red) == GS.GetScore(GameState.Team.Green)) {
                    Event_GoingIntoOvertime();
                } else {
                    Event_EndOfGame();
                }
                break;
            default:
                Debug.LogWarning("Default case reached in EndOfQuarter... should not happen unless overtime is implemented. In which case, GameMaster.Event_EndOfQuarter needs updating.");
                break;
        }
    }

    public void Event_Halftime() {
        GS.SetQuarter(3);
        GS.SetDown(1);
        GS.SetTimeLeftInQuarter(Constants.TIME_PER_QUARTER);
        endOfQuarterSignaled = false;

        // Whoever lost the coinflip at the start gets possession
        GS.SetPossession(GS.coinflipWinner == GameState.Team.Red ? GameState.Team.Green : GameState.Team.Red);
        SetInitialFieldLocations();

        Invoke("CreateSnapFormation", 5f);
    }

    public void Event_GoingIntoOvertime() {
        Debug.Log("The game is going into overtime! Oh shit!");
    }

    public void Event_EndOfGame() {
        Debug.Log("It's the end of the game! Do something!");
    }

    public void Event_IncompletePass(Unit _thrower, Vector3 _landedPos) {
        ServerSend.BallIncomplete(_thrower.unitId, _landedPos);

        if (GS.down < 4) {
            GS.SetDown(GS.down + 1);
        } else { // Turnover
            GS.SetDown(1);
            GS.SwapPossession();
            GS.SetFreshFieldPosition(GS.lineOfScrimmageLocation);

            ServerSend.TurnoverOnDowns();
        }

        Invoke("CreateSnapFormation", 4f);
    }

    public void Event_Interception(Unit _catcher, Unit _thrower) {
        ServerSend.BallIntercepted(_catcher.unitId, _thrower.unitId);

        GS.SwapPossession();
        justIntercepted = true;
    }

    public void Event_Touchdown(Unit _unit) {
        S.AddToStat(GetClientForUnit(_unit), Stats.Stat.TOUCHDOWNS, 1);
        _unit.RemoveBall();
        ServerSend.UnitScoredTouchdown(_unit.unitId);

        GS.AddPoints(GS.possession, 7);
        GS.SwapPossession();
        GS.SetDown(1);

        SetInitialFieldLocations();
        Invoke("CreateSnapFormation", 4f);
    }

    public void Event_UnitTackledWithBall(Unit _tackled, Unit _tackler) {
        _tackled.RemoveBall();
        ServerSend.UnitTackledWithBall(_tackled.unitId, _tackler.unitId);
        BallDownedAt(_tackled.transform.position.z);
        _tackled.Destroy();
        Invoke("CreateSnapFormation", 4f);
    }

    public void Event_Safety(Unit _unit) {
        _unit.RemoveBall();
        ServerSend.Safety(_unit.unitId);
        _unit.Destroy();

        GS.SwapPossession();
        GS.AddPoints(GS.possession, 2);
        GS.SetDown(1);

        SetInitialFieldLocations();
        Invoke("CreateSnapFormation", 4f);
    }

    public void Event_Touchback(Unit _unit) {
        _unit.RemoveBall();
        ServerSend.Touchback(_unit.unitId);
        _unit.Destroy();
        GS.SetDown(1);
        SetInitialFieldLocations();
        Invoke("CreateSnapFormation", 4f);
    }

    public void Event_UnitWentOutWithBall(Unit _unit) {
        ServerSend.UnitWentOutWithBall(_unit.unitId);

        if (_unit.touchback) {
            Event_Touchback(_unit);
        } else if (_unit.inOwnEndzone) {
            Event_Safety(_unit);
        } else {
            BallDownedAt(_unit.transform.position.z);
        }

        Invoke("CreateSnapFormation", 4f);
    }

    public void Event_FalseStart(Unit _unit) {
        if (_unit.GetComponent<Lineman>() != null) { // exclude linemen so we can push them in
            return;
        }

        ServerSend.FalseStart(_unit.unitId);
        _unit.Destroy();
        GS.Penalty(5f);
        Invoke("CreateSnapFormation", 2f);
    }

    private void BallDownedAt(float location) {
        bool crossedFirstDown;
        if (GS.possession == GameState.Team.Red) {
            crossedFirstDown = location < GS.firstDownLocation;
        } else {
            crossedFirstDown = location > GS.firstDownLocation;
        }

        if (crossedFirstDown || justIntercepted) {
            justIntercepted = false;
            GS.SetDown(1);
            GS.SetFreshFieldPosition(location);
        } else {
            if (GS.down < 4) {
                GS.SetDown(GS.down + 1);
                GS.SetFieldPosition(location, GS.firstDownLocation);
            } else { // Turnover
                GS.SetDown(1);
                GS.SwapPossession();
                GS.SetFreshFieldPosition(location);
                ServerSend.TurnoverOnDowns();
            }
        }
    }
    #endregion

    #region ServerHandle Callbacks
    public void SignalKillGame() {
        killGameSignaled = true;
    }

    private void KillGame() {
        Debug.Log("Ending the game.");

        if (snapFormation != null) Destroy(snapFormation);
        if (firstDownLine != null) Destroy(firstDownLine);
        GS.EndGame();

        // Wipe the stats
        S.Clear();
    }

    public void MoveToIssued(int _clientId, int _unitId, Vector3 _dest, bool _shiftHeld) {
        if (!ClientOwnsUnit(_clientId, _unitId)) {
            return;
        }

        Unit unit = GetUnitById(_unitId);
        if (unit == null || (!GS.hiked && !unit.CanHike)) {
            return;
        }

        unit.AddCommand(new MoveToSpotCommand(unit, _dest), _shiftHeld);
    }

    public void UnitRightClicked(int _clientId, int _clickerUnitId, int _clickedUnitId, bool _shiftHeld) {
        if (!ClientOwnsUnit(_clientId, _clickerUnitId)) {
            return;
        }

        Unit clickerUnit = GetUnitById(_clickerUnitId);
        Unit clickedUnit = GetUnitById(_clickedUnitId);
        if (clickerUnit == null || clickedUnit == null) {
            return;
        }

        if (!GS.hiked && !clickerUnit.CanHike) {
            return;
        }

        float distance = Vector3.Distance(clickerUnit.transform.position, clickedUnit.transform.position);

        // we don't want to allow users to queue up pushes, so we check this manually here
        if (distance < Constants.MINIMUM_PUSH_DISTANCE && !clickedUnit.Charged) {
            // Auto check
            if (!AP.Allowed(clickerUnit)) {
                return;
            }

            AP.RegisterClick(clickerUnit);

            Vector3 dir = (clickedUnit.transform.position - clickerUnit.transform.position).normalized;
            clickedUnit.Pushed(dir, clickerUnit.PushStrength);
            clickerUnit.Pushed(dir, clickerUnit.PushStrength);
        } else {
            clickerUnit.AddCommand(new FollowCommand(clickerUnit, clickedUnit), _shiftHeld);
        }
    }

    public void UnitCharge(int _clientId, int _unitId) {
        if (!ClientOwnsUnit(_clientId, _unitId)) {
            return;
        }

        Unit unit = GetUnitById(_unitId);

        if (!GS.hiked && !unit.CanHike) {
            return;
        }

        unit.Charge();
    }

    public void UnitJuke(int _clientId, int _unitId) {
        if (!ClientOwnsUnit(_clientId, _unitId)) {
            return;
        }

        Unit unit = GetUnitById(_unitId);

        if (!GS.hiked && !unit.CanHike) {
            return;
        }

        unit.Juke();
    }

    public void UnitTackle(int _clientId, int _unitTackling, int _unitBeingTackled, bool _shiftHeld) {
        if (!ClientOwnsUnit(_clientId, _unitTackling)) {
            return;
        }

        Unit unitTackling = GetUnitById(_unitTackling);
        Unit unitBeingTackled = GetUnitById(_unitBeingTackled);

        if (!GS.hiked && !unitTackling.CanHike) {
            return;
        }

        unitTackling.AddCommand(new TackleCommand(unitTackling, unitBeingTackled), _shiftHeld);
    }

    public void UnitStiff(int _clientId, int _unitStiffing, int _unitBeingStiffed, bool _shiftHeld) {
        if (!ClientOwnsUnit(_clientId, _unitStiffing)) {
            return;
        }

        Unit unitStiffing = GetUnitById(_unitStiffing);
        Unit unitBeingStiffed = GetUnitById(_unitBeingStiffed);

        if (!GS.hiked && !unitStiffing.CanHike) {
            return;
        }

        unitStiffing.AddCommand(new StiffCommand(unitStiffing, unitBeingStiffed), _shiftHeld);
    }

    public void UnitThrow(int _clientId, int _unitId, Vector3 _dest) {
        if (!ClientOwnsUnit(_clientId, _unitId)) {
            return;
        }

        if (!GS.hiked) {
            return;
        }

        Unit unit = GetUnitById(_unitId);
        if (!unit.CanThrow) {
            return;
        }

        S.AddToStat(_clientId, Stats.Stat.THROWS, 1);

        unit.CreateAndThrowBall(ballPrefab, _dest);
    }

    public void UnitHike(int _clientId, int _unitId) {
        if (!ClientOwnsUnit(_clientId, _unitId)) {
            return;
        }

        if (GS.hiked) {
            return;
        }

        Unit unit = GetUnitById(_unitId);
        if (!unit.CanHike) {
            return;
        }

        GS.hiked = true;
        unit.ClearCommands();
        unit.StopMoving();
        HikeBall(unit);
    }

    public void UnitStop(int _clientId, int _unitId) {
        if (!ClientOwnsUnit(_clientId, _unitId)) {
            return;
        }

        Unit unit = GetUnitById(_unitId);
        unit.ClearCommands();
        unit.StopMoving();
    }

    private void HikeBall(Unit _unit) {
        Vector3 _pos = _unit.transform.position;
        Vector3 _origin = new Vector3(0f, 0f, GS.lineOfScrimmageLocation);
        // Limit the distance if necessary
        float attemptedRange = Vector3.Distance(_origin, _pos);
        Vector3 direction = Vector3.Normalize(_pos - _origin);
        if (attemptedRange > Constants.BALL_HIKE_RANGE) {
            _pos = _origin + (direction * Constants.BALL_HIKE_RANGE);
        }

        _unit.transform.rotation = Quaternion.LookRotation(direction * -1);
        ServerSend.Hiked(_unit.unitId, _origin, _pos);
        Ball ball = Instantiate(ballPrefab, _origin, Quaternion.identity);
        ball.SetHike();
        ball.SetThrower(_unit);
        ball.SetOrigin(_origin);
        ball.SetDestination(_pos);
        ball.Launch();
    }
    #endregion

    #region Helper Functions
    public Unit GetUnitById(int _unitId) {
        if (unitMap == null) {
            Debug.Log($"The unit map was null when trying to get unit {_unitId} by ID.");
            return null;
        }

        Unit unit;
        if (!unitMap.TryGetValue(_unitId, out unit)) {
            Debug.Log($"The unit map contained no unit with ID {_unitId}.");
            return null;
        }

        return unit;
    }

    private bool ClientOwnsUnit(int _clientId, int _unitId) {
        try {
            PlayerInfo player = Server.lobby.GetPlayerInfoByClientId(_clientId);
            Unit unit = GetUnitById(_unitId);

            return player.slotNumber == unit.playerSlotNumber;
        } catch (Exception _ex) {
            Debug.Log($"Client {_clientId} owns unit {_unitId} check errored: {_ex}");
        }

        return false;
    }

    public bool HasPossession(Unit _unit) {
        if (GS.possession == _unit.Team) {
            return true;
        }

        return false;
    }

    public int GetClientForUnit(Unit _unit) {
        int slotNumber = _unit.playerSlotNumber;
        return Server.lobby.GetClientIdForSlot(slotNumber);
    }
    #endregion
}
