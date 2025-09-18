using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace CinematicSequencer.Serialization
{
    /// <summary>
    /// JSON形式でタイムラインデータのシリアライズ/デシリアライズを行うクラス
    /// </summary>
    public class JsonTimelineSerializer : ITimelineSerializer // SequenceDataJsonSerializer : ISequenceDataSerializer
    {
        private readonly JsonSerializerSettings _serializerSettings;

        public JsonTimelineSerializer()
        {
            _serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            _serializerSettings.Converters.Add(new StringEnumConverter());
        }

        public byte[] Serialize<T>(T value)
        {
            var json = JsonConvert.SerializeObject(value, _serializerSettings);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] data)
        {
            var json = System.Text.Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<T>(json, _serializerSettings);
        }

        public bool TryGetFormatVersion(byte[] data, out string value)
        {
            value = string.Empty;

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(data);
                var jsonObject = JObject.Parse(json);

                if (!jsonObject.TryGetValue(Constants.FormatVersionKey, out var versionToken))
                {
                    return false;
                }

                value = versionToken.ToString();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool TryGetSequenceDataInfo(byte[] data, out CinematicSequenceDataInfo info)
        {
            info = new CinematicSequenceDataInfo(Guid.Empty, "", -1);

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(data);
                var jsonObject = JObject.Parse(json);

                if (jsonObject.TryGetValue(CinematicSequenceDataInfo.IdPropertyKey, out var idToken))
                {
                    info.Id = Guid.Parse(idToken.ToString());
                }

                if (jsonObject.TryGetValue(CinematicSequenceDataInfo.NamePropertyKey, out var nameToken))
                {
                    info.Name = nameToken.ToString();
                }

                if (jsonObject.TryGetValue(CinematicSequenceDataInfo.TrackCountPropertyKey, out var trackCountToken))
                {
                    info.TrackCount = int.Parse(trackCountToken.ToString());
                }

                return info.Id != Guid.Empty && !string.IsNullOrEmpty(info.Name) && info.TrackCount >= 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}