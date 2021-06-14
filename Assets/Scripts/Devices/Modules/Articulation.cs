/*
 * Copyright (c) 2020 LG Electronics Inc.
 *
 * SPDX-License-Identifier: MIT
 */

using UnityEngine;

public class Articulation
{
	public enum DriveType {NONE, FORCE_AND_VELOCITY, POSITION_AND_VELOCITY};
	private DriveType driveType = DriveType.NONE;

	private ArticulationBody joint = null;
	private ArticulationJointType jointType = ArticulationJointType.FixedJoint;

	public ArticulationJointType Type => jointType;

	public Articulation(in ArticulationBody joint)
	{
		this.joint = joint;
		this.jointType = this.joint.jointType;
	}

	public Articulation(in GameObject target)
	{
		var body = target.GetComponentInChildren<ArticulationBody>();
		this.joint = body;
		this.jointType = this.joint.jointType;
	}

	public void Reset()
	{
		this.joint.velocity = Vector3.zero;
		this.joint.angularVelocity = Vector3.zero;
	}

	public void SetDriveType(in DriveType type)
	{
		this.driveType = type;
	}

	public bool IsRevoluteType()
	{
		return (Type.Equals(ArticulationJointType.RevoluteJoint) || Type.Equals(ArticulationJointType.SphericalJoint)) ? true : false;
	}

	protected void SetJointVelocity(in float velocity, in int targetDegree = 0)
	{
		if (this.joint != null)
		{
			var jointVelocity = this.joint.jointVelocity;
			jointVelocity[targetDegree] = velocity;
			this.joint.jointVelocity = jointVelocity;
		}
	}

	/// <returns>in radian for angular and in meters for linear</param>
	public float GetJointPosition(in int index = 0)
	{
		return (this.joint == null) ? 0 : this.joint.jointPosition[index];
	}

	/// <returns>torque for angular and force for linear</param>
	public float GetJointForce(in int index = 0)
	{
		return (this.joint == null) ? 0 : this.joint.jointForce[index];
	}

	/// <returns>in radian for angular and in meters for linear</param>
	public float GetJointVelocity(in int index = 0)
	{
		return (this.joint == null) ? 0 : this.joint.jointVelocity[index];
	}

	/// <param name="target">force or torque desired for FORCE_AND_VELOCITY type and position for POSITION_AND_VELOCITY.</param>
	/// <param name="targetVelocity">angular velocity in degrees per second.</param>
	public void Drive(in DriveType type, in float target, in float targetVelocity)
	{
		if (this.joint == null)
		{
			Debug.LogWarning("ArticulationBody is empty, please set target body first");
			return;
		}

		// Arccording to document(https://docs.unity3d.com/2020.3/Documentation/ScriptReference/ArticulationDrive.html)
		// F = stiffness * (currentPosition - target) - damping * (currentVelocity - targetVelocity).
		var drive = DeviceHelper.GetDrive(ref this.joint);

		switch (type)
		{
			case DriveType.FORCE_AND_VELOCITY:
				drive.damping = target;
				break;
			case DriveType.POSITION_AND_VELOCITY:
				drive.target = target;
				break;
		}
		drive.targetVelocity = targetVelocity;
		DeviceHelper.SetDrive(ref this.joint, drive);
	}
}