using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Unit : MonoBehaviour
{
    public abstract float MoveSpeed { get; }
    public abstract float ThrowRange { get; }
    public abstract float ChargeSpeed { get; }
    public abstract float PushStrength { get; }
    public abstract float TackleStrength { get; }

    public virtual bool CanCatch => true;
    public virtual bool CanHike => false;

    public bool CanThrow => hasBall && canThrow;

    public bool Charged { get => charged; }
    public float Speed { get => Charged ? ChargeSpeed : MoveSpeed; }
    public Vector3 Velocity { get => RB.velocity; }

    // Observer pattern actions
    public event Action OnJuke;

    public int ClickCount { get; set; }

    public GameState.Team Team {
        get {
            return gameObject.tag == "Team1" ? GameState.Team.Red : GameState.Team.Green;
        }
    }

    [Header("Identification")]
    public int unitId;
    public int playerSlotNumber;

    [Header("Cooldowns")]
    private bool chargeOnCooldown = false;
    private bool jukeOnCooldown = false;
    private bool tackleOnCooldown = false;
    private bool stiffOnCooldown = false;
    private float chargeCooldownRemaining = 0f;
    private float jukeCooldownRemaining = 0f;
    private float tackleCooldownRemaining = 0f;
    private float stiffCooldownRemaining = 0f;

    // Ability status
    private bool charged = false;
    private bool juked = false;
    private float chargeDurationRemaining = 0f;
    private float jukeDurationRemaining = 0f;

    // Ball status
    private bool hasBall = false;
    public bool canThrow = true;

    // Endzone status
    public bool inOwnEndzone = false;
    public bool inEnemyEndzone = false;
    public bool touchback = false;

    // Internal tracking for client updates
    private Vector3 previousPosition;
    private Quaternion previousRotation;

    // For controlling movement
    private Rigidbody RB;
    private RigidbodyConstraints originalFreeze;

    private Queue<ICommand> commands;
    private ICommand currentCommand;

    private Grid grid;

    // For sending events up to the game master
    private GameMaster GM;

    void Awake() {
        GM = GameMaster.instance;

        grid = GameObject.Find("A*").GetComponent<Grid>();

        RB = gameObject.GetComponent<Rigidbody>();
        originalFreeze = RB.constraints;

        commands = new Queue<ICommand>();

        previousPosition = transform.position;
        previousRotation = transform.rotation;

        ClickCount = 0;
    }

    void Start() {
        FreezePosition(true);
        UpdateGrid(true);
    }

    void FixedUpdate() {
        if (currentCommand != null && !currentCommand.IsFinished) {
            currentCommand.FixedUpdate();
        } else if (!GameState.instance.hiked) {
            if (this.CanHike) {
                transform.rotation = Quaternion.LookRotation(GameState.instance.Center - transform.position);
            } else {
                transform.rotation = Quaternion.LookRotation(Team == GameState.Team.Red ? Vector3.back : Vector3.forward);
            }
        }

        UpdateGrid();
        FixedUpdatePosition();
    }

    private void UpdateGrid(bool _forceUpdate = false) {
        if (_forceUpdate || transform.position != previousPosition) {
            grid.UpdateUnitPosition(this);
        }
    }

    private void FixedUpdatePosition() {
        if (transform.position != previousPosition) {
            // Debug.Log($"[UNIT] Sending position update, unit #: {unitId}");
            ServerSend.UnitPositionUpdate(unitId, transform.position);
            previousPosition = transform.position;
        }

        if (transform.rotation != previousRotation) {
            // Debug.Log($"[UNIT] Sending rotation update, unit #: {unitId}");
            ServerSend.UnitRotationUpdate(unitId, transform.rotation);
            previousRotation = transform.rotation;
        }
    }

    void Update() {
        UpdateDurations();
        UpdateCooldowns();

        ProcessCommands();
    }

    #region Update Durations and Cooldowns
    private void UpdateDurations() {
        if (charged) {
            chargeDurationRemaining -= Time.deltaTime;
            if (chargeDurationRemaining <= 0f) {
                charged = false;
            }
        }

        if (juked) {
            jukeDurationRemaining -= Time.deltaTime;
            if (jukeDurationRemaining <= 0f) {
                juked = false;
            }
        }
    }

    private void UpdateCooldowns() {
        if (chargeOnCooldown) {
            chargeCooldownRemaining -= Time.deltaTime;

            if (chargeCooldownRemaining <= 0f) {
                chargeOnCooldown = false;
            }
        }

        if (jukeOnCooldown) {
            jukeCooldownRemaining -= Time.deltaTime;

            if (jukeCooldownRemaining <= 0f) {
                jukeOnCooldown = false;
            }
        }

        if (tackleOnCooldown) {
            tackleCooldownRemaining -= Time.deltaTime;

            if (tackleCooldownRemaining <= 0f) {
                tackleOnCooldown = false;
            }
        }

        if (stiffOnCooldown) {
            stiffCooldownRemaining -= Time.deltaTime;

            if (stiffCooldownRemaining <= 0f) {
                stiffOnCooldown = false;
            }
        }
    }
    #endregion

    #region Command Handling
    private void ProcessCommands() {
        if (currentCommand != null && currentCommand.IsFinished == false) {
            return;
        }

        if (commands.Count == 0) {
            return;
        }

        currentCommand = commands.Dequeue();
        currentCommand.Execute();
    }

    public void AddCommand(ICommand _command, bool _shiftHeld = false) {
        if (!_shiftHeld) {
            ClearCommands();
        }

        commands.Enqueue(_command);
    } 

    public void ClearCommands() {
        foreach (ICommand c in commands) {
            c.CleanUp();
        }

        commands.Clear();
        currentCommand = null;
    }

    public void Destroy() {
        OnDestroy();
        Destroy(gameObject);
    }

    private void OnDestroy() {
        ClearCommands();
        grid.UpdateUnitDestroyed(this);
    }
    #endregion

    #region Movement Helpers
    public void MoveTo(Vector3 _location) {
        FreezePosition(false);

        // To ensure all unit velocities are on the same plane
        Vector3 pos = transform.position;
        pos.y = 1;
        _location.y = 1;

        Vector3 dir = (_location - pos).normalized;
        RB.velocity = dir * Speed;

        transform.rotation = Quaternion.Euler(
            transform.rotation.eulerAngles.x,
            Quaternion.LookRotation(dir).eulerAngles.y,
            transform.rotation.eulerAngles.z
        );
    }

    public void StopMoving() {
        RB.velocity = Vector3.zero;
        FreezePosition(true);
    }

    private void FreezePosition(bool freeze) {
        if (freeze) {
            RB.constraints = 
                RigidbodyConstraints.FreezePosition
                | RigidbodyConstraints.FreezeRotation;
        } else {
            RB.constraints = 
                originalFreeze
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;
        }
    }
    #endregion

    #region Abilities
    public void Charge() {
        if (chargeOnCooldown) {
            return;
        }

        chargeOnCooldown = true;
        charged = true;

        chargeCooldownRemaining = Constants.COOLDOWN_CHARGE;
        chargeDurationRemaining = Constants.DURATION_CHARGE;

        ServerSend.UnitCharged(unitId, chargeCooldownRemaining, chargeDurationRemaining);
    }

    public void Juke() {
        if (jukeOnCooldown) {
            return;
        }

        // Put it on cooldown
        jukeOnCooldown = true;
        jukeCooldownRemaining = Constants.COOLDOWN_JUKE;

        // Eliminate all active commands
        ClearCommands();

        // Stop moving
        StopMoving();

        // Invoke Juke action for any units that have this unit targeted in a command
        // Observer pattern
        OnJuke?.Invoke();

        // Initiate the juke
        juked = true;
        jukeDurationRemaining = Constants.DURATION_JUKE;

        // Let the client know a juke occurred
        ServerSend.UnitJuked(unitId, jukeCooldownRemaining, jukeDurationRemaining);
    }

    public void Tackle(Unit unitToTackle) {
        if (tackleOnCooldown) {
            return;
        }

        // Put it on cooldown
        tackleOnCooldown = true;
        tackleCooldownRemaining = Constants.COOLDOWN_TACKLE;

        // Stop moving
        StopMoving();

        // Rotate this unit toward the unit being tackled
        Vector3 dir = Vector3.Normalize(unitToTackle.transform.position - transform.position);
        transform.rotation = Quaternion.LookRotation(dir);

        // Cause the target to react
        unitToTackle.Tackled(this, dir);

        // Let the client know that a tackle occurred
        ServerSend.UnitTackled(unitId, unitToTackle.unitId, tackleCooldownRemaining);
    }

    public void Stiff(Unit unitToStiff) {
        if (stiffOnCooldown) {
            Debug.Log("Stiff is on cooldown! Cooldown remaining: " + stiffCooldownRemaining);
            return;
        }

        // Put it on cooldown
        stiffOnCooldown = true;
        stiffCooldownRemaining = Constants.COOLDOWN_STIFF;

        // Stop moving
        StopMoving();

        // Rotate this unit toward the unit being stiffed
        Vector3 dir = Vector3.Normalize(unitToStiff.transform.position - transform.position);
        transform.rotation = Quaternion.LookRotation(dir);

        // Cause the target to react
        unitToStiff.Stiffed(this);

        // Let the client know that a stiff occurred
        ServerSend.UnitStiffed(unitId, unitToStiff.unitId, stiffCooldownRemaining);
    }
    #endregion

    public void CreateAndThrowBall(Ball ballReference, Vector3 destination) {
        // Limit the throw's range if necessary
        float attemptedRange = Vector3.Distance(destination, transform.position);
        Vector3 direction = Vector3.Normalize(destination - transform.position);
        if (attemptedRange > ThrowRange) {
            // Normalize the vector (keep the direction, make magnitude 1) then multiply by throw range
            destination = transform.position + (direction * ThrowRange);
        }

        ClearCommands();
        StopMoving();

        transform.rotation = Quaternion.LookRotation(direction);
        ServerSend.BallThrown(unitId, transform.position, destination);
        RemoveBall();
        Vector3 startPos = transform.position;
        startPos.y += 0.5f; // bit higher release than center of body, around the head
        Ball ball = Instantiate(ballReference, startPos, Quaternion.identity);
        ball.SetThrower(this);
        ball.SetOrigin(startPos);
        ball.SetDestination(destination);
        ball.Launch();
    }

    #region Abilities Received
    public void Tackled(Unit tackledBy, Vector3 _dir) {
        ClearCommands();
        StopMoving();

        if (hasBall) {
            if (inOwnEndzone) {
                if (touchback) {
                    GM.Event_Touchback(this);
                } else {
                    GM.Event_Safety(this);
                }
            } else {
                GM.Event_UnitTackledWithBall(this, tackledBy);
            }
        } else {
            Pushed(_dir, tackledBy.TackleStrength);
        }
    }

    public void Stiffed(Unit stiffedBy) {
        ClearCommands();
        StopMoving();
    }

    public void Pushed(Vector3 direction, float strength) {
        ClearCommands();
        StopMoving();

        direction.y = 0;
        transform.Translate(direction * strength, Space.World);
    }


    public void CaughtBall(Ball ball, bool onHike) {
        GiveBall();

        if (onHike) {
            ServerSend.HikeCaught(unitId);
            if (inEnemyEndzone) {
                GM.Event_Touchdown(this);
            }
        } else {
            Unit thrower = ball.GetThrower();
            ServerSend.BallCaught(unitId, thrower.unitId);

            if (inEnemyEndzone) {
                GM.Event_Touchdown(this);
            } else if (inOwnEndzone && thrower.gameObject.tag != gameObject.tag) {
                // Since there are no kickoffs, the only time we allow a touchback is if we catch an interception in our own endzone. So we set this to true here and remove it after leaving the endzone. To be checked on tackle for touchback.
                touchback = true;
            }
        }
    }
    #endregion

    #region Cooldowns
    public float GetChargeCooldown() {
        return chargeCooldownRemaining;
    }

    public float GetJukeCooldown() {
        return jukeCooldownRemaining;
    }

    public float GetTackleCooldown() {
        return tackleCooldownRemaining;
    }

    public float GetStiffCooldown() {
        return stiffCooldownRemaining;
    }

    public bool IsChargeOnCooldown() {
        return chargeOnCooldown;
    }

    public bool IsJukeOnCooldown() {
        return jukeOnCooldown;
    }

    public bool IsTackleOnCooldown() {
        return tackleOnCooldown;
    }

    public bool IsStiffOnCooldown() {
        return stiffOnCooldown;
    }
    #endregion

    #region Ball Interactions
    public void GiveBall() {
        hasBall = true;
    }

    public void RemoveBall() {
        hasBall = false;
    }

    public bool HasBall() {
        return hasBall;
    }

    // for setting canThrow = false and reflecting that back to client
    public void CantThrow() {
        canThrow = false;

        ServerSend.UnitCantThrow(unitId);
    }
    #endregion

    #region Collisions
    void OnCollisionEnter(Collision collision) {
        if (collision.collider.gameObject.layer == gameObject.layer) {
            Unit collidedWith = collision.collider.gameObject.GetComponent<Unit>();
            if (collidedWith == null) {
                Debug.Log("collidedWith was null, don't think this should happen.");
                return;
            }

            if (currentCommand != null && !currentCommand.IsFinished) {
                currentCommand.OnCollisionEnter(collision, collidedWith);
            }
        }
    }

    void OnTriggerEnter(Collider collision) {
        GameObject entityCollidingWith = collision.gameObject;
        if (entityCollidingWith.tag == "Endzone") {
            if ((gameObject.tag == "Team1" && entityCollidingWith == GM.southEndzoneTrigger) ||
                (gameObject.tag == "Team2" && entityCollidingWith == GM.northEndzoneTrigger)) {
                if (hasBall) {
                    GameMaster.instance.Event_Touchdown(this);
                } else {
                    inEnemyEndzone = true;
                }
            } else {
                inOwnEndzone = true;
            }
            
        } else if (entityCollidingWith.tag == "OutOfBounds") {
            if (hasBall) {
                GM.Event_UnitWentOutWithBall(this);
            }
        } else if (entityCollidingWith.tag == "LineOfScrimmage") {
            if (!GameState.instance.hiked) {
                GM.Event_FalseStart(this);
                return;
            }

            CantThrow();
        }
    }

    void OnTriggerExit(Collider collision) {
        GameObject entityCollidingWith = collision.gameObject;
        if (entityCollidingWith.tag == "Endzone") {
            inEnemyEndzone = false;
            inOwnEndzone = false;
            touchback = false;
        }
    }
    #endregion
}
