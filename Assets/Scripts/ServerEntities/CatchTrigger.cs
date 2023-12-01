using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatchTrigger : MonoBehaviour
{
    public List<Unit> UnitsInside { get; private set; }

    private void Awake() {
        UnitsInside = new List<Unit>();
        Debug.Log("Created new list.");
    }

    private void OnTriggerEnter(Collider collision) {
        GameObject entityCollidingWith = collision.gameObject;
        Unit unitCollidingWith = entityCollidingWith.GetComponent<Unit>();

        if (unitCollidingWith != null) {
            Debug.Log($"{unitCollidingWith.gameObject.name} has entered the catch trigger zone.");

            if (UnitsInside == null) {
                Debug.Log("UnitsInside was null!");
            }

            UnitsInside.Add(unitCollidingWith);
        }
    }

    private void OnTriggerExit(Collider collision) {
        GameObject entityCollidingWith = collision.gameObject;
        Unit unitCollidingWith = entityCollidingWith.GetComponent<Unit>();

        if (unitCollidingWith) {
            Debug.Log($"{unitCollidingWith.gameObject.name} has left the catch trigger zone.");

            UnitsInside.Remove(unitCollidingWith);
        }
    }
}
