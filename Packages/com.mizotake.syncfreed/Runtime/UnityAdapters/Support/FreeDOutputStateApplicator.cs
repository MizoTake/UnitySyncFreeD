using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Support
{
    public static class FreeDOutputStateApplicator
    {
        public static CameraSyncState Apply(in CameraSyncState state, MountProfile mountProfile, FirmwareBehaviorProfile firmwareProfile)
        {
            if (mountProfile == null)
            {
                return state;
            }

            var adjustedState = state;
            adjustedState.Corrected = Apply(state.Corrected, mountProfile, firmwareProfile);
            return adjustedState;
        }

        private static PoseState Apply(in PoseState pose, MountProfile mountProfile, FirmwareBehaviorProfile firmwareProfile)
        {
            var usesImageSensorBasedPosition = firmwareProfile == null || firmwareProfile.UsesImageSensorBasedPosition;
            var usesImageSensorBasedOrientation = firmwareProfile == null || firmwareProfile.UsesImageSensorBasedOrientation;
            var outputPosition = PosePositionToUnity(pose) + PoseVectorToUnity(mountProfile.TrackingOriginOffsetMm);
            var outputRotation = PoseRotationToUnity(pose);
            if (usesImageSensorBasedOrientation)
            {
                outputRotation *= RotationOffsetToUnity(mountProfile.RotationOffsetDeg);
            }

            if (usesImageSensorBasedPosition)
            {
                outputPosition += outputRotation * PoseVectorToUnity(mountProfile.SensorOffsetMm);
            }

            var adjustedPose = pose;
            ApplyUnityPosition(ref adjustedPose, outputPosition);
            if (usesImageSensorBasedOrientation)
            {
                ApplyUnityRotation(ref adjustedPose, outputRotation);
            }

            return adjustedPose;
        }

        private static Vector3 PosePositionToUnity(in PoseState pose)
        {
            return new Vector3((float)pose.Xmm, (float)pose.Zmm, (float)pose.Ymm);
        }

        private static Quaternion PoseRotationToUnity(in PoseState pose)
        {
            return Quaternion.Euler((float)-pose.TiltDeg, (float)-pose.PanDeg, (float)pose.RollDeg);
        }

        private static Quaternion RotationOffsetToUnity(Vector3Data rotationOffsetDeg)
        {
            return Quaternion.Euler((float)-rotationOffsetDeg.X, (float)-rotationOffsetDeg.Z, (float)rotationOffsetDeg.Y);
        }

        private static Vector3 PoseVectorToUnity(Vector3Data vector)
        {
            return new Vector3((float)vector.X, (float)vector.Z, (float)vector.Y);
        }

        private static void ApplyUnityPosition(ref PoseState pose, Vector3 unityPosition)
        {
            pose.Xmm = unityPosition.x;
            pose.Ymm = unityPosition.z;
            pose.Zmm = unityPosition.y;
        }

        private static void ApplyUnityRotation(ref PoseState pose, Quaternion unityRotation)
        {
            var euler = unityRotation.eulerAngles;
            pose.PanDeg = NormalizeSignedAngle(-euler.y);
            pose.TiltDeg = NormalizeSignedAngle(-euler.x);
            pose.RollDeg = NormalizeSignedAngle(euler.z);
        }

        private static float NormalizeSignedAngle(float angle)
        {
            return Mathf.Repeat(angle + 180f, 360f) - 180f;
        }
    }
}
