using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Landscape Builder. Copyright (c) 2016-2018 SCSM Pty Ltd. All rights reserved.
namespace LandscapeBuilder
{
    /// <summary>
    /// Sample script to move a NPC along a navmesh path.
    /// As this script is subject to change, if you'd like to re-use
    /// this code in your game project, create a new class in your
    /// game and copy the contents of this class into yours. Ensure
    /// that you use your own namespace.
    /// </summary>
    public class LBNPCNav : MonoBehaviour
    {
        #region Public variables
        public bool useNavMeshAgent = false;
        public float speed = 1.0f;
        public float stoppingDistance = 2f;
        public float acceleration = 3f;
        /// <summary>
        /// Animation speed when transform movement is 1.0
        /// The animation speed will automatically be adjusted
        /// based on the speed of the transform. e.g. if speed = 2
        /// animation speed will be 2 x animSpeedMultipier
        /// </summary>
        public float animSpeedMultiplier = 1.0f;
        #if LANDSCAPE_BUILDER
        public LandscapeBuilder.LBMapPath mapPath = null;
        public GameObject meshesParent = null;
        public LandscapeBuilder.LBRandom lbRandom = null;
        #endif
        #endregion

        #region Private variables
        private NavMeshAgent nmAgent = null;    
        private bool validPath = false;
        private int numPathPoints = 0;
        private Vector3 targetLocation = Vector3.zero;
        private Vector3 prevPosition = Vector3.zero;
        private Vector3 currPosition = Vector3.zero;
        private float objSpeed = 0f;

        // Variables used with LBMapPath
        private Vector3 startLocation = Vector3.zero;
        private Vector3 endLocation = Vector3.zero;

        // Variables used with meshes
        private List<Vector3> wayPoints = null;
        private int numWayPoints = 0;        

        // Animator varibles
        private Animator animator = null;
        private readonly string ANisWalking = "isWalking";
        private readonly string ANanimSpeed = "animSpeed";

        #endregion

        #region Initialisation

        void Awake()
        {
            Initialise();
        }

