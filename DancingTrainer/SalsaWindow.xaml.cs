using LightBuzz.Vitruvius;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DancingTrainer
{
    /// <summary>
    /// Interaction logic for SalsaWindow.xaml
    /// </summary>
    public partial class SalsaWindow : Window, IDancingTrainerComponent
    {
        /// <summary>
        /// Reference to KinectWindow
        /// </summary>
        private KinectWindow kinWin;

        /// <summary>
        /// Reference to MainWindow
        /// </summary>
        MainWindow mainWin;

        /// <summary>
        /// Reference to SalsaBeatManager
        /// </summary>
        SalsaBeatManager beatMan;

        /// <summary>
        /// Array of the feedback.
        /// </summary>
        public Feedback[] FeedbackArray { get; set; }

        /// <summary>
        /// Timer to select feedback that gets displayed.
        /// </summary>
        private System.Timers.Timer feedbackTimer;

        /// <summary>
        /// Current feedback counter.
        /// </summary>
        private int currentFeedbackCounter = 0;

        /// <summary>
        /// Smile Feedback.
        /// </summary>
        Feedback smile;

        /// <summary>
        /// Off beat or reset dancing feedback
        /// </summary>
        Feedback offbeat;

        /// <summary>
        /// focus or look straight feedback
        /// </summary>
        Feedback focus;

        /// <summary>
        /// Move body feedback.
        /// </summary>
        Feedback movebody;

        /// <summary>
        /// Session Start
        /// </summary>
        public DateTime SessionStart { get; set; }

        /// <summary>
        /// Array of the bodies.
        /// </summary>
        public Body[] Bodies { get; private set; }

        /// <summary>
        /// List for gestures per body
        /// </summary>
        private List<GestureDetectorSalsa> gestureDetectorSalsa;

        /// <summary>
        /// bool if applicatio is running.
        /// </summary>
        public bool isRunning = false;

        // needed ???
        int fileCounter = 1;
        List<string> stanceData = new List<string>();
        public List<string> gestureValuesList = new List<string>();

        /// <summary>
        /// Memorize previous joint positions.
        /// </summary>
        Dictionary<JointType, CameraSpacePoint> prevJointPositions = new Dictionary<JointType, CameraSpacePoint>();

        /// <summary>
        /// Dictionary for the motion angle.s
        /// </summary>
        Dictionary<JointType, double> motionAngles = new Dictionary<JointType, double>();

        /// <summary>
        /// reference vector for the motion angles
        /// </summary>
        Vector3D referenceMovement = new Vector3D(0.0, 0.0, -1.0);

        /// <summary>
        /// default mode.
        /// </summary>
        public string mode = "normal";       

        /// <summary>
        /// List of the plotted salsa steps.
        /// </summary>
        private List<(double, int)> plotSalsaSteps = new List<(double, int)>();

        /// <summary>
        /// Delegate to set image source on UI.
        /// </summary>
        /// <param name="b">BitmapImage</param>
        delegate void SetImageSourceDelegate(BitmapImage b);

        /// <summary>
        /// Delegate to set label content
        /// </summary>
        /// <param name="s">string</param>
        delegate void SetLabelContentDelegate(string s);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mw">MainWindow</param>
        /// <param name="kw">KinectWindow</param>
        /// <param name="bm">SalsaBeatManager</param>
        public SalsaWindow(MainWindow mw, KinectWindow kw, SalsaBeatManager bm)
        {
            InitializeComponent();
            label_BeatCounter.Content = "-";
            mode = "normal";
            kinWin = kw;
            System.IO.Directory.CreateDirectory("temp");

            mainWin = mw;
            beatMan = bm;
            ComboBoxItem musicItem = (ComboBoxItem)mainWin.combobox_MusicList.SelectedItem;
            this.Title = "Salsa: " + musicItem.Content.ToString();

            feedbackTimer = new System.Timers.Timer { Interval = 3000 };
            feedbackTimer.Elapsed += FeedbackTimer_Elapsed;

            // define all your feedback
            smile = new Feedback(new BitmapImage(new Uri(@"images\Smile.png", UriKind.RelativeOrAbsolute)), "Smile");
            offbeat = new Feedback(new BitmapImage(new Uri(@"images\Offbeat.png", UriKind.RelativeOrAbsolute)), "Reset Dancing");
            focus = new Feedback(new BitmapImage(new Uri(@"images\Focus.png", UriKind.RelativeOrAbsolute)), "Look straight");
            movebody = new Feedback(new BitmapImage(new Uri(@"images\MoveYourBody.png", UriKind.RelativeOrAbsolute)), "Move Body");

            FeedbackArray = new Feedback[4];
            FeedbackArray[0] = offbeat;
            FeedbackArray[1] = movebody;
            FeedbackArray[2] = focus;
            FeedbackArray[3] = smile;

            // init the gesture detector salsa list
            // one gesture detector for every potential body
            gestureDetectorSalsa = new List<GestureDetectorSalsa>();
            for (int i = 0; i < kinWin.kinectSensor.BodyFrameSource.BodyCount; i++)
            {
                gestureDetectorSalsa.Add(new GestureDetectorSalsa(this, kinWin.kinectSensor, beatMan));
            }

            this.Closing += SalsaWindow_Closing;
        }

        /// <summary>
        /// Play.
        /// </summary>
        public void Play()
        {
            try
            {
                plotSalsaSteps.Clear();
                foreach (Feedback item in FeedbackArray)
                {
                    item.ClearSchedule();
                }
            }
            catch (Exception)
            {
                // nothing
            }
            isRunning = true;
            Menu.IsEnabled = false;
            if (mode == "normal")
            {
                StartBodyCapturing();
                StartFaceCapturing();
                if (kinWin.isRecording)
                {
                    feedbackTimer.Start();
                }
                //vfw.Open("test.avi", (int)kinWin.ColorBitmap.Width, (int)kinWin.ColorBitmap.Height, 30, VideoCodec.MPEG4);
            }
            else
            {
                StartFaceCapturing(); // for testing
                StartBodyCapturing();
            }
        }

        /// <summary>
        /// Pause.
        /// </summary>
        public void Pause()
        {
            isRunning = false;
            if (mode == "normal")
            {
                PauseBodyCapturing();
                feedbackTimer.Stop();
            }
            else
            {
                PauseBodyCapturing();
            }
        }

        /// <summary>
        /// Stop.
        /// </summary>
        public void Stop()
        {
            isRunning = false;
            Menu.IsEnabled = true;
            // Reset the stepping images
            SetSalsaStepsWithBeat(0);
            img_Feedback_Icon.Source = null;
            img_Left_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            img_Right_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            label_Feedback_Icon.Content = "";

            foreach (var item in gestureDetectorSalsa)
            {
                if (item.resetTimer.Enabled)
                {
                    item.resetTimer.Stop();
                }
            }

            if (mode == "normal")
            {

                StopBodyCapturing();
                StopFaceCapturing();
                feedbackTimer.Stop();
                System.IO.File.WriteAllLines("motiondata" + fileCounter + ".csv", stanceData);
                File.WriteAllLines("GestureValues" + fileCounter + ".csv", gestureValuesList);
                fileCounter++;
                stanceData.Clear();
                gestureValuesList.Clear();
                //vfw.Close();
            }
            else
            {
                StopBodyCapturing();
            }

        }

        /// <summary>
        /// Event to close the window.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">CancelEventArgs</param>
        private void SalsaWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainWin.img_Play.IsEnabled = false;
            mainWin.img_Pause.IsEnabled = false;
            mainWin.img_Stop.IsEnabled = false;
            mainWin.button_Load.IsEnabled = true;

            //disable Speech Control
            try
            {
                mainWin.DisableSpeechControl();
            }
            catch (Exception) { }
            Dispose();
        }

        /// <summary>
        /// Saves the feedback schedule.
        /// </summary>
        public void SaveFeedbackSchedule()
        {
            SaveFileDialog sfdg = new SaveFileDialog
            {
                Title = "Save Feedback as Json",
                InitialDirectory = @"c:\",
                Filter = "json files (*.json)|*.json|All files (*.*)|*.*",
                FilterIndex = 1,
                CheckFileExists = false,
                CheckPathExists = true,
                RestoreDirectory = true,
                OverwritePrompt = true,
                CreatePrompt = false
            };
            sfdg.ShowDialog();
            if (sfdg.FileName != "")
            {
                dynamic timeline = new JObject();
                timeline.Date = SessionStart.ToShortDateString();
                timeline.TotalDuration = beatMan.totalDuration;
                timeline.Song = mainWin.combobox_MusicList.Text;
                timeline.BPM = beatMan.BPM;
                timeline.Name = "Test";
                timeline.Feedback = new JArray() as dynamic;
                timeline.PlotSalsaSteps = new JArray() as dynamic;

                List<(string name, double feedback_start, double display_start, double feedback_end)> temp;
                temp = new List<(string name, double feedback_start, double display_start, double feedback_end)>();

                // collect data for feedback array in json
                foreach (Feedback f in FeedbackArray)
                {
                    foreach (var elem in f.Schedule)
                    {
                        temp.Add((f.Instruction, elem.Item1, elem.Item2, elem.Item3));
                    }
                }
                temp = temp.OrderBy(x => x.feedback_start).ThenBy(y => y.display_start).ToList();

                // add data to feedback array in json
                var feedbackObject = new JObject();
                foreach (var item in temp)
                {
                    feedbackObject = new JObject
                    {
                        { "Instruction", item.ToTuple().Item1 },
                        { "Feedback Start", item.ToTuple().Item2 },
                        { "Display Start", item.ToTuple().Item3 },
                        { "Feedback End", item.ToTuple().Item4 }
                    };

                    timeline.Feedback.Add(feedbackObject);
                }

                // add data to plot salsa array
                var plotSalsaObj = new JObject();
                foreach ((double, int) item in plotSalsaSteps)
                {
                    plotSalsaObj = new JObject()
                    {
                        {"ms", item.Item1 },
                        {"beat", item.Item2}
                    };
                    timeline.PlotSalsaSteps.Add(plotSalsaObj);
                }



                string json = JsonConvert.SerializeObject(timeline);
                File.WriteAllText(sfdg.FileName, json);

                //string fname;
                //if (sfdg.FileName.Contains(".json"))
                //{
                //    fname = sfdg.FileName.Replace(".json", ".avi");
                //}
                //else
                //{
                //    fname = sfdg.FileName + ".avi";
                //}
                //DialogResult message = System.Windows.Forms.MessageBox.Show("Saving Video: Window closes on finish.");
                //WriteVideo(fname);
                //close on finish ?
            }
        }

        /// <summary>
        /// Sets the images on the UI for the Salsa Steps
        /// </summary>
        /// <param name="salsa_pos">Int of the step</param>
        private void SetSalsaStepsWithBeat(int salsa_pos)
        {
            if (!mi_ShowSalsaSteps.IsChecked)
            {
                return;
            }
            switch (salsa_pos)
            {
                default:
                    img_Left_Forward.Source = null;
                    img_Left_Neutral.Source = new BitmapImage(new Uri(@"images\LeftLight.png", UriKind.RelativeOrAbsolute));
                    img_Left_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    img_Left_Backward.Source = null;

                    img_Right_Forward.Source = null;
                    img_Right_Neutral.Source = new BitmapImage(new Uri(@"images\RightLight.png", UriKind.RelativeOrAbsolute));
                    img_Right_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    img_Right_Backward.Source = null;
                    break;
                case 1:
                    if (mi_Straight.IsEnabled)
                    {
                        // side step
                        img_Left_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                        img_Left_Neutral.Source = new BitmapImage(new Uri(@"images\Left.png", UriKind.RelativeOrAbsolute));
                        img_Left_Forward.Source = null;
                    }
                    if (mi_Side.IsEnabled)
                    {
                        // forth and back step
                        img_Left_Forward.Source = new BitmapImage(new Uri(@"images\Left.png", UriKind.RelativeOrAbsolute));
                        img_Left_Forward.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                        img_Left_Neutral.Source = null;
                    }
                    img_Left_Backward.Source = null;
                    img_Right_Forward.Source = null;
                    img_Right_Neutral.Source = new BitmapImage(new Uri(@"images\RightLight.png", UriKind.RelativeOrAbsolute));
                    img_Right_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    img_Right_Backward.Source = null;
                    break;
                case 2:
                    if (mi_Straight.IsEnabled)
                    {
                        // side step
                        img_Left_Neutral.Source = new BitmapImage(new Uri(@"images\LeftLight.png", UriKind.RelativeOrAbsolute));
                        img_Left_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                        img_Left_Forward.Source = null; ;
                    }
                    if (mi_Side.IsEnabled)
                    {
                        // forth and back
                        img_Left_Forward.Source = new BitmapImage(new Uri(@"images\LeftLight.png", UriKind.RelativeOrAbsolute));
                        img_Left_Forward.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                        img_Left_Neutral.Source = null;

                    }
                    img_Left_Backward.Source = null;
                    img_Right_Forward.Source = null;
                    img_Right_Neutral.Source = new BitmapImage(new Uri(@"images\Right.png", UriKind.RelativeOrAbsolute));
                    img_Right_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    img_Right_Backward.Source = null;
                    break;
                case 3:
                    img_Left_Forward.Source = null;
                    img_Left_Neutral.Source = new BitmapImage(new Uri(@"images\Left.png", UriKind.RelativeOrAbsolute));
                    img_Left_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    img_Left_Backward.Source = null;

                    img_Right_Forward.Source = null;
                    img_Right_Neutral.Source = new BitmapImage(new Uri(@"images\RightLight.png", UriKind.RelativeOrAbsolute));
                    img_Right_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    img_Right_Backward.Source = null;
                    break;
                case 5:
                    img_Left_Forward.Source = null;
                    img_Left_Neutral.Source = new BitmapImage(new Uri(@"images\LeftLight.png", UriKind.RelativeOrAbsolute));
                    img_Left_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    img_Left_Backward.Source = null;
                    img_Right_Forward.Source = null;

                    if (mi_Straight.IsEnabled)
                    {
                        // side step
                        img_Right_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                        img_Right_Neutral.Source = new BitmapImage(new Uri(@"images\Right.png", UriKind.RelativeOrAbsolute));
                        img_Right_Backward.Source = null;
                    }
                    if (mi_Side.IsEnabled)
                    {
                        // fort and back
                        img_Right_Neutral.Source = null;
                        img_Right_Backward.Source = new BitmapImage(new Uri(@"images\Right.png", UriKind.RelativeOrAbsolute));
                        img_Right_Backward.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    }
                    break;
                case 6:
                    img_Left_Forward.Source = null;
                    img_Left_Neutral.Source = new BitmapImage(new Uri(@"images\Left.png", UriKind.RelativeOrAbsolute));
                    img_Left_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    img_Left_Backward.Source = null;
                    img_Right_Forward.Source = null;
                    if (mi_Straight.IsEnabled)
                    {
                        // side step                        
                        img_Right_Neutral.Source = new BitmapImage(new Uri(@"images\RightLight.png", UriKind.RelativeOrAbsolute));
                        img_Right_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                        img_Right_Backward.Source = null;
                    }
                    if (mi_Side.IsEnabled)
                    {
                        // fort and back
                        img_Right_Neutral.Source = null;
                        img_Right_Backward.Source = new BitmapImage(new Uri(@"images\RightLight.png", UriKind.RelativeOrAbsolute));
                        img_Right_Backward.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    }
                    break;
                case 7: img_Left_Forward.Source = null;
                    img_Left_Neutral.Source = new BitmapImage(new Uri(@"images\LeftLight.png", UriKind.RelativeOrAbsolute));
                    img_Left_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    img_Left_Backward.Source = null;

                    img_Right_Forward.Source = null;
                    img_Right_Neutral.Source = new BitmapImage(new Uri(@"images\Right.png", UriKind.RelativeOrAbsolute));
                    img_Right_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    img_Right_Backward.Source = null;
                    break;
            }
        }

        /// <summary>
        /// Show salsa steps for normal mode
        /// </summary>
        /// <param name="salsa_pos">Int of the steps</param>
        public void ShowSteps(int salsa_pos)
        {
            if (mode != "normal")
            {
                return;
            }
            SetSalsaStepsWithBeat(salsa_pos);
        }

        /// <summary>
        /// Show salsa steps for tutorial mode
        /// </summary>
        /// <param name="salsa_pos">int of the steps</param>
        public void ShowStepsTutorial(int salsa_pos)
        {
            if (mode == "normal")
            {
                return;
            }
            SetSalsaStepsWithBeat(salsa_pos);
        }

        /// <summary>
        /// Start to capture the body
        /// </summary>
        private void StartBodyCapturing()
        {
            kinWin.bodyFrameHandler.bodyFrameReader.IsPaused = false;
            kinWin.bodyFrameHandler.bodyFrameReader.FrameArrived += BodyFrameReader_FrameArrived;
        }

        /// <summary>
        /// Pauses to capture the body.
        /// </summary>
        private void PauseBodyCapturing()
        {
            kinWin.bodyFrameHandler.bodyFrameReader.IsPaused = true;
        }

        /// <summary>
        /// Stops to capture the body.
        /// </summary>
        private void StopBodyCapturing()
        {
            kinWin.bodyFrameHandler.bodyFrameReader.FrameArrived -= BodyFrameReader_FrameArrived;
        }

        /// <summary>
        /// Event to read the arrived body frame
        /// </summary>
        /// <param name="sender">object.</param>
        /// <param name="e">BodyFrameArrivedEventAargs.</param>
        private void BodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            if (!isRunning) return;

            // a new frame is arrived so check if the leg is raised or not
            // first get/set all the values of the body

            Bodies = kinWin.bodyFrameHandler.bodies;
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.Bodies == null)
                    {
                        this.Bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.Bodies);

                    // get the floor plane of the frame

                    dataReceived = true;
                }
            }
            // avoid setting the values again by getting them from bodyframehandler???
            if (dataReceived)
            {
                for (int i = 0; i < Bodies.Length; i++)
                {
                    Body body = this.Bodies[i];
                    ulong trackingId = body.TrackingId;

                    // if the current body TrackingId changed, update the corresponding gesture detector with the new value
                    if (trackingId != this.gestureDetectorSalsa[i].TrackingId)
                    {
                        this.gestureDetectorSalsa[i].TrackingId = trackingId;

                        // if the current body is tracked, unpause its detector to get VisualGestureBuilderFrameArrived events
                        // if the current body is not tracked, pause its detector so we don't waste resources trying to get invalid gesture results
                        this.gestureDetectorSalsa[i].IsPaused = trackingId == 0;
                    }

                    if (Bodies[i] == null)
                    {
                        Console.WriteLine("Body " + i + " is null");
                    }
                    else if (Bodies[i].IsTracked)
                    {
                        // activate its gesture detector salsa
                        Console.WriteLine("##########################");
                        foreach (GestureDetectorSalsa gds in gestureDetectorSalsa)
                        {
                            if (gds.TrackingId == Bodies[i].TrackingId)
                            {
                                double t = beatMan.StopWatch.Elapsed.TotalMilliseconds - beatMan.timerStopwatchOffset;
                                Console.WriteLine("Gesture Detector " + gds.currentSalsaBeatCounter.ToString());
                                Console.WriteLine("Time passed: {0}", gds.timePassed);
                                Console.WriteLine("Original " + mainWin.label_BeatCounter.Content);
                                Console.WriteLine(t.ToString());
                                Console.WriteLine((beatMan.BeatCounter * beatMan.MSPB).ToString());
                                Console.WriteLine(beatMan.millisecondsPast.ToString());
                                Console.WriteLine(gds.IsGestureToTheBeat);
                                if (gds.IsGestureToTheBeat) {
                                    // you are to the beat
                                    //OFFBEAT.LowerHand(DateTime.Now.Subtract(SessionStart).TotalMilliseconds);
                                    //offbeat.LowerHand(beatMan.StopWatch.Elapsed.TotalMilliseconds);
                                    offbeat.LowerHand(t);
                                    Console.WriteLine("to the beat");
                                }
                                else
                                {
                                    // you are off the beat
                                    //OFFBEAT.RaiseHand(DateTime.Now.Subtract(SessionStart).TotalMilliseconds);
                                    //offbeat.RaiseHand(beatMan.StopWatch.Elapsed.TotalMilliseconds);
                                    offbeat.RaiseHand(t);
                                    Console.WriteLine("off the beat");
                                }
                                // assume that only one body is tracked at the time
                                if (mode == "normal")
                                {
                                    plotSalsaSteps.Add((t, gds.currentSalsaBeatCounter));
                                }

                                if (mode != "normal")
                                {
                                    Console.WriteLine(gds.currentSalsaBeatCounter);
                                    ShowStepsTutorial(gds.currentSalsaBeatCounter >= 8 ? 1 : gds.currentSalsaBeatCounter + 1);
                                    label_BeatCounter.Content = (gds.currentSalsaBeatCounter >= 8 ? 1 : gds.currentSalsaBeatCounter + 1).ToString();
                                }
                            }
                        }
                        Console.WriteLine("##########################");

                        // FOCUS feedback: check for the angle of the neck
                        Joint neck = Bodies[i].Joints[JointType.Neck];
                        Joint head = Bodies[i].Joints[JointType.Head];
                        Joint spineShoulder = Bodies[i].Joints[JointType.SpineShoulder];
                        double neckAngle = neck.Angle(head, spineShoulder);
                        
                        try
                        {
                            progbar_Focus.Value = neckAngle / 180.0;
                        }
                        catch (Exception) { }
                        if (neckAngle >= 170 && neckAngle <= 190)
                        {
                            // end feedback
                            //FOCUS.LowerHand(DateTime.Now.Subtract(SessionStart).TotalMilliseconds);
                            focus.LowerHand(beatMan.StopWatch.Elapsed.TotalMilliseconds - beatMan.timerStopwatchOffset);
                            progbar_Focus.Foreground = System.Windows.Media.Brushes.Green;
                        }
                        else
                        {
                            // start feedback
                            //FOCUS.RaiseHand(DateTime.Now.Subtract(SessionStart).TotalMilliseconds);
                            focus.RaiseHand(beatMan.StopWatch.Elapsed.TotalMilliseconds - beatMan.timerStopwatchOffset);
                            progbar_Focus.Foreground = System.Windows.Media.Brushes.Red;
                        }

                        // MOVEBODY feedback: check for the angle of elbow and shoulder
                        bool motionConfidence = UpperBodyMotionCapturing(Bodies[i]);
                        if (motionConfidence)
                        {
                            movebody.LowerHand(beatMan.StopWatch.Elapsed.TotalMilliseconds - beatMan.timerStopwatchOffset);
                        }
                        else
                        {
                            movebody.RaiseHand(beatMan.StopWatch.Elapsed.TotalMilliseconds - beatMan.timerStopwatchOffset);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Detect upper body motion.
        /// </summary>
        /// <param name="body">Body</param>
        /// <returns>Bool if upper body motion is detecteed</returns>
        private bool UpperBodyMotionCapturing(Body body)
        {
            Joint shoulderRight = body.Joints[JointType.ShoulderRight];
            Joint shoulderLeft = body.Joints[JointType.ShoulderLeft];
            Joint elbowLeft = body.Joints[JointType.ElbowLeft];
            Joint elbowRight = body.Joints[JointType.ElbowRight];
            Joint wristLeft = body.Joints[JointType.WristLeft];
            Joint wristRight = body.Joints[JointType.WristRight];
            Joint handLeft = body.Joints[JointType.HandLeft];
            Joint handRight = body.Joints[JointType.HandRight];

            if (prevJointPositions.Count == 0)
            {
                prevJointPositions.Add(JointType.ShoulderLeft, shoulderLeft.Position);
                prevJointPositions.Add(JointType.ShoulderRight, shoulderRight.Position);
                prevJointPositions.Add(JointType.ElbowLeft, elbowLeft.Position);
                prevJointPositions.Add(JointType.ElbowRight, elbowRight.Position);
                prevJointPositions.Add(JointType.WristLeft, wristLeft.Position);
                prevJointPositions.Add(JointType.WristRight, wristRight.Position);
                prevJointPositions.Add(JointType.HandLeft, handLeft.Position);
                prevJointPositions.Add(JointType.HandRight, handRight.Position);
            }

            double shoulderLeftMotionAngle = Angle3D(RoundVector3D(shoulderLeft.Position.ToVector3(), 2) - RoundVector3D(prevJointPositions[JointType.ShoulderLeft].ToVector3(), 2), referenceMovement);
            double shoulderRightMotionAngle = Angle3D(RoundVector3D(shoulderRight.Position.ToVector3(), 2) - RoundVector3D(prevJointPositions[JointType.ShoulderRight].ToVector3(), 2), referenceMovement);
            double elbowLeftMotionAngle = Angle3D(RoundVector3D(elbowLeft.Position.ToVector3(), 2) - RoundVector3D(prevJointPositions[JointType.ElbowLeft].ToVector3(), 2), referenceMovement);
            double elbowRightMotionAngle = Angle3D(RoundVector3D(elbowRight.Position.ToVector3(), 2) - RoundVector3D(prevJointPositions[JointType.ElbowRight].ToVector3(), 2), referenceMovement);
            double wristLeftMotionAngle = Angle3D(RoundVector3D(wristLeft.Position.ToVector3(), 2) - RoundVector3D(prevJointPositions[JointType.WristLeft].ToVector3(), 2), referenceMovement);
            double wristRightMotionAngle = Angle3D(RoundVector3D(wristRight.Position.ToVector3(), 2) - RoundVector3D(prevJointPositions[JointType.WristRight].ToVector3(), 2), referenceMovement);
            double handLeftMotionAngle = Angle3D(RoundVector3D(handLeft.Position.ToVector3(), 2) - RoundVector3D(prevJointPositions[JointType.HandLeft].ToVector3(), 2), referenceMovement);
            double handRightMotionAngle = Angle3D(RoundVector3D(handRight.Position.ToVector3(), 2) - RoundVector3D(prevJointPositions[JointType.HandRight].ToVector3(), 2), referenceMovement);


            //Console.WriteLine((shoulderLeftMotionAngle, shoulderRightMotionAngle, elbowLeftMotionAngle,
            //    elbowRightMotionAngle, wristLeftMotionAngle, wristRightMotionAngle, handLeftMotionAngle, handRightMotionAngle));

            motionAngles[JointType.ShoulderLeft] = shoulderLeftMotionAngle;
            motionAngles[JointType.ShoulderRight] = shoulderRightMotionAngle;
            motionAngles[JointType.ElbowLeft] = elbowLeftMotionAngle;
            motionAngles[JointType.ElbowRight] = elbowRightMotionAngle;
            motionAngles[JointType.WristLeft] = wristLeftMotionAngle;
            motionAngles[JointType.WristRight] = wristRightMotionAngle;
            motionAngles[JointType.HandLeft] = handLeftMotionAngle;
            motionAngles[JointType.HandRight] = handRightMotionAngle;
            
            double conf = 0;
            foreach (var item in motionAngles)
            {
                if (35.0 < item.Value && item.Value < 145.0)
                {
                    switch (item.Key)
                    {
                        case JointType.ShoulderLeft:
                            conf += 0.1;
                            break;
                        case JointType.ShoulderRight:
                            conf += 0.1;
                            break;
                        default:
                            conf += 0.1333;
                            break;
                    }
                }
            }
            //sum = sum / 15.0;
            Console.WriteLine("Motion confidence: " + conf.ToString());
            Console.WriteLine("Motion angles length: " + motionAngles.Count);
            Console.WriteLine("Is there a motion? " + (conf >= 0, 77).ToString());
            progbar_MoveBody.Value = conf;
            if (conf >= 0.77)
            {
                progbar_MoveBody.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                progbar_MoveBody.Foreground = System.Windows.Media.Brushes.Red;
            }

            Vector3D[] data = new Vector3D[]
            {
                shoulderLeft.Position.ToVector3(),shoulderRight.Position.ToVector3(),elbowLeft.Position.ToVector3(),
                elbowRight.Position.ToVector3(),wristLeft.Position.ToVector3(),
                wristRight.Position.ToVector3(),handLeft.Position.ToVector3(),handRight.Position.ToVector3()
            };

            stanceData.Add(string.Join("\t", data));

            // update position for the next frame
            prevJointPositions[JointType.ShoulderLeft] = shoulderLeft.Position;
            prevJointPositions[JointType.ShoulderRight] = shoulderRight.Position;
            prevJointPositions[JointType.ElbowLeft] = elbowLeft.Position;
            prevJointPositions[JointType.ElbowRight] = elbowRight.Position;
            prevJointPositions[JointType.WristLeft] = wristLeft.Position;
            prevJointPositions[JointType.WristRight] = wristRight.Position;
            prevJointPositions[JointType.HandLeft] = handLeft.Position;
            prevJointPositions[JointType.HandRight] = handRight.Position;


            return (conf > 0.77);
        }

        /// <summary>
        /// Rounds the components of a 3D vector.
        /// </summary>
        /// <param name="u">Vector3D</param>
        /// <param name="digits">int to be rounded on</param>
        /// <returns>rounded Vector3d</returns>
        private Vector3D RoundVector3D(Vector3D u, int digits)
        {
            return new Vector3D(Math.Round(u.X, digits), Math.Round(u.X, digits), Math.Round(u.Z, digits));
        }

        /// <summary>
        /// Computes the angle of two vectors.
        /// </summary>
        /// <param name="v1">Vector3D</param>
        /// <param name="v2">Vector3D</param>
        /// <returns>double</returns>
        private double Angle3D(Vector3D v1, Vector3D v2)
        {
            double dot = v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
            double length = v1.Length * v2.Length;
            double angle = Math.Acos(dot / length);
            if (angle is double.NaN)
            {
                return 0.0;
            }
            angle = angle * 360.0 / 2.0 / Math.PI;
            // return always the smaller one
            if (angle > 90.0)
            {
                angle = 180 - angle;
            }
            return angle;
        }

        /// <summary>
        /// Stops face capturing.
        /// </summary>
        private void StopFaceCapturing()
        {
            for (int i = 0; i < kinWin.faceFrameHandler.bodyCount; i++)
            {
                Console.WriteLine("stop face capturing for loop");
                if (kinWin.faceFrameHandler.faceFrameReaders[i] != null)
                {
                    // wire handler for face frame arrival
                    kinWin.faceFrameHandler.faceFrameReaders[i].FrameArrived -= Reader_FaceFrameArrived;
                    Console.WriteLine("stop face capturing unsubscribed to the event");
                    //break;
                }
            }
        }

        /// <summary>
        /// Sarts face capturing.
        /// </summary>
        private void StartFaceCapturing()
        {
            for (int i = 0; i < kinWin.faceFrameHandler.bodyCount; i++)
            {
                if (kinWin.faceFrameHandler.faceFrameReaders[i] != null)
                {
                    // wire handler for face frame arrival
                    kinWin.faceFrameHandler.faceFrameReaders[i].IsPaused = false;
                    kinWin.faceFrameHandler.faceFrameReaders[i].FrameArrived += Reader_FaceFrameArrived;
                }
            }
        }

        /// <summary>
        /// Event to choose the feedback for display.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">ElapsedEventArgs</param>
        private void FeedbackTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // reset
            if (currentFeedbackCounter >= FeedbackArray.Length - 1)
            {
                currentFeedbackCounter = 0;
            }

            while (currentFeedbackCounter < FeedbackArray.Length)
            {
                if (FeedbackArray[currentFeedbackCounter].IsActive)
                {
                    if (!FeedbackArray[currentFeedbackCounter].IsDisplayed)
                    {
                        Console.WriteLine(FeedbackArray[currentFeedbackCounter].Instruction + " is active and going to be displayed");
                        // can not access object because it is used by another thread
                        SetImageSource(FeedbackArray[currentFeedbackCounter].Source);
                        SetLabelContent(FeedbackArray[currentFeedbackCounter].Instruction);
                        FeedbackArray[currentFeedbackCounter].StartTalking(beatMan.StopWatch.Elapsed.TotalMilliseconds);
                        FeedbackArray[currentFeedbackCounter].IsDisplayed = true;
                    }
                    else
                    {
                        Console.WriteLine(FeedbackArray[currentFeedbackCounter].Instruction + " is active and displayed.");
                    }
                    break;
                }
                else
                {
                    Console.WriteLine(FeedbackArray[currentFeedbackCounter].Instruction + " is not active. Clear UI.");
                    SetImageSource(null);
                    SetLabelContent("");
                    FeedbackArray[currentFeedbackCounter].IsDisplayed = false;
                    currentFeedbackCounter++;
                }
            }

        }

        /// <summary>
        /// Set session start
        /// </summary>
        /// <param name="dt">DateTime</param>
        public void SetSessionStart(DateTime dt)
        {
            this.SessionStart = dt;
        }

        /// <summary>
        /// Event to read the arrived face frame.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">FaceFrameArrivedEventArgs</param>
        public void Reader_FaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (FaceFrame ff = e.FrameReference.AcquireFrame())
            {
                if (ff != null)
                {
                    int index = kinWin.faceFrameHandler.GetFaceSourceIndex(ff.FaceFrameSource);

                    if (kinWin.faceFrameHandler.ValidateFaceBoxAndPoints(ff.FaceFrameResult))
                    {

                        if (ff.FaceFrameResult.FaceProperties[FaceProperty.Happy] == DetectionResult.No)
                        {
                            label_Smile.Foreground = System.Windows.Media.Brushes.Red;
                        }
                        else
                        {
                            label_Smile.Foreground = System.Windows.Media.Brushes.Green;
                        }
                    }
                    else
                    {
                        kinWin.faceFrameHandler.faceFrameResults[index] = null;
                    }
                }
            }

            if (!isRunning) return;

            using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame != null)
                {
                    // get the index of the face source from the face source array
                    int index = kinWin.faceFrameHandler.GetFaceSourceIndex(faceFrame.FaceFrameSource);

                    // check if this face frame has valid face frame results
                    if (kinWin.faceFrameHandler.ValidateFaceBoxAndPoints(faceFrame.FaceFrameResult))
                    {
                        // store this face frame result to draw later
                        // KW.faceFrameHandler.faceFrameResults[index] = faceFrame.FaceFrameResult;
                        if (faceFrame.FaceFrameResult.FaceProperties[FaceProperty.Happy] == DetectionResult.No)
                        {
                            // start feedback
                            smile.RaiseHand(beatMan.StopWatch.Elapsed.TotalMilliseconds - beatMan.timerStopwatchOffset);
                            label_Smile.Foreground = System.Windows.Media.Brushes.Red;
                        }
                        else
                        {
                            smile.LowerHand(beatMan.StopWatch.Elapsed.TotalMilliseconds - beatMan.timerStopwatchOffset);
                            label_Smile.Foreground = System.Windows.Media.Brushes.Green;
                        }
                    }
                    else
                    {
                        // indicates that the latest face frame result from this reader is invalid
                        kinWin.faceFrameHandler.faceFrameResults[index] = null;
                    }
                }
            }
        }

        /// <summary>
        /// Set Image Source.
        /// </summary>
        /// <param name="b">BitmapImage</param>
        private void SetImageSource(BitmapImage b)
        {
            SetImageSourceDelegate simd = delegate (BitmapImage bim)
            {
                img_Feedback_Icon.Source = bim;
            };
            img_Feedback_Icon.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, simd, b);
        }

        /// <summary>
        /// Set label content on UI.
        /// </summary>
        /// <param name="s">string</param>
        private void SetLabelContent(string s)
        {
            SetLabelContentDelegate slcd = delegate (string instr)
            {
                label_Feedback_Icon.Content = instr;
            };
            label_Feedback_Icon.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, slcd, s);
        }

        /// <summary>
        /// Event to change to normal mode.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void MenuItem_OnNormal_Click(object sender, RoutedEventArgs e)
        {
            mi_Normal.IsEnabled = false;
            mi_Tutorial.IsEnabled = true;
            mi_Experimental.IsEnabled = true;
            mode = "normal";

            label_FAndBProgress_Left.Visibility = Visibility.Hidden;
            progbar_ForthAndBackProgress_Left.Visibility = Visibility.Hidden;
            label_FootTap_Left.Visibility = Visibility.Hidden;
            progbar_FootTap_Left.Visibility = Visibility.Hidden;
            label_FootTap_Right.Visibility = Visibility.Hidden;
            progbar_FootTap_Right.Visibility = Visibility.Hidden;
            label_MoveBody.Visibility = Visibility.Hidden;
            progbar_MoveBody.Visibility = Visibility.Hidden;
            label_Focus.Visibility = Visibility.Hidden;
            progbar_Focus.Visibility = Visibility.Hidden;
            label_Smile.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Event to change to tutorial mode.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void MenuItem_OnTutorial_Click(object sender, RoutedEventArgs e)
        {
            mi_Normal.IsEnabled = true;
            mi_Tutorial.IsEnabled = false;
            mi_Experimental.IsEnabled = true;
            mode = "tutorial";

            SalsaTutorialDescription dialog = new SalsaTutorialDescription();
            dialog.Show();

            label_FAndBProgress_Left.Visibility = Visibility.Hidden;
            progbar_ForthAndBackProgress_Left.Visibility = Visibility.Hidden;
            label_FootTap_Left.Visibility = Visibility.Hidden;
            progbar_FootTap_Left.Visibility = Visibility.Hidden;
            label_FootTap_Right.Visibility = Visibility.Hidden;
            progbar_FootTap_Right.Visibility = Visibility.Hidden;
            label_MoveBody.Visibility = Visibility.Hidden;
            progbar_MoveBody.Visibility = Visibility.Hidden;
            label_Focus.Visibility = Visibility.Hidden;
            progbar_Focus.Visibility = Visibility.Hidden;
            label_Smile.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Event to change to experimental mode.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void MenuItem_OnExperimental_Click(object sender, RoutedEventArgs e)
        {
            mi_Normal.IsEnabled = true;
            mi_Tutorial.IsEnabled = true;
            mi_Experimental.IsEnabled = false;
            mode = "experimental";

            label_FAndBProgress_Left.Visibility = Visibility.Visible;
            progbar_ForthAndBackProgress_Left.Visibility = Visibility.Visible;
            label_FootTap_Left.Visibility = Visibility.Visible;
            progbar_FootTap_Left.Visibility = Visibility.Visible;
            label_FootTap_Right.Visibility = Visibility.Visible;
            progbar_FootTap_Right.Visibility = Visibility.Visible;
            label_MoveBody.Visibility = Visibility.Visible;
            progbar_MoveBody.Visibility = Visibility.Visible;
            label_Focus.Visibility = Visibility.Visible;
            progbar_Focus.Visibility = Visibility.Visible;
            label_Smile.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Event to save performance.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void MenuItem_OnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFeedbackSchedule();
        }

        /// <summary>
        /// Event to enable or disable audio beat support.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void MenuItem_AudioBeatSupport_Click(object sender, RoutedEventArgs e)
        {
            if (mi_AudioBeatSupport.IsChecked)
            {
                mainWin.SetMediaElementAudioSource("music with beat");
            }
            else
            {
                mainWin.SetMediaElementAudioSource("music");
            }
        }

        /// <summary>
        /// Event to enable or disable beat count support.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void MenuItem_BeatCountSupport_Click(object sender, RoutedEventArgs e)
        {
            if (mi_BeatCountSupport.IsChecked)
            {
                label_BeatCounter.Visibility = Visibility.Visible;
                label_BeatCounterDescription.Visibility = Visibility.Visible;
            }
            else
            {
                label_BeatCounter.Visibility = Visibility.Hidden;
                label_BeatCounterDescription.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Event to enable or disable salsa steps images.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void MenuItem_ShowSalsaSteps_Click(object sender, RoutedEventArgs e)
        {
            // TO DO
            if (mi_ShowSalsaSteps.IsChecked)
            {
                // Show the steps          
                try
                {
                    // if beat counter label is '-' than set it to default.
                    SetSalsaStepsWithBeat(Int32.Parse(label_BeatCounter.ToString()));
                }
                catch (Exception)
                {
                    SetSalsaStepsWithBeat(8);
                }             
            }
            else
            {
                // do not show the steps
                img_Left_Backward.Source = null;
                img_Left_Forward.Source = null;
                img_Left_Neutral.Source = null;
                img_Right_Backward.Source = null;
                img_Right_Forward.Source = null;
                img_Right_Neutral.Source = null;
            }
        }

        /// <summary>
        /// Event to enable or disable side steps.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void MenuItem_Side_Click(object sender, RoutedEventArgs e)
        {
            mi_Side.IsEnabled = false;
            mi_Straight.IsEnabled = true;
            referenceMovement = new Vector3D(-1.0, 0.0, 0.0);
        }

        /// <summary>
        /// Event to enable or disable forth and back steps.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void MenuItem_Straight_Click(object sender, RoutedEventArgs e)
        {
            mi_Side.IsEnabled = true;
            mi_Straight.IsEnabled = false;
            referenceMovement = new Vector3D(0.0, 0.0, -1.0);
        }


        /// <summary>
        /// Event to open and visualize performance.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofdg = new OpenFileDialog
            {
                Title = "Open Json as Feedback Timeline",
                InitialDirectory = @"c:\",
                Filter = "json files (*.json)|*.json|All files (*.*)|*.*",
                FilterIndex = 1,
                CheckFileExists = false,
                CheckPathExists = true,
                RestoreDirectory = true,
            };
            if (ofdg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SalsaDashboard timeline = new SalsaDashboard(ofdg.FileName);
                timeline.Show();
            }
        }

        /// <summary>
        /// Disposes object.s
        /// </summary>
        private void Dispose()
        {
            try
            {
                feedbackTimer.Dispose();
                // Record.Dispose();
                beatMan.Dispose();
                foreach (GestureDetectorSalsa item in gestureDetectorSalsa)
                {
                    item.Dispose();
                }
            }
            catch (Exception) { }           
        }
    }
}
