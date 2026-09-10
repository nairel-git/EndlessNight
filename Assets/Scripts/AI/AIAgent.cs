using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class AIAgent : MonoBehaviour
{
    [Header("Target & Layers")]
    [SerializeField] private GameObject targetPlayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Breadcrumb Settings")]
    [SerializeField] private float recordDistance = 0.8f;
    [SerializeField] private float waypointReachRadius = 0.6f;
    [SerializeField] private int maxBreadcrumbs = 50;

    [Header("Clearances")]
    [SerializeField] private float bodyPadding = 0.4f;
    [SerializeField] private float sideOffset = 1.6f;

    [Header("Distance Zones")]
    [SerializeField] private float startFollowDistance = 4.5f;
    [SerializeField] private float stopFollowDistance = 2.2f;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private float turnSpeed = 8.0f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private CharacterController controller;
    private readonly List<Vector3> breadcrumbs = new List<Vector3>();
    private bool isFollowing = false;
    private float verticalVelocity = 0f;
    

    private Vector3 currentTargetPos;
    private bool canSeePlayer;
    private bool isNearWall;



    private List<Ray> whiskerRays = new List<Ray>();
    private List<bool> whiskerHits = new List<bool>();
    private List<Ray> pathGridRays = new List<Ray>();
    private List<bool> pathGridHits = new List<bool>();

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (targetPlayer == null) return;

        UpdatePlayerTrail();

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);

     
        canSeePlayer = IsPathClearGrid(targetPlayer.transform.position, usePadding: true, saveGizmos: true);
        

        isNearWall = CheckWallProximity();

 
        if (!isFollowing && distanceToPlayer > startFollowDistance)
            isFollowing = true;
        else if (isFollowing && distanceToPlayer <= stopFollowDistance)
            isFollowing = false;


        if (!isFollowing)
        {
            LookAtSmooth(targetPlayer.transform.position);
            ApplyGravityOnly();
            return;
        }


        if (canSeePlayer && !isNearWall)
        {
            currentTargetPos = GetBestSidePosition();
            breadcrumbs.Clear();
        }
        else
        {
            currentTargetPos = GetBestBreadcrumbTarget();
        }

        MoveTo(currentTargetPos, distanceToPlayer);
    }

    private void UpdatePlayerTrail()
    {
        Vector3 playerPos = targetPlayer.transform.position;

        if (breadcrumbs.Count == 0)
        {
            breadcrumbs.Add(playerPos);
            return;
        }

        Vector3 lastPoint = breadcrumbs[breadcrumbs.Count - 1];
        float dist2D = Vector2.Distance(new Vector2(playerPos.x, playerPos.z), new Vector2(lastPoint.x, lastPoint.z));

        if (dist2D >= recordDistance)
        {
            breadcrumbs.Add(playerPos);
            if (breadcrumbs.Count > maxBreadcrumbs) breadcrumbs.RemoveAt(0);
        }
    }

    private bool CheckWallProximity()
    {
        whiskerRays.Clear();
        whiskerHits.Clear();

        bool hitWall = false;
        float whiskerLength = controller.radius + bodyPadding + 0.1f;

        float[] heights = { 
            controller.stepOffset + 0.1f, 
            controller.height * 0.5f, 
            controller.height * 0.9f 
        };

        Vector3[] directions = { transform.right, -transform.right };

        foreach (float h in heights)
        {
            Vector3 origin = transform.position + Vector3.up * h;
            foreach (Vector3 dir in directions)
            {
                bool hit = Physics.Raycast(origin, dir, whiskerLength, obstacleLayer);
                if (hit) hitWall = true;

                whiskerRays.Add(new Ray(origin, dir * whiskerLength));
                whiskerHits.Add(hit);
            }
        }

        return hitWall;
    }

    /// <summary>
    /// MALHA DE LASERS: Dispara 12 Linecasts (3 Alturas x 4 Larguras) formando um túnel perfeito.
    /// Impossível bugar por nascer dentro de paredes.
    /// </summary>
    private bool IsPathClearGrid(Vector3 targetPos, bool usePadding, bool saveGizmos = false)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0; 
        float distance = dir.magnitude;

        if (distance <= 0.1f) return true;

        Vector3 right = Vector3.Cross(Vector3.up, dir.normalized);
        float castRadius = controller.radius + (usePadding ? bodyPadding : 0f);

        // 3 Alturas: Canela, Cintura e Cabeça
        float[] heights = { 
            controller.stepOffset + 0.1f, 
            controller.height * 0.5f, 
            controller.height * 0.9f 
        };

        // 4 Larguras: Extrema Esquerda, Meio Esquerda, Meio Direita, Extrema Direita
        float[] widths = { 
            -castRadius, 
            -castRadius * 0.33f, 
            castRadius * 0.33f, 
            castRadius 
        };

        if (saveGizmos)
        {
            pathGridRays.Clear();
            pathGridHits.Clear();
        }

        bool pathIsClear = true;

        foreach (float h in heights)
        {
            foreach (float w in widths)
            {
                Vector3 origin = transform.position + Vector3.up * h + right * w;
                Vector3 target = targetPos + Vector3.up * h + right * w;
                
                Vector3 rayDir = (target - origin).normalized;
                float rayDist = distance - 0.2f;

                bool hit = Physics.Linecast(origin, origin + rayDir * rayDist, obstacleLayer);

                if (saveGizmos)
                {
                    pathGridRays.Add(new Ray(origin, rayDir * rayDist));
                    pathGridHits.Add(hit);
                }

                if (hit)
                {
                    pathIsClear = false;
                    // Se não for para salvar o debug dos gizmos, aborta no primeiro que bater para economizar CPU
                    if (!saveGizmos) return false; 
                }
            }
        }

        return pathIsClear;
    }

    private Vector3 GetBestBreadcrumbTarget()
    {
        if (breadcrumbs.Count == 0) return targetPlayer.transform.position;

        if (!isNearWall)
        {
            for (int i = breadcrumbs.Count - 1; i > 0; i--)
            {
                
                if (IsPathClearGrid(breadcrumbs[i], usePadding: true, saveGizmos: false))
                {
                    breadcrumbs.RemoveRange(0, i); 
                    CheckBreadcrumbArrival(breadcrumbs[0]);
                    return breadcrumbs.Count > 0 ? breadcrumbs[0] : targetPlayer.transform.position;
                }
            }
        }


        if (IsPathClearGrid(breadcrumbs[0], usePadding: false, saveGizmos: false))
        {
            CheckBreadcrumbArrival(breadcrumbs[0]);
            return breadcrumbs.Count > 0 ? breadcrumbs[0] : transform.position;
        }

        CheckBreadcrumbArrival(breadcrumbs[0]);
        return breadcrumbs.Count > 0 ? breadcrumbs[0] : transform.position;
    }

    private void CheckBreadcrumbArrival(Vector3 targetPos)
    {
        Vector2 pos2D = new Vector2(transform.position.x, transform.position.z);
        Vector2 target2D = new Vector2(targetPos.x, targetPos.z);

        if (Vector2.Distance(pos2D, target2D) <= waypointReachRadius)
        {
            breadcrumbs.RemoveAt(0);
        }
    }

    private Vector3 GetBestSidePosition()
    {
        Transform pTransform = targetPlayer.transform;
        Vector3 rightPos = pTransform.position + (pTransform.right * sideOffset);
        Vector3 leftPos = pTransform.position - (pTransform.right * sideOffset);

        bool rightFree = !Physics.CheckSphere(rightPos + Vector3.up * (controller.height / 2f), controller.radius, obstacleLayer) && IsPathClearGrid(rightPos, true, false);
        bool leftFree = !Physics.CheckSphere(leftPos + Vector3.up * (controller.height / 2f), controller.radius, obstacleLayer) && IsPathClearGrid(leftPos, true, false);

        if (rightFree && leftFree)
            return Vector3.Distance(transform.position, rightPos) < Vector3.Distance(transform.position, leftPos) ? rightPos : leftPos;

        if (rightFree) return rightPos;
        if (leftFree) return leftPos;

        return targetPlayer.transform.position;
    }

    private void MoveTo(Vector3 destination, float distToPlayer)
    {
        Vector3 moveDirection = destination - transform.position;
        moveDirection.y = 0f;

        if (controller.isGrounded)
            verticalVelocity = -1f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        if (moveDirection.sqrMagnitude > 0.04f)
        {
            float speed = distToPlayer > 7.0f ? sprintSpeed : walkSpeed;
            Vector3 velocity = moveDirection.normalized * speed;
            velocity.y = verticalVelocity;

            controller.Move(velocity * Time.deltaTime);
            LookAtSmooth(transform.position + moveDirection);
        }
        else
        {
            ApplyGravityOnly();
        }
    }

    private void ApplyGravityOnly()
    {
        if (controller.isGrounded)
            verticalVelocity = -1f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
    }

    private void LookAtSmooth(Vector3 lookTarget)
    {
        Vector3 direction = lookTarget - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || controller == null) return;


        for (int i = 0; i < whiskerRays.Count; i++)
        {
            Gizmos.color = whiskerHits[i] ? Color.red : Color.yellow;
            Gizmos.DrawLine(whiskerRays[i].origin, whiskerRays[i].origin + whiskerRays[i].direction);
            if (whiskerHits[i]) Gizmos.DrawSphere(whiskerRays[i].origin + whiskerRays[i].direction, 0.05f);
        }

      
        for (int i = 0; i < pathGridRays.Count; i++)
        {
           
            Gizmos.color = pathGridHits[i] ? new Color(1, 0, 0, 0.5f) : new Color(0, 1, 0, 0.2f);
            Gizmos.DrawLine(pathGridRays[i].origin, pathGridRays[i].origin + pathGridRays[i].direction);
            if (pathGridHits[i]) Gizmos.DrawSphere(pathGridRays[i].origin + pathGridRays[i].direction, 0.08f);
        }

        if (targetPlayer == null) return;

       
        if (breadcrumbs != null && breadcrumbs.Count > 0)
        {
            if (isNearWall) Gizmos.color = new Color(1f, 0f, 0f, 0.8f); 
            else if (canSeePlayer) Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
            else Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);

            for (int i = 0; i < breadcrumbs.Count; i++)
            {
                Gizmos.DrawSphere(breadcrumbs[i] + Vector3.up * 0.1f, 0.15f);
                if (i < breadcrumbs.Count - 1) Gizmos.DrawLine(breadcrumbs[i] + Vector3.up * 0.1f, breadcrumbs[i + 1] + Vector3.up * 0.1f);
            }
        }

    
        if (isFollowing && currentTargetPos != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.2f, currentTargetPos + Vector3.up * 0.2f);
            Gizmos.DrawWireSphere(currentTargetPos + Vector3.up * 0.1f, waypointReachRadius);
        }
    }
}