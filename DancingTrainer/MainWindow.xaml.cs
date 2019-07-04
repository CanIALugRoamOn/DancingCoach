using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms;
using Deedle;
using System.Windows.Media;
using System.Runtime.InteropServices;
using Microsoft.Speech.Recognition;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;

namespace DancingTrainer
{
    public partial class MainWindow : Window
    {

        /// <summary>
        /// Root path to the beat annotated music library (BAML).
        /// </summary>
        private string rootPath = "";

        /// <summary>
        /// DataFrame for the songs in the BAML.
        /// </summary> 
        private Frame<int, string> dfBaml;

        /// <summary>
        /// List of strings with all avaiblable genres.
        /// </summary>
        private List<string> genres = new List<string>();

        /// <summary>
        /// Starting time of the dance session.
        /// </summary>
        private DateTime sessionStart;

        /// <summary>
        /// Reference to the beat manager.
        /// </summary>
        private SalsaBeatManager beatMan;

        /// <summary>
        /// Reference to the salsa window.
        /// </summary>
        private SalsaWindow salWin;

        /// <summary>
        /// Stream for 32b-16b conversion.
        /// </summary>
        private KinectAudioStream convertStream = null;

        /// <summary>
        /// Speech recognition engine using audio data from Kinect.
        /// </summary>
        private SpeechRecognitionEngine speechEngine = null;

        /// <summary>
        /// Delegate to be able to write on the UI.
        /// </summary>
        /// <param name="s">String Array</param>
        delegate void WriteOnUI(string[] s);

        /// <summary>
        /// Initializes the main window..
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            kw_KinWin.isRecording = false;
        }

        /// <summary>
        /// Click to browse to the BAML.
        /// </summary>
        /// <param name="sender">Object.</param>
        /// <param name="e">RoutedEventArgs</param>
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

        /// <summary>
        /// Sets the genre when the BAML was loaded.
        /// </summary>
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

