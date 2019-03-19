using LightBuzz.Vitruvius;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Accord.Video.FFMPEG;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Windows.Media;

namespace DancingTrainer
{
    /// <summary>
    /// Interaction logic for SalsaWindow.xaml
    /// </summary>
    public partial class SalsaWindow : Window, IDancingTrainerComponent
    {

        public KinectWindow kinWin;
        MainWindow mainWin;
        SalsaBeatManager beatMan;

        // schedule the freedback during runtime
        public Feedback[] FeedbackArray { get; set; }

        private System.Timers.Timer feedbackTimer;
        private int currentFeedbackCounter = 0;

        // define all your feedback
        Feedback smile;
        Feedback offbeat;
        Feedback focus;
        Feedback movebody;

        public DateTime SessionStart { get; set; }
        public Body[] Bodies { get; private set; }

        private List<GestureDetectorSalsa> gestureDetectorSalsa;
        public bool isRunning = false;

        // needed ???
        int fileCounter = 100;
        List<string> stanceData = new List<string>();

        Dictionary<JointType, CameraSpacePoint> prevJointPositions = new Dictionary<JointType, CameraSpacePoint>();
        Dictionary<JointType, double> motionAngles = new Dictionary<JointType, double>();
        Vector3D referenceMovement = new Vector3D(0.0,0.0,-1.0);
        public string mode = "normal";
        public List<ImageSource> Video { get; set; } = new List<ImageSource>();
        //private VideoFileWriter vfw = new VideoFileWriter();
        public Task Record { get; set; }
        public WriteableBitmap ColorBitmap { get; private set; }

        private List<(double, int)> plotSalsaSteps = new List<(double, int)>();

        public SalsaWindow(MainWindow mw, KinectWindow kw, SalsaBeatManager bm)
        {
            InitializeComponent();
            label_BeatCounter.Content = "-";
            mode = "normal";
            kinWin = kw;
            //kinWin.colorFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
            
            mainWin = mw;
            beatMan = bm;
            this.Title = "Salsa: " + mainWin.combobox_MusicList.SelectedItem.ToString();
            //BM.timer.Elapsed += Timer_Elapsed;

            feedbackTimer = new System.Timers.Timer{Interval = 3000};
            feedbackTimer.Elapsed += FeedbackTimer_Elapsed;

            // define all your feedback
            smile = new Feedback(new BitmapImage(new Uri(@"images\Smile.png", UriKind.RelativeOrAbsolute)), "Smile");
            offbeat = new Feedback(new BitmapImage(new Uri(@"images\Offbeat.png", UriKind.RelativeOrAbsolute)), "Follow Beat");
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


            // create the colorFrameDescription from the ColorFrameSource using Bgra format
            FrameDescription colorFrameDescription = kinWin.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            ColorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, System.Windows.Media.PixelFormats.Bgr32, null);
            this.Closing += SalsaWindow_Closing;
        }

        private void ColorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            if (kinWin.isRecording)
            {
                Video.Add(kinWin.ImageSource);
            }
            

            //using (ColorFrame cframe = e.FrameReference.AcquireFrame())
            //{
            //    if (cframe != null)
            //    {
            //        if (kinWin.isRecording)
            //        {
            //            WriteableBitmap b = kinWin.ColorBitmap;
            //            // add the frame to the video list
            //            //record.ContinueWith(x => video.Add(kinWin.ColorBitmap));
            //            Record = Task.Factory.StartNew(() => Video.Add(b));
            //            //Record = Task.Factory.StartNew(() => Video.Add(kinWin.ColorBitmap));

            //            //if (vfw.IsOpen)
            //            //{
            //            //    vfw.WriteVideoFrame(BitmapSourceToBitmap(kinWin.ColorBitmap));
            //            //}
            //            //Bitmap btm = BitmapSourceToBitmap(kinWin.ColorBitmap);

