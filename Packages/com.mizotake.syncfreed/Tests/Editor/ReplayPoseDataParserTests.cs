using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class ReplayPoseDataParserTests
    {
        [Test]
        public void ParseJson_ReadsWrappedKeyframes()
        {
            const string json = "{\"keyframes\":[{\"timeSeconds\":0.0,\"position\":{\"x\":0.0,\"y\":1.0,\"z\":-10.0},\"rotation\":{\"x\":0.0,\"y\":0.0,\"z\":0.0},\"focalLengthMm\":35.0},{\"timeSeconds\":1.0,\"position\":{\"x\":1.0,\"y\":1.5,\"z\":-9.0},\"rotation\":{\"x\":0.0,\"y\":20.0,\"z\":0.0},\"focalLengthMm\":40.0}]}";

            var keyframes = ReplayPoseDataParser.Parse(json, ReplayDataFormat.Json);

            Assert.That(keyframes, Has.Length.EqualTo(2));
            Assert.That(keyframes[1].TimeSeconds, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(keyframes[1].Position.y, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(keyframes[1].Rotation.y, Is.EqualTo(20f).Within(0.0001f));
            Assert.That(keyframes[1].FocalLengthMm, Is.EqualTo(40f).Within(0.0001f));
        }

        [Test]
        public void ParseCsv_SkipsHeaderAndComments()
        {
            const string csv = "# ReplaySample\nTimeSeconds,PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,FocalLengthMm\n0.0,0.0,1.0,-10.0,0.0,0.0,0.0,35.0\n1.0,1.0,1.2,-9.0,0.0,15.0,0.0,42.0\n";

            var keyframes = ReplayPoseDataParser.Parse(csv, ReplayDataFormat.Csv);

            Assert.That(keyframes, Has.Length.EqualTo(2));
            Assert.That(keyframes[0].TimeSeconds, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(keyframes[1].Position.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(keyframes[1].Rotation.y, Is.EqualTo(15f).Within(0.0001f));
            Assert.That(keyframes[1].FocalLengthMm, Is.EqualTo(42f).Within(0.0001f));
        }
    }
}
