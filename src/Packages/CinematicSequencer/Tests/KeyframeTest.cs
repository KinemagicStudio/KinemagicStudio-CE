using NUnit.Framework;

namespace CinematicSequencer.Animation.Tests
{
    public class KeyframeTest
    {
        [Test]
        public void Keyframe_CompareTo_SameTimeDifferentValue_ReturnsZero()
        {
            var key1 = new Keyframe(time: 0.5f, value: 1.0f);
            var key2 = new Keyframe(time: 0.5f, value: 2.0f);

            Assert.True(key1.CompareTo(key2) == 0);
        }

        [Test]
        public void Keyframe_CompareTo_DifferentTimes_ReturnsCorrectOrder()
        {
            var key1 = new Keyframe(time: 0.5f, value: 1.0f);
            var key2 = new Keyframe(time: 1.0f, value: 2.0f);

            Assert.True(key1.CompareTo(key2) < 0);
            Assert.True(key2.CompareTo(key1) > 0);
        }

        [Test]
        public void Keyframe_CompareTo_ApproximatelyEqualTimes_ReturnsZero()
        {
            var key1 = new Keyframe(time: 0.0f, value: 1.0f);
            var key2 = new Keyframe(time: 0.001f, value: 2.0f);
            var key3 = new Keyframe(time: 0.0001f, value: 3.0f);
            
            var key4 = new Keyframe(time: 9999.0f, value: 1.0f);
            var key5 = new Keyframe(time: 9999.001f, value: 2.0f);
            var key6 = new Keyframe(time: 9999.0001f, value: 3.0f);

            Assert.True(key1.CompareTo(key2) < 0);
            Assert.True(key1.CompareTo(key3) == 0);
            Assert.True(key2.CompareTo(key3) > 0);

            Assert.True(key4.CompareTo(key5) < 0);
            Assert.True(key4.CompareTo(key6) == 0);
            Assert.True(key5.CompareTo(key6) > 0);
        }

        [Test]
        public void Keyframe_CompareTo_NaN_ReturnsCorrectOrder()
        {
            var key1 = new Keyframe(time: float.NaN, value: 1.0f);
            var key2 = new Keyframe(time: 0.5f, value: 2.0f);
            Assert.True(key1.CompareTo(key2) < 0);
            Assert.True(key2.CompareTo(key1) > 0);
        }

        [Test]
        public void Keyframe_CompareTo_BothNaN_ReturnsZero()
        {
            var key1 = new Keyframe(time: float.NaN, value: 1.0f);
            var key2 = new Keyframe(time: float.NaN, value: 2.0f);

            Assert.True(key1.CompareTo(key2) == 0);
        }
    }
}
