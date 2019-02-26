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
using Microsoft.Kinect;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace PresentationTrainer
{
    public  class JudgementMaker
    {
        public ArrayList tempMistakeList;
        public ArrayList mistakeList;
        public ArrayList bodyMistakes;
        public ArrayList tempGoodiesList;
        public ArrayList GoodiesList;
        public ArrayList voiceAndMovementsList;
        public ArrayList audioMovementMistakeTempList;

        public double TIME_TO_CONSIDER_ACTION = 300;
        public double TIME_TO_CONSIDER_CORRECTION = 100;
        public double TIME_TO_CONSIDER_INTERRUPTION = 6000000;// 6000; //changed

        public double ThresholdDefaultHandMovement = 1000;
        public double HandMovementFactor = 3770;
        public double bufferTime = 400;

        public double timeBetweenGestures = 0;
        public double timeLastGesture = 0;
        public double timePausing = 0;
        public double timePreviousPauses = 0;

        public double ThresholdSmile = 25000;
        public double lastSmile = 0;

        public bool gesturesDone = false;

        public int resetGestureImage = 0;
        public bool resetGestureImageBool = false;

        public double lastPostureImageTime;
        public double TIME_TO_NEW_POSTURE_IMAGE = 15000;
        public bool resetPostureImage = false;

     //   public bool periodicMov = false;

        public int[] nolongerBodyErrors;

        public PresentationAction myVoiceAndMovementObject;

        public ArmMovementsCalc armMovementsCalc;

        public BodyFramePreAnalysis bfpa;
        public AudioPreAnalysis apa;
        public FaceFramePreAnalisys ffpa;
        Body oldBody;
        MainWindow parent;
        PeriodicMovements periodicMovements;
        PresentationAction highVolume=null;
        PresentationAction LowVolume=null;

        public double difTime;

        public JudgementMaker(MainWindow parent)
        {
           this.parent = parent;
           myVoiceAndMovementObject = new PresentationAction();
           periodicMovements = new PeriodicMovements();

           armMovementsCalc = new ArmMovementsCalc(this);
           tempMistakeList = new ArrayList();
           mistakeList = new ArrayList();
           GoodiesList = new ArrayList();
           voiceAndMovementsList = new ArrayList();
           audioMovementMistakeTempList = new ArrayList();
           bodyMistakes = new ArrayList();
           lastSmile = DateTime.Now.TimeOfDay.TotalMilliseconds;
           lastPostureImageTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
        }

        public void clearLists()
        {
            tempMistakeList.Clear(); 
            mistakeList.Clear();
            voiceAndMovementsList.Clear(); 
            audioMovementMistakeTempList.Clear(); 
            bodyMistakes.Clear();
            GoodiesList.Clear();
        }

        #region analyzeCycle

        public void analyze()
        {
            ffpa = this.parent.faceFrameHandler.faceFramePreAnalysis;
            bfpa = this.parent.bodyFrameHandler.bodyFramePreAnalysis;
            apa = this.parent.audioHandler.audioPreAnalysis;

            searchMistakes();

            saveCurrentBodyAsOldBody();
            

            
        }

        private void searchMistakes()
        {
            
            addBodyMistakes();
            deleteBodyMistakes();
            findMistakesInVoiceAndMovement();
            deleteVoiceAndMovementsMistakes();
            mistakeList = new ArrayList(bodyMistakes);
            mistakeList.AddRange(audioMovementMistakeTempList);
            
            if(bfpa.body!=null)
            {
                if (periodicMovements.checPeriodicMovements(bfpa.body))
                {
                    PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.DANCING);
                  //  pa.interrupt = true;
                   // periodicMov = true;
                    mistakeList.Insert(0, pa);
                }

            }
           
            

           //todo add mistakes together
        }

        #endregion


        
        #region addingMistakes

        #region addingBodyMistakes


        private void addPostureImages()
        {
            double currenTime =  DateTime.Now.TimeOfDay.TotalMilliseconds;
            if(resetPostureImage==true || currenTime-lastPostureImageTime > TIME_TO_NEW_POSTURE_IMAGE )
            {
                if (MainWindow.postureImages[0] == null)
                {
                    MainWindow.postureImages[0] = parent.videoHandler.kinectImage.Source.CloneCurrentValue();
                    lastPostureImageTime = currenTime;
                }
                else if (MainWindow.postureImages[1] == null &&
                    currenTime - lastPostureImageTime > 5000)
                {
                    MainWindow.postureImages[1] = parent.videoHandler.kinectImage.Source.CloneCurrentValue();
                    lastPostureImageTime = currenTime;
                }
                else if (currenTime - lastPostureImageTime > 5000)
                {
                    MainWindow.postureImages[2] = parent.videoHandler.kinectImage.Source.CloneCurrentValue();
                    lastPostureImageTime = currenTime;
                }
            }
        }

        private void addBodyMistakes()
        {
            addNewMistakesToTemp();
            findMistakesInTempList();
            addPostureImages();

        }

        private void addNewMistakesToTemp()
        {
            
            foreach (PresentationAction fb in bfpa.currentMistakes)
            {
                int x = 0;
                foreach (PresentationAction fa in tempMistakeList)
                {
                    if (fa.myMistake == fb.myMistake)
                    {
                        x = 1;
                        break;
                    }
                }
                if (x == 0)
                {
                    fb.timeStarted = DateTime.Now.TimeOfDay.TotalMilliseconds;
                    tempMistakeList.Add(fb);
                }
            }
        }

        private void findMistakesInTempList()
        {
            foreach (PresentationAction fa in tempMistakeList)
            {
                foreach (PresentationAction fb in bfpa.currentMistakes)
                {
                    if (fa.myMistake == fb.myMistake)
                    {
                        if (findMistakeInMistakeList(fb) == false)
                        {
                            if (checkTimeToPutMistake(fa))
                            {

                                bodyMistakes.Add(fa);
                                resetPostureImage = true;
                                
                                fa.firstImage = parent.videoHandler.kinectImage.Source.CloneCurrentValue();
                            }
                        }
                        else
                        {
                            if (checkTimeToPutInterruption(fa))
                            {
                                fa.interrupt = true;
                            }
                        }
                    }
                }
            }
        }

        

        private bool findMistakeInMistakeList(PresentationAction fb)
        {
            bool found=false;
            foreach(PresentationAction fa in mistakeList)
            {
                if(fb.myMistake==fa.myMistake)
                {
                    found = true;
                    break;
                }   
            }
            return found;
        }

        private bool checkTimeToPutMistake(PresentationAction fa)
        {
            bool result = false;
            double currentTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
            if (currentTime - fa.timeStarted > TIME_TO_CONSIDER_ACTION)
            {
                result = true;
            }
            return result;
        }

        private bool checkTimeToPutInterruption(PresentationAction fa)
        {
            bool result = false;
            double currentTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
            if (currentTime - fa.timeStarted > TIME_TO_CONSIDER_INTERRUPTION)
            {
                result = true;
            }
            return result;
        }

        #endregion

        #region voiceAndMovementStuff

        #region handleLists

        public void findMistakesInVoiceAndMovement()
        {
            audioMovementMistakeTempList = new ArrayList();
            handleHmmm();
            handleFace();

            if (myVoiceAndMovementObject.timeStarted == 0)
            {
                resetMyVoiceAndMovement();
            }
            if (myVoiceAndMovementObject.isSpeaking == true && apa.isSpeaking == true)
            {
                handleSpeakingTime();
                if(MainWindow.speakTimes.Count==0)
                {
                    MainWindow.speakTimes.Add(DateTime.Now.TimeOfDay.TotalMilliseconds);
                }
            }
            else if (myVoiceAndMovementObject.isSpeaking == false && apa.isSpeaking == true)
            {
                MainWindow.speakTimes.Add(DateTime.Now.TimeOfDay.TotalMilliseconds);
                logPauses();
                resetMyVoiceAndMovement();
                timePreviousPauses = timePreviousPauses + timePausing;
                handleSpeakingTime();

            }
            else if (myVoiceAndMovementObject.isSpeaking == false && apa.isSpeaking == false)
            {
                handlePauses();
            }
            else if (myVoiceAndMovementObject.isSpeaking == true && apa.isSpeaking == false)
            {
                if(MainWindow.speakTimes.Count>0)
                {
                    MainWindow.speakTimes.Add(DateTime.Now.TimeOfDay.TotalMilliseconds);
                }
               
                logSpeaking();
                resetMyVoiceAndMovement();
                handlePauses();
            }

        }

        

      

        #endregion

      
       

        private void handleHmmm()
        {
            if(apa.foundHmmm==true)
            {
                PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.HMMMM);
                pa.timeStarted = myVoiceAndMovementObject.timeStarted;
                pa.isVoiceAndMovementMistake = true;
                audioMovementMistakeTempList.Add(pa);
            }
        }

        private void putSmile()
        {
            PresentationAction pa = new PresentationAction(PresentationAction.GoodType.SMILE);
            pa.timeStarted = DateTime.Now.TimeOfDay.TotalMilliseconds;
            if(GoodiesList.Count==0)
            {
                GoodiesList.Add(pa); //TODO make it work for more goodies
            }
        }
        private void removeSmile()
        {
            GoodiesList.Clear();
        }

        private void handleFace()
        {
            double currentTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
            if(ffpa.smilingTemp==true)
            {
                lastSmile = currentTime;
                putSmile();
                

            }
            else
            {
                removeSmile();
            }
            if(currentTime-lastSmile>ThresholdSmile)
            {
                PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.SERIOUS);
                pa.timeStarted = currentTime;// -lastSmile - ThresholdSmile;
                pa.isVoiceAndMovementMistake = true;
                pa.firstImage = parent.videoHandler.kinectImage.Source.CloneCurrentValue();
                if (currentTime - lastSmile > ThresholdSmile+6000)
                {
                    pa.interrupt = true;
                }
                audioMovementMistakeTempList.Add(pa);
                
                
            }
        }

        private void handleSpeakingTime()
        {
            handleVolume();
            handleTimeSpeaking();
            if(bfpa.body!=null)
            {
         //      handleHandMovements();
               handleMovementsWithAngles();
                
            }
            
            
        }
        
        // Gianluca: copy this method
        private void handleMovementsWithAngles()
        {
            double currentTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
            difTime = (currentTime - myVoiceAndMovementObject.timeStarted);
            armMovementsCalc.calcArmMovements();
            if(timeLastGesture==0)
            {
                timeLastGesture = currentTime;
            }
            if(armMovementsCalc.currentGesture!=PresentationTrainer.ArmMovementsCalc.Gesture.nogesture)
            {
                timeLastGesture = currentTime;
                timePreviousPauses = 0;
                
            }
            if (armMovementsCalc.prePreviousGesture == ArmMovementsCalc.Gesture.nogesture &&
                armMovementsCalc.currentGesture != armMovementsCalc.prePreviousGesture )
            {
                MainWindow.gestureTimes.Add(currentTime);
                //parent.videoHandler.kinectImage.Source.CloneCurrentValue();
            }
            if (armMovementsCalc.prePreviousGesture != armMovementsCalc.currentGesture &&
                armMovementsCalc.currentGesture == ArmMovementsCalc.Gesture.nogesture && MainWindow.gestureTimes.Count>0)
            {
                MainWindow.gestureTimes.Add(currentTime);
            }

    

            assignPictures();

            timeBetweenGestures = currentTime - timeLastGesture + timePreviousPauses;
            if (timeBetweenGestures>5000)
            { 
                PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.HANDS_NOT_MOVING);
                pa.timeStarted = myVoiceAndMovementObject.timeStarted;
                pa.isVoiceAndMovementMistake = true;
                if (myVoiceAndMovementObject.firstImage != null)
                {
                    System.Windows.Media.ImageSource im = parent.videoHandler.kinectImage.Source;
                    //  pa.firstImage = myVoiceAndMovementObject.firstImage.CloneCurrentValue();
                    myVoiceAndMovementObject.lastImage = im.CloneCurrentValue();
                    //  pa.lastImage = im.CloneCurrentValue();
                }

                audioMovementMistakeTempList.Add(pa);
            }
        }

        private void assignPictures()
        {
            if(resetGestureImageBool==false)
            {
                 if(MainWindow.gestureImages[0]==null)
                {
                    resetGestureImage=0;
                }
                else if(MainWindow.gestureImages[1]==null)
                {
                    resetGestureImage=1;
                }
                else 
                {
                    resetGestureImage=2;
                }
               
                resetGestureImageBool=true;
            }
          
            if (armMovementsCalc.prePreviousGesture != armMovementsCalc.currentGesture &&
               resetGestureImageBool == true)
            {
                if(resetGestureImage==0)
                {
                    MainWindow.gestureImages[0] = parent.videoHandler.kinectImage.Source.CloneCurrentValue();
                }
                else if(resetGestureImage==1)
                {
                    MainWindow.gestureImages[1] = parent.videoHandler.kinectImage.Source.CloneCurrentValue();
                }
                else if( resetGestureImage==2)
                {
                    MainWindow.gestureImages[2] = parent.videoHandler.kinectImage.Source.CloneCurrentValue();
                }
                
            }
            if (armMovementsCalc.currentGesture == ArmMovementsCalc.Gesture.nogesture )
            {
                resetGestureImageBool = false;
            }
        }

        private void handleTimeSpeaking()
        {
            double currentTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
            if (currentTime - myVoiceAndMovementObject.timeStarted > apa.ThresholdIsSpeakingLongTime)
            {
                PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.LONG_TALK);
                pa.timeStarted = myVoiceAndMovementObject.timeStarted;
                pa.isVoiceAndMovementMistake = true;
                if (currentTime - myVoiceAndMovementObject.timeStarted > apa.ThresholdIsSpeakingVeryLongTime)
                {
                    pa.interrupt = true;
                }

                audioMovementMistakeTempList.Add(pa);
            }
        }

        

        private void handleHandMovements()
        {
            
            double currentTime = DateTime.Now.TimeOfDay.TotalMilliseconds;

            CameraSpacePoint handLeft = bfpa.body.Joints[JointType.HandLeft].Position;
            CameraSpacePoint handRight = bfpa.body.Joints[JointType.HandRight].Position;
            CameraSpacePoint hipLeft = bfpa.body.Joints[JointType.HipLeft].Position;
            CameraSpacePoint hipRight = bfpa.body.Joints[JointType.HipRight].Position;

            double leftDistance = Math.Sqrt(
                (handLeft.X - hipLeft.X) * (handLeft.X - hipLeft.X) +
                (handLeft.Y - hipLeft.Y) * (handLeft.Y - hipLeft.Y) +
                (handLeft.Z - hipLeft.Z) * (handLeft.Z - hipLeft.Z));

            double rightDistance = Math.Sqrt(
                (handRight.X - hipRight.X) * (handRight.X - hipRight.X) +
                (handRight.Y - hipRight.Y) * (handRight.Y - hipRight.Y) +
                (handRight.Z - hipRight.Z) * (handRight.Z - hipRight.Z));

            if(myVoiceAndMovementObject.leftHandHipDistance==0)
            {
                myVoiceAndMovementObject.leftHandHipDistance = leftDistance;
                myVoiceAndMovementObject.rightHandHipDistance = rightDistance;
            }
            
            double rightMovement = Math.Abs(myVoiceAndMovementObject.rightHandHipDistance - rightDistance);
            double leftMovement = Math.Abs(myVoiceAndMovementObject.leftHandHipDistance - leftDistance);

            myVoiceAndMovementObject.leftHandHipDistance = leftDistance;
            myVoiceAndMovementObject.rightHandHipDistance = rightDistance;

            double currentHandMovement = rightMovement;

            if(rightMovement>leftMovement)
            {
                myVoiceAndMovementObject.totalHandMovement = myVoiceAndMovementObject.totalHandMovement + rightMovement * HandMovementFactor;
            }
            else
            {
                currentHandMovement = leftMovement;
                myVoiceAndMovementObject.totalHandMovement = myVoiceAndMovementObject.totalHandMovement + leftMovement * HandMovementFactor;
            }
            
            double difTime= (currentTime - myVoiceAndMovementObject.timeStarted);
            if(difTime>0 )
            {
                myVoiceAndMovementObject.averageHandMovement = myVoiceAndMovementObject.totalHandMovement / difTime ;
            }

            if (myVoiceAndMovementObject.averageHandMovement < 1 )//&& currentHandMovement < 0.006)
            {
                PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.HANDS_NOT_MOVING);
                pa.timeStarted = myVoiceAndMovementObject.timeStarted;
                pa.isVoiceAndMovementMistake = true;
                if(myVoiceAndMovementObject.firstImage!=null)
                {
                    System.Windows.Media.ImageSource im = parent.videoHandler.kinectImage.Source;  
                  //  pa.firstImage = myVoiceAndMovementObject.firstImage.CloneCurrentValue();
                    myVoiceAndMovementObject.lastImage = im.CloneCurrentValue();
                  //  pa.lastImage = im.CloneCurrentValue();
                }
                
                audioMovementMistakeTempList.Add(pa);
            }
            else if (difTime > 2000) //use for debugging
            {
                int aa = 0;
                aa++;
          //     myVoiceAndMovementObject.totalHandMovement = difTime + 1000;
            }
            

        }
        private void handleVolume()
        {
            double currentTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
            if(myVoiceAndMovementObject.minVolume==0)
            {
                myVoiceAndMovementObject.minVolume = apa.averageVolume;
                myVoiceAndMovementObject.maxVolume = apa.averageVolume;
            }
            if(apa.averageVolume<myVoiceAndMovementObject.minVolume)
            {
                myVoiceAndMovementObject.minVolume = apa.averageVolume;
            }
            if(apa.averageVolume>myVoiceAndMovementObject.maxVolume)
            {
                myVoiceAndMovementObject.maxVolume = apa.averageVolume;
            }
            if(apa.averageVolume>apa.ThresholdIsSpeakingLoud)
            {
                LowVolume = null;
                if(highVolume==null)
                {
                    highVolume = new PresentationAction(PresentationAction.MistakeType.HIGH_VOLUME);
                    highVolume.isVoiceAndMovementMistake = true;
                }
                if (currentTime - highVolume.timeStarted  > TIME_TO_CONSIDER_ACTION)
                {
                    
                    audioMovementMistakeTempList.Add(highVolume);
                }
            }
            else if(apa.averageVolume<apa.ThresholdIsSpeakingSoft)
            {
                highVolume = null;

                if (LowVolume == null)
                {
                    LowVolume = new PresentationAction(PresentationAction.MistakeType.LOW_VOLUME);
                    LowVolume.isVoiceAndMovementMistake = true;
                }
                if (currentTime - LowVolume.timeStarted > TIME_TO_CONSIDER_ACTION)
                {
                    audioMovementMistakeTempList.Add(LowVolume);
                }
            }
            else
            {
                highVolume = null;
                LowVolume = null;
            }
            if(currentTime - myVoiceAndMovementObject.timeStarted>3000 && 
                myVoiceAndMovementObject.maxVolume- myVoiceAndMovementObject.minVolume < 0.001)
            {
                PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.LOW_MODULATION);
                {
                    pa.timeStarted = myVoiceAndMovementObject.timeStarted;
                    pa.isVoiceAndMovementMistake = true;
                    audioMovementMistakeTempList.Add(pa);
                }
            }


        }
        public void handlePauses()
        {
            double currentTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
            timePausing =  currentTime - myVoiceAndMovementObject.timeStarted;
            if (timePausing > apa.ThresholdIsLongPauseTime)
            {
                PresentationAction pa = new PresentationAction(PresentationAction.MistakeType.LONG_PAUSE);
                pa.timeStarted = myVoiceAndMovementObject.timeStarted;
                pa.isVoiceAndMovementMistake = true;
                if (currentTime - myVoiceAndMovementObject.timeStarted > apa.ThresholdIsVeryLongPauseTime)
                {
                    pa.interrupt = true;
                }

                audioMovementMistakeTempList.Add(pa);
                
            }
        }

        public void resetMyVoiceAndMovement()
        {
            myVoiceAndMovementObject.isSpeaking = apa.isSpeaking;
            myVoiceAndMovementObject.timeStarted = DateTime.Now.TimeOfDay.TotalMilliseconds;
            myVoiceAndMovementObject.interrupt = false;
            myVoiceAndMovementObject.totalHandMovement = ThresholdDefaultHandMovement;
            myVoiceAndMovementObject.minVolume = 0;
            myVoiceAndMovementObject.maxVolume = 0;
            myVoiceAndMovementObject.rightHandHipDistance = 0;
            myVoiceAndMovementObject.leftHandHipDistance = 0;
            myVoiceAndMovementObject.averageHandMovement = 1;
            audioMovementMistakeTempList = new ArrayList();
            if (parent.videoHandler.kinectImage.Source!=null)
            {
                myVoiceAndMovementObject.firstImage = null;
                myVoiceAndMovementObject.firstImage = parent.videoHandler.kinectImage.Source.CloneCurrentValue();
            }

            armMovementsCalc.resetMaxAndMin();
            gesturesDone = false;
            
        }

        #endregion

        #endregion

        #region deletingMistakes

        #region deletingBodyMistakes
        private void deleteBodyMistakes()
        {
            findNoMistakesInBodyTempList();
            removeMistakesBodyTemp();
            removeBodyMistakes();
        }

        private void findNoMistakesInBodyTempList()
        {
            nolongerBodyErrors = new int[tempMistakeList.Count];
            int i = 0;
            foreach (PresentationAction fa in tempMistakeList)
            {
                int x = 0;
                foreach (PresentationAction fb in bfpa.currentMistakes)
                {
                    if (fa.myMistake == fb.myMistake)
                    {
                        x = 1;
                        fa.timeFinished = DateTime.Now.TimeOfDay.TotalMilliseconds;
                        break;
                    }
                }
                if (x == 0)
                {
                    double currentTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
              //      if (currentTime - fa.timeFinished > TIME_TO_CONSIDER_CORRECTION)
              //      {
                        nolongerBodyErrors[i] = 1;
               //     }
                }
                i++;
            }
        }

        private void removeMistakesBodyTemp()
        {
            for (int i = tempMistakeList.Count; i > 0; i--)
            {
                if (nolongerBodyErrors[i - 1] == 1)
                {
                    tempMistakeList.RemoveAt(i - 1);
                }
            }
        }

        private void removeBodyMistakes()
        {
            int[] nolongerMistakes = new int[bodyMistakes.Count];
            int i = 0;
            foreach (PresentationAction fa in bodyMistakes)
            {
                for (int j = tempMistakeList.Count; j > 0; j--)
                {
                    if (((PresentationAction)tempMistakeList[j - 1]).myMistake == fa.myMistake)
                    {
                        nolongerMistakes[i] = 1;
                    }
                }
                i++;
            }
            for (int ii = bodyMistakes.Count; ii > 0; ii--)
            {
                if (nolongerMistakes[ii - 1] == 0)
                {
                    bodyMistakes.RemoveAt(ii - 1);
                }
            }
        }

        #endregion

        #region deletingVoiceandMovements

        private void deleteVoiceAndMovementsMistakes()
        {
            int x= 0;
            int[] mistakesToDelete= new int [mistakeList.Count];
            foreach (PresentationAction pa in  mistakeList)
            {
                foreach(PresentationAction pb in audioMovementMistakeTempList)
                {
                    if(pb.myMistake==pa.myMistake)
                    {
                        mistakesToDelete[x] = 1;
                        break;
                    }
                }
                x++;
            }
            for (int j= mistakeList.Count; j>0;j--)
            {
                if(mistakesToDelete[j-1]==1 && ((PresentationAction)mistakeList[j-1]).isVoiceAndMovementMistake)
                {
                    mistakeList.RemoveAt(j - 1);
                }
            }
        }
        #endregion
        #endregion

        #region logging
        private void logSpeaking()
        {
            MainWindow.stringSpeakingTime = MainWindow.stringSpeakingTime +System.Environment.NewLine+
                "<speaking>" + System.Environment.NewLine + "<time started> " + myVoiceAndMovementObject.timeStarted +
                "</time started>\n<time finished>" +
               DateTime.Now.TimeOfDay.TotalMilliseconds + "</time finished>" + System.Environment.NewLine + "<average volume>" +
               myVoiceAndMovementObject.averageVolume + "</average volume>" + System.Environment.NewLine + "<max volume>" +
               myVoiceAndMovementObject.maxVolume + "</max volume>" + System.Environment.NewLine + "<min volume>" +
               myVoiceAndMovementObject.minVolume + "</min volume>"+System.Environment.NewLine+"</speaking>";
        }

        private void logPauses()
        {
            MainWindow.stringPausingTime = MainWindow.stringPausingTime + System.Environment.NewLine +
                "<pause>"+System.Environment.NewLine+"<time started> " + myVoiceAndMovementObject.timeStarted +
                "</time started>" + System.Environment.NewLine + "<time finished>" +
               DateTime.Now.TimeOfDay.TotalMilliseconds + "</time finished>" + System.Environment.NewLine + "</pause>";
        }
        #endregion

        private void saveCurrentBodyAsOldBody()
        {
            bfpa.setOldBody();
        }

    }
}