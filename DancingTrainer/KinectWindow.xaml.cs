using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DancingTrainer
{
    /// <summary>
    /// Interaction logic for KinectWindows.xaml
    /// </summary>
    public partial class KinectWindow : UserControl
    {

 
        public KinectSensor kinectSensor { get; private set; }
        public InfraredFrameReader frameReader = null;

        public DepthFrameReader depthFrameReader { get; private set; }

        public FaceFrameHandler faceFrameHandler;
        public VolumeHandler volumeHandler;

        /// <summary>
        /// Reader for color frames
        /// </summary>
        public ColorFrameReader colorFrameReader { get; private set; } = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap colorBitmap = null;

        public BodyFrameHandler bodyFrameHandler;

        //public ConnectorHub.ConnectorHub myConectorHub;
        
        // default is false
        public bool isRecording = false;

        public KinectWindow()
        {
            InitializeComponent();
            
            this.kinectSensor = KinectSensor.GetDefault();
            
            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
            
            // wire handler for frame arrival
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;
            
            // create the colorFrameDescription from the ColorFrameSource using Bgra format
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            // create the bitmap to display
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);


            this.kinectSensor.Open();
            this.frameReader = this.kinectSensor.InfraredFrameSource.OpenReader();

            bodyFrameHandler = new BodyFrameHandler(this.kinectSensor, this);
            faceFrameHandler = new FaceFrameHandler(this.kinectSensor);
            volumeHandler = new VolumeHandler(this.kinectSensor);


            try
            {
                //myConectorHub = new ConnectorHub.ConnectorHub();

                //myConectorHub.init();
                //myConectorHub.sendReady();
                setValuesNames();
                //myConectorHub.startRecordingEvent += MyConectorHub_startRecordingEvent;
                //myConectorHub.stopRecordingEvent += MyConectorHub_stopRecordingEvent;
                


            }
            catch (Exception e)
            {
                int x = 1;
            }

            //this.Closing += MainWindow_Closing1;
        }
        

        private void MyConectorHub_stopRecordingEvent(object sender)
        {
            isRecording = false;
        }

        private void MyConectorHub_startRecordingEvent(object sender)
        {
            isRecording = true;
        }

        // Gianluca add this to start recording from the Dancing Trainer
        private void DancingTrainer_PlayEvent(object sender)
        {
            isRecording = true;
        }

        private void DancingTrainer_StopEvent(object sender)
        {
            isRecording = false;
        }

        #region setValueNames
        private void setValuesNames()
        {
            //justforTestNames = new List<string>();
            List<string> names = new List<string>();
            string temp;

            temp = "Volume";
            names.Add(temp);

            temp = "0_Engaged";
            names.Add(temp);
            temp = "0_Happy";
            names.Add(temp);
            temp = "0_LeftEyeClosed";
            names.Add(temp);
            temp = "0_LookingAway";
            names.Add(temp);
            temp = "0_MouthOpen";
            names.Add(temp);
            temp = "0_MouthMoved";
            names.Add(temp);
            temp = "0_RightEyeClosed";
            names.Add(temp);
            temp = "0_WearingGlasses";
            names.Add(temp);


            temp = "0AnkleRight_X";
            names.Add(temp);
            temp = "0_AnkleRight_Y";
            names.Add(temp);
            temp = "0_AnkleRight_Z";
            names.Add(temp);
            temp = "0_AnkleLeft_X";
            names.Add(temp);
            temp = "0_AnkleLeft_Y";
            names.Add(temp);
            temp = "0_AnkleLeft_Z";
            names.Add(temp);
            temp = "0_ElbowRight_X";
            names.Add(temp);
            temp = "0_ElbowRight_Y";
            names.Add(temp);
            temp = "0_ElbowRight_Z";
            names.Add(temp);
            temp = "0_ElbowLeft_X";
            names.Add(temp);
            temp = "0_ElbowLeft_Y";
            names.Add(temp);
            temp = "0_ElbowLeft_Z";
            names.Add(temp);
            temp = "0_HandRight_X";
            names.Add(temp);
            temp = "0_HandRight_Y";
            names.Add(temp);
            temp = "0_HandRight_Z";
            names.Add(temp);
            temp = "0_HandLeft_X";
            names.Add(temp);
            temp = "0_HandLeft_Y";
            names.Add(temp);
            temp = "0_HandLeft_Z";
            names.Add(temp);
            temp = "0_HandRightTip_X";
            names.Add(temp);
            temp = "0_HandRightTip_Y";
            names.Add(temp);
            temp = "0_HandRightTip_Z";
            names.Add(temp);
            temp = "0_HandLeftTip_X";
            names.Add(temp);
            temp = "0_HandLeftTip_Y";
            names.Add(temp);
            temp = "0_HandLeftTip_Z";
            names.Add(temp);
            temp = "0_Head_X";
            names.Add(temp);
            temp = "0_Head_Y";
            names.Add(temp);
            temp = "0_Head_Z";
            names.Add(temp);
            temp = "0_HipRight_X";
            names.Add(temp);
            temp = "0_HipRight_Y";
            names.Add(temp);
            temp = "0_HipRight_Z";
            names.Add(temp);
            temp = "0_HipLeft_X";
            names.Add(temp);
            temp = "0_HipLeft_Y";
            names.Add(temp);
            temp = "0_HipLeft_Z";
            names.Add(temp);
            temp = "0_ShoulderRight_X";
            names.Add(temp);
            temp = "0_ShoulderRight_Y";
            names.Add(temp);
            temp = "0_ShoulderRight_Z";
            names.Add(temp);
            temp = "0_ShoulderLeft_X";
            names.Add(temp);
            temp = "0_ShoulderLeft_Y";
            names.Add(temp);
            temp = "0_ShoulderLeft_Z";
            names.Add(temp);
            temp = "0_SpineMid_X";
            names.Add(temp);
            temp = "0_SpineMid_Y";
            names.Add(temp);
            temp = "0_SpineMid_Z";
            names.Add(temp);
            temp = "0_SpineShoulder_X";
            names.Add(temp);
            temp = "0_SpineShoulder_Y";
            names.Add(temp);
            temp = "0_SpineShoulder_Z";
            names.Add(temp);


            temp = "1_Engaged";
            names.Add(temp);
            temp = "1_Happy";
            names.Add(temp);
            temp = "1_LeftEyeClosed";
            names.Add(temp);
            temp = "1_LookingAwa_Y";
            names.Add(temp);
            temp = "1_MouthOpen";
            names.Add(temp);
            temp = "1_MouthMoved";
            names.Add(temp);
            temp = "1_RightEyeClosed";
            names.Add(temp);
            temp = "1_WearingGlasses";
            names.Add(temp);

            temp = "1_AnkleRight_X";
            names.Add(temp);
            temp = "1_AnkleRight_Y";
            names.Add(temp);
            temp = "1_AnkleRight_Z";
            names.Add(temp);
            temp = "1_AnkleLeft_X";
            names.Add(temp);
            temp = "1_AnkleLeft_Y";
            names.Add(temp);
            temp = "1_AnkleLeft_Z";
            names.Add(temp);
            temp = "1_ElbowRight_X";
            names.Add(temp);
            temp = "1_ElbowRight_Y";
            names.Add(temp);
            temp = "1_ElbowRight_Z";
            names.Add(temp);
            temp = "1_ElbowLeft_X";
            names.Add(temp);
            temp = "1_ElbowLeft_Y";
            names.Add(temp);
            temp = "1_ElbowLeft_Z";
            names.Add(temp);
            temp = "1_HandRight_X";
            names.Add(temp);
            temp = "1_HandRight_Y";
            names.Add(temp);
            temp = "1_HandRight_Z";
            names.Add(temp);
            temp = "1_HandLeft_X";
            names.Add(temp);
            temp = "1_HandLeft_Y";
            names.Add(temp);
            temp = "1_HandLeft_Z";
            names.Add(temp);
            temp = "1_HandRightTip_X";
            names.Add(temp);
            temp = "1_HandRightTip_Y";
            names.Add(temp);
            temp = "1_HandRightTip_Z";
            names.Add(temp);
            temp = "1_HandLeftTip_X";
            names.Add(temp);
            temp = "1_HandLeftTip_Y";
            names.Add(temp);
            temp = "1_HandLeftTip_Z";
            names.Add(temp);
            temp = "1_Head_X";
            names.Add(temp);
            temp = "1_Head_Y";
            names.Add(temp);
            temp = "1_Head_Z";
            names.Add(temp);
            temp = "1_HipRight_X";
            names.Add(temp);
            temp = "1_HipRight_Y";
            names.Add(temp);
            temp = "1_HipRight_Z";
            names.Add(temp);
            temp = "1_HipLeft_X";
            names.Add(temp);
            temp = "1_HipLeft_Y";
            names.Add(temp);
            temp = "1_HipLeft_Z";
            names.Add(temp);
            temp = "1_ShoulderRight_X";
            names.Add(temp);
            temp = "1_ShoulderRight_Y";
            names.Add(temp);
            temp = "1_ShoulderRight_Z";
            names.Add(temp);
            temp = "1_ShoulderLeft_X";
            names.Add(temp);
            temp = "1_ShoulderLeft_Y";
            names.Add(temp);
            temp = "1_ShoulderLeft_Z";
            names.Add(temp);
            temp = "1_SpineMid_X";
            names.Add(temp);
            temp = "1_SpineMid_Y";
            names.Add(temp);
            temp = "1_SpineMid_Z";
            names.Add(temp);
            temp = "1_SpineShoulder_X";
            names.Add(temp);
            temp = "1_SpineShoulder_Y";
            names.Add(temp);
            temp = "1_SpineShoulder_Z";
            names.Add(temp);

            temp = "2_Engaged";
            names.Add(temp);
            temp = "2_happy";
            names.Add(temp);
            temp = "2_LeftEyeClosed";
            names.Add(temp);
            temp = "2_LookingAwa_Y";
            names.Add(temp);
            temp = "2_MouthOpen";
            names.Add(temp);
            temp = "2_MouthMoved";
            names.Add(temp);
            temp = "2_RightEyeClosed";
            names.Add(temp);
            temp = "2_WearingGlasses";
            names.Add(temp);

            temp = "2_AnkleRight_X";
            names.Add(temp);
            temp = "2_AnkleRight_Y";
            names.Add(temp);
            temp = "2_AnkleRight_Z";
            names.Add(temp);
            temp = "2_AnkleLeft_X";
            names.Add(temp);
            temp = "2_AnkleLeft_Y";
            names.Add(temp);
            temp = "2_AnkleLeft_Z";
            names.Add(temp);
            temp = "2_ElbowRight_X";
            names.Add(temp);
            temp = "2_ElbowRight_Y";
            names.Add(temp);
            temp = "2_ElbowRight_Z";
            names.Add(temp);
            temp = "2_ElbowLeft_X";
            names.Add(temp);
            temp = "2_ElbowLeft_Y";
            names.Add(temp);
            temp = "2_ElbowLeft_Z";
            names.Add(temp);
            temp = "2_HandRight_X";
            names.Add(temp);
            temp = "2_HandRight_Y";
            names.Add(temp);
            temp = "2_HandRight_Z";
            names.Add(temp);
            temp = "2_HandLeft_X";
            names.Add(temp);
            temp = "2_HandLeft_Y";
            names.Add(temp);
            temp = "2_HandLeft_Z";
            names.Add(temp);
            temp = "2_HandRightTip_X";
            names.Add(temp);
            temp = "2_HandRightTip_Y";
            names.Add(temp);
            temp = "2_HandRightTip_Z";
            names.Add(temp);
            temp = "2_HandLeftTip_X";
            names.Add(temp);
            temp = "2_HandLeftTip_Y";
            names.Add(temp);
            temp = "2_HandLeftTip_Z";
            names.Add(temp);
            temp = "2_Head_X";
            names.Add(temp);
            temp = "2_Head_Y";
            names.Add(temp);
            temp = "2_Head_Z";
            names.Add(temp);
            temp = "2_HipRight_X";
            names.Add(temp);
            temp = "2_HipRight_Y";
            names.Add(temp);
            temp = "2_HipRight_Z";
            names.Add(temp);
            temp = "2_HipLeft_X";
            names.Add(temp);
            temp = "2_HipLeft_Y";
            names.Add(temp);
            temp = "2_HipLeft_Z";
            names.Add(temp);
            temp = "2_ShoulderRight_X";
            names.Add(temp);
            temp = "2_ShoulderRight_Y";
            names.Add(temp);
            temp = "2_ShoulderRight_Z";
            names.Add(temp);
            temp = "2_ShoulderLeft_X";
            names.Add(temp);
            temp = "2_ShoulderLeft_Y";
            names.Add(temp);
            temp = "2_ShoulderLeft_Z";
            names.Add(temp);
            temp = "2_SpineMid_X";
            names.Add(temp);
            temp = "2_SpineMid_Y";
            names.Add(temp);
            temp = "2_SpineMid_Z";
            names.Add(temp);
            temp = "2_SpineShoulder_X";
            names.Add(temp);
            temp = "2_SpineShoulder_Y";
            names.Add(temp);
            temp = "2_SpineShoulder_Z";
            names.Add(temp);

            temp = "3_Engaged";
            names.Add(temp);
            temp = "3_happy";
            names.Add(temp);
            temp = "3_LeftEyeClosed";
            names.Add(temp);
            temp = "3_LookingAwa_Y";
            names.Add(temp);
            temp = "3_MouthOpen";
            names.Add(temp);
            temp = "3_MouthMoved";
            names.Add(temp);
            temp = "3_RightEyeClosed";
            names.Add(temp);
            temp = "3_WearingGlasses";
            names.Add(temp);

            temp = "3_AnkleRight_X";
            names.Add(temp);
            temp = "3_AnkleRight_Y";
            names.Add(temp);
            temp = "3_AnkleRight_Z";
            names.Add(temp);
            temp = "3_AnkleLeft_X";
            names.Add(temp);
            temp = "3_AnkleLeft_Y";
            names.Add(temp);
            temp = "3_AnkleLeft_Z";
            names.Add(temp);
            temp = "3_ElbowRight_X";
            names.Add(temp);
            temp = "3_ElbowRight_Y";
            names.Add(temp);
            temp = "3_ElbowRight_Z";
            names.Add(temp);
            temp = "3_ElbowLeft_X";
            names.Add(temp);
            temp = "3_ElbowLeft_Y";
            names.Add(temp);
            temp = "3_ElbowLeft_Z";
            names.Add(temp);
            temp = "3_HandRight_X";
            names.Add(temp);
            temp = "3_HandRight_Y";
            names.Add(temp);
            temp = "3_HandRight_Z";
            names.Add(temp);
            temp = "3_HandLeft_X";
            names.Add(temp);
            temp = "3_HandLeft_Y";
            names.Add(temp);
            temp = "3_HandLeft_Z";
            names.Add(temp);
            temp = "3_HandRightTip_X";
            names.Add(temp);
            temp = "3_HandRightTip_Y";
            names.Add(temp);
            temp = "3_HandRightTip_Z";
            names.Add(temp);
            temp = "3_HandLeftTip_X";
            names.Add(temp);
            temp = "3_HandLeftTip_Y";
            names.Add(temp);
            temp = "3_HandLeftTip_Z";
            names.Add(temp);
            temp = "3_Head_X";
            names.Add(temp);
            temp = "3_Head_Y";
            names.Add(temp);
            temp = "3_Head_Z";
            names.Add(temp);
            temp = "3_HipRight_X";
            names.Add(temp);
            temp = "3_HipRight_Y";
            names.Add(temp);
            temp = "3_HipRight_Z";
            names.Add(temp);
            temp = "3_HipLeft_X";
            names.Add(temp);
            temp = "3_HipLeft_Y";
            names.Add(temp);
            temp = "3_HipLeft_Z";
            names.Add(temp);
            temp = "3_ShoulderRight_X";
            names.Add(temp);
            temp = "3_ShoulderRight_Y";
            names.Add(temp);
            temp = "3_ShoulderRight_Z";
            names.Add(temp);
            temp = "3_ShoulderLeft_X";
            names.Add(temp);
            temp = "3_ShoulderLeft_Y";
            names.Add(temp);
            temp = "3_ShoulderLeft_Z";
            names.Add(temp);
            temp = "3_SpineMid_X";
            names.Add(temp);
            temp = "3_SpineMid_Y";
            names.Add(temp);
            temp = "3_SpineMid_Z";
            names.Add(temp);
            temp = "3_SpineShoulder_X";
            names.Add(temp);
            temp = "3_SpineShoulder_Y";
            names.Add(temp);
            temp = "3_SpineShoulder_Z";
            names.Add(temp);

            temp = "4_Engaged";
            names.Add(temp);
            temp = "4_happy";
            names.Add(temp);
            temp = "4_LeftEyeClosed";
            names.Add(temp);
            temp = "4_LookingAwa_Y";
            names.Add(temp);
            temp = "4_MouthOpen";
            names.Add(temp);
            temp = "4_MouthMoved";
            names.Add(temp);
            temp = "4_RightEyeClosed";
            names.Add(temp);
            temp = "4_WearingGlasses";
            names.Add(temp);

            temp = "4_AnkleRight_X";
            names.Add(temp);
            temp = "4_AnkleRight_Y";
            names.Add(temp);
            temp = "4_AnkleRight_Z";
            names.Add(temp);
            temp = "4_AnkleLeft_X";
            names.Add(temp);
            temp = "4_AnkleLeft_Y";
            names.Add(temp);
            temp = "4_AnkleLeft_Z";
            names.Add(temp);
            temp = "4_ElbowRight_X";
            names.Add(temp);
            temp = "4_ElbowRight_Y";
            names.Add(temp);
            temp = "4_ElbowRight_Z";
            names.Add(temp);
            temp = "4_ElbowLeft_X";
            names.Add(temp);
            temp = "4_ElbowLeft_Y";
            names.Add(temp);
            temp = "4_ElbowLeft_Z";
            names.Add(temp);
            temp = "4_HandRight_X";
            names.Add(temp);
            temp = "4_HandRight_Y";
            names.Add(temp);
            temp = "4_HandRight_Z";
            names.Add(temp);
            temp = "4_HandLeft_X";
            names.Add(temp);
            temp = "4_HandLeft_Y";
            names.Add(temp);
            temp = "4_HandLeft_Z";
            names.Add(temp);
            temp = "4_HandRightTip_X";
            names.Add(temp);
            temp = "4_HandRightTip_Y";
            names.Add(temp);
            temp = "4_HandRightTip_Z";
            names.Add(temp);
            temp = "4_HandLeftTip_X";
            names.Add(temp);
            temp = "4_HandLeftTip_Y";
            names.Add(temp);
            temp = "4_HandLeftTip_Z";
            names.Add(temp);
            temp = "4_Head_X";
            names.Add(temp);
            temp = "4_Head_Y";
            names.Add(temp);
            temp = "4_Head_Z";
            names.Add(temp);
            temp = "4_HipRight_X";
            names.Add(temp);
            temp = "4_HipRight_Y";
            names.Add(temp);
            temp = "4_HipRight_Z";
            names.Add(temp);
            temp = "4_HipLeft_X";
            names.Add(temp);
            temp = "4_HipLeft_Y";
            names.Add(temp);
            temp = "4_HipLeft_Z";
            names.Add(temp);
            temp = "4_ShoulderRight_X";
            names.Add(temp);
            temp = "4_ShoulderRight_Y";
            names.Add(temp);
            temp = "4_ShoulderRight_Z";
            names.Add(temp);
            temp = "4_ShoulderLeft_X";
            names.Add(temp);
            temp = "4_ShoulderLeft_Y";
            names.Add(temp);
            temp = "4_ShoulderLeft_Z";
            names.Add(temp);
            temp = "4_SpineMid_X";
            names.Add(temp);
            temp = "4_SpineMid_Y";
            names.Add(temp);
            temp = "4_SpineMid_Z";
            names.Add(temp);
            temp = "4_SpineShoulder_X";
            names.Add(temp);
            temp = "4_SpineShoulder_Y";
            names.Add(temp);
            temp = "4_SpineShoulder_Z";
            names.Add(temp);

            temp = "5_Engaged";
            names.Add(temp);
            temp = "5_happy";
            names.Add(temp);
            temp = "5_LeftEyeClosed";
            names.Add(temp);
            temp = "5_LookingAwa_Y";
            names.Add(temp);
            temp = "5_MouthOpen";
            names.Add(temp);
            temp = "5_MouthMoved";
            names.Add(temp);
            temp = "5_RightEyeClosed";
            names.Add(temp);
            temp = "5_WearingGlasses";
            names.Add(temp);

            temp = "5_AnkleRight_X";
            names.Add(temp);
            temp = "5_AnkleRight_Y";
            names.Add(temp);
            temp = "5_AnkleRight_Z";
            names.Add(temp);
            temp = "5_AnkleLeft_X";
            names.Add(temp);
            temp = "5_AnkleLeft_Y";
            names.Add(temp);
            temp = "5_AnkleLeft_Z";
            names.Add(temp);
            temp = "5_ElbowRight_X";
            names.Add(temp);
            temp = "5_ElbowRight_Y";
            names.Add(temp);
            temp = "5_ElbowRight_Z";
            names.Add(temp);
            temp = "5_ElbowLeft_X";
            names.Add(temp);
            temp = "5_ElbowLeft_Y";
            names.Add(temp);
            temp = "5_ElbowLeft_Z";
            names.Add(temp);
            temp = "5_HandRight_X";
            names.Add(temp);
            temp = "5_HandRight_Y";
            names.Add(temp);
            temp = "5_HandRight_Z";
            names.Add(temp);
            temp = "5_HandLeft_X";
            names.Add(temp);
            temp = "5_HandLeft_Y";
            names.Add(temp);
            temp = "5_HandLeft_Z";
            names.Add(temp);
            temp = "5_HandRightTip_X";
            names.Add(temp);
            temp = "5_HandRightTip_Y";
            names.Add(temp);
            temp = "5_HandRightTip_Z";
            names.Add(temp);
            temp = "5_HandLeftTip_X";
            names.Add(temp);
            temp = "5_HandLeftTip_Y";
            names.Add(temp);
            temp = "5_HandLeftTip_Z";
            names.Add(temp);
            temp = "5_Head_X";
            names.Add(temp);
            temp = "5_Head_Y";
            names.Add(temp);
            temp = "5_Head_Z";
            names.Add(temp);
            temp = "5_HipRight_X";
            names.Add(temp);
            temp = "5_HipRight_Y";
            names.Add(temp);
            temp = "5_HipRight_Z";
            names.Add(temp);
            temp = "5_HipLeft_X";
            names.Add(temp);
            temp = "5_HipLeft_Y";
            names.Add(temp);
            temp = "5_HipLeft_Z";
            names.Add(temp);
            temp = "5_ShoulderRight_X";
            names.Add(temp);
            temp = "5_ShoulderRight_Y";
            names.Add(temp);
            temp = "5_ShoulderRight_Z";
            names.Add(temp);
            temp = "5_ShoulderLeft_X";
            names.Add(temp);
            temp = "5_ShoulderLeft_Y";
            names.Add(temp);
            temp = "5_ShoulderLeft_Z";
            names.Add(temp);
            temp = "5_SpineMid_X";
            names.Add(temp);
            temp = "5_SpineMid_Y";
            names.Add(temp);
            temp = "5_SpineMid_Z";
            names.Add(temp);
            temp = "5_SpineShoulder_X";
            names.Add(temp);
            temp = "5_SpineShoulder_Y";
            names.Add(temp);
            temp = "5_SpineShoulder_Z";
            names.Add(temp);


            //myConectorHub.setValuesName(names);
        }
        #endregion

        public void setValues(Body[] bodies)
        {
            if (isRecording)
            {
                int counter = 0;
                List<string> values = new List<string>();
                values.Add(volumeHandler.averageVolume.ToString());

                foreach (Body body in bodies)
                {
                    try
                    {
                        values.Add(faceFrameHandler.values[counter, 0]);
                        values.Add(faceFrameHandler.values[counter, 1]);
                        values.Add(faceFrameHandler.values[counter, 2]);
                        values.Add(faceFrameHandler.values[counter, 3]);
                        values.Add(faceFrameHandler.values[counter, 4]);
                        values.Add(faceFrameHandler.values[counter, 5]);
                        values.Add(faceFrameHandler.values[counter, 6]);
                        values.Add(faceFrameHandler.values[counter, 7]);


                        values.Add(body.Joints[JointType.AnkleRight].Position.X + "");
                        values.Add(body.Joints[JointType.AnkleRight].Position.Y + "");
                        values.Add(body.Joints[JointType.AnkleRight].Position.Z + "");

                        values.Add(body.Joints[JointType.AnkleLeft].Position.X + "");
                        values.Add(body.Joints[JointType.AnkleLeft].Position.Y + "");
                        values.Add(body.Joints[JointType.AnkleLeft].Position.Z + "");

                        values.Add(body.Joints[JointType.ElbowRight].Position.X + "");
                        values.Add(body.Joints[JointType.ElbowRight].Position.Y + "");
                        values.Add(body.Joints[JointType.ElbowRight].Position.Z + "");

                        values.Add(body.Joints[JointType.ElbowLeft].Position.X + "");
                        values.Add(body.Joints[JointType.ElbowLeft].Position.Y + "");
                        values.Add(body.Joints[JointType.ElbowLeft].Position.Z + "");

                        values.Add(body.Joints[JointType.HandRight].Position.X + "");
                        values.Add(body.Joints[JointType.HandRight].Position.Y + "");
                        values.Add(body.Joints[JointType.HandRight].Position.Z + "");

                        values.Add(body.Joints[JointType.HandLeft].Position.X + "");
                        values.Add(body.Joints[JointType.HandLeft].Position.Y + "");
                        values.Add(body.Joints[JointType.HandLeft].Position.Z + "");

                        values.Add(body.Joints[JointType.HandTipRight].Position.X + "");
                        values.Add(body.Joints[JointType.HandTipRight].Position.Y + "");
                        values.Add(body.Joints[JointType.HandTipRight].Position.Z + "");

                        values.Add(body.Joints[JointType.HandTipLeft].Position.X + "");
                        values.Add(body.Joints[JointType.HandTipLeft].Position.Y + "");
                        values.Add(body.Joints[JointType.HandTipLeft].Position.Z + "");

                        values.Add(body.Joints[JointType.Head].Position.X + "");
                        values.Add(body.Joints[JointType.Head].Position.Y + "");
                        values.Add(body.Joints[JointType.Head].Position.Z + "");

                        values.Add(body.Joints[JointType.HipRight].Position.X + "");
                        values.Add(body.Joints[JointType.HipRight].Position.Y + "");
                        values.Add(body.Joints[JointType.HipRight].Position.Z + "");

                        values.Add(body.Joints[JointType.HipLeft].Position.X + "");
                        values.Add(body.Joints[JointType.HipLeft].Position.Y + "");
                        values.Add(body.Joints[JointType.HipLeft].Position.Z + "");

                        values.Add(body.Joints[JointType.ShoulderRight].Position.X + "");
                        values.Add(body.Joints[JointType.ShoulderRight].Position.Y + "");
                        values.Add(body.Joints[JointType.ShoulderRight].Position.Z + "");

                        values.Add(body.Joints[JointType.ShoulderLeft].Position.X + "");
                        values.Add(body.Joints[JointType.ShoulderLeft].Position.Y + "");
                        values.Add(body.Joints[JointType.ShoulderLeft].Position.Z + "");

                        values.Add(body.Joints[JointType.SpineMid].Position.X + "");
                        values.Add(body.Joints[JointType.SpineMid].Position.Y + "");
                        values.Add(body.Joints[JointType.SpineMid].Position.Z + "");

                        values.Add(body.Joints[JointType.SpineShoulder].Position.X + "");
                        values.Add(body.Joints[JointType.SpineShoulder].Position.Y + "");
                        values.Add(body.Joints[JointType.SpineShoulder].Position.Z + "");


                        if (body.Joints[JointType.ShoulderRight].Position.X != 0)
                        {
                            int xxx = counter;
                        }

                    }
                    catch
                    {

                    }
                    counter++;
                }
                //myConectorHub.storeFrame(values);
            }




        }


        public ImageSource ImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        


        /// <summary>
        /// Handles the color frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }

                        this.colorBitmap.Unlock();

                    }
                }

                myImage.Source = ImageSource;
            }
        }



        private void MainWindow_Closing1(object sender, CancelEventArgs e)
        {
            try
            {
                //myConectorHub.close();
            }
            catch
            {

            }
            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (volumeHandler != null)
            {
                volumeHandler.close();
            }
            if (faceFrameHandler != null)
            {
                faceFrameHandler.close();
            }
            if (bodyFrameHandler != null)
            {
                bodyFrameHandler.close();
            }
            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
            //Environment.Exit(0);

        }

        private void MainWindow_Closing(object sender)
        {
            try
            {
                //myConectorHub.close();
            }
            catch
            {

            }
            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (volumeHandler != null)
            {
                volumeHandler.close();
            }
            if (faceFrameHandler != null)
            {
                faceFrameHandler.close();
            }
            if (bodyFrameHandler != null)
            {
                bodyFrameHandler.close();
            }
            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
            Environment.Exit(0);

        }
    }
}
