using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICommand
{
    void Execute();
    void FixedUpdate();
    void OnCollisionEnter(Collision _collision, Unit _with);
    void CleanUp();

    bool IsFinished { get; }
}
