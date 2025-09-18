using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace CinematicSequencer.Serialization
{
    public class JsonClipDataSerializer : IClipDataSerializer // ClipDataJsonSerializer : ISequenceDataSerializer
    {
        private readonly JsonSerializerSettings _serializerSettings;

        public JsonClipDataSerializer()
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

        public bool TryGetClipDataInfo(byte[] data, out ClipDataInfo info)
        {
            info = new ClipDataInfo(Guid.Empty, "", DataType.Unknown);

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(data);
                var jsonObject = JObject.Parse(json);

                if (jsonObject.TryGetValue(ClipDataInfo.IdPropertyKey, out var idToken))
                {
                    info.Id = Guid.Parse(idToken.ToString());
                }

                if (jsonObject.TryGetValue(ClipDataInfo.NamePropertyKey, out var nameToken))
                {
                    info.Name = nameToken.ToString();
                }

                if (jsonObject.TryGetValue(ClipDataInfo.TypePropertyKey, out var clipTypeToken))
                {
                    info.Type = Enum.Parse<DataType>(clipTypeToken.ToString());
                }

                // foreach (var property in jsonObject.Properties())
                // {
                //     if (property.Name.Equals(ClipDataInfo.IdPropertyKey, StringComparison.OrdinalIgnoreCase))
                //     {
                //         info.Id = Guid.Parse(property.Value.ToString());
                //     }
                //     else if (property.Name.Equals(ClipDataInfo.NamePropertyKey, StringComparison.OrdinalIgnoreCase))
                //     {
                //         info.Name = property.Value.ToString();
                //     }
                //     else if (property.Name.Equals(ClipDataInfo.TypePropertyKey, StringComparison.OrdinalIgnoreCase))
                //     {
                //         info.Type = Enum.Parse<ClipType>(property.Value.ToString());
                //     }
                // }

                return info.Id != Guid.Empty && !string.IsNullOrEmpty(info.Name) && info.Type != DataType.Unknown;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}