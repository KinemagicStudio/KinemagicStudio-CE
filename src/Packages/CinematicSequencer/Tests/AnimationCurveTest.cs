using NUnit.Framework;

namespace CinematicSequencer.Animation.Tests
{
    public class AnimationCurveTest
    {
        [Test]
        public void AddKey_ValidKey_AddsKey()
        {
            var key0 = new Keyframe(time: 1f, value: 1f);
            var key1 = new Keyframe(time: 2f, value: 2f);
            var key2 = new Keyframe(time: 3f, value: 3f);

            var curve = new AnimationCurve();
            curve.AddKey(key1);
            curve.AddKey(key2);
            curve.AddKey(key0);

            Assert.AreEqual(3, curve.Keys.Count);
            Assert.AreEqual(key0, curve.Keys[0]);
            Assert.AreEqual(key1, curve.Keys[1]);
            Assert.AreEqual(key2, curve.Keys[2]);
        }

        [Test]
        public void AddKey_ReturnsIndex()
        {
            var time = 1f;      // 1000[ms]
            var time1 = 1.001f; // 1001[ms]

            var curve = new AnimationCurve();
            var index1 = curve.AddKey(new Keyframe(time: time, value: 0f));
            var index2 = curve.AddKey(new Keyframe(time: time1, value: 1f));

            Assert.AreEqual(2, curve.Keys.Count);
            Assert.AreEqual(0, index1);
            Assert.AreEqual(1, index2);
        }

        [Test]
        public void AddKey_Test03()
        {
            var time = 1f;         // 1000[ms]
            var time1 = 1.0001f;   // 1000.1[ms]
            var time2 = 1.00001f;  // 1000.01[ms]
            var time3 = 1.000001f; // 1000.001[ms]

            var curve = new AnimationCurve();
            var index1 = curve.AddKey(new Keyframe(time: time, value: 0f));
            var index2 = curve.AddKey(new Keyframe(time: time1, value: 1f));
            var index3 = curve.AddKey(new Keyframe(time: time2, value: 2f));
            var index4 = curve.AddKey(new Keyframe(time: time3, value: 3f));

            Assert.AreEqual(1, curve.Keys.Count);
            Assert.AreEqual(0, index1);
            Assert.AreEqual(-1, index2);
            Assert.AreEqual(-1, index3);
            Assert.AreEqual(-1, index4);
        }

        [Test]
        public void FindKeyIndex_ValidTime_ReturnsCorrectIndex()
        {
            var time1 = 0.001f;
            var time2 = 1f;
            var time3 = 1.001f;

            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(time: 0f,        value: 0f));
            curve.AddKey(new Keyframe(time: time2,     value: 2f));
            curve.AddKey(new Keyframe(time: time1,     value: 1f));
            curve.AddKey(new Keyframe(time: time2 * 2, value: 4f));
            curve.AddKey(new Keyframe(time: time3,     value: 3f));

            var index1 = curve.FindKeyIndex(time1);
            var index2 = curve.FindKeyIndex(time2);
            var index3 = curve.FindKeyIndex(time3);

            Assert.AreEqual(1, index1);
            Assert.AreEqual(2, index2);
            Assert.AreEqual(3, index3);
        }

        [Test]
        public void FindKeyIndex_Test02()
        {
            var time = 1f;         // 1000[ms]
            var time1 = 1.1f;      // 1100[ms]
            var time2 = 1.01f;     // 1010[ms]
            var time3 = 1.001f;    // 1001[ms]
            var time4 = 1.0001f;   // 1000.1[ms]
            var time5 = 1.00001f;  // 1000.01[ms]
            var time6 = 1.000001f; // 1000.001[ms]

            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(time: time, value: 1f));
            curve.AddKey(new Keyframe(time: time + 1f, value: 2f));
            curve.AddKey(new Keyframe(time: time - 1f, value: 0f));

            var index1 = curve.FindKeyIndex(time1);
            var index2 = curve.FindKeyIndex(time2);
            var index3 = curve.FindKeyIndex(time3);
            var index4 = curve.FindKeyIndex(time4);
            var index5 = curve.FindKeyIndex(time5);
            var index6 = curve.FindKeyIndex(time6);

