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
        /// <summary>
        /// Milliseconds per beat is the intervall for a Timer.
        /// </summary>
        float MSPB { get; set; }

        /// <summary>
        /// Stopwatch to measure time.
        /// </summary>
        Stopwatch StopWatch { get; set; }

        /// <summary>
        /// Starts the beat.
        /// </summary>
        void Play();

        /// <summary>
        /// Pauses the beat.
        /// </summary>
        void Pause();

        /// <summary>
        /// Stops the beat.
        /// </summary>
        void Stop();

    }
}
