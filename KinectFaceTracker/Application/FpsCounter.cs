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
        private int _framesSinceLastRecompute;
        private double _fps;
        private TimeSpan _delta;

        public void NewFrame()
        {
            _framesSinceLastRecompute++;

            if ((_delta = DateTime.Now - _lastRecomputeTime).TotalMilliseconds > _recomputeInterval)
            {
                _fps = 1000f * _framesSinceLastRecompute / _delta.TotalMilliseconds;
                _framesSinceLastRecompute = 0;
                _lastRecomputeTime = DateTime.Now;
            }
        }

    }

    
}
