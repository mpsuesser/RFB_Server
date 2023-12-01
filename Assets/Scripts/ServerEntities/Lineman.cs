using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lineman : Unit {
    public override float MoveSpeed { get { return Constants.MOVE_SPEED_LM; } }
    public override float ThrowRange { get { return 0f; } } // can't throw

    public override float ChargeSpeed { get { return Constants.CHARGE_SPEED; } }
    public override float PushStrength { get { return Constants.PUSH_STRENGTH; } }
    public override float TackleStrength { get { return Constants.TACKLE_STRENGTH; } }

    public override bool CanCatch => false;
}
