/*
 * Copyright (c) 2020 LG Electronics Inc.
 *
 * SPDX-License-Identifier: MIT
 */

using System.Collections.Generic;
using System.Collections;
using UE = UnityEngine;
using UEAI = UnityEngine.AI;

namespace SDF
{
	namespace Helper
	{
		[UE.DefaultExecutionOrder(590)]
		public class Actor : Base
		{
			private static int AvoidancePriorityNumber = 0;

			private struct waypointToward
			{
				public float linearSpeed;
				public float angularSpeed;
				public UE.Vector3 tranlateTo;
				public UE.Quaternion rotateTo;
			};

			private const float DistanceEpsilon = UE.Vector3.kEpsilon * 10;
			private const float AngleEpsilon = UE.Quaternion.kEpsilon * 50000;

			private List<waypointToward> waypointTowards = new List<waypointToward>();
			private int waypointTowardsIndex = 0;
			private double elapsedTimeSinceAnimationStarted = 0;
			private bool _followingWaypoint = false;
			private UE.Pose targetPose = new UE.Pose();

			[UE.Header("SDF Properties")]
			public bool isStatic = false;
			private SDF.Actor.Script script = null;

			private UE.CapsuleCollider capsuleCollider = null;
			private UE.SkinnedMeshRenderer skinMeshRenderer = null;
			private UEAI.NavMeshAgent navMeshAgent = null;

			public bool HasWayPoints => (script.trajectories != null && script.trajectories.Count > 0);

			new public void Reset()
			{
				base.Reset();

				var animationComponent = GetComponent<UE.Animation>();
				animationComponent.Rewind();

				RestartWayPointFollowing();
			}

			void Start()
			{
				capsuleCollider = GetComponentInChildren<UE.CapsuleCollider>();
				skinMeshRenderer = GetComponentInChildren<UE.SkinnedMeshRenderer>();

				if (script != null && script.auto_start && HasWayPoints)
				{
					StartWaypointFollowing();
				}

				StartCoroutine(SetAgentColliderSize());
			}

			private IEnumerator SetAgentColliderSize()
			{
				yield return new UE.WaitForFixedUpdate();

				var localBounds = skinMeshRenderer.localBounds;
				var bounds = skinMeshRenderer.bounds;
				var radius = UE.Mathf.Min(bounds.extents.x, bounds.extents.z);
				capsuleCollider.radius = radius;
				capsuleCollider.height = bounds.size.y;
				var center = capsuleCollider.center;
				center.y = bounds.extents.y;
				capsuleCollider.center = center;

				navMeshAgent = gameObject.GetComponent<UEAI.NavMeshAgent>();
				if (navMeshAgent == null)
				{
					navMeshAgent = gameObject.AddComponent<UEAI.NavMeshAgent>();
				}

				const float agentMarginRatio = 1.5f;
				navMeshAgent.radius = radius * agentMarginRatio;
				navMeshAgent.height = capsuleCollider.height;
				navMeshAgent.obstacleAvoidanceType = UEAI.ObstacleAvoidanceType.MedQualityObstacleAvoidance;
				navMeshAgent.avoidancePriority = AvoidancePriorityNumber++;
				navMeshAgent.autoBraking = true;
				navMeshAgent.autoRepath = true;
				navMeshAgent.autoTraverseOffMeshLink = false;

				yield return null;
			}

			void LateUpdate()
			{
				if (_followingWaypoint && waypointTowardsIndex < waypointTowards.Count)
				{
					if (elapsedTimeSinceAnimationStarted < script.delay_start)
					{
						elapsedTimeSinceAnimationStarted += UE.Time.timeAsDouble;
						// UE.Debug.Log("waiting for start: " + elapsedTimeSinceAnimationStarted);
						return;
					}

					var deltaTime = UE.Time.deltaTime;
					var waypoint = waypointTowards[waypointTowardsIndex];
					var linearSpeed = waypoint.linearSpeed;
					var angularSpeed = waypoint.angularSpeed;
					var moveTo = waypoint.tranlateTo;
					var rotateTo = waypoint.rotateTo;

					var currentPose = CurrentActorPose();
					var nextPosition = UE.Vector3.MoveTowards(currentPose.position, moveTo, linearSpeed * deltaTime);
					var nextRotation = UE.Quaternion.RotateTowards(currentPose.rotation, rotateTo, angularSpeed * deltaTime);

					SetActorPose(nextPosition, nextRotation);

					var diffPos = UE.Vector3.Distance(nextPosition, moveTo);
					var diffRot = UE.Quaternion.Angle(nextRotation, rotateTo);
					// UE.Debug.Log("pos diff:" + diffPos + ", rot diff: " + diffRot);
					if ((diffPos < DistanceEpsilon) && (diffRot < AngleEpsilon))
					{
						waypointTowardsIndex++;
						// UE.Debug.Log("go next waypoint: " + waypointTowardsIndex);
					}

					if (script.loop && waypointTowardsIndex >= waypointTowards.Count)
					{
						StopWaypointFollowing();
						StartWaypointFollowing();
						// UE.Debug.Log("Loop again");
					}
				}
			}

