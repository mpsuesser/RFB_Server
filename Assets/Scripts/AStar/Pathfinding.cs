using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Pathfinding : MonoBehaviour {
    private Grid grid;
    private LayerMask unitMask;

    private float blockageCheckDistance = 10f;

    private void Awake() {
        grid = gameObject.GetComponent<Grid>();
        unitMask = grid.unitMask;
    }

    private void Update() {
    }

    public bool CheckForUnitBlockage(Unit _unit, Vector3 _dest, Unit _ignore = null) {
        float dist = (_dest - _unit.transform.position).magnitude;
        Vector3 dir = (_dest - _unit.transform.position).normalized;

        Ray ray = new Ray(_unit.transform.position, dir);
        RaycastHit[] hits = Physics.SphereCastAll(ray, .5f, Mathf.Min(blockageCheckDistance, dist), unitMask);
        foreach (RaycastHit hit in hits) {
            Unit unitHit = hit.collider.gameObject.GetComponent<Unit>();
            if (unitHit != _unit) {
                if (_ignore != null && unitHit == _ignore) {
                    continue;
                }

                return true;
            }
        }

        return false;
    }

    public Vector3[] FindPath (Unit _unit, Vector3 _targetPos, Unit _target = null) {
        Node startNode = grid.NodeFromWorldPoint(_unit.transform.position);
        Node targetNode = grid.NodeFromWorldPoint(_targetPos);
        if (!targetNode.WalkableByTo(_unit, _target)) {
            targetNode = GetWalkableNodeNear(targetNode, _unit, _target);
            if (targetNode == null) {
                Debug.Log("[PATHFINDING] Target node was null after seeking nearby walkable node!");
                return null;
            }
        }

        Heap<Node> openSet = new Heap<Node>(grid.MaxSize);
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0) {
            // Get the node in our open set with the lowest fCost (hCost if tie)
            Node currentNode = openSet.RemoveFirst();
            closedSet.Add(currentNode);

            // If current is the target, then we've found a path
            if (currentNode == targetNode) {
                return RetracePath(startNode, targetNode);
            }

            foreach (Node neighbor in grid.GetNeighbors(currentNode)) {
                // If neighbor is not walkable or is in closed, skip
                if (neighbor.WalkableByTo(_unit, _target) == false
                    || closedSet.Contains(neighbor)) {
                    continue;
                }

                // Update all neighbors by checking if distance when jumping from this node is less than their existing gCost
                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor)) {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor)) {
                        openSet.Add(neighbor);
                    } else {
                        openSet.UpdateItem(neighbor);
                    }
                }
            }
        }

        Debug.Log($"[PATHFINDING] Could not find a path for {_unit.gameObject.name}!");
        return null;
    }

    private Vector3[] RetracePath(Node _start, Node _end) {
        List<Node> path = new List<Node>();
        Node currentNode = _end;

        while (currentNode != _start) {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        Vector3[] waypoints = SimplifyPath(path);
        Array.Reverse(waypoints);
        return waypoints;
    }

    // Get the last node in every continuous line.
    private Vector3[] SimplifyPath(List<Node> _path) {
        List<Vector3> waypoints = new List<Vector3>();
        Vector2 prevDirection = Vector2.zero;

        // for debug purposes only
        List<Node> simplified = new List<Node>();

        for (int i = 1; i < _path.Count; i++) {
            Vector2 newDirection = new Vector2(_path[i-1].gridX - _path[i].gridX, _path[i-1].gridY - _path[i].gridY);
            if (newDirection != prevDirection) {
                waypoints.Add(_path[i].worldPosition);
                simplified.Add(_path[i]);
            }
            prevDirection = newDirection;
        }

        grid.path = simplified;
        return waypoints.ToArray();
    }

    private int GetDistance(Node _A, Node _B) {
        int dstX = Mathf.Abs(_A.gridX - _B.gridX);
        int dstY = Mathf.Abs(_A.gridY - _B.gridY);

        if (dstX > dstY) {
            return (14 * dstY) // diagonal moves
                + (10 * (dstX - dstY)); // horizontal moves
        } else {
            return (14 * dstX) // diagonal moves
                + (10 * (dstY - dstX)); // horizontal moves
        }
    }

    // We want to get the nearest node to _node that is walkable.
    private Node GetWalkableNodeNear(Node _node, Unit _requestingUnit, Unit _targetUnit = null) {
        Queue<Node> neighbors = new Queue<Node>();
        HashSet<Node> checkedNodes = new HashSet<Node>();

        foreach (Node n in grid.GetNeighbors(_node)) {
            neighbors.Enqueue(n);
        }

        while (checkedNodes.Count < 1000) { // arbitrary, don't want to infinite loop
            Node beingChecked = neighbors.Dequeue();
            if (beingChecked.WalkableByTo(_requestingUnit, _targetUnit)) {
                return beingChecked;
            }

            checkedNodes.Add(beingChecked);
            foreach (Node n in grid.GetNeighbors(beingChecked)) {
                if (!checkedNodes.Contains(n)) {
                    neighbors.Enqueue(n);
                }
            }
        }

        return null;
    }
}
