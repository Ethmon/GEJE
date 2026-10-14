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
        Stopwatch clock = Stopwatch.StartNew();   // must be running, or the first delta is 0
        public TimeSpan DeltaTime()
        {
            TimeSpan delta = clock.Elapsed;

            clock.Restart();
            return delta;
        }

    }
}