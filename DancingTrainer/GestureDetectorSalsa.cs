using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;

namespace DancingTrainer
{
    class GestureDetectorSalsa
    {
        /// <summary> Path to the gesture database that was trained with VGB </summary>
        private readonly string gestureDatabase = @"vgbDatabase\basicSalsaSteps.gbd";

        /// <summary> Gesture frame source which should be tied to a body tracking ID </summary>
        private VisualGestureBuilderFrameSource vgbFrameSource = null;

        /// <summary> Gesture frame reader which will handle gesture events coming from the sensor </summary>
        private readonly VisualGestureBuilderFrameReader vgbFrameReader = null;

        /// <summary>
        /// Reference to the step of the user.
        /// </summary>
        public int currentSalsaBeatCounter = 8;

        /// <summary>
        /// Total time passed in ms.
        /// </summary>
        public double timePassed = 0;

        /// <summary>
        /// Holds the values of the gestures.
        /// </summary>
        public Dictionary<String, float> gestureValues;

        /// <summary>
        /// Reference to the SalsaWindow
        /// </summary>
        SalsaWindow salWin;

        /// <summary>
        /// Reference to the SalsaBeatManager
        /// </summary>
        SalsaBeatManager beatMan;

        /// <summary>
        /// Bool if the gesture is to the beat or not.
        /// </summary>
        public bool IsGestureToTheBeat { get; private set; } = false;

        /// <summary>
        /// If a step is recognized to long without changing it gets resetted to the most possible step the user performs
        /// </summary>
        public Timer resetTimer;

        /// <summary>
        /// Bool to train basic forth and back steps.
        /// </summary>
        private bool straightSteps;

        /// <summary>
        /// Bool to train basic side steps.
        /// </summary>
        private bool sideSteps;

        /// <summary>
        /// Delegate to write the progress and confidence of the salsa step recognition.
        /// </summary>
        /// <param name="t">(string, float)</param>
        delegate void WriteOnUi((string, float) t);

