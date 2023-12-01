using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    // Singleton pattern
    public static GameState instance;
    void Awake() {
        if (instance != null) {
            Debug.LogError("More than one GameState instance in scene!");
            return;
        }
        instance = this;
    }

    private static GameMaster GM;

    public Vector3 Center => new Vector3(0f, 0f, lineOfScrimmageLocation);

    public enum Team {
        Red = 0,
        Green
    }

    public bool gameStarted { get; private set; }
    public Team coinflipWinner;
    public int quarter;
    public float timeLeftInQuarter;
    public bool clockStopped;
    public int redScore;
    public int greenScore;

    public Team possession;
    public bool hiked;
    public int down;
    private static string[] downSuffixes = new string[4] { "st", "nd", "rd", "th" };
    public float firstDownLocation;
    public float lineOfScrimmageLocation;

    // Start is called before the first frame update
    void Start() {
        GM = GameMaster.instance;

        gameStarted = false;
        clockStopped = true;
    }

    public void InitializeGameState() {
        SetQuarter(1);
        SetDown(1);
        SetTimeLeftInQuarter(Constants.TIME_PER_QUARTER);

        redScore = 0;
        greenScore = 0;
        ServerSend.GameState_Score(redScore, greenScore);

        StartClock();
    }

    public void StartGame() {
        gameStarted = true;
    }
    
    public void EndGame() {
        gameStarted = false;
    }

    void Update() {
        if (gameStarted && !clockStopped) {
            timeLeftInQuarter -= Time.deltaTime;

            if (timeLeftInQuarter < 0f) {
                Debug.Log("[GAMESTATE] Signalling end of quarter.");
                GameMaster.instance.GameState_EndOfQuarter();
            }
        }
    }

    public void SetDown(int _down) {
        down = _down;
        ServerSend.GameState_Down(down);
    }

    public void SetQuarter(int _quarter) {
        Debug.Log($"SetQuarter({_quarter}) called");
        quarter = _quarter;

        ServerSend.GameState_Quarter(quarter);
    }

    public void SetTimeLeftInQuarter(float _timeLeftInQuarter) {
        timeLeftInQuarter = _timeLeftInQuarter;

        ServerSend.GameState_TimeLeftInQuarter(timeLeftInQuarter);
    }

    public void SetFieldPosition(float _lineOfScrimmageLocation, float _firstDownLocation) {
        lineOfScrimmageLocation = _lineOfScrimmageLocation;
        firstDownLocation = _firstDownLocation;

        ServerSend.GameState_FieldPosition(lineOfScrimmageLocation, firstDownLocation);
    }

    public void SetFreshFieldPosition(float _lineOfScrimmageLocation) {
        float _firstDownLocation;
        if (possession == Team.Red) {
            _firstDownLocation = _lineOfScrimmageLocation - 10f;
        } else {
            _firstDownLocation = _lineOfScrimmageLocation + 10f;
        }

        SetFieldPosition(_lineOfScrimmageLocation, _firstDownLocation);
    }

    public void Penalty(float _penaltyYards) {
        float _newLineOfScrimmageLocation;
        if (possession == Team.Red) {
            _newLineOfScrimmageLocation = Mathf.Min(lineOfScrimmageLocation + _penaltyYards, Constants.LINE_OF_SCRIMMAGE_CONSTRAINT);
        } else {
            _newLineOfScrimmageLocation = Mathf.Max(lineOfScrimmageLocation - _penaltyYards, -Constants.LINE_OF_SCRIMMAGE_CONSTRAINT);
        }

        SetFieldPosition(_newLineOfScrimmageLocation, firstDownLocation);
    }

    public void SetPossession(Team _team) {
        possession = _team;

        ServerSend.GameState_Possession(possession);
    }

    public void SwapPossession() {
        if (possession == Team.Red) {
            SetPossession(Team.Green);
        } else {
            SetPossession(Team.Red);
        }
    }

    public void StopClock() {
        Debug.Log("Stopping clock, timeleft: " + timeLeftInQuarter);
        clockStopped = true;

        ServerSend.GameState_StopClock(clockStopped);
        ServerSend.GameState_TimeLeftInQuarter(timeLeftInQuarter);
    }

    public void StartClock() {
        Debug.Log("Starting clock, timeleft: " + timeLeftInQuarter);
        clockStopped = false;

        ServerSend.GameState_StopClock(clockStopped);
        ServerSend.GameState_TimeLeftInQuarter(timeLeftInQuarter);
    }

    public void AddPoints(Team team, int points) {
        if (team == Team.Red) {
            redScore += points;
        } else {
            greenScore += points;
        }

        ServerSend.GameState_Score(redScore, greenScore);
    }

    public int GetScore(Team team) {
        if (team == Team.Red) {
            return redScore;
        } else {
            return greenScore;
        }
    }
}
