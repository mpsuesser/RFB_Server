using UnityEngine;

public class StiffCommand : MoveTowardUnitCommand {
    public StiffCommand(Unit _source, Unit _target) :
        base(_source, _target, Constants.MINIMUM_STIFF_DISTANCE)
    {}

    protected override void DoAction(Unit _source, Unit _target) {
        if (_target != null) {
            _source.Stiff(_target);
        }
    }
}
