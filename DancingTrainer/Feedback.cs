using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows.Media.Imaging;

namespace DancingTrainer
{
    class Feedback
    {
        public BitmapImage source;
        public string instruction;
        public bool isActive = false;
        public bool isDisplayed = false;
        public bool isBlocked;
        public Timer blockAndRelease;

        // one entry represents feedback_start, displayed_start, displayed_end, feedback_end in milliseconds
        // -1 stands for undefined
        public List<(double, double, double)> schedule = new List<(double, double, double)>();

        public Feedback(BitmapImage s, string i)
        {
            this.source = s;
            this.instruction = i;
            blockAndRelease = new Timer(5000);
            blockAndRelease.Elapsed += BlockÁndRelease_Elapsed;
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
            if (isBlocked)
            {
                return;
            }

            if (!isActive)
            {
                schedule.Add((feedback_start, double.NaN, double.NaN));
                this.isActive = true;
                //Console.WriteLine(this.instruction + " is not recognized and was not active before: " + this.is_active);
            }
            else
            {
                isActive = true;
                //Console.WriteLine(this.instruction + " is not recognized and was active before: " + this.is_active);
            }
        }

        public void LowerHand(double feedback_end)
        {
            if (isActive)
            {
                (double, double, double) lastEntry = PopLastListElement(schedule);
                lastEntry.Item3 = feedback_end;
                schedule.Add(lastEntry);
                isDisplayed = false;
                isActive = false;
                isBlocked = true;
                blockAndRelease.Start();
                //Console.WriteLine(this.instruction + " is recognized and was active before: " + this.is_active);
            }
            else
            {
                isDisplayed = false;
                isActive = false;
                //Console.WriteLine(this.instruction + " is recognized and was not active before: " + this.is_active);
            }
        }

        private void BlockÁndRelease_Elapsed(object sender, ElapsedEventArgs e)
        {
            isBlocked = false;
        }

        public void StartTalking(double display_start)
        {
            if (isActive)
            {
                (double, double, double) lastEntry = PopLastListElement(schedule);
                lastEntry.Item2 = display_start;
                schedule.Add(lastEntry);
            }
            else
            {
                Console.WriteLine("Started talking without raising the hand");
            }
        }

        public void ClearSchedule()
        {
            schedule.Clear();
        }
    }
}
