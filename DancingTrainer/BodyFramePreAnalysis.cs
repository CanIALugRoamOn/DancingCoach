/**
 * ****************************************************************************
 * Copyright (C) 2016 Open Universiteit Nederland
 * <p/>
 * This library is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * <p/>
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 * <p/>
 * You should have received a copy of the GNU Lesser General Public License
 * along with this library.  If not, see <http://www.gnu.org/licenses/>.
 * <p/>
 * Contributors: Jan Schneider
 * ****************************************************************************
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Collections;

namespace PresentationTrainer
{
    public class BodyFramePreAnalysis
    {
         public Body body;
         public static Dictionary<JointType, Joint> bodyOld = new Dictionary<JointType, Joint>();

        public bool armsCrossed =false;
        public bool legsCrossed = false;
        public bool rightHandUnderHips = false;
        public bool leftHandUnderHips = false;
        public bool rightHandBehindBack = false;
        public bool leftHandBehindBack = false;
        public bool hunch = false;
        public bool resetPosture = false;
        public bool leanIn = false;
        public bool rightArmClosePosture = false;
        public bool leftArmClosePosture = false;
        public bool rightLean=false;
        public bool leftLean = false;
        public bool areHandsMoving = false;

        public bool heroLegRight = false;
        public bool heroLegLeft = false;
        public bool heroArmsRight = false;
        public bool heroArmsLeft = false;
        public bool heroBack = false;
        public bool heroNeck = false;
        public bool heroMistake=true;
        public bool heroWinning = false;

        public static bool rightHandUp = false;


        public double shouldersAngle;
        public double hipsAngle ;
        public int rightOpenness;
        public int leftOpenness;
        public int totalOpenness;
        CameraSpacePoint rightHandPrevious ;
        CameraSpacePoint leftHandPrevious ;

        public ArrayList currentMistakes;
        public ArrayList currentGoodies;

        public enum Posture { good, bad }; //TODO
        public enum HandMovement { good, notEnough, tooMuch };//TODO

        public Posture bodyPosture; //TODO
        public HandMovement handMovement; //TODO


        // This moving stuff I think should go to rulesAnalizer
        public bool needToMoveMore = false; //TODO
        public bool isMoving = false; //TODO
        public double startedMoving; //TODO
        public double stopMoving; //TODO
        public float ThresholdMovingTime = 1000; //TODO
        public float ThresholdIsMovingDistance = 0.035f; //TODO


        public double angleRightForearmRightArmA = 2000;
        public double angleRightForearmRightArmB = 2000;
        public double angleRightForearmRightArmC = 0;



        public double prevAngleRightForearmRightArmA = 2000;
        public double prevAngleRightForearmRightArmB = 2000;
        public double prevAngleRightForearmRightArmC = 0;


        public double angleRightArmShoulderLineA = 2000;
        public double angleRightArmShoulderLineB = 2000;
        public double angleRightArmShoulderLineC = 0;

        public double prevAngleRightArmShoulderLineA = 2000;
        public double prevAngleRightArmShoulderLineB = 2000;
        public double prevAngleRightArmShoulderLineC = 0;

        public double angleLeftForearmLeftArmA = 2000;
        public double angleLeftForearmLeftArmB = 2000;
        public double angleLeftForearmLeftArmC = 0;

        public double prevAngleLeftForearmLeftArmA=2000;
        public double prevAngleLeftForearmLeftArmB=2000;
        public double prevAngleLeftForearmLeftArmC = 0;

        public double angleLeftArmShoulderLineA = 0;
        public double angleLeftArmShoulderLineB = 2000;
        public double angleLeftArmShoulderLineC = 0;

        public double prevAngleLeftArmShoulderLineA = 0;
        public double prevAngleLeftArmShoulderLineB = 2000;
        public double prevAngleLeftArmShoulderLineC = 0;

        public BodyFramePreAnalysis(Body body)
        {
            this.body = body;
            if(body!=null)
            {
                rightHandPrevious = body.Joints[JointType.HandRight].Position;
                leftHandPrevious = body.Joints[JointType.HandLeft].Position;
            }
            currentMistakes = new ArrayList();
            currentGoodies = new ArrayList();
            
        }

        public void setBody(Body body)
        {
             this.body = body;
             if (body != null)
             {
                 rightHandPrevious = body.Joints[JointType.HandRight].Position;
                 leftHandPrevious = body.Joints[JointType.HandLeft].Position;
             }
        }


        public void setOldBody()
        {
            if (this.body != null)
            {
                BodyFramePreAnalysis.bodyOld.Clear();
                foreach (KeyValuePair<JointType, Joint> kp in this.body.Joints)
                {
                    BodyFramePreAnalysis.bodyOld.Add(kp.Key, kp.Value);
                }


                int x = 0;
                x++;
            }

        }
      
        public void getbigGesture()
        {

        }
        
        // Gianluca
        public void getArmsAngles()
        {
            getAnglesForearm();
            getAnglesShoulder();

            
        }

        void getAnglesForearm()
        {
            double forearmLineX = body.Joints[JointType.WristRight].Position.X - body.Joints[JointType.ElbowRight].Position.X;
            double forearmLineZ = body.Joints[JointType.WristRight].Position.Z - body.Joints[JointType.ElbowRight].Position.Z;
            double forearmLineY = body.Joints[JointType.WristRight].Position.Y - body.Joints[JointType.ElbowRight].Position.Y;

            angleRightForearmRightArmA = Math.Atan(forearmLineY /
                (Math.Sqrt(forearmLineX * forearmLineX + forearmLineZ * forearmLineZ)) * 180 / Math.PI);
            angleRightForearmRightArmB = Math.Atan(forearmLineX / forearmLineZ) / Math.PI * 180;


            forearmLineX = body.Joints[JointType.WristLeft].Position.X - body.Joints[JointType.ElbowLeft].Position.X;
            forearmLineZ = body.Joints[JointType.WristLeft].Position.Z - body.Joints[JointType.ElbowLeft].Position.Z;
            forearmLineY = body.Joints[JointType.WristLeft].Position.Y - body.Joints[JointType.ElbowLeft].Position.Y;

            angleLeftForearmLeftArmA = Math.Atan(forearmLineY /
                (Math.Sqrt(forearmLineX * forearmLineX + forearmLineZ * forearmLineZ)) * 180 / Math.PI);
            angleLeftForearmLeftArmB = Math.Atan(forearmLineX /
                Math.Sqrt(forearmLineY * forearmLineY + forearmLineZ * forearmLineZ)) / Math.PI * 180; 
        }
        void getAnglesShoulder()
        {
            double armLineX = body.Joints[JointType.ElbowRight].Position.X - body.Joints[JointType.ShoulderRight].Position.X;
            double armLineZ = body.Joints[JointType.ElbowRight].Position.Z - body.Joints[JointType.ShoulderRight].Position.Z;
            double armLineY = body.Joints[JointType.ElbowRight].Position.Y - body.Joints[JointType.ShoulderRight].Position.Y;

            angleRightArmShoulderLineA = Math.Atan(armLineX /
                Math.Sqrt(armLineZ * armLineZ + armLineY * armLineY)) / Math.PI * 180;
            angleRightArmShoulderLineB = Math.Atan(armLineZ /
                Math.Sqrt(armLineY * armLineY + armLineX * armLineX)) / Math.PI * 180;
           

            armLineX = body.Joints[JointType.ElbowLeft].Position.X - body.Joints[JointType.ShoulderLeft].Position.X;
            armLineZ = body.Joints[JointType.ElbowLeft].Position.Z - body.Joints[JointType.ShoulderLeft].Position.Z;
            armLineY = body.Joints[JointType.ElbowLeft].Position.Y - body.Joints[JointType.ShoulderLeft].Position.Y;

            angleLeftArmShoulderLineA = Math.Atan(armLineX /
                Math.Sqrt(armLineZ * armLineZ + armLineY * armLineY)) / Math.PI * 180;
            angleLeftArmShoulderLineB = Math.Atan(armLineZ /
                Math.Sqrt(armLineY * armLineY + armLineX * armLineX)) / Math.PI * 180;
        }



        public void analyzePosture()
        {
            currentMistakes = new ArrayList();
            currentGoodies = new ArrayList();

            bodyPosture = Posture.good;
            getShouldersAngle();
            getArmsCrossed();
            getHunch();
            getRightHandUnderHip();
            getLeftHandUnderHip();
            getRightHandBehindBack();
            getRightArmClosePosture();
            getLeftArmClosePosture();
            getRightLean();
            getLeftLean();
            getResetPosture();
            getLegsCrossed();

            calcMovingHands();
            getArmsAngles();
            searchPause();

           

        //    calcIsMovingArms();
           
        }

        private void searchPause()
        {
            if (body.Joints[JointType.HandRight].Position.Y  > body.Joints[JointType.Head].Position.Y
                && body.Joints[JointType.HandLeft].Position.Y > body.Joints[JointType.Head].Position.Y)
            {
                MainWindow.stopGesture = true;
            }
            else
            {
                MainWindow.stopGesture = false;
            }

        }
      
      

        public void getShouldersAngle()
        {
            double xLine = body.Joints[JointType.ShoulderRight].Position.X - body.Joints[JointType.ShoulderLeft].Position.X;
            double zLine = body.Joints[JointType.ShoulderRight].Position.Z - body.Joints[JointType.ShoulderLeft].Position.Z;

            shouldersAngle = Math.Asin(zLine /
                (Math.Sqrt(zLine * zLine + xLine * xLine)));


            CameraSpacePoint left = body.Joints[JointType.HandLeft].Position;
            CameraSpacePoint right = body.Joints[JointType.HandRight].Position;

            if (left.Y > right.Y)
                rightHandUp = false;
            else
                rightHandUp = true;
        }

        public void getHipsAngle()
        {

        }

        public void getArmsCrossed()
        {
            bool useAngles=false;

            if(useAngles)
            {
                double x1 = body.Joints[JointType.HandRight].Position.X * Math.Cos(shouldersAngle);
                double z1 = body.Joints[JointType.HandRight].Position.Z * Math.Sin(shouldersAngle);

                double x2 = body.Joints[JointType.HandLeft].Position.X * Math.Cos(shouldersAngle);
                double z2 = body.Joints[JointType.HandLeft].Position.Z * Math.Sin(shouldersAngle);


                if (x1 - z1 < x2 - z2)
                {
                    armsCrossed = true;
                    bodyPosture = Posture.bad;

                    PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.ARMSCROSSED);
                    currentMistakes.Add(pa);
                }
                else
                {
                    armsCrossed = false;
                }
            }
            else
            {
                if(body.Joints[JointType.HandRight].Position.X+0.1< body.Joints[JointType.HandLeft].Position.X)
                {
                    armsCrossed = true;
                    bodyPosture = Posture.bad;
                    PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.ARMSCROSSED);
                    currentMistakes.Add(pa);
                }
                else
                {
                    armsCrossed = false;
                }
            }
            
        }

       

        public void getHunch()
        {

            double x1 = body.Joints[JointType.Head].Position.X * Math.Sin(shouldersAngle);
            double z1 = body.Joints[JointType.Head].Position.Z * Math.Cos(shouldersAngle);

            double x2 = body.Joints[JointType.SpineShoulder].Position.X * Math.Sin(shouldersAngle);
            double z2 = body.Joints[JointType.SpineShoulder].Position.Z * Math.Cos(shouldersAngle);



            double distanceShoulders = Math.Sqrt((body.Joints[JointType.ShoulderRight].Position.X - body.Joints[JointType.ShoulderLeft].Position.X) *
                (body.Joints[JointType.ShoulderRight].Position.X - body.Joints[JointType.ShoulderLeft].Position.X) +
                (body.Joints[JointType.ShoulderRight].Position.Z - body.Joints[JointType.ShoulderLeft].Position.Z) *
                (body.Joints[JointType.ShoulderRight].Position.Z - body.Joints[JointType.ShoulderLeft].Position.Z));



            if (-x1 + z1 < -x2 + z2 - distanceShoulders * 0.05)
            {
                
                hunch = true;
                bodyPosture = Posture.bad;
                PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.HUNCHBACK);
                currentMistakes.Add(pa);
            }
            else
            {
                hunch = false;
            }
           
           
        }

        public void getRightHandUnderHip()
        {
            double x1 = body.Joints[JointType.HandRight].Position.X * Math.Cos(shouldersAngle);
            double z1 = body.Joints[JointType.HandRight].Position.Z * Math.Sin(shouldersAngle);

            double x2 = body.Joints[JointType.HipRight].Position.X * Math.Cos(shouldersAngle);
            double z2 = body.Joints[JointType.HipRight].Position.Z * Math.Sin(shouldersAngle);

            double x3 = (body.Joints[JointType.HipLeft].Position.X - body.Joints[JointType.HipRight].Position.X)
                * (body.Joints[JointType.HipLeft].Position.X - body.Joints[JointType.HipRight].Position.X);
            double z3 = (body.Joints[JointType.HipLeft].Position.Z - body.Joints[JointType.HipRight].Position.Z)
                * (body.Joints[JointType.HipLeft].Position.Z - body.Joints[JointType.HipRight].Position.Z);
            double length1 = Math.Sqrt(x3 + z3);


            if(body.Joints[JointType.HandTipRight].Position.Y< body.Joints[JointType.HipRight].Position.Y &&
                x1 - z1 +length1*.01 < x2 - z2)
            {


                rightHandUnderHips = true;
                bodyPosture = Posture.bad;
                PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.RIGHTHANDUNDERHIP);
                currentMistakes.Add(pa);
            }
            else
            {
                rightHandUnderHips = false;
            }
        }

        public void getLeftHandUnderHip()
        {
            double x1 = body.Joints[JointType.HandLeft].Position.X * Math.Cos(shouldersAngle);
            double z1 = body.Joints[JointType.HandLeft].Position.Z * Math.Sin(shouldersAngle);

            double x2 = body.Joints[JointType.HipLeft].Position.X * Math.Cos(shouldersAngle);
            double z2 = body.Joints[JointType.HipLeft].Position.Z * Math.Sin(shouldersAngle);

            double x3 = (body.Joints[JointType.HipLeft].Position.X - body.Joints[JointType.HipRight].Position.X)
                * (body.Joints[JointType.HipLeft].Position.X - body.Joints[JointType.HipRight].Position.X);
            double z3 = (body.Joints[JointType.HipLeft].Position.Z - body.Joints[JointType.HipRight].Position.Z)
                * (body.Joints[JointType.HipLeft].Position.Z - body.Joints[JointType.HipRight].Position.Z);
            double length1 = Math.Sqrt(x3 + z3);

            if (body.Joints[JointType.HandTipLeft].Position.Y < body.Joints[JointType.HipLeft].Position.Y &&
                x2 - z2 < x1 - z1 + length1 * .01)
            {
                leftHandUnderHips = true;
                bodyPosture = Posture.bad;
                PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.LEFTHANDUNDERHIP);
                currentMistakes.Add(pa);
            }
            else
            {
                leftHandUnderHips = false;
            }
        }
        public void getLegsCrossed()
        {
             bool useAngles = false;
             if (useAngles)
             {

             }
             else
             {
                 if (body.Joints[JointType.AnkleRight].Position.X < body.Joints[JointType.AnkleLeft].Position.X)
                 {
                     legsCrossed = true;
                     bodyPosture = Posture.bad;
                     PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.LEGSCROSSED);
                     currentMistakes.Add(pa);
                 }
                 else
                 {
                     legsCrossed = false;
                 }
             }
        }
        public void getResetPosture()
        {
            bool useAngles = false;
            if (useAngles)
            {
                
            }
            else
            {
                float x = Math.Abs(body.Joints[JointType.HandTipRight].Position.X - body.Joints[JointType.HandTipLeft].Position.X);
                float y = Math.Abs(body.Joints[JointType.HandTipRight].Position.Y - body.Joints[JointType.HandTipLeft].Position.Y);
                float z = Math.Abs(body.Joints[JointType.HandTipRight].Position.Z - body.Joints[JointType.HandTipLeft].Position.Z);

                if (armsCrossed==false&& legsCrossed==false && 
                    rightHandBehindBack==false && leftHandBehindBack == false
                    && rightLean==false && leftLean==false && hunch==false
                    && body.HandRightState != HandState.Closed && body.HandLeftState!= HandState.Closed
                    && leftArmClosePosture == false && rightArmClosePosture == false
                    && x<0.04
                    && y<0.04 
                    && z<0.04)
                {
                    resetPosture = true;
                    bodyPosture = Posture.good;
                    PresentationAction pa = new PresentationAction(PresentationAction.GoodType.RESETPOSTURE);
                    currentGoodies.Add(pa);
                }
                else
                {
                    resetPosture = false;                    
                }
            }
               
        }
        public void getRightHandBehindBack()
        {
             bool useAngles=false;

             if (useAngles)
             {
                 double x1 = body.Joints[JointType.HandRight].Position.X * Math.Sin(shouldersAngle);
                 double z1 = body.Joints[JointType.HandRight].Position.Z * Math.Cos(shouldersAngle);

                 double x2 = body.Joints[JointType.SpineMid].Position.X * Math.Sin(shouldersAngle);
                 double z2 = body.Joints[JointType.SpineMid].Position.Z * Math.Cos(shouldersAngle);

                 if (-x1 + z1 < -x2 + z2)
                 {

                     rightHandBehindBack = true;
                     bodyPosture = Posture.bad;
                     PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.RIGHTHANDBEHINDBACK);
                     currentMistakes.Add(pa);
                 }
                 else
                 {
                     rightHandBehindBack = false;
                 }
             }
             else
             {
                 if (body.Joints[JointType.HandRight].Position.Z > body.Joints[JointType.SpineMid].Position.Z)
                 {
                     rightHandBehindBack = true;
                     bodyPosture = Posture.bad;
                     PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.RIGHTHANDBEHINDBACK);
                     currentMistakes.Add(pa);
                 }
                 else
                 {
                     rightHandBehindBack = false;
                 }
             }
        }
        public void getLeftHandBehindBack()
        {
            bool useAngles = false;

            if (useAngles)
            {
                double x1 = body.Joints[JointType.HandLeft].Position.X * Math.Sin(shouldersAngle);
                double z1 = body.Joints[JointType.HandLeft].Position.Z * Math.Cos(shouldersAngle);

                double x2 = body.Joints[JointType.SpineMid].Position.X * Math.Sin(shouldersAngle);
                double z2 = body.Joints[JointType.SpineMid].Position.Z * Math.Cos(shouldersAngle);

                if (-x1 + z1 < -x2 + z2)
                {

                    leftHandBehindBack = true;
                    bodyPosture = Posture.bad;
                    PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.LEFTHANDBEHINDBACK);
                    currentMistakes.Add(pa);
                }
                else
                {
                    leftHandBehindBack = false;
                }
            }
            else
            {
                if (body.Joints[JointType.HandLeft].Position.Z > body.Joints[JointType.SpineMid].Position.Z)
                {
                    leftHandBehindBack = true;
                    bodyPosture = Posture.bad;
                    PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.LEFTHANDBEHINDBACK);
                    currentMistakes.Add(pa);
                }
                else
                {
                    leftHandBehindBack = false;
                }
            }
        }
        void getRightArmClosePosture()
        {
            bool useAngles = false;

            if (useAngles)
            {
            }
            else
            {
                if (body.Joints[JointType.HandRight].Position.X < body.Joints[JointType.ShoulderRight].Position.X
                    && body.Joints[JointType.HandRight].Position.Y > body.Joints[JointType.ElbowRight].Position.Y)
                {
                    rightArmClosePosture = true;
                }
                else
                {
                    rightArmClosePosture = false;
                }
            }

        }
        void getLeftArmClosePosture()
        {
            bool useAngles = false;

            if (useAngles)
            {
            }
            else
            {
                if (body.Joints[JointType.HandLeft].Position.X > body.Joints[JointType.ShoulderLeft].Position.X
                    && body.Joints[JointType.HandLeft].Position.Y > body.Joints[JointType.ElbowLeft].Position.Y)
                {
                    leftArmClosePosture = true;
                }
                else
                {
                    leftArmClosePosture = false;
                }
            }

        }
        void getRightLean()
        {
            bool useAngles = false;

            if (useAngles)
            {
            }
            else
            {
                if (body.Joints[JointType.ShoulderRight].Position.X < body.Joints[JointType.HipRight].Position.X)
                {
                    rightLean = true;
                    bodyPosture = Posture.bad;
                    PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.RIGHTLEAN);
                    currentMistakes.Add(pa);
                }
                else
                {
                    rightLean = false;
                }
            }
        }
        void getLeftLean()
        {
            bool useAngles = false;

            if (useAngles)
            {
            }
            else
            {
                if (body.Joints[JointType.ShoulderLeft].Position.X > body.Joints[JointType.HipLeft].Position.X)
                {
                    leftLean = true;
                    bodyPosture = Posture.bad;
                    PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.LEFTLEAN);
                    currentMistakes.Add(pa);
                }
                else
                {
                    leftLean = false;
                }
            }
        }

        #region movingHands

        //probably this should go to rulesAnalizer or at least be invoqued there
        public void calcIsMovingArms() //TODO
        {
            if (areHandsMoving)
            {
                if (isMoving == false)
                {
                    isMoving = true;
                    TimeSpan now = new TimeSpan(DateTime.Now.Ticks);
                    startedMoving = now.TotalMilliseconds;
                    needToMoveMore = false;
                    handMovement = HandMovement.good;
                }
            }
            else
            {
                if (isMoving)
                {
                    isMoving = false;
                    TimeSpan now = new TimeSpan(DateTime.Now.Ticks);
                    stopMoving = now.TotalMilliseconds;
                }
                else
                {
                    TimeSpan now = new TimeSpan(DateTime.Now.Ticks);
                    if (now.TotalMilliseconds - stopMoving > ThresholdMovingTime)
                    {
                        needToMoveMore = true;
                        handMovement = HandMovement.notEnough;
                    }

                }
            }
        }

        void calcMovingHands()//TODO
        {
            CameraSpacePoint currentRightHand = body.Joints[JointType.HandRight].Position;
            CameraSpacePoint currentLeftHand = body.Joints[JointType.HandLeft].Position;

            double rightHandMovement = (currentRightHand.X - rightHandPrevious.X) * (currentRightHand.X - rightHandPrevious.X)
                + (currentRightHand.Y - rightHandPrevious.Y) * (currentRightHand.Y - rightHandPrevious.Y)
                + (currentRightHand.Z - rightHandPrevious.Z) * (currentRightHand.Z - rightHandPrevious.Z);
            rightHandMovement = Math.Sqrt(rightHandMovement);

            double leftHandMovement = (currentLeftHand.X - leftHandPrevious.X) * (currentLeftHand.X - leftHandPrevious.X)
                + (currentLeftHand.Y - leftHandPrevious.Y) * (currentLeftHand.Y - leftHandPrevious.Y)
                + (currentLeftHand.Z - leftHandPrevious.Z) * (currentLeftHand.Z - leftHandPrevious.Z);
            leftHandMovement = Math.Sqrt(leftHandMovement);

            double totalMovement;
            leftHandPrevious = currentLeftHand;
            rightHandPrevious = currentRightHand;

            if (leftHandMovement >= rightHandMovement)
            {
                totalMovement = leftHandMovement;
            }
            else
            {
                totalMovement = rightHandMovement;
            }
            if (totalMovement > ThresholdIsMovingDistance)
            {
                areHandsMoving = true;
            }
            else
            {
                areHandsMoving = false;
            }
        }

        #endregion
    }
}
