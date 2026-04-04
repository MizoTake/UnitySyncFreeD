using MizoTake.SyncFreeD.Editor.Support;
using MizoTake.SyncFreeD.ScriptableObjects;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class SyncFreeDSupportSummaryTests
    {
        [Test]
        public void Build_WhenOutputPresetMissing_RequestsOutputPreset()
        {
            var cameraObject = new GameObject("Support Summary Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();

            var snapshot = SyncFreeDSupportSummary.Build(sync);

            Assert.That(snapshot.HasSource, Is.True);
            Assert.That(snapshot.HasOutput, Is.True);
            Assert.That(snapshot.HasOutputPreset, Is.False);
            Assert.That(snapshot.Tone, Is.EqualTo(SyncFreeDStatusTone.ActionNeeded));
            Assert.That(snapshot.GuidanceMessage, Does.Contain("送信設定 Asset"));

            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void Build_WhenPresetAssigned_ReportsReadyState()
        {
            var cameraObject = new GameObject("Support Summary Ready Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var controller = cameraObject.AddComponent<FreeDControllerBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            var outputProfile = ScriptableObject.CreateInstance<FreeDUdpOutputProfileAsset>();
            var controllerProfile = ScriptableObject.CreateInstance<FreeDControllerProfileAsset>();
            output.SetOutputProfileAsset(outputProfile, true);
            controller.SetProfileAsset(controllerProfile, true);

            var snapshot = SyncFreeDSupportSummary.Build(sync);

            Assert.That(snapshot.HasOutputPreset, Is.True);
            Assert.That(snapshot.HasControllerPreset, Is.True);
            Assert.That(snapshot.Tone, Is.EqualTo(SyncFreeDStatusTone.Ready));
            Assert.That(snapshot.StatusTitle, Does.Contain("確認しやすい状態"));
            Assert.That(snapshot.GuidanceMessage, Does.Contain("概ね整っています"));

            Object.DestroyImmediate(outputProfile);
            Object.DestroyImmediate(controllerProfile);
            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void GetSampleScenePath_ReturnsAssetsSamplePath()
        {
            var path = SyncFreeDSupportSummary.GetSampleScenePath(SyncFreeDSampleId.FreeDController);
            Assert.That(path, Does.Contain("Assets/Samples/SyncFreeD/FreeDControllerSample"));
        }

        [Test]
        public void CreateOutputPresetFromBehaviour_CreatesAssetAndAssignsIt()
        {
            var cameraObject = new GameObject("Preset Output Camera");
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var asset = SyncFreeDSupportSummary.CreateOutputPresetFromBehaviour(output, "TestGeneratedOutputPreset");

            Assert.That(asset, Is.Not.Null);
            Assert.That(output.OutputProfileAsset, Is.EqualTo(asset));
            Assert.That(asset.Value.DestinationIpAddress, Is.EqualTo("127.0.0.1"));

            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(assetPath))
            {
                UnityEditor.AssetDatabase.DeleteAsset(assetPath);
            }

            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void CreateControllerPresetFromBehaviour_CreatesAssetAndAssignsIt()
        {
            var cameraObject = new GameObject("Preset Controller Camera");
            var controller = cameraObject.AddComponent<FreeDControllerBehaviour>();
            var asset = SyncFreeDSupportSummary.CreateControllerPresetFromBehaviour(controller, "TestGeneratedControllerPreset");

            Assert.That(asset, Is.Not.Null);
            Assert.That(controller.ProfileAsset, Is.EqualTo(asset));
            Assert.That(asset.Value.MoveSpeedMetersPerSecond, Is.GreaterThan(0f));

            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(assetPath))
            {
                UnityEditor.AssetDatabase.DeleteAsset(assetPath);
            }

            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void LoadSharedPresets_ReturnAssetsFromSamples()
        {
            Assert.That(SyncFreeDSupportSummary.LoadSharedOutputPreset(), Is.Not.Null);
            Assert.That(SyncFreeDSupportSummary.LoadSharedControllerPreset(), Is.Not.Null);
        }

        [Test]
        public void ApplySharedPresets_AssignsSharedAssets()
        {
            var cameraObject = new GameObject("Shared Preset Camera");
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var controller = cameraObject.AddComponent<FreeDControllerBehaviour>();

            Assert.That(SyncFreeDSupportSummary.ApplySharedOutputPreset(output), Is.True);
            Assert.That(SyncFreeDSupportSummary.ApplySharedControllerPreset(controller), Is.True);
            Assert.That(output.OutputProfileAsset, Is.Not.Null);
            Assert.That(controller.ProfileAsset, Is.Not.Null);
            Assert.That(UnityEditor.AssetDatabase.GetAssetPath(output.OutputProfileAsset), Is.EqualTo(SyncFreeDSupportSummary.SharedOutputPresetPath));
            Assert.That(UnityEditor.AssetDatabase.GetAssetPath(controller.ProfileAsset), Is.EqualTo(SyncFreeDSupportSummary.SharedControllerPresetPath));

            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void GetRecommendedSampleId_WhenControllerExists_ReturnsControllerSample()
        {
            var cameraObject = new GameObject("Recommended Controller Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            cameraObject.AddComponent<FreeDControllerBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();

            Assert.That(SyncFreeDSupportSummary.GetRecommendedSampleId(sync), Is.EqualTo(SyncFreeDSampleId.FreeDController));
            Assert.That(SyncFreeDSupportSummary.GetRecommendedSampleReason(sync), Does.Contain("FreeDControllerSample"));

            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void GetRecommendedSampleId_WhenNothingSelected_ReturnsBasicVirtualCamera()
        {
            Assert.That(SyncFreeDSupportSummary.GetRecommendedSampleId(null), Is.EqualTo(SyncFreeDSampleId.BasicVirtualCamera));
        }

        [Test]
        public void EnsureController_WhenMissing_AddsControllerAndAppliesSharedPreset()
        {
            var cameraObject = new GameObject("Ensure Controller Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();

            var controller = SyncFreeDSupportSummary.EnsureController(sync);

            Assert.That(controller, Is.Not.Null);
            Assert.That(sync.GetComponent<FreeDControllerBehaviour>(), Is.EqualTo(controller));
            Assert.That(controller.ProfileAsset, Is.Not.Null);

            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void Build_WhenFirmwareDoesNotSupportOutputMode_ReportsWarningState()
        {
            var cameraObject = new GameObject("Firmware Warning Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            var firmwareAsset = ScriptableObject.CreateInstance<FirmwareBehaviorProfileAsset>();
            firmwareAsset.Value.SupportsMulticast = false;
            SetPrivateField(output, "packetSendMode", Networking.PacketSendMode.Multicast);
            SetPrivateField(sync, "firmwareBehaviorProfileAsset", firmwareAsset);

            var snapshot = SyncFreeDSupportSummary.Build(sync);

            Assert.That(snapshot.HasConfigurationWarning, Is.True);
            Assert.That(snapshot.ConfigurationWarning, Does.Contain("Multicast"));
            Assert.That(snapshot.Tone, Is.EqualTo(SyncFreeDStatusTone.Warning));

            Object.DestroyImmediate(firmwareAsset);
            Object.DestroyImmediate(cameraObject);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
