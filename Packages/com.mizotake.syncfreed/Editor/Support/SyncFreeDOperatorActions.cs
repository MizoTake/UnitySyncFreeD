using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using UnityEditor;
using UnityEngine;

namespace MizoTake.SyncFreeD.Editor.Support
{
    public static class SyncFreeDOperatorActions
    {
        public static bool TryTranslate(SyncFreeDBehaviour behaviour, Vector3 localTranslationMeters)
        {
            var controller = GetController(behaviour);
            if (controller == null)
            {
                return false;
            }

            Undo.RecordObject(controller.ControlledTransform, "SyncFreeD Translate");
            controller.ApplyTranslation(localTranslationMeters);
            FinalizeOperation(behaviour, controller);
            return true;
        }

        public static bool TryRotate(SyncFreeDBehaviour behaviour, Vector3 deltaEulerDegrees)
        {
            var controller = GetController(behaviour);
            if (controller == null)
            {
                return false;
            }

            Undo.RecordObject(controller.ControlledTransform, "SyncFreeD Rotate");
            controller.ApplyRotation(deltaEulerDegrees);
            FinalizeOperation(behaviour, controller);
            return true;
        }

        public static bool TryAdjustLens(SyncFreeDBehaviour behaviour, float focalLengthDeltaMm, float focusDistanceDeltaMeters)
        {
            var controller = GetController(behaviour);
            var camera = controller != null ? controller.ControlledCamera : null;
            if (controller == null || camera == null)
            {
                return false;
            }

            Undo.RecordObject(camera, "SyncFreeD Adjust Lens");
            controller.ApplyLensDelta(focalLengthDeltaMm, focusDistanceDeltaMeters);
            FinalizeOperation(behaviour, controller);
            return true;
        }

        public static bool TryReset(SyncFreeDBehaviour behaviour)
        {
            var controller = GetController(behaviour);
            if (controller == null)
            {
                return false;
            }

            Undo.RecordObject(controller.ControlledTransform, "SyncFreeD Reset Pose");
            if (controller.ControlledCamera != null)
            {
                Undo.RecordObject(controller.ControlledCamera, "SyncFreeD Reset Lens");
            }

            controller.ResetPoseAndLens();
            FinalizeOperation(behaviour, controller);
            return true;
        }

        public static bool TryManualRefresh(SyncFreeDBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return false;
            }

            var result = behaviour.ManualTick();
            EditorUtility.SetDirty(behaviour);
            SceneView.RepaintAll();
            return result;
        }

        private static FreeDControllerBehaviour GetController(SyncFreeDBehaviour behaviour)
        {
            return behaviour != null ? behaviour.GetComponent<FreeDControllerBehaviour>() : null;
        }

        private static void FinalizeOperation(SyncFreeDBehaviour behaviour, FreeDControllerBehaviour controller)
        {
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(controller.ControlledTransform);
            if (controller.ControlledCamera != null)
            {
                EditorUtility.SetDirty(controller.ControlledCamera);
            }

            if (Application.isPlaying && behaviour != null)
            {
                behaviour.ManualTick();
                EditorUtility.SetDirty(behaviour);
            }

            SceneView.RepaintAll();
        }
    }
}