            Assert.AreEqual(-1, index1);
            Assert.AreEqual(-1, index2);
            Assert.AreEqual(-1, index3);
            Assert.AreEqual(1, index4);
            Assert.AreEqual(1, index5);
            Assert.AreEqual(1, index6);
        }

        [Test]
        public void RemoveKey_ValidKey_RemovesKey()
        {
            var key1 = new Keyframe(time: 1f, value: 1f);
            var key2 = new Keyframe(time: 2f, value: 2f);
            var key3 = new Keyframe(time: 3f, value: 3f);

            var curve = new AnimationCurve();
            curve.AddKey(key1);
            curve.AddKey(key2);
            curve.AddKey(key3);

            curve.RemoveKey(1);

            Assert.AreEqual(2, curve.Keys.Count);
            Assert.AreEqual(key1, curve.Keys[0]);
            Assert.AreEqual(key3, curve.Keys[1]);
        }

        // [Test]
        // public void RemoveKey_InvalidIndex_DoesNotRemoveKey()
        // {
        //     var key1 = new Keyframe(time: 1f, value: 1f);
        //     var key2 = new Keyframe(time: 2f, value: 2f);
        //     var key3 = new Keyframe(time: 3f, value: 3f);

        //     var curve = new AnimationCurve();
        //     curve.AddKey(key1);
        //     curve.AddKey(key2);
        //     curve.AddKey(key3);

        //     curve.RemoveKey(5); // Invalid index

        //     Assert.AreEqual(3, curve.Keys.Count);
        //     Assert.AreEqual(key1, curve.Keys[0]);
        //     Assert.AreEqual(key2, curve.Keys[1]);
        //     Assert.AreEqual(key3, curve.Keys[2]);
        // }

        [Test]
        public void RemoveKeyAtTime_ValidTime_RemovesKey()
        {
            var key1 = new Keyframe(time: 1f, value: 1f);
            var key2 = new Keyframe(time: 2f, value: 2f);
            var key3 = new Keyframe(time: 3f, value: 3f);

            var curve = new AnimationCurve();
            curve.AddKey(key1);
            curve.AddKey(key2);
            curve.AddKey(key3);

            var result = curve.RemoveKeyAtTime(2f);

            Assert.AreEqual(true, result);
            Assert.AreEqual(2, curve.Keys.Count);
            Assert.AreEqual(key1, curve.Keys[0]);
            Assert.AreEqual(key3, curve.Keys[1]);
        }

        [Test]
        public void RemoveKey_FirstKey_DoesNotRemoveKey()
        {
            var key1 = new Keyframe(time: 1f, value: 1f);
            var key2 = new Keyframe(time: 2f, value: 2f);
            var key3 = new Keyframe(time: 3f, value: 3f);

            var curve = new AnimationCurve();
            curve.AddKey(key1);
            curve.AddKey(key2);
            curve.AddKey(key3);

            var result = curve.RemoveKeyAtTime(0f);

            Assert.AreEqual(false, result);
            Assert.AreEqual(3, curve.Keys.Count);
        }

        [Test]
        public void RemoveKey_DoesNotRemoveKey()
        {
            var key1 = new Keyframe(time: 1f, value: 1f);
            var key2 = new Keyframe(time: 2f, value: 2f);
            var key3 = new Keyframe(time: 3f, value: 3f);
            var key4 = new Keyframe(time: 4f, value: 4f);

            var curve = new AnimationCurve();
            curve.AddKey(key1);
            curve.AddKey(key2);
            curve.AddKey(key3);
            curve.AddKey(key4);

            var result1 = curve.RemoveKey(3);
            var result2 = curve.RemoveKey(2);
            var result3 = curve.RemoveKey(1);

            Assert.AreEqual(true, result1);
            Assert.AreEqual(true, result2);
            Assert.AreEqual(false, result3);
            Assert.AreEqual(2, curve.Keys.Count);
        }

        [Test]
        public void RemoveKeyAtTime_ApproximatelyEqualTime_RemovesKey()
        {
            var key0 = new Keyframe(time: 0f, value: 0f);
            var key1 = new Keyframe(time: 1f, value: 1f);
            var key2 = new Keyframe(time: 2f, value: 2f);
            var key3 = new Keyframe(time: 3f, value: 3f);
            var key4 = new Keyframe(time: 4f, value: 4f);

            var curve = new AnimationCurve();
            curve.AddKey(key0);
            curve.AddKey(key1);
            curve.AddKey(key2);
            curve.AddKey(key3);
            curve.AddKey(key4);

            var result1 = curve.RemoveKeyAtTime(1.0001f);   // 1000.1[ms]
            var result2 = curve.RemoveKeyAtTime(2.00001f);  // 2000.01[ms]
            var result3 = curve.RemoveKeyAtTime(3.000001f); // 3000.001[ms]

            Assert.AreEqual(true, result1);
            Assert.AreEqual(true, result2);
            Assert.AreEqual(true, result3);
            Assert.AreEqual(2, curve.Keys.Count);
        }

        [Test]
        public void RemoveKeyAtTime_InvalidTime_DoesNotRemoveKey()
        {
            var key1 = new Keyframe(time: 1f, value: 1f);
            var key2 = new Keyframe(time: 2f, value: 2f);
            var key3 = new Keyframe(time: 3f, value: 3f);

            var curve = new AnimationCurve();
            curve.AddKey(key1);
            curve.AddKey(key2);
            curve.AddKey(key3);

            var result1 = curve.RemoveKeyAtTime(2.001f); // 2001[ms]
            var result2 = curve.RemoveKeyAtTime(4f);

            Assert.AreEqual(false, result1);
            Assert.AreEqual(false, result2);
            Assert.AreEqual(3, curve.Keys.Count);
            Assert.AreEqual(key1, curve.Keys[0]);
            Assert.AreEqual(key2, curve.Keys[1]);
            Assert.AreEqual(key3, curve.Keys[2]);
        }

        [Test]
        public void FindSegment_ValidTime_ReturnsCorrectSegment()
        {
            var timeMs1 = 1;    // 0.001[s]
            var timeMs2 = 500;  // 0.5[s]
            var timeMs3 = 1001; // 1.001[s]

            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(time: 0f, value: 0f));
            curve.AddKey(new Keyframe(time: 0.5f, value: 1f));
            curve.AddKey(new Keyframe(time: 1.0f, value: 2f));
            curve.AddKey(new Keyframe(time: 1.2f, value: 3f));
            curve.AddKey(new Keyframe(time: 2.0f, value: 4f));

            curve.FindSegment(timeMs1, out var low1, out var high1);
            curve.FindSegment(timeMs2, out var low2, out var high2);
            curve.FindSegment(timeMs3, out var low3, out var high3);

            Assert.AreEqual(0, low1);
            Assert.AreEqual(1, high1);
            Assert.AreEqual(1, low2);
            Assert.AreEqual(2, high2);
            Assert.AreEqual(2, low3);
            Assert.AreEqual(3, high3);
        }
    }
}
