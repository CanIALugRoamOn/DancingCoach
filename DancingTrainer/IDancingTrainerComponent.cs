using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DancingTrainer
{
    interface IDancingTrainerComponent
    {
        /// <summary>
        /// Session start.
        /// </summary>
        DateTime SessionStart { get; set; }

        /// <summary>
        /// Array that holds all the feedback.
        /// </summary>
        Feedback[] FeedbackArray { get; set; }
        
        /// <summary>
        /// Play.
        /// </summary>
        void Play();

        /// <summary>
        /// Pause.
        /// </summary>
        void Pause();

        /// <summary>
        /// Stop.
        /// </summary>
        void Stop();

    }
}