        public void Initialise()
        {
            nmAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            prevPosition = transform.position;

            try
            {
                #region Initial setup
                if (nmAgent == null && useNavMeshAgent)
                {
                    nmAgent = gameObject.AddComponent<NavMeshAgent>();
                }

                if (nmAgent != null && animator != null)
                {
                    animator.SetFloat(ANanimSpeed, 0f);
                }
                #endregion

                #if LANDSCAPE_BUILDER
                if (nmAgent != null && nmAgent.isOnNavMesh)
                {
                    #region MapPath navigation
                    // Use the Landscape Builder MapPath as a method of simple navigation
                    if (mapPath != null && mapPath.lbPath != null && mapPath.lbPath.positionList != null)
                    {
                        numPathPoints = mapPath.lbPath.positionList.Count;

                        if (numPathPoints > 1)
                        {
                            startLocation = mapPath.lbPath.positionList[0];
                            endLocation = mapPath.lbPath.positionList[numPathPoints - 1];

                            if (animator != null)
                            {
                                animator.SetBool(ANisWalking, true);
                                animator.SetFloat(ANanimSpeed, speed * animSpeedMultiplier);

                                // Init navigation
                                nmAgent.stoppingDistance = stoppingDistance;
                                nmAgent.acceleration = acceleration;
                                nmAgent.speed = speed;
                                nmAgent.nextPosition = startLocation;
                                prevPosition = startLocation;
                                currPosition = startLocation;
                                targetLocation = endLocation;
                                nmAgent.SetDestination(targetLocation);
                            }

                            validPath = true;
                        }
                    }
                    #endregion

                    #region Mesh Navigation
                    // Use the child meshes as potential locations to navigate over
                    else if (meshesParent != null)
                    {
                        // Get an array of all the meshes under the supplied parent gameobject
                        MeshFilter[] meshFilters = meshesParent.GetComponentsInChildren<MeshFilter>();
                        MeshFilter meshFilter = null;
                        int numMeshes = (meshFilters == null ? 0 : meshFilters.Length);

                        int numVerts = 0;

                        // Loop through all the child meshes to discover how many verts are in them
                        for (int mshIdx = 0; mshIdx < numMeshes; mshIdx++)
                        {
                            meshFilter = meshFilters[mshIdx];
                            if (meshFilter != null && meshFilter.sharedMesh != null)
                            {
                                numVerts += meshFilter.sharedMesh.vertexCount;
                            }
                        }

                        //Debug.Log("[DEBUG] " + numVerts + " in " + numMeshes + " meshes under " + meshesParent.name);

                        // Check for "reasonable" number of verts
                        if (numVerts > 1 && numVerts < 10000)
                        {
                            // Create an empty list with sufficient capacity to avoid expansion of the list.
                            wayPoints = new List<Vector3>(numVerts);

                            // Construct a list of potential way points
                            for (int mshIdx = 0; mshIdx < numMeshes; mshIdx++)
                            {
                                meshFilter = meshFilters[mshIdx];
                                if (meshFilter != null && meshFilter.sharedMesh != null)
                                {
                                    wayPoints.AddRange(meshFilter.sharedMesh.vertices);
                                }
                            }

                            // TODO: Remove wayPoints that are inside other objects.

                            // Pre-count waypoints so we don't need to do it in FixedUpdate().
                            numWayPoints = (wayPoints == null ? 0 : wayPoints.Count);

                            if (numWayPoints > 1)
                            {
                                lbRandom = new LBRandom();
                                // Give it an arbitary seed based on the current location of the NPC.
                                if (lbRandom != null) { lbRandom.SetSeed(Mathf.RoundToInt(this.transform.position.x)); }

                                if (animator != null)
                                {
                                    animator.SetBool(ANisWalking, true);
                                    animator.SetFloat(ANanimSpeed, speed * animSpeedMultiplier);

                                    // Init navigation
                                    startLocation = this.transform.position;
                                    nmAgent.stoppingDistance = stoppingDistance;
                                    nmAgent.acceleration = acceleration;
                                    nmAgent.speed = speed;
                                    nmAgent.nextPosition = startLocation;
                                    prevPosition = startLocation;
                                    currPosition = startLocation;

                                    SetNextWayPoint();
                                }
                            }
                        }
                    }

                    #endregion
                }
                #endif


            }
            catch (UnityException uEx)
            {
                Debug.LogWarning("LBNPCNav.Initialise " + uEx.Message);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("LBNPCNav.Initialise " + ex.Message);
            }
        }

        #endregion

        #region Update Methods

        void FixedUpdate()
        {
            if (validPath || numWayPoints > 1)
            {
                currPosition = transform.position;

                // Update the animation speed
                if (animator != null && nmAgent != null)
                {
                    // Get the distance travelled and smooth it based on the previous speed
                    objSpeed = Mathf.Lerp(objSpeed, (currPosition - prevPosition).magnitude, 0.7f);

                    // navmeshagent uses m/sec so x by 60.
                    animator.SetFloat(ANanimSpeed, objSpeed * 60f * animSpeedMultiplier);
                }

                // Get the distance to the target
                float distToTarget = Vector3.Distance(currPosition, targetLocation);
                      
                if (validPath && distToTarget < 1.2f)
                {
                    // when close to the destination, go to the next location
                    if (targetLocation.x == endLocation.x && targetLocation.z == endLocation.z) { targetLocation = startLocation; }
                    else if (targetLocation.x == startLocation.x && targetLocation.z == startLocation.z) { targetLocation = endLocation; }
                    nmAgent.SetDestination(targetLocation);
                }
                // NOTE: if there are any objects on the navmesh, the NPC may get "stuck"
                else if (numWayPoints > 1 && distToTarget < 1.2f)
                {
                    SetNextWayPoint();
                }

                prevPosition = currPosition;
            }
        }

        #endregion

        #region Public Member Methods



        #endregion

        #region Private Member Methods

        /// <summary>
        /// Set the next waypoint by randomly selecting it from the
        /// list of available waypoints.
        /// To Do: Don't return waypoints inside other objects.
        /// </summary>
        private void SetNextWayPoint()
        {
            #if LANDSCAPE_BUILDER
            if (numWayPoints > 1 && lbRandom != null && nmAgent != null)
            {
                int nextWayPoint = lbRandom.Range(0, numWayPoints + 1);
                targetLocation = wayPoints[nextWayPoint];
                nmAgent.SetDestination(targetLocation);
            }
            #endif
        }

        #endregion
    }
}