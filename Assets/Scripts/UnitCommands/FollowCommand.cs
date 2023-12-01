using UnityEngine;

public class FollowCommand : MoveTowardUnitCommand {
    public FollowCommand(Unit _source, Unit _target) :
        base(_source, _target, Constants.UNIT_FOLLOW_STOP_DISTANCE)
    {}

    protected override void DoAction(Unit _source, Unit _target) {}
}