        /// <summary>
        /// Load the music when the genre was changed in the respective ComboBox.
        /// </summary>
        /// <param name="sender">Object.</param>
        /// <param name="e">SelectionChangedEventArgs</param>
        private void Combobox_GenreList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // now that the genre is set, set the songs
            LoadMusicList();
        }

        /// <summary>
        /// Loads the music into the respective ComboBox using the previously selected genre.
        /// </summary>
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
                item = new ComboBoxItem
                {
                    Content = temp.Get(key).Get("Song").ToString()
                };

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
            }
            
            combobox_MusicList.SelectedIndex = 0;
        }

        /// <summary>
        /// Load a dancing trainer component, derived by the selected genre. Currently only Salsa.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void Button_Load_Click(object sender, RoutedEventArgs e)
        {
            SetMediaElementAudioSource("music");
            // pass bpm and seconds to the BeatManager
            // get the songs bpm and length in secodns
            ComboBoxItem item = (ComboBoxItem) combobox_MusicList.SelectedItem;
            var bpm = dfBaml.Rows.Where(df => df.Value.Get("Song").ToString().Contains(item.Content.ToString()))
                    .Select(df => df.Value.Get("BPM"));
            var length = dfBaml.Rows.Where(df => df.Value.Get("Song").ToString().Contains(item.Content.ToString()))
                .Select(df => df.Value.Get("Length"));

            int seconds = Int32.Parse(length.FirstValue().ToString());

            // TOTEST
            // main memory stays the same if a dancing trainer component is closed
            // therefore it rises on every new load
            // maybe it is better to use setter and getter such that the new object does not instanciate every time
            // maybe use object is null and instanciate only on first load

            // this is passed such that the class can write back to the UI
            beatMan = new SalsaBeatManager(this, (int)bpm.FirstValue(), seconds);

            // Load (Salsa) Dancing Trainer component
            //if (salWin is null)
            //{
            //    // instanciate new object
            //}
            //else
            //{
            //    // reset object and show it
            //    // salWin.Show();
            //}
            salWin = new SalsaWindow(this, kw_KinWin, beatMan);
            salWin.Closing += SalWin_Closing;
            beatMan.SetSalsaWindow(salWin);
            
            img_Play.IsEnabled = true;

            button_Load.IsEnabled = false;
            salWin.Show();

            // init speech control
            // sensitive, care for demonstration
            // InitializeSpeechControl();
        }

        /// <summary>
        /// Event to stop the music when the salsa window is closes. 
        /// </summary>
        /// <param name="sender">objects</param>
        /// <param name="e">CancelEventArgs</param>
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

        /// <summary>
        /// Sets the audio source of the media element.
        /// </summary>
        /// <param name="mode">String to choose the audio source with music, beat or music with beat.</param>
        public void SetMediaElementAudioSource(string mode)
        {
            ComboBoxItem musicItem = (ComboBoxItem) combobox_MusicList.SelectedItem;
            string songSelected = musicItem.Content.ToString();
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

        /// <summary>
        /// Event to set the maxiumum of the slider to the duration of the audio source.
        /// </summary>
        /// <param name="sender">Object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void Medelem_Audioplayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            slider_Time.Maximum = medelem_Audioplayer.NaturalDuration.TimeSpan.TotalMilliseconds;
        }

        /// <summary>
        /// Event to stop when the media element has ended.
        /// </summary>
        /// <param name="sender">Object</param>
        /// <param name="e">RoutedEventArgs</param>
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

        /// <summary>
        /// Plays the music and if possible start the accoring dancing trainer component.
        /// </summary>
        private void Play()
        {
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

                beatMan.Play();
                salWin.Play();
            }
        }

        /// <summary>
        /// Pauses the music and if possible start the accoring dancing trainer component.
        /// </summary>
        private void Pause()
        {
            medelem_Audioplayer.Pause();
            kw_KinWin.isRecording = false;
            beatMan.Pause();
            salWin.Pause();
            img_Play.IsEnabled = true;
            img_Pause.IsEnabled = false;
            img_Stop.IsEnabled = true;
        }

        /// <summary>
        /// Stops the music and if possible start the accoring dancing trainer component.
        /// </summary>
        private void Stop()
        {
            medelem_Audioplayer.Stop();
            kw_KinWin.isRecording = false;
            beatMan.Stop();
            salWin.Stop();
            img_Play.IsEnabled = true;
            img_Pause.IsEnabled = true;
            img_Stop.IsEnabled = false;
        }

        /// <summary>
        /// Event to start play when the play image is clicked.
        /// </summary>
        /// <param name="sender">Object</param>
        /// <param name="e">MouseButtonEventArgs</param>
        private void Img_Play_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Play();
        }

        /// <summary>
        /// Event to pause when the pause image is clicked.
        /// </summary>
        /// <param name="sender">Object</param>
        /// <param name="e">MouseButtonEventArgs</param>
        private void Img_Pause_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Pause();
        }

        /// <summary>
        /// Event to stop when the stop image is clicked.
        /// </summary>
        /// <param name="sender">Object</param>
        /// <param name="e">MouseButtonEventArgs</param>
        private void Img_Stop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Stop();
        }

        /// <summary>
        /// Initializes values for the MediaElement.
        /// </summary>
        void InitializePropertyValues()
        {
            // Set the media's starting Volume and SpeedRatio to the current value of the
            // their respective slider controls.
            medelem_Audioplayer.Volume = (double)slider_Volume.Value;
            medelem_Audioplayer.SpeedRatio = (double)slider_Speed.Value;
        }

        /// <summary>
        /// Event to update the volume of the MediaElement.
        /// </summary>
        /// <param name="sender">Object</param>
        /// <param name="args">RoutedPropertyChangedEventArgs</param>
        private void Slider_Volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            medelem_Audioplayer.Volume = (double)slider_Volume.Value;
        }

        /// <summary>
        /// Event to update the speed ratio of the MediaElement.
        /// </summary>
        /// <param name="sender">Object</param>
        /// <param name="args">RoutedPropertyChangedEventArgs</param>
        private void Slider_Speed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            medelem_Audioplayer.SpeedRatio = (double)slider_Speed.Value;
        }

        /// <summary>
        /// Event to update the slider value of the MediaElement.
        /// </summary>
        /// <param name="sender">Object</param>
        /// <param name="args">RoutedPropertyChangedEventArgs</param>
        private void Slider_Time_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int SliderValue = (int)slider_Time.Value;

            // Overloaded constructor takes the arguments days, hours, minutes, seconds, miniseconds.
            // Create a TimeSpan with miliseconds equal to the slider value.
            TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
            medelem_Audioplayer.Position = ts;
        }

        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
        /// process audio from Kinect device.
        /// </summary>
        /// <returns>RecognizerInfo</returns>
        private static RecognizerInfo TryGetKinectRecognizer()
        {
            IEnumerable<RecognizerInfo> recognizers;

            // This is required to catch the case when an expected recognizer is not installed.
            // By default - the x86 Speech Runtime is always expected. 
            try
            {
                recognizers = SpeechRecognitionEngine.InstalledRecognizers();
            }
            catch (COMException)
            {
                return null;
            }

            foreach (RecognizerInfo recognizer in recognizers)
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }


        /// <summary>
        /// Initializes the speech control for speech recogntion.
        /// </summary>
        private void InitializeSpeechControl()
        {
            
            if (this.kw_KinWin.kinectSensor != null)
            {
                // open the sensor
                if (!this.kw_KinWin.kinectSensor.IsOpen)
                {
                    this.kw_KinWin.kinectSensor.Open();
                }

                // grab the audio stream
                IReadOnlyList<AudioBeam> audioBeamList = this.kw_KinWin.kinectSensor.AudioSource.AudioBeams;
                System.IO.Stream audioStream = audioBeamList[0].OpenInputStream();

                // create the convert stream
                this.convertStream = new KinectAudioStream(audioStream);
            }
            else
            {
                return;
            }

            RecognizerInfo ri = TryGetKinectRecognizer();

            if (null != ri)
            {

                this.speechEngine = new SpeechRecognitionEngine(ri.Id);
               
                //Use this code to create grammar programmatically rather than from
                //a grammar file.
                
                var speechControlGrammar = new Choices();
                speechControlGrammar.Add(new SemanticResultValue("play", "PLAY"));
                speechControlGrammar.Add(new SemanticResultValue("pause", "PAUSE"));
                speechControlGrammar.Add(new SemanticResultValue("stop", "STOP"));
                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(speechControlGrammar);
                var g = new Grammar(gb);
                this.speechEngine.LoadGrammar(g);

                // Create a grammar from grammar definition XML file.
                //using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(Properties.Resources.SpeechGrammar)))
                //{
                //    var g = new Grammar(memoryStream);
                //    this.speechEngine.LoadGrammar(g);
                //}

                this.speechEngine.SpeechRecognized += this.SpeechRecognized;
                //this.speechEngine.SpeechRecognitionRejected += this.SpeechRejected;

                // let the convertStream know speech is going active
                this.convertStream.SpeechActive = true;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                this.speechEngine.SetInputToAudioStream(
                    this.convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                this.speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
        }

        /// <summary>
        /// Disable the speech control
        /// </summary>
        public void DisableSpeechControl()
        {
            if (null != this.convertStream)
            {
                this.convertStream.SpeechActive = false;
            }

            if (null != this.speechEngine)
            {
                this.speechEngine.SpeechRecognized -= this.SpeechRecognized;
                this.speechEngine.RecognizeAsyncStop();
            }
        }


        /// <summary>
        /// Event to recognize speech.
        /// </summary>
        /// <param name="sender">Object</param>
        /// <param name="e">SpeechRecognizedEventArgs</param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.4;

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                switch (e.Result.Semantics.Value.ToString())
                {
                    case "PLAY":
                        Play();
                        break;

                    case "PAUSE":
                        Pause();
                        break;

                    case "STOP":
                        Stop();
                        break;
                }
            }
        }
    }
}
