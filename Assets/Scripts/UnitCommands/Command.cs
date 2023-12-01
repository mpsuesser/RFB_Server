using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class Command : ICommand {
    protected Unit SourceUnit { get; set; }

    protected bool frozen;
    private bool finished;
    public bool IsFinished => finished;

    private Pathfinding PF;
    private Coroutine pathfindingCoroutine;
    private Coroutine freezeCoroutine;

    protected Vector3[] pathWaypoints;
    protected bool hasPath;
    protected int waypointIndex;

    protected Command(Unit _source) {
        SourceUnit = _source;

        PF = GameObject.Find("A*").GetComponent<Pathfinding>();

        finished = false;
        frozen = false;
    }

    protected IEnumerator Freeze() {
        SourceUnit.StopMoving();
        frozen = true;
        yield return new WaitForSeconds(Constants.FREEZE_TIME);
        frozen = false;
    }

    protected abstract bool CheckForBlockage(Pathfinding PF, Unit _source);
    protected abstract Vector3[] FindPath(Pathfinding PF, Unit _source);
    private IEnumerator UpdatePathing() {
        while (!finished) {
            if (CheckForBlockage(PF, SourceUnit)) {
                pathWaypoints = FindPath(PF, SourceUnit);
                hasPath = true;
                waypointIndex = 0;

                if (pathWaypoints == null) {
                    Debug.Log("Path is null! Finishing.");
                    Finish();
                    yield break;
                }
            } else {
                pathWaypoints = null;
                hasPath = false;
                waypointIndex = 0;
            }

            yield return new WaitForSeconds(Constants.PATHFINDING_WAIT);
        }
    }

    public void OnCollisionEnter(Collision _collision, Unit _with) {
        // 1 (Convert to 2D)
        Vector2 sourcePos = Util.ConvertTo2D(SourceUnit.transform.position);
        Vector2 withPos = Util.ConvertTo2D(_with.transform.position);
        Vector2 sourceVel = Util.ConvertTo2D(SourceUnit.Velocity);
        Vector2 withVel = Util.ConvertTo2D(_with.Velocity);

        // if a unit is stationary, do nothing
        if (sourceVel == Vector2.zero || withVel == Vector2.zero) {
            return;
        }

        // slope of line given two points: (y2 - y1)/(x2 - x1)
        float sourceSlope = (sourcePos.y - sourceVel.y) / (sourcePos.x - sourceVel.x);
        float withSlope = (withPos.y - withVel.y) / (withPos.x - withVel.x);

        // equation of a line using point-slope formula: y - y1 = m(x - x1)
        // y - sourcePos.y = sourceSlope * (x - sourcePos.x)
        // y = (sourceSlope * x) - (sourceSlope * sourcePos.x) - sourcePos.y
        // intercept = - (sourceSlope * sourcePos.x) - sourcePos.y
        float sourceIntercept = -(sourceSlope * sourcePos.x) - sourcePos.y;
        float withIntercept = -(withSlope * withPos.x) - withPos.y;

        // Set them equal to each other, solving for x where both equations follow y=mx+b
        // (sourceSlope * x) + sourceIntercept = (withSlope * x) + withIntercept
        // (sourceSlope * x) = (withSlope * x) + withIntercept - sourceIntercept
        // (sourceSlope * x) - (withSlope * x) = withIntercept - sourceIntercept
        // x * (sourceSlope - withSlope) = (withIntercept - sourceIntercept)
        // x = (withIntercept - sourceIntercept) / (sourceSlope - withSlope)
        float x = (withIntercept - sourceIntercept) / (sourceSlope - withSlope);

        // Plug x into either equation to get y, in the form y=mx+b
        float y = (sourceSlope * x) + sourceIntercept;

        // Put it all together
        Vector2 intersectionPoint = new Vector2(x, y);

        // Check each unit's distance to the intersection point
        float sourceDistance = (sourcePos - intersectionPoint).magnitude;
        float withDistance = (withPos - intersectionPoint).magnitude;

        // If this source unit is closer to the point of intersection, let's just continue moving
        if (sourceDistance <= withDistance) {
            return;
        }

        // Otherwise, we freeze, since that means the other unit is closer to the point of intersection
        freezeCoroutine = SourceUnit.StartCoroutine(Freeze());
        Debug.Log($"Freezing {SourceUnit.gameObject.name}!");
        Debug.Log("sourcePos: " + sourcePos);
        Debug.Log("sourceVel: " + sourceVel);
        Debug.Log("withPos: " + withPos);
        Debug.Log("withVel: " + withVel);
        Debug.Log("sourceSlope: " + sourceSlope);
        Debug.Log("withSlope: " + withSlope);
        Debug.Log("sourceIntercept: " + sourceIntercept);
        Debug.Log("withIntercept: " + withIntercept);
        Debug.Log("x: " + x);
        Debug.Log("y: " + y);
        Debug.Log("sourceDistance: " + sourceDistance);
        Debug.Log("withDistance: " + withDistance);
    }

    public abstract void FixedUpdate();

    public virtual void Execute() {
        pathfindingCoroutine = SourceUnit.StartCoroutine(UpdatePathing());
    }

    public virtual void CleanUp() {
        finished = true;
        if (pathfindingCoroutine != null) SourceUnit.StopCoroutine(pathfindingCoroutine);
        if (freezeCoroutine != null) SourceUnit.StopCoroutine(freezeCoroutine);
    }

    protected void Finish() {
        CleanUp();
        SourceUnit.StopMoving();
    }
}
