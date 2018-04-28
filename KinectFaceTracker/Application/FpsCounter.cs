using System;

namespace App.KinectTracked
{
    class FpsCounter
    {
        public double Fps => _fps;

        private DateTime _lastTime;
        private int _framesRendered;
        private double _fps;
        private TimeSpan _delta;

        public void NewFrame()
        {
            _framesRendered++;

            if ((_delta = DateTime.Now - _lastTime).TotalMilliseconds > 800)
            {
                _fps = 1000f * _framesRendered / _delta.TotalMilliseconds;
                _framesRendered = 0;
                _lastTime = DateTime.Now;
            }
        }

    }

    
}
