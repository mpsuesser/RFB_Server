public static class Constants
{
    public const string VERSION = "0.4.0";

    // Server
    public const int SERVER_LISTEN_PORT = 7777;
    public const int TICKS_PER_SEC = 30;
    public const int MS_PER_TICK = 1000 / TICKS_PER_SEC;

    // Gameplay Constants
    public const int PLAYERS_TO_START = 1;
    public const float TIME_PER_QUARTER = 300f;
    public const float MINIMUM_STIFF_DISTANCE = 3.5f;
    public const float MINIMUM_TACKLE_DISTANCE = 2f;
    public const float MINIMUM_PUSH_DISTANCE = 2f;
    public const float UNIT_FOLLOW_STOP_DISTANCE = 1.5f;

    public const float PUSH_STRENGTH = .3f;
    public const float TACKLE_STRENGTH = 1.5f;

    public const float MOVE_SPEED_RB = 6f;
    public const float MOVE_SPEED_WR = 5.5f;
    public const float MOVE_SPEED_TE = 5f;
    public const float MOVE_SPEED_QB = 4.5f;
    public const float MOVE_SPEED_LM = 4f;

    public const float CHARGE_SPEED = 6.5f;

    public const float THROW_RANGE_QB = 60f;
    public const float THROW_RANGE_RB = 20f;
    public const float THROW_RANGE_TE = 15f;
    public const float THROW_RANGE_WR = 10f;

    public const float COOLDOWN_CHARGE = 20f;
    public const float COOLDOWN_JUKE = 10f;
    public const float COOLDOWN_TACKLE = 3f;
    public const float COOLDOWN_STIFF = 10f;

    public const float DURATION_CHARGE = 5f;
    public const float DURATION_JUKE = 1.5f;

    public const float BALL_HIKE_SPEED = 5f;
    public const float BALL_THROW_SPEED = 10f;
    public const float BALL_HIKE_RANGE = 10f;
    public const float MAX_CATCH_DISTANCE_FROM_ORIGIN = 3.5f;
    public const float MAX_CATCH_DISTANCE_FROM_DESTINATION = 4.5f;

    public const float LINE_OF_SCRIMMAGE_CONSTRAINT = 50f;

    // Pathfinding
    public const float WAYPOINT_BUFFER = 0.5f;
    public const float PATHFINDING_WAIT = 1.5f;
    public const float FREEZE_TIME = 0.5f;
    public const float COLLISION_INTERSECTION_CONSTANT = 50f;

    // Auto prevention
    public const float CLICK_EXPIRY_TIME = 1f; // in seconds, how long a click is registered for
    public const int CPS_MAX = 5; // CPS may be misleading, it's actually max clicks per CLICK_EXPIRY_TIME
}
