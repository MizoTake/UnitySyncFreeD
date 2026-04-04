using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class ReplayPoseKeyframeParserTests
    {
        [Test]
        public void TryParseJson_LoadsAndSortsKeyframes()
        {
            const string json = "{\"keyframes\":[{\"TimeSeconds\":2.0,\"Position\":{\"x\":2.0,\"y\":3.0,\"z\":4.0},\"Rotation\":{\"x\":5.0,\"y\":6.0,\"z\":7.0},\"FocalLengthMm\":55.0},{\"TimeSeconds\":1.0,\"Position\":{\"x\":1.0,\"y\":2.0,\"z\":3.0},\"Rotation\":{\"x\":4.0,\"y\":5.0,\"z\":6.0},\"FocalLengthMm\":45.0}]}";

            var result = ReplayPoseKeyframeParser.TryParse(json, ReplayDataFormat.Json, out var keyframes, out var error);

            Assert.That(result, Is.True, error);
            Assert.That(keyframes.Length, Is.EqualTo(2));
            Assert.That(keyframes[0].TimeSeconds, Is.EqualTo(1f));
            Assert.That(keyframes[1].FocalLengthMm, Is.EqualTo(55f));
        }

        [Test]
        public void TryParseCsv_LoadsHeaderedRows()
        {
            const string csv = "TimeSeconds,PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,FocalLengthMm\n0.0,0.0,1.0,-10.0,0.0,0.0,0.0,35.0\n1.0,1.0,1.2,-9.0,0.0,20.0,0.0,40.0";

            var result = ReplayPoseKeyframeParser.TryParse(csv, ReplayDataFormat.Csv, out var keyframes, out var error);

            Assert.That(result, Is.True, error);
            Assert.That(keyframes.Length, Is.EqualTo(2));
            Assert.That(keyframes[1].Position.x, Is.EqualTo(1f));
            Assert.That(keyframes[1].Rotation.y, Is.EqualTo(20f));
        }
    }
}
