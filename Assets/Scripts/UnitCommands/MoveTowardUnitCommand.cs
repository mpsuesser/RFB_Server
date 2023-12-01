using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MoveTowardUnitCommand : Command
{
    protected Unit TargetUnit { get; set; }
    protected float MinimumDistance { get; set; }

    private Vector3 Destination => TargetUnit.transform.position;

    protected MoveTowardUnitCommand(Unit _source, Unit _target, float _minimumDistance) :
        base(_source) {

        TargetUnit = _target;
        MinimumDistance = _minimumDistance;

        TargetUnit.OnJuke += TargetJuked;
    }

    public override void Execute() {
        if (TargetUnit == null) {
            Finish();
            return;
        }

        base.Execute();
    }

    protected override bool CheckForBlockage(Pathfinding PF, Unit _source)
        => PF.CheckForUnitBlockage(_source, Destination, TargetUnit);

    protected override Vector3[] FindPath(Pathfinding PF, Unit _source)
        => PF.FindPath(_source, Destination, TargetUnit);

    public override void FixedUpdate() {
        if (TargetUnit == null) {
            Finish();
            return;
        }

        // Check if we've arrived at the unit
        float distanceToUnit = (Destination - SourceUnit.transform.position).magnitude;
        if (distanceToUnit <= MinimumDistance) {
            DoAction(SourceUnit, TargetUnit);
            Finish();
            return;
        }

        // If not, and if we have no path, let's just move toward the unit
        if (!hasPath) {
            SourceUnit.MoveTo(Destination);
        } else { // Otherwise, move along the path
            Vector3 waypoint = pathWaypoints[waypointIndex];
            Vector3 normalizedPosition = SourceUnit.transform.position;
            normalizedPosition.y = waypoint.y;

            float distanceToWaypoint = (waypoint - normalizedPosition).magnitude;
            float distanceThisFrame = ((SourceUnit.Charged) ? SourceUnit.ChargeSpeed : SourceUnit.MoveSpeed) * Time.fixedDeltaTime;

            if ((distanceToWaypoint - Constants.WAYPOINT_BUFFER) <= distanceThisFrame) {
                waypointIndex++;
                if (waypointIndex >= pathWaypoints.Length) {
                    Debug.Log("Reached end of unit follow path without touching unit. Should not happen.");
                    Finish();
                    return;
                }

                waypoint = pathWaypoints[waypointIndex];
            }

            SourceUnit.MoveTo(waypoint);
        }
    }

    protected void TargetJuked() => Finish();

    public override void CleanUp() {
        if (TargetUnit != null) {
            TargetUnit.OnJuke -= TargetJuked;
        }

        base.CleanUp();
    }

    protected abstract void DoAction(Unit _source, Unit _target);
}
