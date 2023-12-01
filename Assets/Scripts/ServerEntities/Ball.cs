using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Ball : MonoBehaviour {
    private static float ThrowSpeed = Constants.BALL_THROW_SPEED;
    private static float HikeSpeed = Constants.BALL_HIKE_SPEED;

    private bool AirCatchable {
        get {
            return hiked
                || ((transform.position - origin).magnitude < Constants.MAX_CATCH_DISTANCE_FROM_ORIGIN);
                // || ((transform.position - destination).magnitude < Constants.MAX_CATCH_DISTANCE_FROM_DESTINATION);
        }
    }

    private Vector3 origin;
    private Vector3 destination;
    private Unit thrower;
    private bool hiked;
    private bool landed;

    private Rigidbody rb;
    private ConstantForce cf;
    private float gravity;

    private int frameCount;

    public GameObject throwMarkerPrefab;
    private GameObject throwMarker;
    public GameObject catchTriggerPrefab;
    private GameObject catchTrigger;

    void Awake() {
        rb = gameObject.GetComponent<Rigidbody>();
        cf = gameObject.GetComponent<ConstantForce>();
        gravity = cf.force.y; // will only affect the look of the throw. time in the air will always remain the same
        hiked = false;
        landed = false;
        frameCount = 0;
    }

    void FixedUpdate() {
        frameCount++;

        if ((new Vector2(transform.position.x, transform.position.z) - new Vector2(destination.x, destination.z)).magnitude < 0.1f) {
            Debug.Log("Landed from FU");
            Landed();
        }

        // set the ball rotation to match the current velocity vector
        if (rb.velocity.normalized != Vector3.zero) {
            transform.rotation = Quaternion.LookRotation(rb.velocity.normalized);
        }

        ServerSend.UpdateBall(transform.position, transform.rotation, frameCount);
    }

    void Caught(Unit catcher) {
        catcher.CaughtBall(this, hiked);

        if (catcher.tag != thrower.tag) {
            GameMaster.instance.Event_Interception(catcher, thrower);
        }

        RemoveBall();
    }

    void Landed() {
        if (landed) {
            return;
        }

        landed = true;

        Unit catcher;
        if (CheckForCatch(out catcher)) {
            Caught(catcher);
        } else {
            GameMaster.instance.Event_IncompletePass(thrower, transform.position);
            RemoveBall();
        }
    }

    private bool CheckForCatch(out Unit catcher) {
        catcher = null;

        List<Unit> eligibleUnits = catchTrigger.GetComponent<CatchTrigger>().UnitsInside;
        if (eligibleUnits.Count == 0) {
            return false;
        }

        float shortestDistance = -1f;
        foreach (Unit unit in eligibleUnits) {
            if (!unit.CanCatch || unit == thrower) {
                continue;
            }

            float distance = (unit.transform.position - origin).magnitude;
            if (shortestDistance < 0f || distance < shortestDistance) {
                shortestDistance = distance;
                catcher = unit;
            }
        }

        return !(catcher == null);
    }

    void RemoveBall() {
        Destroy(throwMarker);
        Destroy(catchTrigger);
        Destroy(gameObject);
    }

    public void SetOrigin(Vector3 _origin) {
        origin = _origin;
    }

    public void SetDestination(Vector3 _destination) {
        destination = _destination;
        destination.y = -1;
        CreateMarker();
    }

    void CreateMarker() {
        Vector3 markerLocation = destination;
        markerLocation.y += (float)0.01;
        throwMarker = Instantiate(throwMarkerPrefab, markerLocation, Quaternion.Euler(90, 0, 0));

        Vector3 catchTriggerLocation = markerLocation;
        catchTriggerLocation.y = .1f;
        catchTrigger = Instantiate(catchTriggerPrefab, catchTriggerLocation, Quaternion.identity);
    }

    public void SetThrower(Unit _thrower) {
        thrower = _thrower;
    }

    public void SetHike() {
        hiked = true;
    }

    public Unit GetThrower() {
        return thrower;
    }

    // ----- Effects -----

    public void Launch() {
        Vector3 direction = (destination - transform.position).normalized;
        float distance = Vector3.Distance(destination, transform.position);

        // vertical distance = (initial vertical velocity)(time) - (1/2)(acceleration gravity)(time)^2
        //
        // Vx = ThrowSpeed
        // time = distance / ThrowSpeed
        // Vy = -(time/2 * gravity)
        // initial velocity vector = sqrt(Vx^2 + Vy^2)
        // acceleration gravity = gravity
        // launch angle = arctan(Vy/Vx) * 180/Pi

        float Vx = hiked ? HikeSpeed : ThrowSpeed; // will remain constant
        Vector3 horizontalVelocityVector = direction * Vx; // direction is flat on Y axis
        float timeInAir = distance / Vx;
        float peakTime = timeInAir / 2;
        float Vy = (timeInAir / 2) * gravity * -1;
        Vector3 verticalVelocityVector = Vector3.up * Vy;
        Vector3 totalVelocityVector = horizontalVelocityVector + verticalVelocityVector;

        /*
        // displacement = velocity * t + (1/2) * acceleration * t^2
        float peakHeight = (Vy * peakTime) + ((float)0.5 * gravity * peakTime * peakTime);
        Debug.Log("Peak height: " + peakHeight);
        WillReachMaxHeight = peakHeight > MaxHeight;
        if (WillReachMaxHeight) {
            // To get time given acceleration (a), initial velocity (u), and displacement (s), solve for t using the quadratic formula:
            // a(x^2) + bx + c
            // 
            // x = (-b +- sqrt(b^2 - 4ac))/2a

            // (1/2)a(t^2) + ut - s = 0
            //
            // t = (-u +- sqrt(u^2 - 4(a/2)(-s))) / (2(a/2))
            // a = gravity
            // s = MaxHeight
            // u = Vy;

            // t1 = (-Vy + sqrt(Vy*Vy - (4 * (gravity/2) * (-MaxHeight))) / (2(gravity/2))
            // t2 = (-Vy - sqrt(Vy*Vy - (4 * (gravity/2) * (-MaxHeight))) / (2(gravity/2))

            // TODO: simplify after confirming values are accurate
            float MaxHeightStartTime = (-Vy + Mathf.Sqrt(Vy * Vy - (4 * (gravity / 2) * (-MaxHeight)))) / (2 * (gravity / 2));
            float MaxHeightEndTime = (-Vy - Mathf.Sqrt(Vy * Vy - (4 * (gravity / 2) * (-MaxHeight)))) / (2 * (gravity / 2));

            Debug.Log("MaxHeightStartTime: " + MaxHeightStartTime);
            Debug.Log("MaxHeightEndTime: " + MaxHeightEndTime);

            // displacement = velocity * t + (1/2) * acceleration * t^2
            // horizontal displacement = Vx * TIME + (1/2) * 
            MaxHeightStartDistance = Vx * MaxHeightStartTime;
            MaxHeightEndDistance = Vx * MaxHeightEndTime;

            Debug.Log("MaxHeightStartDistance: " + MaxHeightStartDistance);
            Debug.Log("MaxHeightEndDistance: " + MaxHeightEndDistance);
        } */

        rb.AddForce(totalVelocityVector, ForceMode.VelocityChange);

        // All values below can be discarded -- old
        /* float peakHeight = (Vy * peakTime) + ((float)0.5 * gravity * peakTime * peakTime); // displacement = velocity * t + (1/2) * acceleration * t^2
        float initialVelocity = Mathf.Sqrt((Vx * Vx) + (Vy * Vy)); // a^2 + b^2 = c^2
        // float launchAngle = Math.Atan(Vy / Vx) * Math.Rad2Deg;
        Vector3 peakMidpoint = new Vector3(
            (transform.position.x + destination.x) / 2,
            transform.position.y + peakHeight,
            (transform.position.z + destination.z) / 2);
        Vector3 forceVector = (peakMidpoint - transform.position).normalized * initialVelocity;

        Debug.Log("Distance: " + distance);
        Debug.Log("Gravity: " + gravity);
        Debug.Log("Time in air: " + timeInAir);
        Debug.Log("peakTime: " + peakTime);
        Debug.Log("Start pos: " + transform.position);
        Debug.Log("Destination: " + destination);
        Debug.Log("initialVelocity: " + initialVelocity);
        Debug.Log("peakHeight: " + peakHeight);
        Debug.Log("peakMidpoint: " + peakMidpoint);
        Debug.Log("Force vector: " + forceVector);
        Debug.Log("Total velocity vector: " + totalVelocityVector);
        Debug.DrawLine(transform.position, peakMidpoint, Color.white, 10f);
        Debug.DrawLine(peakMidpoint, destination, Color.white, 10f); */
    }

    void OnTriggerEnter(Collider collision) {
        GameObject entityCollidingWith = collision.gameObject;
        Unit unitCollidingWith = entityCollidingWith.GetComponent<Unit>();
        if (    unitCollidingWith != null
            &&  unitCollidingWith.CanCatch
            &&  (unitCollidingWith != thrower || hiked)
            &&  (AirCatchable || catchTrigger.GetComponent<CatchTrigger>().UnitsInside.Contains(unitCollidingWith))
        ) {
            Caught(unitCollidingWith);
        } else if (entityCollidingWith.tag == "Field") {
            Landed();
        }
    }
}
