namespace MizoTake.SyncFreeD.Core.Models
{
    [System.Serializable]
    public sealed class CurveDefinition
    {
        public CurveKeyframe[] Keys = new[] { new CurveKeyframe(0d, 0d), new CurveKeyframe(1d, 1d) };

        public double Evaluate(double time)
        {
            if (Keys == null || Keys.Length == 0)
            {
                return 0d;
            }

            if (Keys.Length == 1 || time <= Keys[0].Time)
            {
                return Keys[0].Value;
            }

            for (var index = 1; index < Keys.Length; index++)
            {
                var previous = Keys[index - 1];
                var current = Keys[index];
                if (time > current.Time)
                {
                    continue;
                }

                var range = current.Time - previous.Time;
                if (range <= 0d)
                {
                    return current.Value;
                }

                var t = (time - previous.Time) / range;
                return previous.Value + ((current.Value - previous.Value) * t);
            }

            return Keys[Keys.Length - 1].Value;
        }
    }
}
