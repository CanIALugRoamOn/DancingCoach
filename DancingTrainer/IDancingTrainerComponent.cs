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
        DateTime SessionStart { get; set; }

        Feedback[] FeedbackArray { get; set; }
        //List<WriteableBitmap> Video { get; set; }
        Task Record { get; set; }
        void Play();

        void Pause();

        void Stop();

    }
}
