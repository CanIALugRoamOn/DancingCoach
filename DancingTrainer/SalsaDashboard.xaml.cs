using Newtonsoft.Json.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DancingTrainer
{
    /// <summary>
    /// Interaction logic for Chart.xaml
    /// </summary>
    public partial class SalsaDashboard : Window, INotifyPropertyChanged
    {
        private PlotModel model;

        public event PropertyChangedEventHandler PropertyChanged;

        public PlotModel Model
        {
            get
            {
                return this.model;
            }

            set
            {
                if (this.model != value)
                {
                    this.model = value;
                    Console.WriteLine(PropertyChanged == null);
                    NotifyPropertyChanged();
                }
            }
        }
        public SalsaDashboard(string json_path)
        {
            InitializeComponent();
            this.DataContext = this;
            JObject jobj = JObject.Parse(File.ReadAllText(json_path));
            foreach (JToken item in jobj["Feedback"])
            {
                AddFeedback(item["Instruction"].ToString(), (double)item["Feedback Start"], (double)item["Display Start"], (double)item["Feedback End"], (double)jobj["TotalDuration"]);
            }
            this.Title += " " + jobj["Name"] + " " + jobj["Date"] + " Duration: " + jobj["TotalDuration"] + " ms";

            PlotModel(jobj);
        }

        private void AddFeedback(string feedbackName, double recognition_start, double displayed_start, double recognition_end, double total_duration)
        {
            switch (feedbackName)
            {
                case "Smile":
                    AddRectangleToTimeline(timeline_Smile, recognition_start, displayed_start, recognition_end, total_duration);                  
                    break;
                case "Look straight":
                    AddRectangleToTimeline(timeline_Focus, recognition_start, displayed_start, recognition_end, total_duration);
                    break;
                case "Follow Beat":
                    AddRectangleToTimeline(timeline_OffTheBeat, recognition_start, displayed_start, recognition_end, total_duration);
                    break;
                case "Move Body":
                    AddRectangleToTimeline(timeline_MoveYourBody, recognition_start, displayed_start, recognition_end, total_duration);
                    break;
            }
        }

        private void AddRectangleToTimeline(Grid timeline, double rec_start, double dis_start, double rec_end, double total_duration)
        {
            int recognition_start = Convert.ToInt32(timeline.Width * (rec_start / total_duration));

            // never displayed
            if (rec_start >=  0 & double.IsNaN(dis_start) & rec_end >= 0)
            {
                int recognition_end = Convert.ToInt32(timeline.Width * (rec_end / total_duration));
                timeline.Children.Add(new Rectangle()
                {
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                    Margin = new Thickness() { Left = recognition_start, Bottom = 0, Right = 0, Top = 0 },
                    Width = recognition_end - recognition_start,
                    Fill = new SolidColorBrush(Colors.LightGray)
                });
            }

            // never ended
            if (rec_start >= 0 & dis_start >= 0 & double.IsNaN(rec_end))
            {
                int displayed_start = Convert.ToInt32(timeline.Width * (dis_start / total_duration));
                timeline.Children.Add(new Rectangle()
                {
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                    Margin = new Thickness() { Left = recognition_start, Bottom = 0, Right = 0, Top = 0 },
                    Width = displayed_start - recognition_start,
                    Fill = new SolidColorBrush(Colors.LightGray)
                });
                timeline.Children.Add(new Rectangle()
                {
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                    Margin = new Thickness() { Left = displayed_start, Bottom = 0, Right = 0, Top = 0 },
                    Width = timeline.Width - displayed_start,
                    Fill = new SolidColorBrush(Colors.Gray)
                });

            }

            // every value is not NaN
            if (rec_start >= 0 & dis_start >= 0 & rec_end >= 0)
            {
                int recognition_end = Convert.ToInt32(timeline.Width * (rec_end / total_duration));
                int displayed_start = Convert.ToInt32(timeline.Width * (dis_start / total_duration));
                timeline.Children.Add(new Rectangle()
                {
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                    Margin = new Thickness() { Left = recognition_start, Bottom = 0, Right = 0, Top = 0 },
                    Width = displayed_start - recognition_start,
                    Fill = new SolidColorBrush(Colors.LightGray)
                });
                timeline.Children.Add(new Rectangle()
                {
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                    Margin = new Thickness() { Left = displayed_start, Bottom = 0, Right = 0, Top = 0 },
                    Width = recognition_end - displayed_start,
                    Fill = new SolidColorBrush(Colors.Gray)
                });
            }

            // if never displayed and never ended recognized
            if (rec_start >= 0 & double.IsNaN(dis_start) & double.IsNaN(rec_end))
            {
                timeline.Children.Add(new Rectangle()
                {
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                    Margin = new Thickness() { Left = recognition_start, Bottom = 0, Right = 0, Top = 0 },
                    Width = timeline.Width - recognition_start,
                    Fill = new SolidColorBrush(Colors.LightGray)
                });
            }
        }


        private void PlotModel(JObject jobj)
        {
            var tmp = new PlotModel
            {
                Title = "Salsa Steps: " + jobj["Song"].ToString(),
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                PlotMargins = new OxyThickness(50, 0, 0, 40)
            };

            var ls = new LineSeries() { Title=jobj["Name"].ToString()};
            foreach (JToken point in jobj["PlotSalsaSteps"])
            {
                ls.Points.Add(new DataPoint((double)point["ms"], (double)point["beat"]));
            }
            tmp.Series.Add(ls);

            tmp.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Milliseconds" });
            
            var ls2 = new LineSeries() { Title = jobj["BPM"].ToString() + " BPM" };
            int MSPB = (int)(60f / (double)jobj["BPM"] * 1000f);
            for (int i = 0; i < (double)jobj["TotalDuration"] + MSPB; i+= MSPB)
            {
                int beat = (i / MSPB % 8) + 1;
                Console.WriteLine(i / MSPB % 8);
                ls2.Points.Add(new DataPoint(i, beat));
                ls2.Points.Add(new DataPoint(i+MSPB, beat));

            }
            tmp.Series.Add(ls2);
            //tmp.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = jobj["BPM"].ToString() });
            this.Model = tmp;
            // plot_SalsaSteps.Model = model;
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }






}
