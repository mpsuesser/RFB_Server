using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveToSpotCommand : Command
{
    private Vector3 Destination { get; set; }

    public MoveToSpotCommand(Unit _unitMoving, Vector3 _dest) :
        base(_unitMoving) {

        _dest.y = SourceUnit.transform.position.y;
        Destination = _dest;
    }

    protected override bool CheckForBlockage(Pathfinding PF, Unit _source)
        => PF.CheckForUnitBlockage(_source, Destination);

    protected override Vector3[] FindPath(Pathfinding PF, Unit _source)
        => PF.FindPath(_source, Destination);

    public override void FixedUpdate() {
        // Check if we've arrived at the destination
        float distanceToDestination = (Destination - SourceUnit.transform.position).magnitude;
        float distanceThisFrame = ((SourceUnit.Charged) ? SourceUnit.ChargeSpeed : SourceUnit.MoveSpeed) * Time.fixedDeltaTime;
        if (distanceToDestination <= distanceThisFrame) {
            Finish();
            return;
        }

        // If we have no path, let's just move toward our destination
        if (!hasPath) {
            if (!frozen) {
                SourceUnit.MoveTo(Destination);
            }
        } else { // Otherwise, move along the path
            if (pathWaypoints == null || pathWaypoints.Length == 0) {
                Finish();
                return;
            }

            Vector3 waypoint = pathWaypoints[waypointIndex];
            Vector3 normalizedPosition = SourceUnit.transform.position;
            normalizedPosition.y = waypoint.y;

            float distanceToWaypoint = (waypoint - normalizedPosition).magnitude;

            if ((distanceToWaypoint - Constants.WAYPOINT_BUFFER) <= distanceThisFrame) {
                waypointIndex++;
                if (waypointIndex >= pathWaypoints.Length) {
                    Debug.Log("Reached end of unit follow path without touching unit. Should not happen.");
                    Finish();
                    return;
                }

                waypoint = pathWaypoints[waypointIndex];
            }

            if (!frozen) {
                SourceUnit.MoveTo(waypoint);
            }
        }
    }
}
