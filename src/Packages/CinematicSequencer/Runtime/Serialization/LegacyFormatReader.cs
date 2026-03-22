using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using CinematicSequencer.Animation;

namespace CinematicSequencer.Serialization
{
    /// <summary>
    /// v1形式のJSONを読み込み、v2のモデルに変換する。
    /// </summary>
    public class LegacyFormatReader
    {
        private static readonly Dictionary<string, TrackType> DataTypeToTrackType = new()
        {
            { "CameraPose", TrackType.CameraPose },
            { "LightPose", TrackType.LightPose },
            { "CameraProperties", TrackType.CameraProperties },
            { "LightProperties", TrackType.LightProperties },
            { "Effect", TrackType.Effect },
            { "Audio", TrackType.Audio },
        };

        /// <summary>
        /// v1形式のSequence JSONをv2のSequenceに変換する。
        /// </summary>
        public Sequence ReadV1Sequence(byte[] data)
        {
            var json = System.Text.Encoding.UTF8.GetString(data);
            var jsonObject = JObject.Parse(json);

            var id = Guid.Parse(jsonObject.Value<string>("Id"));
            var name = jsonObject.Value<string>("Name");

            var tracks = new List<Track>();
            var tracksArray = jsonObject["Tracks"] as JArray;
            if (tracksArray != null)
            {
                foreach (var trackToken in tracksArray)
                {
                    var trackName = trackToken.Value<string>("Name");
                    var dataTypeStr = trackToken.Value<string>("Type");
                    var targetId = trackToken.Value<int>("TargetId");

                    if (!DataTypeToTrackType.TryGetValue(dataTypeStr, out var trackType))
                    {
                        continue;
                    }

                    var clips = new List<Clip>();
                    var clipsArray = trackToken["Clips"] as JArray;
                    if (clipsArray != null)
                    {
                        foreach (var clipToken in clipsArray)
                        {
                            var clipDataId = Guid.Parse(clipToken.Value<string>("ClipDataId"));
                            var startTime = clipToken.Value<float>("StartTime");
                            var duration = clipToken.Value<float>("Duration");
                            var timeScale = clipToken.Value<float>("TimeScale");

                            var placement = new TimeRange(startTime, duration * (1.0f / timeScale));
                            var sourceRange = new TimeRange(0f, duration);
                            var playbackRate = timeScale;

                            clips.Add(new Clip(
                                GuidExtensions.CreateVersion7(),
                                clipDataId,
                                placement,
                                playbackRate,
                                sourceRange
                            ));
                        }
                    }

                    tracks.Add(new Track(
                        GuidExtensions.CreateVersion7(),
                        trackName,
                        trackType,
                        targetId,
                        tracks.Count,
                        clips
                    ));
                }
            }

            return new Sequence(id, name, tracks);
        }
    }
}
