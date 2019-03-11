using LightBuzz.Vitruvius;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("FootRaised.dll")]

namespace DancingTrainer
{
    class KinectManager
    {

        public KinectWindow KW;
        public SalsaBeatManager BM;

        // first tuple value indicates if left leg is raised
        // second tuple value indicates if right leg is raised
        private (bool,bool)[] wasLegRaised;

        public Body[] bodies;

        public Vector4 floorPlane { get; private set; }

        double minFootToFloorDistance = 10;

        public int filecounter = 1;

        List<string> stanceData = new List<string>();

        public KinectManager(KinectWindow kw, SalsaBeatManager bm)
        {
            KW = kw;
            BM = bm;

            // make it of the same length as body count
            // such that the indices correspond to each other
            wasLegRaised = new (bool, bool)[kw.bodyFrameHandler.bodyCount];
        }

        public void Play()
        {
            KW.isRecording = true;

            // listen to the beat
            BM.Timer.Elapsed += Timer_Elapsed;
            //double timestamp = BM.millisecondsPast;

            // if a new frame arrives then check if the leg is raised or not
            //KW.bodyFrameHandler.bodyFrameReader.FrameArrived += BodyFrameReader_FrameArrived;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // look if a step is beeing made
            Console.WriteLine("Look for a step");
            return;
        }

        private void BodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            // a new frame is arrived so check if the leg is raised or not
            // first get/set all the values of the body
            bodies = new Body[KW.bodyFrameHandler.bodyCount];
            bool dataReceived = false;
            
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);

                    // get the floor plane of the frame
                    floorPlane = bodyFrame.FloorClipPlane;

                    dataReceived = true;
                }
            }
            // avoid setting the values again by getting them from bodyframehandler???
            if (dataReceived)
            {
                KW.setValues(bodies);
                CheckIfLegIsRaised();
            }
            else
            {
                Console.WriteLine("Body data not received");
            }
                       
        }

        private void CheckIfLegIsRaised()
        {
            if (bodies == null){ return; };

            (bool, bool)[] temp = new (bool, bool)[bodies.Length];
            
            // get the position of the ankle, foot and knee
            for (int i = 0; i < bodies.Length; i++)
            {
                if (bodies[i] == null)
                {
                    Console.WriteLine("Body " + i + " is null");                   
                }
                else if (bodies[i].IsTracked)
                {

                    // check for the angle of the neck
                    Joint neck = bodies[i].Joints[JointType.Neck];
                    Joint head = bodies[i].Joints[JointType.Head];
                    Joint spineShoulder = bodies[i].Joints[JointType.SpineShoulder];
                    double neckAngle = neck.Angle(head, spineShoulder);
                    Console.WriteLine(neckAngle);                          
                }
                else
                {
                    //Console.WriteLine("Body " + i + " is not tracked.");
                    temp[i] = (false,false);
                }   
            }

            // know we can check if a step is beeing made in this frame
            CheckForStep(temp);
        }

        private double Pitch(Vector4 quaternion)
        {
            double value1 = 2.0 * (quaternion.W * quaternion.X + quaternion.Y * quaternion.Z);
            double value2 = 1.0 - 2.0 * (quaternion.X * quaternion.X + quaternion.Y * quaternion.Y);

            double roll = Math.Atan2(value1, value2);

            return roll * (180.0 / Math.PI);
        }

        private double Yaw(Vector4 quaternion)
        {
            double value = 2.0 * (quaternion.W * quaternion.Y - quaternion.Z * quaternion.X);
            value = value > 1.0 ? 1.0 : value;
            value = value < -1.0 ? -1.0 : value;

            double pitch = Math.Asin(value);

            return pitch * (180.0 / Math.PI);
        }

        private double Roll(Vector4 quaternion)
        {
            double value1 = 2.0 * (quaternion.W * quaternion.Z + quaternion.X * quaternion.Y);
            double value2 = 1.0 - 2.0 * (quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z);

            double yaw = Math.Atan2(value1, value2);

            return yaw * (180.0 / Math.PI);
        }

        private double PlanePointDistance(CameraSpacePoint position)
        {
            double numerator = floorPlane.X * position.X + floorPlane.Y * position.Y + floorPlane.Z * position.Z + floorPlane.W;
            double denominator = Math.Sqrt(floorPlane.X * floorPlane.X + floorPlane.Y * floorPlane.Y + floorPlane.Z * floorPlane.Z);
            return numerator / denominator;
        }

        private void CheckForStep((bool, bool)[] legRaisedinCurrentFrame)
        {
            for (int i = 0; i < this.wasLegRaised.Length; i++)
            {
                if (bodies[i].IsTracked)
                {
                    // remember first item holds left information
                    // and second item right information
                    (string stepStatusLeft, bool newLegRaisedLeft) = GetStepStatus(this.wasLegRaised[i].Item1, legRaisedinCurrentFrame[i].Item1);
                    (string stepStatusRight, bool newLegRaisedRight) = GetStepStatus(this.wasLegRaised[i].Item2, legRaisedinCurrentFrame[i].Item2);

                
                    Console.WriteLine("Body " + i);
                    Console.WriteLine("Left " + stepStatusLeft);
                    Console.WriteLine("Right " + stepStatusRight);

                    this.wasLegRaised[i] = (newLegRaisedLeft, newLegRaisedRight);
                }
               
            }
        }

        private (string,bool) GetStepStatus(bool itemLeft, bool itemRight)
        {
            if (itemLeft == true && itemRight == false)
            {
                return ("STEP ENDED", false);
            }
            if (itemLeft == true && itemRight == true)
            {
                return ("STEP DOING", true);
            }
            if (itemLeft == false && itemRight == false)
            {
                return ("NO STEP", false);
            }
            if (itemLeft == false && itemRight == true)
            {
                return ("STEP STARTED", true);
            }
            return ("UNDEFINED", false);
        }

        public void Stop()
        {
            // stop listening to the beat
            BM.Timer.Elapsed -= Timer_Elapsed;
            KW.isRecording = false;

            // write array to csv
            File.WriteAllLines("stanceData"+filecounter+".csv", stanceData);
            filecounter++;
            stanceData.Clear();
        }
    }
}
