using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;

namespace DancingTrainer
{
    public class BeatManager
    {
        // beats per minute
        public int BPM;

        // length of the track in seconds
        public int length;

        // milliseconds per beat
        public float MSPB;

        // total number of beats
        readonly int totalBeats;

        public int beatCounter = 0;

        SalsaWindow SW;
        MainWindow MW;
        public System.Timers.Timer timer { get; private set; }
        public System.Timers.Timer toTheBeatTimer { get; private set; }

        // total milliseconds past
        public double millisecondsPast = 0;

        public Stopwatch stopWatch = new Stopwatch();
        public double timerStopwatchOffset;
        public double totalDuration;

        public BeatManager(MainWindow mw, int bpm, int length)
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
                SW.ShowSteps(beatCounter%8);               
            };
            //Console.WriteLine("Beat: " + beatCounter % 8);
            DispatcherOperation test = MW.label_BeatCounter.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, woui, new string[] { content });
            SW.label_BeatCounter.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, woui, new string[] { content });
        }

        public void CountBeat()
        {
            beatCounter = 1;
            WriteBeatCounterLabel((beatCounter % 8).ToString());
            timer = new System.Timers.Timer();
            // human reaction is 250ms on average
            // give the user time to react
            Console.WriteLine(MSPB);
            timer.Interval = MSPB;
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;

            Stopwatch s = new Stopwatch();
            s.Start();
            timer.Start();
            //toTheBeatTimer.Start();
            s.Stop();
            timerStopwatchOffset = s.Elapsed.TotalMilliseconds;
            stopWatch.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            WriteBeatCounterLabel((beatCounter % 8 + 1).ToString());
            beatCounter++;
            millisecondsPast += MSPB;
            if (beatCounter >= totalBeats)
            {
                Stop();
            }
        }

        public void Pause()
        {            
            // pause the counter
            timer.Elapsed -= Timer_Elapsed;
            stopWatch.Stop();
        }

        public void Stop()
        {
            timer.Stop();
            totalDuration = stopWatch.Elapsed.TotalMilliseconds;
            stopWatch.Reset();
            // reset the counter and the milli seconds past
            beatCounter = 0;
            millisecondsPast = 0;
            WriteBeatCounterLabel("-");
        }

        private void InitToTheBeatTimer()
        {
            toTheBeatTimer = new System.Timers.Timer();
            // 100 ms interval is where humans perceive real time
            // or can not distinguish a delay
            toTheBeatTimer.Interval = 100;
            toTheBeatTimer.AutoReset = false;
        }
    }
}