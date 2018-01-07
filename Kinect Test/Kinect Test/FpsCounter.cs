using System;

namespace Kinect_Test
{
    class FpsCounter
    {
        public float Fps => _fps;

        private DateTime _lastTime;
        private int _framesRendered;
        private float _fps;
        private TimeSpan _delta;

        public void NewFrame()
        {
            _framesRendered++;

            if ((_delta = DateTime.Now - _lastTime).TotalMilliseconds > 800)
            {
                _fps = 1000f * _framesRendered / _delta.Milliseconds;
                _framesRendered = 0;
                _lastTime = DateTime.Now;
            }
        }

    }

    
}
