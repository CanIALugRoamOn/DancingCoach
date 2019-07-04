using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows.Media.Imaging;

namespace DancingTrainer
{
    public class Feedback : IFeedback
    {
        /// <summary>
        /// Feedback icon.
        /// </summary>
        public BitmapImage Source { get; set; }

        /// <summary>
        /// Feedback instruction.
        /// </summary>
        public string Instruction { get; set; }

        /// <summary>
        /// Shows if the feedback is active.
        /// </summary>
        public bool IsActive { get; set; } = false;

        /// <summary>
        /// Shows if the feedback is displayed.
        /// </summary>
        public bool IsDisplayed { get; set; } = false;

        /// <summary>
        /// Shows if the feedback is blocked
        /// </summary>
        public bool IsBlocked { get; set; }

        /// <summary>
        /// Timer that blocks and releases feedback.
        /// </summary>
        public Timer blockAndRelease;

        // 
        /// <summary>
        /// List that schedules the feedback.
        /// Entries represent feedback_start, displayed_start, displayed_end, feedback_end in milliseconds.
        // If an entry is undefined the value is -1.
        /// </summary>
        public List<(double, double, double)> Schedule { get; set; } = new List<(double, double, double)>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="s">Source for the icon.</param>
        /// <param name="i">String for the instruction.</param>
        public Feedback(BitmapImage s, string i)
        {
            this.Source = s;
            this.Instruction = i;
            blockAndRelease = new Timer(5000);
            blockAndRelease.Elapsed += BlockAndRelease_Elapsed;
            blockAndRelease.AutoReset = false;
        }

        /// <summary>
        /// Shows the last element of the feedback schedule.
        /// </summary>
        /// <param name="l">List of the feedback schedule.</param>
        /// <returns>Tuple with three entries.</returns>
        private (double, double, double) PopLastListElement(List<(double, double, double)> l)
        {
            try
            {
                (double, double, double) lastEntry = l.Last();
                l.RemoveAt(l.Count - 1);
                return lastEntry;
            }
            catch (Exception)
            {
                return (double.NaN, double.NaN, double.NaN);
            }
            
        }

        /// <summary>
        /// Sets the starting time of the recognition.
        /// </summary>
        /// <param name="feedback_start">Double</param>
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

        /// <summary>
        /// Sets the ending time of the recognition.
        /// </summary>
        /// <param name="feedback_end">Double</param>
        public void LowerHand(double feedback_end)
        {
            // to be sure that nothing happens if its blocked
            if (IsBlocked)
            {
                return;
            }
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
        }

        /// <summary>
        /// Event to release the blocked feedback
        /// </summary>
        /// <param name="sender">Object</param>
        /// <param name="e">ElapsedEventArgs</param>
        private void BlockAndRelease_Elapsed(object sender, ElapsedEventArgs e)
        {
            IsBlocked = false;
        }

        /// <summary>
        /// Starts the starting time of the display.
        /// </summary>
        /// <param name="display_start"></param>
        public void StartTalking(double display_start)
        {
            if (IsBlocked)
            {
                return;
            }
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

        /// <summary>
        /// Clears the schedule.
        /// </summary>
        public void ClearSchedule()
        {
            Schedule.Clear();
        }
    }
}
