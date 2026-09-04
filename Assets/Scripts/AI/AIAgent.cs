using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class AIAgent : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform followTarget;
    public bool followEnabled = true;

    [Header("Grid Settings")]
    public int gridSize = 10;
    public float nodeSpacing = 1.5f;
    [Tooltip("How often (in seconds) to update the path.")]
    public float updateRate = 0.15f;

    [Header("Raycast Settings")]
    public float rayStartHeight = 8f;
    public float rayLength = 20f;

    public float safetyMultiplier = 1.8f;

    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float turnSpeed = 8f;
    public float stopDistance = 0.5f;

    [Header("Layers")]
    public LayerMask WalkableLayer;
    public LayerMask NonWalkableLayer;

    // Internal State
    private Vector3 targetPosition;
    private Node[,] gridNodes;
    private List<Vector3> currentPath = new List<Vector3>();
    private CharacterController controller;
    private Vector3 gridCenterPos;
    private bool isUsingDirectPath = false; // Debug info

    void OnEnable()
    {
        controller = GetComponent<CharacterController>();
        gridNodes = new Node[gridSize, gridSize];
        StartCoroutine(AIUpdateLoop());
    }

    IEnumerator AIUpdateLoop()
    {
        while (true)
        {
            if (followEnabled && followTarget != null)
            {
                targetPosition = followTarget.position;

                // 1. Check if we can go straight to the target without pathfinding
                if (IsPathClear(transform.position, targetPosition))
                {
                    isUsingDirectPath = true;
                    currentPath.Clear();
                    currentPath.Add(targetPosition); // Simple 1-point path
                }
                else
                {
                    isUsingDirectPath = false;
                    // 2. Obstacle detected: Bake grid and use A*
                    BakeGrid();
                    CalculatePath(targetPosition);
                }
            }

            yield return new WaitForSeconds(updateRate);
        }
    }

    void Update()
    {
        MoveAlongPathSmooth();
    }

    // ================= MOVEMENT LOGIC =================

    void MoveAlongPathSmooth()
    {
        if (currentPath == null || currentPath.Count == 0) return;

        // --- NEW: LOOK AHEAD (String Pulling) ---
        // If we have at least 2 points (next node + one after), check if we can skip the next one.
        if (currentPath.Count > 1)
        {
            // Check visibility to the node AFTER the immediate next one
            if (IsPathClear(transform.position, currentPath[1]))
            {
                // We can see the 2nd node clearly, so remove the 1st one (cut the corner)
                currentPath.RemoveAt(0);
            }
        }
        // ----------------------------------------

        Vector3 currentPos = transform.position;
        Vector3 targetWaypoint = currentPath[0];

        // Flatten Y for distance checks
        float distToWaypoint = Vector2.Distance(new Vector2(targetWaypoint.x, targetWaypoint.z), new Vector2(currentPos.x, currentPos.z));

        // Waypoint Switching Logic
        while (distToWaypoint < 0.5f && currentPath.Count > 0)
        {
            if (currentPath.Count == 1)
            {
                if (distToWaypoint <= stopDistance)
                {
                    currentPath.Clear();
                    return; // Stop moving
                }
                break;
            }

            currentPath.RemoveAt(0);

            if (currentPath.Count > 0)
            {
                targetWaypoint = currentPath[0];
                distToWaypoint = Vector2.Distance(new Vector2(targetWaypoint.x, targetWaypoint.z), new Vector2(currentPos.x, currentPos.z));
            }
        }

        if (currentPath.Count == 0) return;

        // Execute Movement
        Vector3 toTarget = targetWaypoint - transform.position;
        toTarget.y = 0; // Keep rotation flat

        if (toTarget.sqrMagnitude > 0.001f)
        {
            Vector3 moveDir = toTarget.normalized;

            // Rotation
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);

            // Velocity
            Vector3 velocity = transform.forward * moveSpeed;
            velocity.y += Physics.gravity.y; // Apply gravity

            controller.Move(velocity * Time.deltaTime);
        }
    }

    // ================= DIRECT PATH CHECK (OPTIMIZATION) =================

    /// <summary>
    /// Checks if we can walk straight to the target without hitting walls or falling in holes.
    /// </summary>
    bool IsPathClear(Vector3 start, Vector3 end)
    {
        Vector3 dir = end - start;
        float dist = dir.magnitude;

        if (dist < 0.1f)
            return true;

        dir.Normalize();

        float radius = controller.radius;
        float height = controller.height;

        // BIGGER safety
        float safetyRadius = radius * (safetyMultiplier + 0.5f);
        float halfHeight = Mathf.Max(height / 2f, radius);

        LayerMask obstacleMask = NonWalkableLayer | WalkableLayer;

        int samples = Mathf.CeilToInt(dist / (nodeSpacing * 0.5f));

        for (int i = 0; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector3 samplePos = Vector3.Lerp(start, end, t);

            Vector3 center = samplePos + controller.center;

            Vector3 p1 = center + Vector3.up * (halfHeight - safetyRadius);
            Vector3 p2 = center - Vector3.up * (halfHeight - safetyRadius);

            // ===== FAT CAPSULE CHECK =====
            if (Physics.CheckCapsule(p1, p2, safetyRadius, obstacleMask, QueryTriggerInteraction.Ignore))
                return false;

            // ===== HOLE CHECK =====
            Vector3 rayOrigin = samplePos + Vector3.up * 1.5f;

            if (!Physics.Raycast(rayOrigin, Vector3.down, 3f, WalkableLayer))
                return false;

            // ===== EDGE CHECK (prevents cliff hugging) =====
            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;

            if (!Physics.Raycast(rayOrigin + side * safetyRadius, Vector3.down, 3f, WalkableLayer))
                return false;

            if (!Physics.Raycast(rayOrigin - side * safetyRadius, Vector3.down, 3f, WalkableLayer))
                return false;
        }

        return true;
    }



    // ================= GRID BAKING =================

    void BakeGrid()
    {
        float px = Mathf.Round(transform.position.x / nodeSpacing) * nodeSpacing;
        float pz = Mathf.Round(transform.position.z / nodeSpacing) * nodeSpacing;
        gridCenterPos = new Vector3(px, transform.position.y, pz);

        int halfSize = gridSize / 2;
        Vector3 startOffset = new Vector3(-halfSize * nodeSpacing, 0, -halfSize * nodeSpacing);
        Vector3 gridOrigin = gridCenterPos + startOffset;

        Array.Clear(gridNodes, 0, gridNodes.Length);

        float radius = controller.radius;
        float height = controller.height;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Vector3 worldPoint = gridOrigin + new Vector3(x * nodeSpacing, 0, y * nodeSpacing);
                Vector3 rayOrigin = new Vector3(worldPoint.x, gridCenterPos.y + rayStartHeight, worldPoint.z);

                // Check for ground
                if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, WalkableLayer, QueryTriggerInteraction.Ignore))
                    continue;

                // Check slope
                if (Vector3.Angle(hit.normal, Vector3.up) > controller.slopeLimit)
                    continue;

                Vector3 groundPoint = hit.point;

                // --- FIX: CHECK INSIDE OBJECTS (WALKABLE OR NOT) ---
                // We verify that the space ABOVE the node is clear of ANY layer (Walkable or NonWalkable).
                // Again, we lift the bottom check so we don't hit the ground point itself.
                float inflatedRadius = radius * (safetyMultiplier + 0.3f);

                Vector3 basePos = groundPoint + Vector3.up * 0.2f;
                Vector3 p1C = basePos + Vector3.up * inflatedRadius;
                Vector3 p2C = basePos + Vector3.up * (height - inflatedRadius);

                LayerMask obstacleMask = NonWalkableLayer | WalkableLayer;

                if (Physics.CheckCapsule(p1C, p2C, inflatedRadius, obstacleMask, QueryTriggerInteraction.Ignore))
                    continue;

                // Extra: reject nodes near cliffs
                Vector3 sideA = basePos + transform.right * inflatedRadius;
                Vector3 sideB = basePos - transform.right * inflatedRadius;

                if (!Physics.Raycast(sideA + Vector3.up, Vector3.down, 3f, WalkableLayer))
                    continue;

                if (!Physics.Raycast(sideB + Vector3.up, Vector3.down, 3f, WalkableLayer))
                    continue;

                gridNodes[x, y] = new Node(groundPoint, x, y);
            }
        }
    }

    // ================= A* PATHFINDING =================

    void CalculatePath(Vector3 targetPos)
    {
        Node startNode = NodeFromWorldPoint(transform.position);
        Node endNode = NodeFromWorldPoint(targetPos);

        if (startNode == null) startNode = GetClosestValidNode(transform.position);
        if (endNode == null) endNode = GetClosestValidNode(targetPos);

        if (startNode == null || endNode == null || startNode == endNode) return;

        List<Vector3> path = FindPath(startNode, endNode);
        if (path != null && path.Count > 0)
            currentPath = path;
    }

    List<Vector3> FindPath(Node start, Node end)
    {
        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        openSet.Add(start);
        start.g = 0;
        start.h = Vector3.Distance(start.position, end.position);

        Node closestReachableNode = start;
        float closestDist = start.h;

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].f < currentNode.f || (openSet[i].f == currentNode.f && openSet[i].h < currentNode.h))
                    currentNode = openSet[i];
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            float d = Vector3.Distance(currentNode.position, end.position);
            if (d < closestDist)
            {
                closestDist = d;
                closestReachableNode = currentNode;
            }

            if (currentNode == end)
                return RetracePath(start, end);

            foreach (Node neighbor in GetNeighbors(currentNode))
            {
                if (closedSet.Contains(neighbor)) continue;
                if (!CanTraverse(currentNode.position, neighbor.position)) continue;

                float newMovementCostToNeighbor = currentNode.g + Vector3.Distance(currentNode.position, neighbor.position);

                if (newMovementCostToNeighbor < neighbor.g || !openSet.Contains(neighbor))
                {
                    neighbor.g = newMovementCostToNeighbor;
                    neighbor.h = Vector3.Distance(neighbor.position, end.position);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        if (closestReachableNode != null && closestReachableNode != start)
            return RetracePath(start, closestReachableNode);

        return null;
    }

    bool CanTraverse(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;

        if (dist < 0.01f)
            return false;

        dir.Normalize();

        float inflatedRadius = controller.radius * (safetyMultiplier + 0.3f);
        float height = controller.height;

        Vector3 center = from + controller.center;
        Vector3 p1 = center + Vector3.up * inflatedRadius;
        Vector3 p2 = center + Vector3.up * (height - inflatedRadius);

        LayerMask obstacleMask = NonWalkableLayer | WalkableLayer;

        if (Physics.CapsuleCast(p1, p2, inflatedRadius, dir, dist, obstacleMask))
            return false;

        return true;
    }

    IEnumerable<Node> GetNeighbors(Node node)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < gridSize && checkY >= 0 && checkY < gridSize)
                {
                    if (gridNodes[checkX, checkY] != null)
                        yield return gridNodes[checkX, checkY];
                }
            }
        }
    }

    List<Vector3> RetracePath(Node start, Node end)
    {
        List<Vector3> path = new List<Vector3>();
        Node curr = end;
        while (curr != start)
        {
            path.Add(curr.position);
            curr = curr.parent;
        }
        path.Reverse();
        return path;
    }

    Node NodeFromWorldPoint(Vector3 worldPos)
    {
        int halfSize = gridSize / 2;
        float percentX = (worldPos.x - gridCenterPos.x) / nodeSpacing;
        float percentY = (worldPos.z - gridCenterPos.z) / nodeSpacing;
        int x = Mathf.RoundToInt(percentX) + halfSize;
        int y = Mathf.RoundToInt(percentY) + halfSize;

        if (x >= 0 && x < gridSize && y >= 0 && y < gridSize)
            return gridNodes[x, y];
        return null;
    }

    Node GetClosestValidNode(Vector3 targetPos)
    {
        Node bestNode = null;
        float minDstSqr = float.MaxValue;

        foreach (var node in gridNodes)
        {
            if (node == null) continue;
            float dstSqr = (node.position - targetPos).sqrMagnitude;
            if (dstSqr < minDstSqr)
            {
                minDstSqr = dstSqr;
                bestNode = node;
            }
        }
        return bestNode;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !enabled) return;

        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = isUsingDirectPath ? Color.green : Color.cyan;
            Vector3 prev = transform.position;
            foreach (var p in currentPath)
            {
                Gizmos.DrawLine(prev, p);
                Gizmos.DrawWireSphere(p, 0.2f);
                prev = p;
            }
        }

        if (gridNodes != null)
        {
            Gizmos.color = new Color(1, 1, 0, 0.8f);
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    if (gridNodes[x, y] != null)
                        Gizmos.DrawWireCube(gridNodes[x, y].position, Vector3.one * 0.2f);
                }
            }
        }
    }
}

