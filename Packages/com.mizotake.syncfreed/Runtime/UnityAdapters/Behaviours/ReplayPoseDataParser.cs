namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    public static class ReplayPoseDataParser
    {
        public static ReplayPoseKeyframe[] Parse(string text, ReplayDataFormat format)
        {
            return ReplayPoseKeyframeParser.Parse(text, format);
        }
    }
}
