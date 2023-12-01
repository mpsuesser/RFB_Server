using UnityEngine;

public class TackleCommand : MoveTowardUnitCommand {
    public TackleCommand(Unit _source, Unit _target) :
        base(_source, _target, Constants.MINIMUM_TACKLE_DISTANCE)
    {}

    protected override void DoAction(Unit _source, Unit _target) {
        if (_target != null) {
            _source.Tackle(_target);
        }
    }
}
