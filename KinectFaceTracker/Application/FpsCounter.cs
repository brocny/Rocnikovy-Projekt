using System;
using System.Collections.Generic;

namespace App
{
    internal class FpsCounter
    {
        public double CurrentFps { get; private set; }
        public double MaxFps { get; private set; } = double.NegativeInfinity;
        public double MinFps { get; private set; } = double.PositiveInfinity;

        public IReadOnlyDictionary<DateTime, double> FpsMeasurements => _fpsMeasurements;
        private readonly Dictionary<DateTime, double> _fpsMeasurements = new Dictionary<DateTime, double>();

        public double AverageFps { get; private set; }

        /// <param name="recomputerInterval">Minimum interval in miliseconds after which fps will be re-evaluated</param>
        public FpsCounter(int recomputerInterval = 800)
        {
            _recomputeInterval = recomputerInterval;
        }

        private readonly int _recomputeInterval;
        private DateTime _lastRecomputeTime;
        private DateTime _firstFrameTime;

        private int _framesAtLastRecompute;
        public int TotalFrames { get; private set; }

        private TimeSpan _delta;

        public void NewFrame()
        {
            TotalFrames++;
            if (TotalFrames == 1)
            {
                _firstFrameTime = _lastRecomputeTime = DateTime.Now;
                return;
            }

            var timeNow = DateTime.Now;
            if ((_delta = timeNow - _lastRecomputeTime).TotalMilliseconds > _recomputeInterval)
            {
                var framesSinceLastRecompute = TotalFrames - _framesAtLastRecompute;
                CurrentFps = 1000d * framesSinceLastRecompute / _delta.TotalMilliseconds;

                if (CurrentFps > MaxFps) MaxFps = CurrentFps;
                if (CurrentFps < MinFps) MinFps = CurrentFps;

                _fpsMeasurements[timeNow] = CurrentFps;

                AverageFps = 1000d * TotalFrames / (timeNow - _firstFrameTime).TotalMilliseconds;

                _framesAtLastRecompute = TotalFrames;
                _lastRecomputeTime = timeNow;
            }
        }

    }

    
}
