using System.Collections.Generic;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Sync
{
    public sealed class DelayCompensator
    {
        private readonly Queue<PoseState> history = new Queue<PoseState>();

        public int HistoryCount => history.Count;

        public void Push(in PoseState pose)
        {
            history.Enqueue(pose);
            while (history.Count > 120)
            {
                history.Dequeue();
            }
        }

        public PoseState SampleDelayed(long currentTimestampTicks, int delayMs)
        {
            if (history.Count == 0)
            {
                return default;
            }

            var targetTicks = currentTimestampTicks - (delayMs * System.TimeSpan.TicksPerMillisecond);
            var selected = default(PoseState);
            foreach (var pose in history)
            {
                selected = pose;
                if (pose.TimestampTicks >= targetTicks)
                {
                    break;
                }
            }

            return selected;
        }

        public void Clear()
        {
            history.Clear();
        }
    }
}
