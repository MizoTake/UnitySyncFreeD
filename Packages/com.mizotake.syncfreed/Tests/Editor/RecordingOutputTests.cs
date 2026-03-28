using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Outputs;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class RecordingOutputTests
    {
        [Test]
        public void Send_StoresFramesAndBuildsCsvLine()
        {
            var output = new RecordingOutput();
            var state = new CameraSyncState
            {
                SourceId = "test",
                CameraId = 3,
                Corrected = new PoseState { PanDeg = 1d, TiltDeg = 2d, RollDeg = 3d, Xmm = 10d, Ymm = 20d, Zmm = 30d },
                Lens = new LensState { FocalLengthMm = 50d },
                CorrectedLens = new LensState { FocalLengthMm = 85d }
            };

            output.Send(state);

            Assert.That(output.Frames.Count, Is.EqualTo(1));
            Assert.That(output.LastCsvLine, Is.EqualTo("test,3,1.000000,2.000000,3.000000,10.000,20.000,30.000,85.000"));
        }
    }
}
