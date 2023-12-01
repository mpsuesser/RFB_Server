using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : IHeapItem<Node> {
    public bool walkable;
    public Vector3 worldPosition;

    // Keeping track of this Node's position in the grid
    public int gridX;
    public int gridY;

    public int gCost;
    public int hCost;
    public int fCost => gCost + hCost;

    public Node parent;

    private List<Unit> unitsTouching;
    private int heapIndex;

    public Node(bool _walkable, Vector3 _worldPos, int _x, int _y) {
        walkable = _walkable;
        worldPosition = _worldPos;

        gridX = _x;
        gridY = _y;

        unitsTouching = new List<Unit>();
    }

    public int HeapIndex {
        get {
            return heapIndex;
        }
        set {
            heapIndex = value;
        }
    }

    public int CompareTo(Node _nodeToCompare) {
        int compare = fCost.CompareTo(_nodeToCompare.fCost);
        if (compare == 0) {
            compare = hCost.CompareTo(_nodeToCompare.hCost);
        }

        return -compare;
    }

    public void AddTouchingUnit(Unit _unit) {
        if (!unitsTouching.Contains(_unit)) {
            unitsTouching.Add(_unit);
        }

        walkable = false;
    }

    public void RemoveTouchingUnit(Unit _unit) {
        if (unitsTouching.Contains(_unit)) {
            unitsTouching.Remove(_unit);
        }

        if (unitsTouching.Count == 0) {
            walkable = true;
        }
    }

    public void Clear() {
        unitsTouching.Clear();
        walkable = true;
    }

    public bool WalkableByTo(Unit _unit, Unit _target) {
        return walkable
            || unitsTouching.Contains(_unit)
            || unitsTouching.Contains(_target);
    }
}
