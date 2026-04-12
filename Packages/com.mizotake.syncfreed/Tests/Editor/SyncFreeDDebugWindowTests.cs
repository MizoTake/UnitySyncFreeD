using System.Reflection;
using MizoTake.SyncFreeD.Editor.Windows;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class SyncFreeDDebugWindowTests
    {
        [Test]
        public void OnSelectionChange_WhenSeparatedOutputRootSelected_ResolvesAssignedSyncBehaviour()
        {
            var cameraObject = new GameObject("Debug Window Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            var outputRoot = new GameObject("Debug Window Output");
            var output = outputRoot.AddComponent<FreeDUdpOutputBehaviour>();
            var window = EditorWindow.GetWindow<SyncFreeDDebugWindow>();
            SetPrivateField(sync, "outputBehaviour", output);

            try
            {
                Selection.activeGameObject = outputRoot;
                InvokeOnSelectionChange(window);

                Assert.That(GetTarget(window), Is.EqualTo(sync));
            }
            finally
            {
                Selection.activeGameObject = null;
                window.Close();
                Object.DestroyImmediate(outputRoot);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void OnSelectionChange_WhenPlainSeparatedRootSelected_ResolvesNearestSceneSyncBehaviour()
        {
            var earlierCameraObject = new GameObject("Earlier Debug Window Camera");
            earlierCameraObject.AddComponent<Camera>();
            earlierCameraObject.AddComponent<UnityCameraSourceBehaviour>();
            earlierCameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            earlierCameraObject.AddComponent<SyncFreeDBehaviour>();
            var targetCameraObject = new GameObject("Target Debug Window Camera");
            targetCameraObject.AddComponent<Camera>();
            targetCameraObject.AddComponent<UnityCameraSourceBehaviour>();
            targetCameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var targetSync = targetCameraObject.AddComponent<SyncFreeDBehaviour>();
            var separatedRoot = new GameObject("Debug Window Helper Root");
            var window = EditorWindow.GetWindow<SyncFreeDDebugWindow>();

            try
            {
                Selection.activeGameObject = separatedRoot;
                InvokeOnSelectionChange(window);

                Assert.That(GetTarget(window), Is.EqualTo(targetSync));
            }
            finally
            {
                Selection.activeGameObject = null;
                window.Close();
                Object.DestroyImmediate(separatedRoot);
                Object.DestroyImmediate(targetCameraObject);
                Object.DestroyImmediate(earlierCameraObject);
            }
        }

        private static void InvokeOnSelectionChange(SyncFreeDDebugWindow window)
        {
            var method = typeof(SyncFreeDDebugWindow).GetMethod("OnSelectionChange", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(window, null);
        }

        private static SyncFreeDBehaviour GetTarget(SyncFreeDDebugWindow window)
        {
            var field = typeof(SyncFreeDDebugWindow).GetField("targetBehaviour", BindingFlags.Instance | BindingFlags.NonPublic);
            return (SyncFreeDBehaviour)field.GetValue(window);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
