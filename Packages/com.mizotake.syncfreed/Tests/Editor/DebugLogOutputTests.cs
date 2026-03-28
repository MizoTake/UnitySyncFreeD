using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Outputs;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class DebugLogOutputTests
    {
        [Test]
        public void Send_FormatsStateAndInvokesSink()
        {
            string message = null;
            var output = new DebugLogOutput(value => message = value);
            var state = new CameraSyncState
            {
                SourceId = "debug-source",
                CameraId = 9,
                Corrected = new PoseState { PanDeg = 10d, TiltDeg = 20d, RollDeg = 30d, Xmm = 100d, Ymm = 200d, Zmm = 300d }
            };

            output.Send(state);

            Assert.That(message, Does.Contain("CameraId=9"));
            Assert.That(message, Does.Contain("SourceId=debug-source"));
            Assert.That(output.LastMessage, Is.EqualTo(message));
        }
    }
}
