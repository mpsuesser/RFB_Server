using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour {
    public LayerMask unitMask;
    public Vector2 gridWorldSize;
    public float nodeRadius;
    public float spaceBufferModifier;

    private Node[,] grid;
    private float nodeDiameter;

    private int gridSizeX;
    private int gridSizeY;
    public int MaxSize => gridSizeX * gridSizeY;

    // int: unit ID
    // hashset: set of all nodes occupied by this unit
    private Dictionary<int, HashSet<Node>> nodesOccupied;

    private void Start() {
        // Get the size of our grid in terms of nodes
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        CreateGrid();

        nodesOccupied = new Dictionary<int, HashSet<Node>>();
    }

    private void CreateGrid() {
        grid = new Node[gridSizeX, gridSizeY];

        Vector3 worldBottomLeft = 
            transform.position
            - Vector3.right * (gridWorldSize.x / 2)
            - Vector3.forward * (gridWorldSize.y / 2);
        
        for (int x = 0; x < gridSizeX; x++) {
            for (int y = 0; y < gridSizeY; y++) {
                Vector3 worldPoint =
                    worldBottomLeft
                    + Vector3.right * (x * nodeDiameter + nodeRadius)
                    + Vector3.forward * (y * nodeDiameter + nodeRadius);

                // note: excluded collision checks since snap formation is created dynamically

                grid[x, y] = new Node(true, worldPoint, x, y);
            }
        }
    }

    public HashSet<Node> GetNeighbors(Node _source) {
        HashSet<Node> neighbors = new HashSet<Node>();

        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                // 0, 0 will be the source node, so skip it
                if (x == 0 && y == 0) {
                    continue;
                }

                int checkX = _source.gridX + x;
                int checkY = _source.gridY + y;

                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbors;
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition) {
        // float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        // float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;
        float percentX = (worldPosition.x / gridWorldSize.x) + 0.5f;
        float percentY = (worldPosition.z / gridWorldSize.y) + 0.5f;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[x, y];
    }

    // Update nodes touching this unit as unwalkable, update old nodes as walkable if they are not touching.
    public void UpdateUnitPosition(Unit _unit) {
        int id = _unit.unitId;
        if (!nodesOccupied.ContainsKey(id)) {
            nodesOccupied.Add(id, new HashSet<Node>());
        }

        // The set that will contain all nodes this unit is currently touching
        HashSet<Node> currentlyOccupied = new HashSet<Node>();

        // Get the current center node this unit is touching
        Vector3 worldPosition = _unit.transform.position;
        Node currentlySittingOn = NodeFromWorldPoint(worldPosition);

        // Populate nodesToCheck with our current node, neighboring nodes, and those neighbors' neighboring nodes
        HashSet<Node> nodesToCheck = new HashSet<Node>();
        HashSet<Node> neighbors = GetNeighbors(currentlySittingOn);
        nodesToCheck.Add(currentlySittingOn);
        nodesToCheck.UnionWith(neighbors);
        foreach (Node n in neighbors) {
            nodesToCheck.UnionWith(GetNeighbors(n));
        }

        // Loop through each node and check if it is touching the unit
        Collider[] touches;
        foreach (Node n in nodesToCheck) {
            touches = Physics.OverlapBox(n.worldPosition, Vector3.one * nodeRadius * spaceBufferModifier, Quaternion.identity, unitMask, QueryTriggerInteraction.Ignore);
            foreach (Collider touch in touches) {
                if (touch.gameObject.GetComponent<Unit>() == _unit) {
                    n.AddTouchingUnit(_unit);
                    currentlyOccupied.Add(n);
                    break;
                }
            }
        }

        // Loop through our old set to see if there are any nodes not currently occupied that were before, and mark them as walkable again if they're clear
        foreach (Node n in nodesOccupied[id]) {
            if (!currentlyOccupied.Contains(n)) {
                n.RemoveTouchingUnit(_unit);
            }
        }

        // Set our occupied set to the new set we created above
        nodesOccupied[id] = currentlyOccupied;
    }

    // Clear all nodes that were previously unwalkable because we had this unit registered there.
    public void UpdateUnitDestroyed(Unit _unit) {
        int id = _unit.unitId;
        if (nodesOccupied.ContainsKey(id)) {
            if (nodesOccupied[id].Count > 0) {
                foreach (Node n in nodesOccupied[id]) {
                    n.RemoveTouchingUnit(_unit);
                }
            }

            nodesOccupied.Remove(id);
        }
    }

    // Reset all nodes
    public void ResetGrid() {
        for (int x = 0; x < gridSizeX; x++) {
            for (int y = 0; y < gridSizeY; y++) {
                grid[x, y].Clear();
            }
        }

        nodesOccupied.Clear();
    }


    public List<Node> path;
    private void OnDrawGizmos() {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        if (grid != null) {
            foreach (Node n in grid) {
                Gizmos.color = n.walkable ? Color.white : Color.red;
                if (path != null) {
                    if (path.Contains(n)) {
                        Gizmos.color = Color.black;
                    }
                }
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
            }
        }
    }
}
