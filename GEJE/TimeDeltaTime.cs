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
        public TimeSpan DeltaTime()
        {
            TimeSpan delta = clock.Elapsed;
            
            clock.Restart();
            return delta;
        }
        
    }
}
