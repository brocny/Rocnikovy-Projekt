using System;

namespace App
{
    internal class FpsCounter
    {
        public double Fps => _fps;
        
        /// <param name="recomputerInterval">Minimum interval in microseconds after which fps will be re-evaluated</param>
        public FpsCounter(int recomputerInterval = 800)
        {
            _recomputeInterval = recomputerInterval;
        }

        private readonly int _recomputeInterval;
        private DateTime _lastRecomputeTime;
        private int _framesAtLastRecompute;
        public int TotalFrames { get; private set; }
        private double _fps;
        private TimeSpan _delta;

        public void NewFrame()
        {
            TotalFrames++;

            if ((_delta = DateTime.Now - _lastRecomputeTime).TotalMilliseconds > _recomputeInterval)
            {
                var framesSinceLastRecompute = TotalFrames - _framesAtLastRecompute;
                _fps = 1000f * framesSinceLastRecompute / _delta.TotalMilliseconds;
                _framesAtLastRecompute = TotalFrames;
                _lastRecomputeTime = DateTime.Now;
            }
        }

    }

    
}
