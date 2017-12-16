using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinect_Test
{
    class FpsCounter
    {
        public int Fps => _fps;

        private DateTime _lastFrameTime;
        private int _framesRendered;
        private int _fps;

        public void NewFrame()
        {
            _framesRendered++;

            if ((DateTime.Now - _lastFrameTime).TotalMilliseconds > 700)
            {
                _fps = _framesRendered;
                _framesRendered = 0;
                _lastFrameTime = DateTime.Now;
            }

        }

    }

    
}
