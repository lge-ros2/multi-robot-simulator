/*
 * Copyright (c) 2021 LG Electronics Inc.
 *
 * SPDX-License-Identifier: MIT
 */

namespace SDF
{
	namespace Import
	{
		public partial class Loader : Base
		{
			protected override void ImportWorld(in SDF.World world)
			{
				if (world == null)
				{
					return;
				}

				// Debug.Log("Import World");
				if (world.gui != null)
				{
					var mainCamera = UnityEngine.Camera.main;
					if (mainCamera != null)
					{
						var cameraPose = world.gui.camera.Pose;
						mainCamera.transform.localPosition = SDF2Unity.GetPosition(cameraPose.Pos);
						mainCamera.transform.localRotation = SDF2Unity.GetRotation(cameraPose.Rot);
					}
				}

				if (world.spherical_coordinates != null)
				{
					var sphericalCoordinates = DeviceHelper.GetSphericalCoordinates();

					var sphericalReference = world.spherical_coordinates;

					sphericalCoordinates.SetCoordinatesReference((float)sphericalReference.latitude_deg, (float)sphericalReference.longitude_deg, (float)sphericalReference.elevation, (float)sphericalReference.heading_deg);
				}

				if (world.GetLights().Count > 0)
				{
					ImportLights(world.GetLights());
				}
			}
		}
	}
}