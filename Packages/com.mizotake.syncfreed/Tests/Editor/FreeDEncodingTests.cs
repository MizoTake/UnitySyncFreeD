using MizoTake.SyncFreeD.Core.Outputs;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class FreeDEncodingTests
    {
        [Test]
        public void EncodeAngle24_UsesSigned24Scale()
        {
            Assert.That(FreeDEncoding.EncodeAngle24(1.0d), Is.EqualTo(32768));
            Assert.That(FreeDEncoding.EncodeAngle24(-1.0d), Is.EqualTo(-32768));
        }

        [Test]
        public void EncodePosition24_UsesSigned24Scale()
        {
            Assert.That(FreeDEncoding.EncodePosition24(10.0d), Is.EqualTo(640));
            Assert.That(FreeDEncoding.EncodePosition24(-10.0d), Is.EqualTo(-640));
        }

        [Test]
        public void EncodeZoom24_UsesFocalLengthMillimeters()
        {
            Assert.That(FreeDEncoding.EncodeZoom24(-1.0d), Is.EqualTo(0));
            Assert.That(FreeDEncoding.EncodeZoom24(35.0d), Is.EqualTo(35000));
        }

        [Test]
        public void EncodeFocus24_UsesInverseDistanceQuantization()
        {
            Assert.That(FreeDEncoding.EncodeFocus24(-1.0d), Is.EqualTo(0));
            Assert.That(FreeDEncoding.EncodeFocus24(1.0d), Is.EqualTo(1 << 18));
        }

        [Test]
        public void Encoders_SaturateFiniteValuesBeforeIntegerConversion()
        {
            Assert.That(FreeDEncoding.EncodeAngle24(double.MaxValue), Is.EqualTo(0x7FFFFF));
            Assert.That(FreeDEncoding.EncodeAngle24(-double.MaxValue), Is.EqualTo(-0x800000));
            Assert.That(FreeDEncoding.EncodePosition24(double.MaxValue), Is.EqualTo(0x7FFFFF));
            Assert.That(FreeDEncoding.EncodePosition24(-double.MaxValue), Is.EqualTo(-0x800000));
            Assert.That(FreeDEncoding.EncodeZoom24(double.MaxValue), Is.EqualTo(0xFFFFFF));
            Assert.That(FreeDEncoding.EncodeFocus24(double.Epsilon), Is.EqualTo(0x7FFFFF));
            Assert.That(FreeDEncoding.EncodeUserArea(double.MaxValue, 0x000A), Is.EqualTo(0xAFFF));
        }
    }
}
