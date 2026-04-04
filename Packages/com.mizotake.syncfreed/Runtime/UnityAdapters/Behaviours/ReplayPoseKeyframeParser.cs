using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    public static class ReplayPoseKeyframeParser
    {
        [Serializable]
        private sealed class ReplayPoseKeyframeCollection
        {
            public ReplayPoseKeyframe[] keyframes;
        }

        [Serializable]
        private sealed class ReplayPoseKeyframeCamelCollection
        {
            public ReplayPoseKeyframeCamel[] keyframes;
        }

        [Serializable]
        private sealed class ReplayPoseKeyframeArrayWrapper
        {
            public ReplayPoseKeyframe[] items;
        }

        [Serializable]
        private sealed class ReplayPoseKeyframeCamelArrayWrapper
        {
            public ReplayPoseKeyframeCamel[] items;
        }

        [Serializable]
        private struct ReplayPoseKeyframeCamel
        {
            public float timeSeconds;
            public Vector3 position;
            public Vector3 rotation;
            public float focalLengthMm;
        }

        public static ReplayPoseKeyframe[] Parse(string text, ReplayDataFormat format)
        {
            return TryParse(text, format, out var keyframes, out _) ? keyframes : Array.Empty<ReplayPoseKeyframe>();
        }

        public static bool TryParse(string text, ReplayDataFormat format, out ReplayPoseKeyframe[] keyframes, out string error)
        {
            var effectiveFormat = ResolveFormat(text, format);
            switch (effectiveFormat)
            {
                case ReplayDataFormat.Json:
                    return TryParseJson(text, out keyframes, out error);
                case ReplayDataFormat.Csv:
                    return TryParseCsv(text, out keyframes, out error);
                default:
                    keyframes = Array.Empty<ReplayPoseKeyframe>();
                    error = "Replay data format could not be detected.";
                    return false;
            }
        }

        public static bool TryParseJson(string text, out ReplayPoseKeyframe[] keyframes, out string error)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                keyframes = Array.Empty<ReplayPoseKeyframe>();
                error = "Replay JSON is empty.";
                return false;
            }

            try
            {
                var trimmed = NormalizeJsonKeys(text.Trim());
                ReplayPoseKeyframe[] parsedKeyframes;
                if (trimmed.StartsWith("[", StringComparison.Ordinal))
                {
                    var wrapper = JsonUtility.FromJson<ReplayPoseKeyframeArrayWrapper>("{\"items\":" + trimmed + "}");
                    if (HasMeaningfulValues(wrapper != null ? wrapper.items : null))
                    {
                        parsedKeyframes = wrapper.items;
                    }
                    else
                    {
                        var camelWrapper = JsonUtility.FromJson<ReplayPoseKeyframeCamelArrayWrapper>("{\"items\":" + trimmed + "}");
                        parsedKeyframes = Convert(camelWrapper != null ? camelWrapper.items : null);
                    }
                }
                else
                {
                    var collection = JsonUtility.FromJson<ReplayPoseKeyframeCollection>(trimmed);
                    if (HasMeaningfulValues(collection != null ? collection.keyframes : null))
                    {
                        parsedKeyframes = collection.keyframes;
                    }
                    else
                    {
                        var camelCollection = JsonUtility.FromJson<ReplayPoseKeyframeCamelCollection>(trimmed);
                        parsedKeyframes = Convert(camelCollection != null ? camelCollection.keyframes : null);
                    }
                }

                return FinalizeKeyframes(parsedKeyframes, out keyframes, out error);
            }
            catch (Exception exception)
            {
                keyframes = Array.Empty<ReplayPoseKeyframe>();
                error = "Replay JSON parse failed: " + exception.Message;
                return false;
            }
        }

        public static bool TryParseCsv(string text, out ReplayPoseKeyframe[] keyframes, out string error)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                keyframes = Array.Empty<ReplayPoseKeyframe>();
                error = "Replay CSV is empty.";
                return false;
            }

            var rows = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var result = new List<ReplayPoseKeyframe>();
            var headerSkipped = false;
            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i].Trim();
                if (string.IsNullOrWhiteSpace(row))
                {
                    continue;
                }

                if (row.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!headerSkipped && IsHeaderRow(row))
                {
                    headerSkipped = true;
                    continue;
                }

                var columns = row.Split(',');
                if (columns.Length < 8)
                {
                    keyframes = Array.Empty<ReplayPoseKeyframe>();
                    error = "Replay CSV row must have 8 columns at line " + (i + 1).ToString(CultureInfo.InvariantCulture) + ".";
                    return false;
                }

                if (!TryParseSingle(columns[0], out var timeSeconds) || !TryParseSingle(columns[1], out var positionX) || !TryParseSingle(columns[2], out var positionY) || !TryParseSingle(columns[3], out var positionZ) || !TryParseSingle(columns[4], out var rotationX) || !TryParseSingle(columns[5], out var rotationY) || !TryParseSingle(columns[6], out var rotationZ) || !TryParseSingle(columns[7], out var focalLengthMm))
                {
                    keyframes = Array.Empty<ReplayPoseKeyframe>();
                    error = "Replay CSV row contains invalid numeric values at line " + (i + 1).ToString(CultureInfo.InvariantCulture) + ".";
                    return false;
                }

                result.Add(new ReplayPoseKeyframe
                {
                    TimeSeconds = timeSeconds,
                    Position = new Vector3(positionX, positionY, positionZ),
                    Rotation = new Vector3(rotationX, rotationY, rotationZ),
                    FocalLengthMm = focalLengthMm
                });
            }

            return FinalizeKeyframes(result.ToArray(), out keyframes, out error);
        }

        private static ReplayDataFormat ResolveFormat(string text, ReplayDataFormat format)
        {
            if (format != ReplayDataFormat.Auto)
            {
                return format;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return ReplayDataFormat.Auto;
            }

            var trimmed = text.TrimStart();
            return trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal) ? ReplayDataFormat.Json : ReplayDataFormat.Csv;
        }

        private static bool FinalizeKeyframes(ReplayPoseKeyframe[] source, out ReplayPoseKeyframe[] keyframes, out string error)
        {
            if (source == null || source.Length == 0)
            {
                keyframes = Array.Empty<ReplayPoseKeyframe>();
                error = "Replay data does not contain any keyframes.";
                return false;
            }

            keyframes = (ReplayPoseKeyframe[])source.Clone();
            Array.Sort(keyframes, (left, right) => left.TimeSeconds.CompareTo(right.TimeSeconds));
            error = string.Empty;
            return true;
        }

        private static bool IsHeaderRow(string row)
        {
            return row.IndexOf("TimeSeconds", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TryParseSingle(string text, out float value)
        {
            return float.TryParse(text.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
        }

        private static string NormalizeJsonKeys(string text)
        {
            return text.Replace("\"timeSeconds\"", "\"TimeSeconds\"").Replace("\"position\"", "\"Position\"").Replace("\"rotation\"", "\"Rotation\"").Replace("\"focalLengthMm\"", "\"FocalLengthMm\"");
        }

        private static ReplayPoseKeyframe[] Convert(ReplayPoseKeyframeCamel[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<ReplayPoseKeyframe>();
            }

            var converted = new ReplayPoseKeyframe[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                converted[i] = new ReplayPoseKeyframe
                {
                    TimeSeconds = source[i].timeSeconds,
                    Position = source[i].position,
                    Rotation = source[i].rotation,
                    FocalLengthMm = source[i].focalLengthMm
                };
            }

            return converted;
        }

        private static bool HasMeaningfulValues(ReplayPoseKeyframe[] source)
        {
            if (source == null || source.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < source.Length; i++)
            {
                if (Math.Abs(source[i].TimeSeconds) > 0.0001f || Math.Abs(source[i].Position.x) > 0.0001f || Math.Abs(source[i].Position.y) > 0.0001f || Math.Abs(source[i].Position.z) > 0.0001f || Math.Abs(source[i].Rotation.x) > 0.0001f || Math.Abs(source[i].Rotation.y) > 0.0001f || Math.Abs(source[i].Rotation.z) > 0.0001f || Math.Abs(source[i].FocalLengthMm) > 0.0001f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