            //        }
            //    }
            //}
        }

        private Bitmap BitmapSourceToBitmap(BitmapSource srs)
        {
            int width = srs.PixelWidth;
            int height = srs.PixelHeight;
            int stride = width * ((srs.Format.BitsPerPixel + 7) / 8);
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(height * stride);
                srs.CopyPixels(new Int32Rect(0, 0, width, height), ptr, height * stride, stride);
                using (var btm = new Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format32bppArgb, ptr))
                {
                    // Clone the bitmap so that we can dispose it and
                    // release the unmanaged memory at ptr
                    return new Bitmap(btm);
                }
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
                
            }
        }


        private void WriteVideo(string filename)
        {
            int width = (int)Video[0].Width;
            int height = (int)Video[0].Height;
            // create instance of video writer
            VideoFileWriter writer = new VideoFileWriter();
            // create new video file
            writer.Open(filename, width, height, 30, VideoCodec.MPEG4);
            Stopwatch s = new Stopwatch();
            for (int i = 0; i < Video.Count; i++)
            {            
                s.Restart();
                Bitmap btm = new Bitmap(BitmapSourceToBitmap((BitmapSource)Video[i]));
                writer.WriteVideoFrame(btm);
                s.Stop();
                Console.WriteLine(s.Elapsed.TotalMilliseconds);

            }
            writer.Close();
        }

        

        //private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    ShowSteps(BM.beatCounter % 8);
        //}

        public void Play()
        {
            try
            {
                Video.Clear();
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
                StartBodyCapturing();
            }        
        }

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
            if (mode == "normal")
            {
               
                StopBodyCapturing();
                StopFaceCapturing();
                feedbackTimer.Stop();
                System.IO.File.WriteAllLines("motiondata" + fileCounter + ".csv", stanceData);
                fileCounter++;
                stanceData.Clear();
                //vfw.Close();
            }
            else
            {
                StopBodyCapturing();
            }
            
        }

        private void SalsaWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainWin.button_Load.IsEnabled = true;
        }


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
                timeline.Date = SessionStart.Date.ToString();
                timeline.TotalDuration = beatMan.totalDuration;
                timeline.Song = mainWin.combobox_MusicList.SelectedItem.ToString();
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
                foreach ((double,int) item in plotSalsaSteps)
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

        private void SetSalsaStepsWithBeat(int salsa_pos)
        {
            switch (salsa_pos)
            {
                default:
                    img_Left_Forward.Source = null;
                    img_Left_Neutral.Source = new BitmapImage(new Uri(@"images\LeftLight.png", UriKind.RelativeOrAbsolute));
                    img_Left_Backward.Source = null;

                    img_Right_Forward.Source = null;
                    img_Right_Neutral.Source = new BitmapImage(new Uri(@"images\RightLight.png", UriKind.RelativeOrAbsolute));
                    img_Right_Backward.Source = null;
                    break;
                case 1:
                    if (mi_Straight.IsEnabled)
                    {
                        img_Left_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                        img_Left_Neutral.Source = new BitmapImage(new Uri(@"images\Left.png", UriKind.RelativeOrAbsolute));
                    }
                    if (mi_Side.IsEnabled)
                    {
                        img_Left_Forward.Source = new BitmapImage(new Uri(@"images\Left.png", UriKind.RelativeOrAbsolute));
                        img_Left_Neutral.Source = null;
                        img_Left_Backward.Source = null;
                        
                    }
                    img_Right_Forward.Source = null;
                    img_Right_Neutral.Source = new BitmapImage(new Uri(@"images\RightLight.png", UriKind.RelativeOrAbsolute));
                    img_Right_Backward.Source = null;
                    break;
                case 2:
                    if (mi_Straight.IsEnabled)
                    {
                        img_Left_Neutral.Source = new BitmapImage(new Uri(@"images\LeftLight.png", UriKind.RelativeOrAbsolute));
                        img_Right_Neutral.Source = new BitmapImage(new Uri(@"images\Right.png", UriKind.RelativeOrAbsolute));
                    }
                    if (mi_Side.IsEnabled)
                    {
                        img_Left_Forward.Source = new BitmapImage(new Uri(@"images\LeftLight.png", UriKind.RelativeOrAbsolute));
                        img_Left_Neutral.Source = null;
                        img_Left_Backward.Source = null;

                        img_Right_Forward.Source = null;
                        img_Right_Neutral.Source = new BitmapImage(new Uri(@"images\Right.png", UriKind.RelativeOrAbsolute));
                        img_Right_Backward.Source = null;
                    }
                    break;
                case 3:
                    if (mi_Straight.IsEnabled)
                    {
                        img_Left_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    }
                    img_Left_Forward.Source = null;
                    img_Left_Neutral.Source = new BitmapImage(new Uri(@"images\Left.png", UriKind.RelativeOrAbsolute));
                    img_Left_Backward.Source = null;

                    img_Right_Forward.Source = null;
                    img_Right_Neutral.Source = new BitmapImage(new Uri(@"images\RightLight.png", UriKind.RelativeOrAbsolute));
                    img_Right_Backward.Source = null;
                    break;
                case 5:                    
                    img_Left_Forward.Source = null;
                    img_Left_Neutral.Source = new BitmapImage(new Uri(@"images\LeftLight.png", UriKind.RelativeOrAbsolute));
                    img_Left_Backward.Source = null;

                    if (mi_Straight.IsEnabled)
                    {
                        img_Right_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                        img_Right_Neutral.Source = new BitmapImage(new Uri(@"images\Right.png", UriKind.RelativeOrAbsolute));
                    }
                    if (mi_Side.IsEnabled)
                    {
                        img_Right_Forward.Source = null;
                        img_Right_Neutral.Source = null;
                        img_Right_Backward.Source = new BitmapImage(new Uri(@"images\Right.png", UriKind.RelativeOrAbsolute));
                    }                    
                    break;
                case 6:
                    if (mi_Straight.IsEnabled)
                    {
                        img_Left_Neutral.Source = new BitmapImage(new Uri(@"images\Left.png", UriKind.RelativeOrAbsolute));
                        img_Right_Neutral.Source = new BitmapImage(new Uri(@"images\RightLight.png", UriKind.RelativeOrAbsolute));
                    }
                    if (mi_Side.IsEnabled)
                    {
                        img_Left_Forward.Source = null;
                        img_Left_Neutral.Source = new BitmapImage(new Uri(@"images\Left.png", UriKind.RelativeOrAbsolute));
                        img_Left_Backward.Source = null;
                        img_Right_Forward.Source = null;
                        img_Right_Neutral.Source = null;
                        img_Right_Backward.Source = new BitmapImage(new Uri(@"images\RightLight.png", UriKind.RelativeOrAbsolute));
                    }                    
                    break;
                case 7:
                    if (mi_Straight.IsEnabled)
                    {
                        img_Right_Neutral.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    }
                    img_Left_Forward.Source = null;
                    img_Left_Neutral.Source = new BitmapImage(new Uri(@"images\LeftLight.png", UriKind.RelativeOrAbsolute));
                    img_Left_Backward.Source = null;

                    img_Right_Forward.Source = null;
                    img_Right_Neutral.Source = new BitmapImage(new Uri(@"images\Right.png", UriKind.RelativeOrAbsolute));
                    img_Right_Backward.Source = null;
                    break;
            }
        }

        // only one method ??? 
        public void ShowSteps(int salsa_pos)
        {
            if (mode != "normal")
            {
                return;
            }
            SetSalsaStepsWithBeat(salsa_pos);
        }

        public void ShowStepsTutorial(int salsa_pos)
        {
            if(mode == "normal")
            {
                return;
            }
            SetSalsaStepsWithBeat(salsa_pos);
        }

        private void StartBodyCapturing()
        {
            kinWin.bodyFrameHandler.bodyFrameReader.IsPaused = false;
            kinWin.bodyFrameHandler.bodyFrameReader.FrameArrived += BodyFrameReader_FrameArrived;
        }

        private void PauseBodyCapturing()
        {
            kinWin.bodyFrameHandler.bodyFrameReader.IsPaused = true;
        }

        private void StopBodyCapturing()
        {
            kinWin.bodyFrameHandler.bodyFrameReader.FrameArrived -= BodyFrameReader_FrameArrived;
        }

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
                                Console.WriteLine((beatMan.BeatCounter*beatMan.MSPB).ToString());
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

                        if (neckAngle >= 170 && neckAngle <= 190)
                        {
                            // end feedback
                            //FOCUS.LowerHand(DateTime.Now.Subtract(SessionStart).TotalMilliseconds);
                            focus.LowerHand(beatMan.StopWatch.Elapsed.TotalMilliseconds - beatMan.timerStopwatchOffset);
                        }
                        else
                        {
                            // start feedback
                            //FOCUS.RaiseHand(DateTime.Now.Subtract(SessionStart).TotalMilliseconds);
                            focus.RaiseHand(beatMan.StopWatch.Elapsed.TotalMilliseconds - beatMan.timerStopwatchOffset);
                        }

                        // MOVEBODY feedback: check for the angle of elbow and shoulder
                        bool motionConfidence = UpperBodyMotionCapturing(Bodies[i]);
                        if (motionConfidence)
                        {
                            movebody.LowerHand(beatMan.StopWatch.Elapsed.TotalMilliseconds-beatMan.timerStopwatchOffset);
                        }
                        else
                        {
                            movebody.RaiseHand(beatMan.StopWatch.Elapsed.TotalMilliseconds - beatMan.timerStopwatchOffset);
                        }

                        //Joint shoulderRight = Bodies[i].Joints[JointType.ShoulderRight];
                        //Joint shoulderLeft = Bodies[i].Joints[JointType.ShoulderLeft];
                        //Joint elbowLeft = Bodies[i].Joints[JointType.ElbowLeft];
                        //Joint elbowRight = Bodies[i].Joints[JointType.ElbowRight];
                        //Joint handLeft = Bodies[i].Joints[JointType.HandLeft];
                        //Joint handRight = Bodies[i].Joints[JointType.HandRight];

                        //double elbowAngleLeft = elbowLeft.Angle(handLeft, shoulderLeft);
                        //double shoulderAngleLeft = shoulderLeft.Angle(elbowLeft, spineShoulder);
                        //double elbowAngleRight = elbowRight.Angle(handRight, shoulderRight);
                        //double shoulderAngleRight = shoulderRight.Angle(handRight, spineShoulder);

                        //Console.WriteLine(elbowAngleLeft.ToString());
                        //Console.WriteLine(shoulderAngleLeft.ToString());
                        //Console.WriteLine(elbowAngleRight.ToString());
                        //Console.WriteLine(shoulderAngleRight.ToString());
                        //if ((elbowAngleLeft >= 135 && shoulderAngleLeft <= 100)||(elbowAngleRight >= 135 && shoulderAngleRight <= 100))
                        //{
                        //    MOVEBODY.RaiseHand(DateTime.Now.Subtract(SessionStart).TotalMilliseconds);
                        //}
                        //else
                        //{
                        //    MOVEBODY.LowerHand(DateTime.Now.Subtract(SessionStart).TotalMilliseconds);
                        //}

                    }
                }
            }
        }

        private bool UpperBodyMotionCapturing(Body body)
        {
            //Joint head = body.Joints[JointType.Head];
            //Joint neck = body.Joints[JointType.Neck];
            //Joint spineShoulder = body.Joints[JointType.SpineShoulder];
            //Joint spineMid = body.Joints[JointType.SpineMid];
            //Joint spineBase = body.Joints[JointType.SpineBase];
            //Joint hipLeft = body.Joints[JointType.HipLeft];
            //Joint hipRight = body.Joints[JointType.HipRight];
            Joint shoulderRight = body.Joints[JointType.ShoulderRight];
            Joint shoulderLeft = body.Joints[JointType.ShoulderLeft];
            Joint elbowLeft = body.Joints[JointType.ElbowLeft];
            Joint elbowRight = body.Joints[JointType.ElbowRight];
            Joint wristLeft = body.Joints[JointType.WristLeft];
            Joint wristRight = body.Joints[JointType.WristRight];
            Joint handLeft = body.Joints[JointType.HandLeft];
            Joint handRight = body.Joints[JointType.HandRight];

            // look only on sideways motion
            //shoulderRight.Position.Y = 0;
            //shoulderLeft.Position.Y = 0;
            //elbowLeft.Position.Y = 0;
            //elbowRight.Position.Y = 0;
            //wristLeft.Position.Y = 0;
            //wristRight.Position.Y = 0;
            //handLeft.Position.Y = 0;
            //handRight.Position.Y = 0;

            if (prevJointPositions.Count == 0)
            {
                //prevJointPositions.Add(JointType.Head, head.Position);
                //prevJointPositions.Add(JointType.Neck, neck.Position);
                //prevJointPositions.Add(JointType.SpineShoulder, spineShoulder.Position);
                //prevJointPositions.Add(JointType.SpineMid, spineMid.Position);
                //prevJointPositions.Add(JointType.SpineBase, spineBase.Position);
                //prevJointPositions.Add(JointType.HipLeft, hipLeft.Position);
                //prevJointPositions.Add(JointType.HipRight, hipRight.Position);
                prevJointPositions.Add(JointType.ShoulderLeft, shoulderLeft.Position);
                prevJointPositions.Add(JointType.ShoulderRight, shoulderRight.Position);
                prevJointPositions.Add(JointType.ElbowLeft, elbowLeft.Position);
                prevJointPositions.Add(JointType.ElbowRight, elbowRight.Position);
                prevJointPositions.Add(JointType.WristLeft, wristLeft.Position);
                prevJointPositions.Add(JointType.WristRight, wristRight.Position);
                prevJointPositions.Add(JointType.HandLeft, handLeft.Position);
                prevJointPositions.Add(JointType.HandRight, handRight.Position);
            }

            
            

            //Console.WriteLine(RoundVector3D(spineMid.Position.ToVector3(), 1).ToString());
            //Console.WriteLine(RoundVector3D(prevJointPositions[JointType.SpineMid].ToVector3(), 1).ToString());
            //Console.WriteLine((RoundVector3D(spineMid.Position.ToVector3(), 1) - RoundVector3D(prevJointPositions[JointType.SpineMid].ToVector3(), 1)).ToString());

            //double headMotionAngle = Angle3D(RoundVector3D(head.Position.ToVector3(), 1) - RoundVector3D(prevJointPositions[JointType.Head].ToVector3(), 1), forwardMovement);
            //double neckMotionAngle = Angle3D(RoundVector3D(neck.Position.ToVector3(), 1) - RoundVector3D(prevJointPositions[JointType.Neck].ToVector3(), 1), forwardMovement);
            //double spineShoulderMotionAngle = Angle3D(RoundVector3D(spineShoulder.Position.ToVector3(), 1) - RoundVector3D(prevJointPositions[JointType.SpineShoulder].ToVector3(), 1), forwardMovement);
            //double spineMidMotionAngle = Angle3D(RoundVector3D(spineMid.Position.ToVector3(), 1) - RoundVector3D(prevJointPositions[JointType.SpineMid].ToVector3(), 1), forwardMovement);
            //double spineBaseMotionAngle = Angle3D(RoundVector3D(spineShoulder.Position.ToVector3(), 1) - RoundVector3D(prevJointPositions[JointType.SpineShoulder].ToVector3(), 1), forwardMovement);
            //double hipLeftMotionAngle = Angle3D(RoundVector3D(hipLeft.Position.ToVector3(), 1) - RoundVector3D(prevJointPositions[JointType.HipLeft].ToVector3(), 1), forwardMovement);
            //double hipRightMotionAngle = Angle3D(RoundVector3D(hipRight.Position.ToVector3(), 1) - RoundVector3D(prevJointPositions[JointType.HipRight].ToVector3(), 1), forwardMovement);
            double shoulderLeftMotionAngle = Angle3D(RoundVector3D(shoulderLeft.Position.ToVector3(), 2) - RoundVector3D(prevJointPositions[JointType.ShoulderLeft].ToVector3(), 2), referenceMovement);
            double shoulderRightMotionAngle = Angle3D(RoundVector3D(shoulderRight.Position.ToVector3(), 2) - RoundVector3D(prevJointPositions[JointType.ShoulderRight].ToVector3(), 2), referenceMovement);
            double elbowLeftMotionAngle = Angle3D(RoundVector3D(elbowLeft.Position.ToVector3(), 2) - RoundVector3D(prevJointPositions[JointType.ElbowLeft].ToVector3(),2), referenceMovement);
            double elbowRightMotionAngle = Angle3D(RoundVector3D(elbowRight.Position.ToVector3(), 2) - RoundVector3D(prevJointPositions[JointType.ElbowRight].ToVector3(), 2), referenceMovement);
            double wristLeftMotionAngle = Angle3D(RoundVector3D(wristLeft.Position.ToVector3(), 2) - RoundVector3D(prevJointPositions[JointType.WristLeft].ToVector3(), 2), referenceMovement);
            double wristRightMotionAngle = Angle3D(RoundVector3D(wristRight.Position.ToVector3(), 2) - RoundVector3D(prevJointPositions[JointType.WristRight].ToVector3(), 2), referenceMovement);
            double handLeftMotionAngle = Angle3D(RoundVector3D(handLeft.Position.ToVector3(), 2) - RoundVector3D(prevJointPositions[JointType.HandLeft].ToVector3(), 2), referenceMovement);
            double handRightMotionAngle = Angle3D(RoundVector3D(handRight.Position.ToVector3(), 2) - RoundVector3D(prevJointPositions[JointType.HandRight].ToVector3(), 2), referenceMovement);

            //Console.WriteLine((headMotionAngle, neckMotionAngle, spineShoulderMotionAngle, spineMidMotionAngle, spineBaseMotionAngle,
            //    hipLeftMotionAngle, hipRightMotionAngle, shoulderLeftMotionAngle, shoulderRightMotionAngle, elbowLeftMotionAngle,
            //    elbowRightMotionAngle, wristLeftMotionAngle, wristRightMotionAngle, handLeftMotionAngle, handRightMotionAngle));

            Console.WriteLine((shoulderLeftMotionAngle, shoulderRightMotionAngle, elbowLeftMotionAngle,
                elbowRightMotionAngle, wristLeftMotionAngle, wristRightMotionAngle, handLeftMotionAngle, handRightMotionAngle));

            //motionAngles[JointType.Head] = headMotionAngle;
            //motionAngles[JointType.Neck] = neckMotionAngle;
            //motionAngles[JointType.SpineShoulder] = spineShoulderMotionAngle;
            //motionAngles[JointType.SpineMid] = spineMidMotionAngle;
            //motionAngles[JointType.SpineBase] = spineBaseMotionAngle;
            //motionAngles[JointType.HipLeft] = hipLeftMotionAngle;
            //motionAngles[JointType.HipRight] = hipRightMotionAngle;
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
            Console.WriteLine("Is there a motion? " + (conf > 0.77).ToString());
            progbar_MoveBody.Value = conf;

            //Vector3D[] data = new Vector3D[]
            //{
            //    head.Position.ToVector3(),neck.Position.ToVector3(),spineShoulder.Position.ToVector3(),spineMid.Position.ToVector3(),
            //    spineBase.Position.ToVector3(),hipLeft.Position.ToVector3(),hipRight.Position.ToVector3(),shoulderLeft.Position.ToVector3(),
            //    shoulderRight.Position.ToVector3(),elbowLeft.Position.ToVector3(),elbowRight.Position.ToVector3(),wristLeft.Position.ToVector3(),
            //    wristRight.Position.ToVector3(),handLeft.Position.ToVector3(),handRight.Position.ToVector3()
            //};

            Vector3D[] data = new Vector3D[]
            {
                shoulderLeft.Position.ToVector3(),shoulderRight.Position.ToVector3(),elbowLeft.Position.ToVector3(),
                elbowRight.Position.ToVector3(),wristLeft.Position.ToVector3(),
                wristRight.Position.ToVector3(),handLeft.Position.ToVector3(),handRight.Position.ToVector3()
            };

            stanceData.Add(string.Join("\t", data));


            // update position for the next frame
            //prevJointPositions[JointType.Head] = head.Position;
            //prevJointPositions[JointType.Neck] = neck.Position;
            //prevJointPositions[JointType.SpineShoulder] = spineShoulder.Position;
            //prevJointPositions[JointType.SpineMid] = spineMid.Position;
            //prevJointPositions[JointType.SpineBase] = spineBase.Position;
            //prevJointPositions[JointType.HipLeft] = hipLeft.Position;
            //prevJointPositions[JointType.HipRight] = hipRight.Position;
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

        private Vector3D RoundVector3D(Vector3D u, int digits)
        {
            return new Vector3D(Math.Round(u.X, digits), Math.Round(u.X, digits),Math.Round(u.Z, digits));
        }


        private double Angle3D(Vector3D v1,Vector3D v2)
        {
            double dot = v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
            double length = v1.Length * v2.Length;
            double angle = Math.Acos(dot / length);
            if (angle is double.NaN)
            {
                return 0.0;
            }               
            angle = angle * 360.0 / 2.0 / Math.PI;
            // return always the really smaller one
            if (angle > 90.0)
            {
                angle = 180 - angle;
            }
            return angle;
        }

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
        private void StartFaceCapturing()
        {
            for (int i = 0; i < kinWin.faceFrameHandler.bodyCount; i++)
            {
                if (kinWin.faceFrameHandler.faceFrameReaders[i] != null)
                {
                    // wire handler for face frame arrival
                    kinWin.faceFrameHandler.faceFrameReaders[i].IsPaused = false;
                    kinWin.faceFrameHandler.faceFrameReaders[i].FrameArrived += Reader_FaceFrameArrived;
                    //break;
                }
            }
        }

        // the teacher who choses the feedback
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
                        //feedbackArray[currentFeedbackCounter].StartTalking(DateTime.Now.Subtract(SessionStart).TotalMilliseconds);
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

        public void SetSessionStart(DateTime dt)
        {
            this.SessionStart = dt;
        }

        public void Reader_FaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
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
                            //SMILE.RaiseHand(DateTime.Now.Subtract(SessionStart).TotalMilliseconds);
                            smile.RaiseHand(beatMan.StopWatch.Elapsed.TotalMilliseconds-beatMan.timerStopwatchOffset);

                        }
                        else
                        {
                            //SMILE.LowerHand(DateTime.Now.Subtract(SessionStart).TotalMilliseconds);
                            smile.LowerHand(beatMan.StopWatch.Elapsed.TotalMilliseconds-beatMan.timerStopwatchOffset);
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

        delegate void SetImageSourceDelegate(BitmapImage b);
        private void SetImageSource(BitmapImage b)
        {
            SetImageSourceDelegate simd = delegate (BitmapImage bim)
            {
                img_Feedback_Icon.Source = bim;
            };
            img_Feedback_Icon.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, simd, b);
        }

        delegate void SetLabelContentDelegate(string s);
        private void SetLabelContent(string s)
        {
            SetLabelContentDelegate slcd = delegate (string instr)
            {
                label_Feedback_Icon.Content = instr;
            };
            label_Feedback_Icon.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, slcd, s);
        }

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
        }

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
        }

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
        }

        private void MenuItem_OnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFeedbackSchedule();
        }

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

        private void MenuItem_ShowSalsaSteps_Click(object sender, RoutedEventArgs e)
        {
            return;
        }
        
        private void MenuItem_Side_Click(object sender, RoutedEventArgs e)
        {
            mi_Side.IsEnabled = false;
            mi_Straight.IsEnabled = true;
            referenceMovement = new Vector3D(-1.0, 0.0, 0.0);
        }

        private void MenuItem_Straight_Click(object sender, RoutedEventArgs e)
        {
            mi_Side.IsEnabled = true;
            mi_Straight.IsEnabled = false;
            referenceMovement = new Vector3D(0.0, 0.0, -1.0);
        }

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
            //ofdg.ShowDialog();
            if (ofdg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SalsaDashboard timeline = new SalsaDashboard(ofdg.FileName);
                timeline.Show();
            }
        }
    }
}
