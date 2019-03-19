using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms;
using Deedle;
using System.Windows.Media;

namespace DancingTrainer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // root path to the beat annotated music library (BAML)
        private string rootPath = "";

        // DataFrame for the songs in the BAML
        private Frame<int, string> dfBaml;

        // List of strings with all genres available
        private List<string> genres = new List<string>();

        private DateTime sessionStart;

        public SalsaBeatManager beatMan;
        private KinectManager kinMan;
        private SalsaWindow salWin;

        //[System.ComponentModel.Browsable(false)]
        //public IntPtr Handle { get; }

        //[System.ComponentModel.Browsable(false)]
        //public bool InvokeRequired { get; }
        
        public MainWindow()
        {
            InitializeComponent();
            kw_KinWin.isRecording = false;      
        }

        private void Button_Browse_Click(object sender, RoutedEventArgs e)
        {           
            // Open window to browse to BAML directory
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = "C:\\Users\\roman\\Documents\\Masterthesis\\BeatAnnotatedMusicLibrary";
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                rootPath = fbd.SelectedPath + @"\";
                textbox_bamlDirectory.Text = rootPath;

                // read the dataframe
                
                dfBaml = Deedle.Frame.ReadCsv(rootPath + "baml.csv", separators: "\t");

                // relative pathing is weird when using the .exe
                dfBaml = Deedle.Frame.ReadCsv(@"baml\baml.csv", separators: "\t");
                // set the genres
                SetGenres();
                combobox_MusicList.IsEnabled = true;
                button_Load.IsEnabled = true;
            }           
        }

        private void SetGenres()
        {
            // select all genres
            var temp = dfBaml.Rows.Select(df => df.Value.Get("Genre"));
            // get distinct genres
            IEnumerable<object> genreValues = temp.Values.ToList().Distinct();
            // join, split and distinct because of ',' separator in the genre column
            List<string> genres = String.Join(",", genreValues).Split(',').Distinct().ToList<string>();
            foreach (string g in genres)
            {
                combobox_GenreList.Items.Add(g);
            }
            combobox_GenreList.SelectedIndex = 0;
            combobox_GenreList.IsEnabled = true;
        }

        private void Combobox_GenreList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // now that the genre is set, set the songs
            LoadMusicList();
        }

        private void LoadMusicList()
        {
            // empty the combobox because of previousely setted items
            combobox_MusicList.Items.Clear();

            // Select the songs with the genre list
            string selectedGenre = combobox_GenreList.SelectedItem.ToString();
            Series<int, ObjectSeries<string>> temp = dfBaml.Rows.Where(df => df.Value.Get("Genre").ToString().Split(',').Contains(selectedGenre));

            ComboBoxItem item;
            foreach (int key in temp.Keys)
            {
                item = new ComboBoxItem();
                item.Content = temp.Get(key).Get("Song").ToString();
                
                int beatsPerMinute = Int32.Parse(temp.Get(key).Get("BPM").ToString());
                if (beatsPerMinute < 90)
                {
                    item.Foreground = Brushes.Green;
                }
                if (beatsPerMinute >= 90 & beatsPerMinute < 110)
                {
                    item.Foreground = Brushes.Yellow;
                }
                if (beatsPerMinute >= 110)
                {
                    item.Foreground = Brushes.Red;
                }
                combobox_MusicList.Items.Add(item);
                //combobox_MusicList.Items.Add(temp.Get(key).Get("Song"));
            }
            
            combobox_MusicList.SelectedIndex = 0;
        }


        private void Button_Load_Click(object sender, RoutedEventArgs e)
        {
            SetMediaElementAudioSource("music");
            // pass bpm and seconds to the BeatManager
            // get the songs bpm and length in secodns
            var bpm = dfBaml.Rows.Where(df => df.Value.Get("Song").ToString().Contains(combobox_MusicList.SelectedItem.ToString()))
                    .Select(df => df.Value.Get("BPM"));
            var length = dfBaml.Rows.Where(df => df.Value.Get("Song").ToString().Contains(combobox_MusicList.SelectedItem.ToString()))
                .Select(df => df.Value.Get("Length"));

            int seconds = Int32.Parse(length.FirstValue().ToString());
            // this is passed such that the class can write back to the UI
            beatMan = new SalsaBeatManager(this, (int)bpm.FirstValue(), seconds);

            // Load (Salsa) Dancing Trainer component
            salWin = new SalsaWindow(this, kw_KinWin, beatMan);
            salWin.Closing += SalWin_Closing;
            beatMan.setSalsaWindow(salWin);
            
            kinMan = new KinectManager(kw_KinWin, beatMan);
            img_Play.IsEnabled = true;

            button_Load.IsEnabled = false;
            salWin.Show();
        }

        private void SalWin_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                this.Stop();
            }
            catch (Exception)
            {
                Console.WriteLine("Probably no music is playing");
            }           
        }

        public void SetMediaElementAudioSource(string mode)
        {
            string songSelected = combobox_MusicList.SelectedItem.ToString();
            string path = "";
            Series<int, object> temp;
            if (mode == "music")
            {
                temp = dfBaml.Rows.Where(df => df.Value.Get("Song").ToString().Contains(songSelected))
                .Select(df => df.Value.Get("Music_file"));
                path = rootPath + temp.FirstValue();
            }
            if (mode=="beat")
            {
                temp = dfBaml.Rows.Where(df => df.Value.Get("Song").ToString().Contains(songSelected))
                          .Select(df => df.Value.Get("BPM_file"));
                path = rootPath + temp.FirstValue();
            }
            if (mode=="music with beat")
            {
                temp = dfBaml.Rows.Where(df => df.Value.Get("Song").ToString().Contains(songSelected))
                          .Select(df => df.Value.Get("Music_BPM_file"));
                path = rootPath + temp.FirstValue();
            }
            // init the media elements source
            Console.WriteLine(path);
            medelem_Audioplayer.Source = new Uri(path);
            Console.WriteLine("btn_Start: Music has been loaded.");
        }

        private void Medelem_Audioplayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            slider_Time.Maximum = medelem_Audioplayer.NaturalDuration.TimeSpan.TotalMilliseconds;
        }

        private void Medelem_Audioplayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            medelem_Audioplayer.Stop();
            try
            {
                this.Stop();
            }
            catch (Exception)
            {
                Console.WriteLine("media ended and stopping:" + e.RoutedEvent.ToString());
            }
        }

        private void Play()
        {
            // for sample data collection of standing and stepping
            InitializePropertyValues();
            medelem_Audioplayer.Play();
            // disable load button
            button_Load.IsEnabled = false;
            // unfortunately audio plays only on second click.
            // so start the beat counting only if there is an audio
            if (medelem_Audioplayer.HasAudio)
            {
                sessionStart = DateTime.Now;
                salWin.SetSessionStart(sessionStart);
                Console.WriteLine("Med Elem has audio");
                kw_KinWin.isRecording = true;
                img_Play.IsEnabled = false;
                img_Pause.IsEnabled = true;
                img_Stop.IsEnabled = true;

                beatMan.CountBeat();
                // start Kinect

                //KM.Play();
                salWin.Play();
            }
        }
        private void Pause()
        {
            medelem_Audioplayer.Pause();
            kw_KinWin.isRecording = false;
            beatMan.Pause();
            salWin.Pause();
            //SW.Stop();
            //SW.SessionEnd = DateTime.Now;
            // save recording?
            img_Play.IsEnabled = true;
            img_Pause.IsEnabled = false;
            img_Stop.IsEnabled = true;
        }

        private void Stop()
        {
            medelem_Audioplayer.Stop();
            kw_KinWin.isRecording = false;
            beatMan.Stop();
            //KM.Stop();
            salWin.Stop();
            img_Play.IsEnabled = true;
            img_Pause.IsEnabled = true;
            img_Stop.IsEnabled = false;
            // enable load button
            //button_Load.IsEnabled = true;
        }

        private void Img_Play_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Play();
            //// for sample data collection of standing and stepping
            //InitializePropertyValues();
            //medelem_Audioplayer.Play();
            //// disable load button
            //button_Load.IsEnabled = false;
            //// unfortunately audio plays only on second click.
            //// so start the beat counting only if there is an audio
            //if (medelem_Audioplayer.HasAudio)
            //{
            //    sessionStart = DateTime.Now;
            //    SW.SetSessionStart(sessionStart);
            //    Console.WriteLine("Med Elem has audio");
            //    kw_KinectWindow.isRecording = true;
            //    img_Play.IsEnabled = false;
            //    img_Pause.IsEnabled = true;
            //    img_Stop.IsEnabled = true;
                
            //    BM.CountBeat();
            //    // start Kinect
                
            //    //KM.Play();
            //    SW.Play();
            //}
        }

        delegate void WriteOnUI(string[] s);

        private void Img_Pause_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Pause();
            //medelem_Audioplayer.Pause();
            //kw_KinectWindow.isRecording = false;
            //BM.Pause();
            //SW.Pause();
            ////SW.Stop();
            ////SW.SessionEnd = DateTime.Now;
            //// save recording?
            //img_Play.IsEnabled = true;
            //img_Pause.IsEnabled = false;
            //img_Stop.IsEnabled = true;           
        }

        private void Img_Stop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Stop();
            //medelem_Audioplayer.Stop();
            //kw_KinectWindow.isRecording = false;
            //BM.Stop();
            ////KM.Stop();
            //SW.Stop();
            //img_Play.IsEnabled = true;
            //img_Pause.IsEnabled = true;
            //img_Stop.IsEnabled = false;
            //// enable load button
            //button_Load.IsEnabled = true;
        }

        void InitializePropertyValues()
        {
            // Set the media's starting Volume and SpeedRatio to the current value of the
            // their respective slider controls.
            medelem_Audioplayer.Volume = (double)slider_Volume.Value;
            medelem_Audioplayer.SpeedRatio = (double)slider_Speed.Value;
        }

        private void Slider_Volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            medelem_Audioplayer.Volume = (double)slider_Volume.Value;
        }

        private void Slider_Speed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            medelem_Audioplayer.SpeedRatio = (double)slider_Speed.Value;
        }

        private void Slider_Time_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int SliderValue = (int)slider_Time.Value;

            // Overloaded constructor takes the arguments days, hours, minutes, seconds, miniseconds.
            // Create a TimeSpan with miliseconds equal to the slider value.
            TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
            medelem_Audioplayer.Position = ts;
        }

        
    }
}
