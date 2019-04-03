using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;

namespace DancingTrainer
{
    public class SalsaBeatManager : IBeatManager
    {
        // beats per minute
        public int BPM;

        // length of the track in seconds
        public int length;

        // milliseconds per beat
        public float MSPB { get; set; }

        // total number of beats
        readonly int totalBeats;

        public int BeatCounter { get; set; } = 0;

        SalsaWindow SW;
        MainWindow MW;
        public Timer BeatTimer { get; private set; }
        public Timer ToTheBeatTimer { get; private set; }

        // total milliseconds past
        public double millisecondsPast = 0;

        public Stopwatch StopWatch { get; set; } = new Stopwatch();
        public double timerStopwatchOffset;
        public double totalDuration;
        bool isRunning = false;

        public SalsaBeatManager(MainWindow mw, int bpm, int length)
        {
            BPM = bpm;
            this.length = length;
            MSPB = 60f / BPM * 1000;
            totalBeats = (int)(length * (BPM / 60f));
            //Console.WriteLine(totalBeats);
            MW = mw;

            // init beat timer
            BeatTimer = new Timer
            {
                Interval = MSPB,
                AutoReset = true
            };
            BeatTimer.Elapsed += Timer_Elapsed;
            
        }

        public void setSalsaWindow(SalsaWindow sw)
        {
            SW = sw;
        }

        delegate void WriteOnUi(string[] s);

        private void WriteBeatCounterLabel(string content)
        {
            WriteOnUi woui = delegate (string[] contentArray)
            {
                MW.label_BeatCounter.Content = contentArray[0];
                SW.label_BeatCounter.Content = contentArray[0];
                SW.ShowSteps((BeatCounter % 8) + 1);
            };
            //Console.WriteLine("Beat: " + beatCounter % 8);
            //DispatcherOperation test = MW.label_BeatCounter.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, woui, new string[] { content });
            SW.label_BeatCounter.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, woui, new string[] { content });
        }

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
                if (SW.mode == "normal")
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

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            BeatCounter++;
            if (SW.mode == "normal")
            {
                WriteBeatCounterLabel((BeatCounter%8+1).ToString());
            }           
            millisecondsPast += MSPB;
            if (BeatCounter >= totalBeats)
            {
                Stop();
            }
        }

        public void Pause()
        {
            // pause the counter
            BeatTimer.Elapsed -= Timer_Elapsed;
            StopWatch.Stop();
        }

        public void Stop()
        {
            BeatTimer.Stop();
            totalDuration = StopWatch.Elapsed.TotalMilliseconds;
            StopWatch.Reset();
            // reset the counter and the milli seconds past
            BeatCounter = 7;
            millisecondsPast = 0;
            WriteBeatCounterLabel("-");
            SW.ShowSteps(0);
            isRunning = false;
        }

        private void InitToTheBeatTimer()
        {
            ToTheBeatTimer = new System.Timers.Timer();
            // 100 ms interval is where humans perceive real time
            // or can not distinguish a delay
            ToTheBeatTimer.Interval = 100;
            ToTheBeatTimer.AutoReset = false;
        }

        public void Dispose()
        {
            BeatTimer.Dispose();
            ToTheBeatTimer.Dispose();            
        }
    }
}