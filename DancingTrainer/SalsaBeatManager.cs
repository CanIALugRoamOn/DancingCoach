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
        public System.Timers.Timer Timer { get; private set; }
        public System.Timers.Timer ToTheBeatTimer { get; private set; }

        // total milliseconds past
        public double millisecondsPast = 0;

        public Stopwatch StopWatch { get; set; } = new Stopwatch();
        public double timerStopwatchOffset;
        public double totalDuration;

        public SalsaBeatManager(MainWindow mw, int bpm, int length)
        {
            BPM = bpm;
            this.length = length;
            MSPB = 60f / BPM * 1000;
            totalBeats = (int)(length * (BPM / 60f));
            //Console.WriteLine(totalBeats);
            MW = mw;
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
                SW.ShowSteps(BeatCounter%8);               
            };
            //Console.WriteLine("Beat: " + beatCounter % 8);
            //DispatcherOperation test = MW.label_BeatCounter.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, woui, new string[] { content });
            SW.label_BeatCounter.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, woui, new string[] { content });
        }

        public void CountBeat()
        {
            BeatCounter = 1;
            WriteBeatCounterLabel((BeatCounter % 8).ToString());
            Timer = new System.Timers.Timer();
            // human reaction is 250ms on average
            // give the user time to react
            Console.WriteLine(MSPB);
            Timer.Interval = MSPB;
            Timer.Elapsed += Timer_Elapsed;
            Timer.AutoReset = true;

            Stopwatch s = new Stopwatch();
            s.Start();
            Timer.Start();
            //toTheBeatTimer.Start();
            s.Stop();
            timerStopwatchOffset = s.Elapsed.TotalMilliseconds;
            StopWatch.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            WriteBeatCounterLabel((BeatCounter % 8 + 1).ToString());
            BeatCounter++;
            millisecondsPast += MSPB;
            if (BeatCounter >= totalBeats)
            {
                Stop();
            }
        }

        public void Pause()
        {            
            // pause the counter
            Timer.Elapsed -= Timer_Elapsed;
            StopWatch.Stop();
        }

        public void Stop()
        {
            Timer.Stop();
            totalDuration = StopWatch.Elapsed.TotalMilliseconds;
            StopWatch.Reset();
            // reset the counter and the milli seconds past
            BeatCounter = 0;
            millisecondsPast = 0;
            WriteBeatCounterLabel("-");
        }

        private void InitToTheBeatTimer()
        {
            ToTheBeatTimer = new System.Timers.Timer();
            // 100 ms interval is where humans perceive real time
            // or can not distinguish a delay
            ToTheBeatTimer.Interval = 100;
            ToTheBeatTimer.AutoReset = false;
        }
    }
}