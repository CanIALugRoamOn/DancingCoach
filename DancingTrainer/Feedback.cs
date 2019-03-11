using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows.Media.Imaging;

namespace DancingTrainer
{
    public class Feedback : IFeedback
    {
        public BitmapImage Source { get; set; }
        public string Instruction { get; set; }
        public bool IsActive { get; set; } = false;
        public bool IsDisplayed { get; set; } = false;
        public bool IsBlocked { get; set; }
        public Timer blockAndRelease;

        // one entry represents feedback_start, displayed_start, displayed_end, feedback_end in milliseconds
        // -1 stands for undefined
        public List<(double, double, double)> Schedule { get; set; } = new List<(double, double, double)>();

        public Feedback(BitmapImage s, string i)
        {
            this.Source = s;
            this.Instruction = i;
            blockAndRelease = new Timer(5000);
            blockAndRelease.Elapsed += BlockAndRelease_Elapsed;
            blockAndRelease.AutoReset = false;
        }

        private (double, double, double) PopLastListElement(List<(double, double, double)> l)
        {
            (double, double, double) lastEntry = l.Last();
            l.RemoveAt(l.Count - 1);
            return lastEntry;
        }

        public void RaiseHand(double feedback_start)
        {
            if (IsBlocked)
            {
                return;
            }

            if (!IsActive)
            {
                Schedule.Add((feedback_start, double.NaN, double.NaN));
                this.IsActive = true;
                //Console.WriteLine(this.instruction + " is not recognized and was not active before: " + this.is_active);
            }
            else
            {
                IsActive = true;
                //Console.WriteLine(this.instruction + " is not recognized and was active before: " + this.is_active);
            }
        }

        public void LowerHand(double feedback_end)
        {
            if (IsActive)
            {
                (double, double, double) lastEntry = PopLastListElement(Schedule);
                lastEntry.Item3 = feedback_end;
                Schedule.Add(lastEntry);
                IsDisplayed = false;
                IsActive = false;
                IsBlocked = true;
                blockAndRelease.Start();
                //Console.WriteLine(this.instruction + " is recognized and was active before: " + this.is_active);
            }
            else
            {
                IsDisplayed = false;
                IsActive = false;
                //Console.WriteLine(this.instruction + " is recognized and was not active before: " + this.is_active);
            }
        }

        private void BlockAndRelease_Elapsed(object sender, ElapsedEventArgs e)
        {
            IsBlocked = false;
        }

        public void StartTalking(double display_start)
        {
            if (IsActive)
            {
                (double, double, double) lastEntry = PopLastListElement(Schedule);
                lastEntry.Item2 = display_start;
                Schedule.Add(lastEntry);
            }
            else
            {
                Console.WriteLine("Started talking without raising the hand");
            }
        }

        public void ClearSchedule()
        {
            Schedule.Clear();
        }
    }
}
