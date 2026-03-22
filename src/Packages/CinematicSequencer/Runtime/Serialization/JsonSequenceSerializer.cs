#if USE_NEWTONSOFT_JSON
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using CinematicSequencer.Animation;

namespace CinematicSequencer.Serialization
{
    /// <summary>
    /// v2 JSON形式でシーケンスデータのシリアライズ/デシリアライズを行う。
    /// </summary>
    public class JsonSequenceSerializer : ISequenceSerializer
    {
        private readonly JsonSerializerSettings _serializerSettings;

        public JsonSequenceSerializer()
        {
            _serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
            };
            _serializerSettings.Converters.Add(new StringEnumConverter());
        }

        public byte[] SerializeSequence(Sequence sequence)
        {
            var json = JsonConvert.SerializeObject(sequence, _serializerSettings);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public Sequence DeserializeSequence(byte[] data)
        {
            var json = System.Text.Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<Sequence>(json, _serializerSettings);
        }

        public byte[] SerializeClipAsset(IClipAsset clipAsset)
        {
            var wrapper = new ClipAssetWrapper(clipAsset);
            var json = JsonConvert.SerializeObject(wrapper, _serializerSettings);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public IClipAsset DeserializeClipAsset(byte[] data)
        {
            var json = System.Text.Encoding.UTF8.GetString(data);
            var jsonObject = JObject.Parse(json);

            var assetType = jsonObject.Value<string>("AssetType");
            return assetType switch
            {
                nameof(AnimationClipAsset) => DeserializeAnimationClipAsset(jsonObject),
                nameof(MotionClipAsset) => DeserializeMotionClipAsset(jsonObject),
                _ => throw new NotSupportedException($"Unknown clip asset type: {assetType}")
            };
        }

        public bool TryGetFormatVersion(byte[] data, out string version)
        {
            version = string.Empty;
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(data);
                var jsonObject = JObject.Parse(json);
                if (jsonObject.TryGetValue("FormatVersion", out var token))
                {
                    version = token.ToString();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private IClipAsset DeserializeAnimationClipAsset(JObject jsonObject)
        {
            var id = Guid.Parse(jsonObject.Value<string>("Id"));
            var name = jsonObject.Value<string>("Name");
            var type = jsonObject.Value<string>("Type");
            var trackType = Enum.Parse<TrackType>(type);

            var descriptorsArray = jsonObject["Properties"] as JArray;
            var descriptors = new List<AnimationPropertyDescriptor>();
            if (descriptorsArray != null)
            {
                foreach (var item in descriptorsArray)
                {
                    descriptors.Add(new AnimationPropertyDescriptor(
                        item.Value<string>("Name"),
                        item.Value<float>("DefaultValue"),
                        item.Value<string>("DisplayName"),
                        item.Value<string>("Group"),
                        item.Value<float?>("MinValue"),
                        item.Value<float?>("MaxValue")
                    ));
                }
            }

            var curvesObject = jsonObject["Curves"] as JObject;
            var curves = new Dictionary<string, AnimationCurve>();
            if (curvesObject != null)
            {
                foreach (var prop in curvesObject.Properties())
                {
                    var keysArray = prop.Value as JArray;
                    var keyframes = new List<Keyframe>();
                    if (keysArray != null)
                    {
                        foreach (var k in keysArray)
                        {
                            keyframes.Add(new Keyframe(
                                k.Value<float>("Time"),
                                k.Value<float>("Value"),
                                k.Value<float>("InTangent"),
                                k.Value<float>("OutTangent"),
                                k.Value<string>("TangentMode") is string mode
                                    ? Enum.Parse<TangentMode>(mode)
                                    : TangentMode.Free
                            ));
                        }
                    }
                    curves[prop.Name] = new AnimationCurve(keyframes);
                }
            }

            return new AnimationClipAsset(id, name, trackType, descriptors.ToArray(), curves);
        }

        private IClipAsset DeserializeMotionClipAsset(JObject jsonObject)
        {
            return new MotionClipAsset(
                Guid.Parse(jsonObject.Value<string>("Id")),
                jsonObject.Value<string>("Name"),
                jsonObject.Value<string>("ExternalSourceId"),
                jsonObject.Value<int>("ClipIndex"),
                jsonObject.Value<float>("CachedDuration")
            );
        }

        /// <summary>
        /// ClipAssetのシリアライズ用ラッパー。型情報を含める。
        /// </summary>
        private class ClipAssetWrapper
        {
            public string AssetType { get; set; }
            public object Data { get; set; }

            public ClipAssetWrapper(IClipAsset clipAsset)
            {
                AssetType = clipAsset.GetType().Name;
                Data = clipAsset;
            }
        }
    }
}
#endif
