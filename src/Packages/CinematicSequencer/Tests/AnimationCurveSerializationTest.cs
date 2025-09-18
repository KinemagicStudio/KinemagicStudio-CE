#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
#else
using System.Text.Json.Serialization; // TODO
#endif
using NUnit.Framework;
// using UnityEngine;

namespace CinematicSequencer.Animation.Tests
{
    public class AnimationCurveSerializationTest
    {
        [Test]
        public void KeyframeSerialization()
        {
            var keyframe = new Keyframe(1.0f, 0.5f, 0.1f, 0.2f, TangentMode.Free);

            var settings = new JsonSerializerSettings()
            {
                Converters = { new StringEnumConverter() }
            };
            var json = JsonConvert.SerializeObject(keyframe, settings);

            var expectedJson = "{\"Time\":1.0,\"Value\":0.5,\"InTangent\":0.1,\"OutTangent\":0.2,\"TangentMode\":\"Free\"}";
            Assert.AreEqual(expectedJson, json);
        }

        [Test]
        public void KeyframeDeserialization()
        {
            var json = "{\"Time\":1.001,\"Value\":0.5,\"InTangent\":0.1,\"OutTangent\":0.2,\"TangentMode\":\"Free\"}";
            var keyframe = JsonConvert.DeserializeObject<Keyframe>(json);

            Assert.AreEqual(1001, keyframe.TimeMs);
            Assert.AreEqual(1.001f, keyframe.Time);
            Assert.AreEqual(0.5f, keyframe.Value);
            Assert.AreEqual(0.1f, keyframe.InTangent);
            Assert.AreEqual(0.2f, keyframe.OutTangent);
            Assert.AreEqual(TangentMode.Free, keyframe.TangentMode);
        }

        [Test]
        public void AnimationCurveSerialization()
        {
            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0.0f, 0.0f));
            curve.AddKey(new Keyframe(1.0f, 1.0f));

            var json = JsonConvert.SerializeObject(curve);

            var deserializedCurve = JsonConvert.DeserializeObject<AnimationCurve>(json);

        }
    }
}
