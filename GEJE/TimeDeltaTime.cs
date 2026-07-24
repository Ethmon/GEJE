using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEJE
{
    public class TimeDeltaTime
    {
        Stopwatch clock = new Stopwatch();
        public long DeltaTime()
        {
            long delta = clock.ElapsedMilliseconds;
            clock.Restart();
            return delta;
        }
        
    }
}
