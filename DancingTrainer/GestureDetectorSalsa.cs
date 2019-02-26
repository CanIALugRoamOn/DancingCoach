using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DancingTrainer
{
    class GestureDetectorSalsa
    {
        /// <summary> Path to the gesture database that was trained with VGB </summary>
        private readonly string gestureDatabase = @"vgbDatabase\basicSalsaSteps.gbd";

        /// <summary> Name of the gestures in the database that we want to track </summary>
        //private readonly string forthAndBackProgressLeft = "ForthAndBackProgress_Left";

        /// <summary> Gesture frame source which should be tied to a body tracking ID </summary>
        private VisualGestureBuilderFrameSource vgbFrameSource = null;

        /// <summary> Gesture frame reader which will handle gesture events coming from the sensor </summary>
        // added readonly ???
        private readonly VisualGestureBuilderFrameReader vgbFrameReader = null;

        public int currentSalsaBeatCounter = 8;
        public double timePassed = 0;
        public List<(double, int)> timePassedList = new List<(double, int)>();
        public string currentSalsaState = "";
        private List<string> salsaStates = new List<string>();
        public Dictionary<String, float> gestureValues;
        public bool gestureToTheBeat;
        SalsaWindow salWin;
        BeatManager beatMan;
        private bool condition = false;
        public bool IsGestureToTheBeat { get; private set; } = false;
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
        public GestureDetectorSalsa(SalsaWindow sw,  KinectSensor kinectSensor, BeatManager bm)
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

            //BM.InitToTheBeatTimer();
            //BM.toTheBeat.Elapsed += ToTheBeat_Elapsed;
        }

        private void ToTheBeat_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!condition)
            {
                return;
            }
            // check if the gesture happend to the beat
            int index = timePassedList.Count - 1;
            int currentBeat = beatMan.beatCounter;
            gestureToTheBeat = false;
            while(timePassedList[index].Item1 <= currentBeat * beatMan.MSPB + 100 && timePassedList[index].Item1 >= currentBeat * beatMan.MSPB - 100)
            {
                if (timePassedList[index].Item2 == currentBeat % 8 + 1)
                {
                    // correct time and correct beat
                    gestureToTheBeat = true;
                    break;
                }
                else
                {
                    if (index > 0)
                    {
                        index--;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        //delegate void WriteOnUi2(float f);
        delegate void WriteOnUi((string, float) t);

        //private void WriteSalsaProgress(float value)
        //{
        //    WriteOnUi2 woui = delegate (float content)
        //    {
        //        salWin.progbar_ForthAndBackProgress_Left.Value = content;
        //        salWin.label_FAndBProgress_Left.Content = "FandBProgress: " + content.ToString();
        //    };
        //    salWin.label_BeatCounter.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, woui, value);
        //}

        //private void WriteFootTapLeft(float value)
        //{
        //    WriteOnUi2 woui = delegate (float content)
        //    {
        //        salWin.progbar_FootTap_Left.Value = content;
        //        salWin.label_FootTap_Left.Content = "FootTap_Left: " + content.ToString();
        //    };
        //    salWin.label_BeatCounter.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, woui, value);
        //}

        //private void WriteFootTapRight(float value)
        //{
        //    WriteOnUi2 woui = delegate (float content)
        //    {
        //        salWin.progbar_FootTap_Right.Value = content;
        //        salWin.label_FootTap_Right.Content = "FootTap_Right: " + content.ToString();
        //    };
        //    salWin.label_BeatCounter.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, woui, value);
        //}

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
                            Console.WriteLine(gesture.Name);
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
            timePassed = beatMan.stopWatch.Elapsed.TotalMilliseconds - beatMan.timerStopwatchOffset;
            double beatsTimeDiff = timePassed - beatMan.beatCounter * beatMan.MSPB;
            // 250 ms delay or maybe 100 ms ???
            // problems if MSPB <= 250 but this is too fast
            // 240 BPM correspond to 250 MSPB
            // dancing based on the reaction to the UI becomes "impossible"

            // look if the beat is the same
            // 3 cases: too early, perfect, too late
            // too early: currentSalsaBeatCounter = beatCounter + 1 and timeDiff >= MSPB - 250
            if (beatMan.beatCounter % 8 + 1 == currentSalsaBeatCounter & beatsTimeDiff >= beatMan.MSPB-250 & beatsTimeDiff < beatMan.MSPB)
            {
                // to the beat
                IsGestureToTheBeat = true;
            }           
            else
            {
                IsGestureToTheBeat = false;
                // perfect: currentSalsaBeatCounter = beatCounter and timeDiff = 0
                // too late: currentSalsaBeatCounter = beatCounter and timeDiff <= 250
                if (beatMan.beatCounter % 8 == currentSalsaBeatCounter & beatsTimeDiff >= 0 & beatsTimeDiff <= 250)
                {
                    // to the beat
                    IsGestureToTheBeat = true;
                }                
            }
            //if (timePassedList.Count <= 0)
            //{
            //    timePassedList.Add(((BM.stopWatch.Elapsed.TotalMilliseconds - BM.timerStopwatchOffset), currentSalsaBeatCounter));
            //}
            //if (temp != currentSalsaBeatCounter)
            //{
            //    timePassedList.Add(((BM.stopWatch.Elapsed.TotalMilliseconds - BM.timerStopwatchOffset), currentSalsaBeatCounter));
            //    condition = true;
            //}
            //timePassed = timePassed + BM.stopWatch.Elapsed.TotalMilliseconds;
        }

        private void DetectSideSteps()
        {
            if (gestureValues["SideStepProgress_Left"] >= 0.95)
            {
                if (gestureValues["SideFootTapping_Right"] >= 0.15 && currentSalsaBeatCounter == 1)
                {
                    currentSalsaBeatCounter = 2;
                    currentSalsaState = "right tap";
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
                    currentSalsaState = "left tap";
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

        private void DetectForthAndBackSteps()
        {
            if (gestureValues["ForthAndBackProgress_Left"] >= 0.95)
            {
                if (gestureValues["FootTapping_Right"] >= 0.6 && currentSalsaBeatCounter == 1)
                {
                    currentSalsaBeatCounter = 2;
                    currentSalsaState = "right tap";
                }
                if (currentSalsaBeatCounter == 8)
                {
                    currentSalsaBeatCounter = 1;
                }
            }
            if (gestureValues["ForthAndBackProgress_Left"] <= 0.05)
            {
                if (gestureValues["FootTapping_Left"] >= 0.05 && currentSalsaBeatCounter == 5)
                {
                    currentSalsaBeatCounter = 6;
                    currentSalsaState = "left tap";
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
    }
}
