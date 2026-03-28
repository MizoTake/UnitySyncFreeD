using MizoTake.SyncFreeD.Core.Models;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class CurveDefinitionTests
    {
        [Test]
        public void Evaluate_LinearlyInterpolatesBetweenKeys()
        {
            var curve = new CurveDefinition
            {
                Keys = new[]
                {
                    new CurveKeyframe(0d, 10d),
                    new CurveKeyframe(2d, 30d)
                }
            };

            var value = curve.Evaluate(1d);

            Assert.That(value, Is.EqualTo(20d).Within(0.0001d));
        }
    }
}
