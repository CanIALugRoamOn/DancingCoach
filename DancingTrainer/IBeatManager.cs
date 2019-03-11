using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DancingTrainer
{
    interface IBeatManager
    {
        float MSPB { get; set; }
        int BeatCounter { get; set; }
        Stopwatch StopWatch { get; set; }
        void CountBeat();
        void Pause();
        void Stop();

    }
}
