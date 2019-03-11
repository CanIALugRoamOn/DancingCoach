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
        BitmapImage Source { get; set; }
        string Instruction { get; set; }
        List<(double, double, double)> Schedule { get; set; }
        bool IsActive { get; set; }
        bool IsDisplayed { get; set; }
        bool IsBlocked { get; set; }
        void RaiseHand(double feedback_start);
        void LowerHand(double feedback_end);
        void StartTalking(double display_start);
    }
}
