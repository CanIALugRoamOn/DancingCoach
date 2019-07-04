using System;
using System.Diagnostics;
using System.Timers;

namespace DancingTrainer
{
    public class SalsaBeatManager : IBeatManager
    {
        /// <summary>
        /// Beats per minute.
        /// </summary>
        public int BPM;

        /// <summary>
        /// Length of the song in seconds.
        /// </summary>
        public int length;

        /// <summary>
        /// Milliseconds per beat.
        /// </summary>
        public float MSPB { get; set; }

        /// <summary>
        /// Total number of beats.
        /// </summary>
        readonly int totalBeats;

        /// <summary>
        /// Beat counter.
        /// </summary>
        public int BeatCounter { get; set; } = 0;

        /// <summary>
        /// Reference to the SalsaWindow.
        /// </summary>
        SalsaWindow salWin;

        /// <summary>
        /// Reference to the MainWindow.
        /// </summary>
        MainWindow mainWin;

        /// <summary>
        /// Timer for the beat.
        /// </summary>
        public Timer BeatTimer { get; private set; }

        /// <summary>
        /// Total milliseconds past
        /// </summary>
        public double millisecondsPast = 0;

        /// <summary>
        /// Stopwatch to measure time.
        /// </summary>
        public Stopwatch StopWatch { get; set; } = new Stopwatch();

        /// <summary>
        /// Offset between timer and stopwatch
        /// </summary>
        public double timerStopwatchOffset;

        /// <summary>
        /// Total duration in ms
        /// </summary>
        public double totalDuration;

        /// <summary>
        /// Bool if the application is running.
        /// </summary>
        bool isRunning = false;

        /// <summary>
        /// Delegate to write the beat as string on the UI
        /// </summary>
        /// <param name="s">String[]</param>
        delegate void WriteOnUi(string[] s);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mw">MainWindow</param>
        /// <param name="bpm">Int for BeatsPerMinute</param>
        /// <param name="length">Int for the length of the song.</param>
        public SalsaBeatManager(MainWindow mw, int bpm, int length)
        {
            BPM = bpm;
            this.length = length;
            MSPB = 60f / BPM * 1000;
            totalBeats = (int)(length * (BPM / 60f));
            mainWin = mw;

            // init beat timer
            BeatTimer = new Timer
            {
                Interval = MSPB,
                AutoReset = true
            };
            BeatTimer.Elapsed += Timer_Elapsed;
            
        }

        /// <summary>
        /// Setter for the salsa window
        /// </summary>
        /// <param name="sw">SalsaWindow</param>
        public void SetSalsaWindow(SalsaWindow sw)
        {
            salWin = sw;
        }

        /// <summary>
        /// Write to the beat counter label on the UI.
        /// </summary>
        /// <param name="content">string</param>
        private void WriteBeatCounterLabel(string content)
        {
            WriteOnUi woui = delegate (string[] contentArray)
            {
                mainWin.label_BeatCounter.Content = contentArray[0];
                salWin.label_BeatCounter.Content = contentArray[0];
                salWin.ShowSteps((BeatCounter % 8) + 1);
            };
            salWin.label_BeatCounter.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, woui, new string[] { content });
        }

        /// <summary>
        /// Play.
        /// </summary>
        public void Play()
        {
            if (isRunning)
            {
                BeatTimer.Elapsed += Timer_Elapsed;
                this.StopWatch.Start();
            }
            else
            {
                //BeatCounter = 1;
                if (salWin.mode == "normal")
                {
                    BeatCounter = 0;
                    WriteBeatCounterLabel((BeatCounter % 8 + 1).ToString());
                }

                try
                {
                    Stopwatch s = new Stopwatch();
                    s.Start();
                    BeatTimer.Start();
                    s.Stop();
                    StopWatch.Start();
                    timerStopwatchOffset = s.Elapsed.TotalMilliseconds;
                    isRunning = true;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

        /// <summary>
        /// Event to count the beat.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">ElapsedEventArgs</param>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            BeatCounter++;
            if (salWin.mode == "normal")
            {
                WriteBeatCounterLabel((BeatCounter%8+1).ToString());
            }           
            millisecondsPast += MSPB;
            if (BeatCounter >= totalBeats)
            {
                Stop();
            }
        }

        /// <summary>
        /// Pause.
        /// </summary>
        public void Pause()
        {
            // pause the counter
            BeatTimer.Elapsed -= Timer_Elapsed;
            StopWatch.Stop();
        }

        /// <summary>
        /// Stop.
        /// </summary>
        public void Stop()
        {
            BeatTimer.Stop();
            totalDuration = StopWatch.Elapsed.TotalMilliseconds;
            StopWatch.Reset();
            // reset the counter and the milli seconds past
            BeatCounter = 7;
            millisecondsPast = 0;
            WriteBeatCounterLabel("-");
            salWin.ShowSteps(0);
            isRunning = false;
        }

        /// <summary>
        /// Disposes objects.
        /// </summary>
        public void Dispose()
        {
            BeatTimer.Dispose();
        }
    }
}