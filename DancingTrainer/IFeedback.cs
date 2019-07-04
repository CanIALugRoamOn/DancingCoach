using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DancingTrainer
{
    interface IFeedback
    {
        /// <summary>
        /// Image for the icon.
        /// </summary>
        BitmapImage Source { get; set; }

        /// <summary>
        /// String for the instruction.
        /// </summary>
        string Instruction { get; set; }

        /// <summary>
        /// List to schedule the recignition and display of feedback.
        /// </summary>
        List<(double, double, double)> Schedule { get; set; }

        /// <summary>
        /// bool if feedback is active.
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// bool if feedback is displayed.
        /// </summary>
        bool IsDisplayed { get; set; }

        /// <summary>
        /// bool if feedback is blocked.
        /// </summary>
        bool IsBlocked { get; set; }

        /// <summary>
        /// Method that sets the start of the recognition.
        /// </summary>
        /// <param name="feedback_start">Double</param>
        void RaiseHand(double feedback_start);

        /// <summary>
        /// Method that sets the end of the recognition.
        /// </summary>
        /// <param name="feedback_end">Double</param>
        void LowerHand(double feedback_end);

        /// <summary>
        /// Method that sets the start of the display.
        /// </summary>
        /// <param name="display_start">Double</param>
        void StartTalking(double display_start);
    }
}