        /// <summary>
        /// TrackingId of the Body
        /// </summary>
        public ulong TrackingId
        {
            get
            {
                return this.vgbFrameSource.TrackingId;
            }

            set
            {
                if (this.vgbFrameSource.TrackingId != value)
                {
                    this.vgbFrameSource.TrackingId = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the detector is currently paused
        /// If the body tracking ID associated with the detector is not valid, then the detector should be paused
        /// </summary>
        public bool IsPaused
        {
            get
            {
                return this.vgbFrameReader.IsPaused;
            }

            set
            {
                if (this.vgbFrameReader.IsPaused != value)
                {
                    this.vgbFrameReader.IsPaused = value;
                }
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sw">SalsaWindow</param>
        /// <param name="kinectSensor">KinectSensor</param>
        /// <param name="bm">SalsaBeatManager</param>
        public GestureDetectorSalsa(SalsaWindow sw,  KinectSensor kinectSensor, SalsaBeatManager bm)
        {
            salWin = sw;
            beatMan = bm;
            gestureValues = new Dictionary<string, float>
            {
                ["ForthAndBackProgress_Left"] = 0,
                ["FootTapping_Left"] = 0,
                ["FootTapping_Right"] = 0
            };

            if (kinectSensor == null)
            {
                throw new ArgumentNullException("kinectSensor");
            }

            // create the vgb source. The associated body tracking ID will be set when a valid body frame arrives from the sensor.
            this.vgbFrameSource = new VisualGestureBuilderFrameSource(kinectSensor, 0);
            //this.vgbFrameSource.TrackingIdLost += this.Source_TrackingIdLost;

            // open the reader for the vgb frames
            this.vgbFrameReader = this.vgbFrameSource.OpenReader();
            if (this.vgbFrameReader != null)
            {
                this.vgbFrameReader.IsPaused = true;
                this.vgbFrameReader.FrameArrived += this.Reader_GestureFrameArrived;
            }

            // load the all gestures from the gesture database
            using (VisualGestureBuilderDatabase database = new VisualGestureBuilderDatabase(this.gestureDatabase))
            {
                // we could load all available gestures in the database with a call to vgbFrameSource.AddGestures(database.AvailableGestures), 
                // but for this program, we only want to track one discrete gesture from the database, so we'll load it by name
                vgbFrameSource.AddGestures(database.AvailableGestures);                
            }

            resetTimer = new Timer() { Interval = beatMan.MSPB * 5, AutoReset=false };
            resetTimer.Elapsed += ResetTimer_Elapsed;

            straightSteps = !salWin.mi_Straight.IsEnabled;
            sideSteps = !salWin.mi_Side.IsEnabled;
        }

        /// <summary>
        /// Event to reset the recognition of the user's salsa steps
        /// </summary>
        /// <param name="sender">Object</param>
        /// <param name="e">ElapsedEventArgs</param>
        private void ResetTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Heureka !!!");
            // forth and back
            if (straightSteps && !sideSteps)
            {
                if (gestureValues["ForthAndBackProgress_Left"] >= 0.95)
                {
                    currentSalsaBeatCounter = 1;
                    if (gestureValues["FootTapping_Right"] >= 0.6)
                    {
                        currentSalsaBeatCounter = 2;
                    }                   
                }
                if (gestureValues["ForthAndBackProgress_Left"] <= 0.05)
                {
                    currentSalsaBeatCounter = 5;
                    if (gestureValues["FootTapping_Left"] >= 0.035)
                    {
                        currentSalsaBeatCounter = 6;
                    }
                }
                if (gestureValues["ForthAndBackProgress_Left"] >= 0.45 && gestureValues["ForthAndBackProgress_Left"] <= 0.55)
                {
                    if (currentSalsaBeatCounter > 4 && currentSalsaBeatCounter <= 8)
                    {
                        currentSalsaBeatCounter = 8;
                    }
                    else
                    {
                        currentSalsaBeatCounter = 4;
                    }
                }
                
            }
            // side steps
            if (sideSteps && !straightSteps)
            {
                if (gestureValues["SideStepProgress_Left"] >= 0.95)
                {
                    currentSalsaBeatCounter = 1;
                    if (gestureValues["SideFootTapping_Right"] >= 0.15)
                    {
                        currentSalsaBeatCounter = 2;
                    }
                }
                if (gestureValues["SideStepProgress_Left"] <= 0.05)
                {
                    currentSalsaBeatCounter = 5;
                    if (gestureValues["SideFootTapping_Left"] >= 0.7)
                    {
                        currentSalsaBeatCounter = 6;
                    }
                }
                if (gestureValues["SideStepProgress_Left"] >= 0.45 && gestureValues["SideStepProgress_Left"] <= 0.55)
                {
                    if (currentSalsaBeatCounter > 4 && currentSalsaBeatCounter <= 8)
                    {
                        currentSalsaBeatCounter = 8;
                    }
                    else
                    {
                        currentSalsaBeatCounter = 4;
                    }
                }
            }
        }

        /// <summary>
        /// Writes the progress/confidence of a gesture to the UI.
        /// </summary>
        /// <param name="tuple">(string,float)</param>
        private void WriteGestureOnUi((string,float) tuple)
        {
            WriteOnUi woui = delegate ((string, float) content)
            {
                switch (content.Item1)
                {
                    case "ForthAndBackProgress_Left":
                        salWin.progbar_ForthAndBackProgress_Left.Value = content.Item2;
                        salWin.label_FAndBProgress_Left.Content = "FandBProgress: " + content.Item2.ToString();
                        break;
                    case "FootTapping_Left":
                        salWin.progbar_FootTap_Left.Value = content.Item2;
                        salWin.label_FootTap_Left.Content = "FootTap_Left: " + content.Item2.ToString();
                        break;
                    case "FootTapping_Right":
                        salWin.progbar_FootTap_Right.Value = content.Item2;
                        salWin.label_FootTap_Right.Content = "FootTap_Right: " + content.Item2.ToString();
                        break;
                }
            };
            salWin.label_BeatCounter.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, woui, tuple);
        }

        /// <summary>
        /// Event to read the gesture frame and detect gestures.
        /// </summary>
        /// <param name="sender">Object</param>
        /// <param name="e">VisualGesturebuilderFrameArrivedEventArgs</param>
        private void Reader_GestureFrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
        {
            int temp = currentSalsaBeatCounter;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            VisualGestureBuilderFrameReference frameReference = e.FrameReference;
            using (VisualGestureBuilderFrame frame = frameReference.AcquireFrame())
            {                
                if (frame != null)
                {
                    // get the discrete gesture results which arrived with the latest frame
                    IReadOnlyDictionary<Gesture, DiscreteGestureResult> discreteResults = frame.DiscreteGestureResults;

                    // get the continous gesture results which arrived with the latest frame
                    IReadOnlyDictionary<Gesture, ContinuousGestureResult> continousResults = frame.ContinuousGestureResults;

                    if (continousResults != null)
                    {
                        foreach (Gesture gesture in this.vgbFrameSource.Gestures)
                        {
                            //float val = gesture.GestureType == GestureType.Continuous ? continousResults[gesture].Progress : discreteResults[gesture].Confidence;
                            //Console.WriteLine(gesture.Name + ": " + val.ToString());
                            //Console.WriteLine(gesture.Name);
                            if (gesture.GestureType == GestureType.Continuous)
                            {
                                float progress = continousResults[gesture].Progress;
                                gestureValues[gesture.Name] = progress;
                                WriteGestureOnUi((gesture.Name, (float)Math.Round(progress, 2)));
                            }
                            if(gesture.GestureType == GestureType.Discrete)
                            {
                                float confidence = discreteResults[gesture].Confidence;
                                gestureValues[gesture.Name] = confidence;
                                WriteGestureOnUi((gesture.Name,(float)Math.Round(confidence, 2)));
                            }
                        }
                        
                        if (!salWin.mi_Straight.IsEnabled)
                        {
                            // gesture detection for forth and back
                            DetectForthAndBackSteps();
                        }
                        if (!salWin.mi_Side.IsEnabled)
                        {
                            DetectSideSteps();
                        }
                    }
                }               
            }
            stopWatch.Stop();
            if (salWin.mode == "normal")
            {
                // do not lose extra time on the stopwatch
                if (currentSalsaBeatCounter == beatMan.BeatCounter)
                {
                    ResetTimer(resetTimer);
                }
                else
                {
                    if (!resetTimer.Enabled)
                    {
                        resetTimer.Start();
                    }
                }
            }
            

            timePassed = beatMan.StopWatch.Elapsed.TotalMilliseconds - beatMan.timerStopwatchOffset;
            double beatsTimeDiff = timePassed - beatMan.BeatCounter * beatMan.MSPB;
            // 250 ms delay or maybe 100 ms ???
            // problems if MSPB <= 250 but this is too fast
            // 240 BPM correspond to 250 MSPB
            // dancing based on the reaction to the UI becomes "impossible"

            // look if the beat is the same
            // 3 cases: too early, perfect, too late
            // too early: currentSalsaBeatCounter = beatCounter + 1 and timeDiff >= MSPB - 250
            if (beatMan.BeatCounter % 8 + 1 == currentSalsaBeatCounter & beatsTimeDiff >= beatMan.MSPB-250 & beatsTimeDiff < beatMan.MSPB)
            {
                // to the beat
                IsGestureToTheBeat = true;
                float a = gestureValues["ForthAndBackProgress_Left"];
                float b = gestureValues["FootTapping_Left"];
                float c = gestureValues["FootTapping_Right"];
                float d = (float)(beatMan.BeatCounter % 8 + 1);
                float f = (float)this.currentSalsaBeatCounter;
                float[] data = new float[] { a, b, c, d, f };
                Console.WriteLine(string.Join(",", data));
                salWin.gestureValuesList.Add(string.Join("\t", data));
            }           
            else
            {
                IsGestureToTheBeat = false;
                // perfect: currentSalsaBeatCounter = beatCounter and timeDiff = 0
                // too late: currentSalsaBeatCounter = beatCounter and timeDiff <= 250
                if (beatMan.BeatCounter % 8 == currentSalsaBeatCounter & beatsTimeDiff >= 0 & beatsTimeDiff <= 250)
                {
                    // to the beat
                    IsGestureToTheBeat = true;
                    float a = gestureValues["ForthAndBackProgress_Left"];
                    float b = gestureValues["FootTapping_Left"];
                    float c = gestureValues["FootTapping_Right"];
                    float d = (float)(beatMan.BeatCounter % 8 + 1);
                    float f = (float)this.currentSalsaBeatCounter;
                    float[] data = new float[] { a, b, c, d, f };
                    Console.WriteLine(string.Join(",", data));
                    salWin.gestureValuesList.Add(string.Join("\t", data));
                }                
            }
        }

        /// <summary>
        /// Detects side steps for salsa
        /// </summary>
        private void DetectSideSteps()
        {
            if (gestureValues["SideStepProgress_Left"] >= 0.95)
            {
                if (gestureValues["SideFootTapping_Right"] >= 0.15 && currentSalsaBeatCounter == 1)
                {
                    currentSalsaBeatCounter = 2;
                }
                if (currentSalsaBeatCounter == 8)
                {
                    currentSalsaBeatCounter = 1;
                }
            }
            if (gestureValues["SideStepProgress_Left"] <= 0.05)
            {
                if (gestureValues["SideFootTapping_Left"] >= 0.7 && currentSalsaBeatCounter == 5)
                {
                    currentSalsaBeatCounter = 6;
                }
                if (currentSalsaBeatCounter == 4)
                {
                    currentSalsaBeatCounter = 5;
                }
            }
            if (gestureValues["SideStepProgress_Left"] >= 0.45 && gestureValues["SideStepProgress_Left"] <= 0.55)
            {
                if (currentSalsaBeatCounter == 7)
                {
                    currentSalsaBeatCounter = 8;
                }
                if (currentSalsaBeatCounter == 6)
                {
                    currentSalsaBeatCounter = 7;
                }
                if (currentSalsaBeatCounter == 3)
                {
                    currentSalsaBeatCounter = 4;
                }
                if (currentSalsaBeatCounter == 2)
                {
                    currentSalsaBeatCounter = 3;
                }
            }
        }

        /// <summary>
        /// detects forth and back steps for salsa
        /// </summary>
        private void DetectForthAndBackSteps()
        {
            if (gestureValues["ForthAndBackProgress_Left"] >= 0.95)
            {
                if (gestureValues["FootTapping_Right"] >= 0.6 && currentSalsaBeatCounter == 1)
                {
                    currentSalsaBeatCounter = 2;
                }
                if (currentSalsaBeatCounter == 8)
                {
                    currentSalsaBeatCounter = 1;
                }
            }
            if (gestureValues["ForthAndBackProgress_Left"] <= 0.05)
            {
                if (gestureValues["FootTapping_Left"] >= 0.03 && currentSalsaBeatCounter == 5)
                {
                    currentSalsaBeatCounter = 6;
                }
                if (currentSalsaBeatCounter == 4)
                {
                    currentSalsaBeatCounter = 5;
                }
            }
            if (gestureValues["ForthAndBackProgress_Left"] >= 0.45 && gestureValues["ForthAndBackProgress_Left"] <= 0.55)
            {

                if (currentSalsaBeatCounter == 7)
                {
                    currentSalsaBeatCounter = 8;
                }
                if (currentSalsaBeatCounter == 6)
                {
                    currentSalsaBeatCounter = 7;
                }
                if (currentSalsaBeatCounter == 3)
                {
                    currentSalsaBeatCounter = 4;
                }
                if (currentSalsaBeatCounter == 2)
                {
                    currentSalsaBeatCounter = 3;
                }
            }
        }

        /// <summary>
        /// Disposes the frame reader and source of the visual gesutre builder
        /// </summary>
        public void Dispose()
        {
            vgbFrameReader.Dispose();
            vgbFrameSource.Dispose();
        }

        /// <summary>
        /// Resets a timer
        /// </summary>
        /// <param name="t">Timer</param>
        public void ResetTimer(Timer t)
        {
            if (t.Enabled)
            {
                t.Stop();
                t.Start();
            }
        }
    }
}
