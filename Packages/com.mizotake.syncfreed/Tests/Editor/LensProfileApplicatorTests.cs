using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Sync;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class LensProfileApplicatorTests
    {
        [Test]
        public void Apply_BackfillsFocalLengthFromZoomNormalizedWithinLensRange()
        {
            var profile = new LensProfile
            {
                MinFocalLengthMm = 20d,
                MaxFocalLengthMm = 80d,
                ZoomCurve = new CurveDefinition { Keys = new[] { new CurveKeyframe(0d, 0d), new CurveKeyframe(1d, 1d) } }
            };

            var lens = LensProfileApplicator.Apply(new LensState { ZoomNormalized = 0.5d }, profile);

            Assert.That(lens.FocalLengthMm, Is.EqualTo(50d).Within(0.0001d));
        }

        [Test]
        public void Apply_ClampsFocalLengthToLensRange()
        {
            var profile = new LensProfile
            {
                MinFocalLengthMm = 20d,
                MaxFocalLengthMm = 80d
            };

            var lens = LensProfileApplicator.Apply(new LensState { FocalLengthMm = 120d }, profile);

            Assert.That(lens.FocalLengthMm, Is.EqualTo(80d).Within(0.0001d));
        }

        [Test]
        public void Apply_BackfillsFocusDistanceFromFocusCurve()
        {
            var profile = new LensProfile
            {
                FocusCurve = new CurveDefinition { Keys = new[] { new CurveKeyframe(0d, 1d), new CurveKeyframe(1d, 5d) } }
            };

            var lens = LensProfileApplicator.Apply(new LensState { FocusNormalized = 0.25d }, profile);

            Assert.That(lens.FocusDistanceMeters, Is.EqualTo(2d).Within(0.0001d));
        }
    }
}