			private void StartWaypointFollowing()
			{
				_followingWaypoint = true;
			}

			private void StopWaypointFollowing()
			{
				_followingWaypoint = false;
				waypointTowardsIndex = 0;
				elapsedTimeSinceAnimationStarted = 0;
			}

			private void RestartWayPointFollowing()
			{
				StopWaypointFollowing();

				if (waypointTowards.Count > 0)
				{
					var initPose = GetPose(0);
					ClearPose();
					SetPose(initPose);
					var waypoint = waypointTowards[waypointTowardsIndex];
					SetActorPose(waypoint.tranlateTo, waypoint.rotateTo);
				}

				StartWaypointFollowing();
			}

			private UE.Pose CurrentActorPose()
			{
				return targetPose;
			}

			private void SetActorPose(in UE.Vector3 newPosition, in UE.Quaternion newRotation)
			{
				targetPose.position = newPosition;
				targetPose.rotation = newRotation;

				var initPose = (GetPoseCount() == 1) ? GetPose(0) : GetPose(1);
				// UE.Debug.Log(GetPoseCount() + " , " + initPose + ", 0: " + GetPose(0) + ", 1: " + GetPose(1));

				transform.localPosition = initPose.position + targetPose.position;
				transform.localRotation = targetPose.rotation * initPose.rotation;
			}

			public void SetScript(in SDF.Actor.Script script)
			{
				this.script = script;

				if (this.script.trajectories != null)
				{
					foreach (var trajectory in script.trajectories)
					{
						// UE.Debug.LogFormat("id:{0} type:{1} tension:{2}", trajectory.id, trajectory.Type, trajectory.tension);
						if (trajectory.waypoints.Count > 0)
						{
							var lastPosition = UE.Vector3.zero;
							var lastRotation = UE.Quaternion.identity;
							var startIndex = 0;
							var firstWayPoint = trajectory.waypoints[0];

							if (firstWayPoint.time == 0)
							{
								lastPosition = SDF2Unity.GetPosition(firstWayPoint.Pose.Pos);
								lastRotation = SDF2Unity.GetRotation(firstWayPoint.Pose.Rot);
								startIndex = 1;
							}

							SetActorPose(lastPosition, lastRotation);

							var lastTime = 0f;
							for (var i = startIndex; i < trajectory.waypoints.Count; i++)
							{
								var waypoint = trajectory.waypoints[i];
								// UE.Debug.Log("Time: " + waypoint.time + ", Position: " + SDF2Unity.GetPosition(waypoint.Pose.Pos) + ", Rotation: " + SDF2Unity.GetRotation(waypoint.Pose.Rot));

								var waypointToward = new waypointToward();
								var nextTime = (float)waypoint.time;
								var nextPosition = SDF2Unity.GetPosition(waypoint.Pose.Pos);
								var nextRotation = SDF2Unity.GetRotation(waypoint.Pose.Rot);

								waypointToward.linearSpeed = UE.Vector3.Distance(nextPosition, lastPosition) / (nextTime - lastTime);
								waypointToward.angularSpeed = UE.Quaternion.Angle(nextRotation, lastRotation) / (nextTime - lastTime);
								waypointToward.tranlateTo = nextPosition;
								waypointToward.rotateTo = nextRotation;

								waypointTowards.Add(waypointToward);
								// UE.Debug.Log("\t Speed(linear/angular): (" + waypointToward.linearSpeed + "/" + waypointToward.angularSpeed + ", pos: " + waypointToward.tranlateTo + ", rot:" + waypointToward.rotateTo);

								lastTime = nextTime;
								lastPosition = nextPosition;
								lastRotation = nextRotation;
							}
						}
					}
				}
			}
		}
	}
}