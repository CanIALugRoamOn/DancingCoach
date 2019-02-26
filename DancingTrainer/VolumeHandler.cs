/**
 * ****************************************************************************
 * Copyright (C) 2018 Das Deutsche Institut für Internationale Pädagogische Forschung (DIPF)
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DancingTrainer
{
    public class VolumeHandler
    {
        private KinectSensor kinectSensor;


        /// <summary>
        /// Speech recognition engine using audio data from Kinect.
        /// </summary>
      //  public SpeechRecognitionEngine speechEngine = null;

        /// <summary>
        /// Number of samples captured from Kinect audio stream each millisecond.
        /// </summary>
        private const int SamplesPerMillisecond = 16;

        /// <summary>
        /// Number of bytes in each Kinect audio stream sample (32-bit IEEE float).
        /// </summary>
        private const int BytesPerSample = sizeof(float);

        /// <summary>
        /// Number of audio samples represented by each column of pixels in wave bitmap.
        /// </summary>
        private const int SamplesPerColumn = 40;

        /// <summary>
        /// Minimum energy of audio to display (a negative number in dB value, where 0 dB is full scale)
        /// </summary>
        private const int MinEnergy = -90;

        /// <summary>
        /// Width of bitmap that stores audio stream energy data ready for visualization.
        /// </summary>
        private const int EnergyBitmapWidth = 300;

        /// <summary>
        /// Height of bitmap that stores audio stream energy data ready for visualization.
        /// </summary>
        private const int EnergyBitmapHeight = 300;

        /// <summary>
        /// Bitmap that contains constructed visualization for audio stream energy, ready to
        /// be displayed. It is a 2-color bitmap with white as background color and blue as
        /// foreground color.
        /// </summary>
        public WriteableBitmap energyBitmap;

        /// <summary>
        /// Rectangle representing the entire energy bitmap area. Used when drawing background
        /// for energy visualization.
        /// </summary>
        private readonly Int32Rect fullEnergyRect = new Int32Rect(0, 0, EnergyBitmapWidth, EnergyBitmapHeight);

        /// <summary>
        /// Array of background-color pixels corresponding to an area equal to the size of whole energy bitmap.
        /// </summary>
        private readonly byte[] backgroundPixels = new byte[EnergyBitmapWidth * EnergyBitmapHeight];
        private readonly byte[] lineVolumePixels = new byte[EnergyBitmapWidth * 3];
        private readonly byte[] lineVolumePixelsIsSpeaking = new byte[EnergyBitmapWidth * 3];
        private readonly byte[] lineVolumePixelsSoft = new byte[EnergyBitmapWidth * 3];
        private readonly byte[] lineVolumePixelsLoud = new byte[EnergyBitmapWidth * 3];


        /// <summary>
        /// Will be allocated a buffer to hold a single sub frame of audio data read from audio stream.
        /// </summary>
        public byte[] audioBuffer = null;

        /// <summary>
        /// Buffer used to store audio stream energy data as we read audio.
        /// We store 25% more energy values than we strictly need for visualization to allow for a smoother
        /// stream animation effect, since rendering happens on a different schedule with respect to audio
        /// capture.
        /// </summary>
      //  public readonly float[] energy = new float[(uint)(EnergyBitmapWidth * 1.25)];
        public readonly float[] energy = new float[(uint)(EnergyBitmapWidth * 1)];

        /// <summary>
        /// Object for locking energy buffer to synchronize threads.
        /// </summary>
        private readonly object energyLock = new object();

        /// <summary>
        /// Reader for audio frames
        /// </summary>
        public AudioBeamFrameReader reader = null;

        /// <summary>
        /// Array of foreground-color pixels corresponding to a line as long as the energy bitmap is tall.
        /// This gets re-used while constructing the energy visualization.
        /// </summary>
        private byte[] foregroundPixels;

        /// <summary>
        /// Sum of squares of audio samples being accumulated to compute the next energy value.
        /// </summary>
        private float accumulatedSquareSum;

        /// <summary>
        /// Number of audio samples accumulated so far to compute the next energy value.
        /// </summary>
        private int accumulatedSampleCount;

        /// <summary>
        /// Index of next element available in audio energy buffer.
        /// </summary>
        public int energyIndex;

        /// <summary>
        /// Number of newly calculated audio stream energy values that have not yet been
        /// displayed.
        /// </summary>
        private int newEnergyAvailable;

        /// <summary>
        /// Error between time slice we wanted to display and time slice that we ended up
        /// displaying, given that we have to display in integer pixels.
        /// </summary>
       // private float energyError;

        /// <summary>
        /// Last time energy visualization was rendered to screen.
        /// </summary>
       // private DateTime? lastEnergyRefreshTime;

        /// <summary>
        /// Index of first energy element that has never (yet) been displayed to screen.
        /// </summary>
       // private int energyRefreshIndex;

        /// <summary>
        /// Stream for 32b-16b conversion.
        /// </summary>
       // private KinectAudioStream convertStream = null;
        public float averageVolume;

        public VolumeHandler(KinectSensor kinectSensor)
        {
           
            this.kinectSensor = kinectSensor;
            // Get its audio source
            AudioSource audioSource = this.kinectSensor.AudioSource;

            // Allocate 1024 bytes to hold a single audio sub frame. Duration sub frame 
            // is 16 msec, the sample rate is 16khz, which means 256 samples per sub frame. 
            // With 4 bytes per sample, that gives us 1024 bytes.
            this.audioBuffer = new byte[audioSource.SubFrameLengthInBytes];

            // Open the reader for the audio frames
            this.reader = audioSource.OpenReader();

            // PixelFormats.Indexed1;
            this.energyBitmap = new WriteableBitmap(EnergyBitmapWidth, EnergyBitmapHeight, 96, 96, PixelFormats.Indexed4, new BitmapPalette(new List<Color> { Colors.White, Colors.Green, Colors.Red, Colors.LightBlue, Colors.Aquamarine, Colors.Pink, Colors.Orange }));

            // Initialize foreground pixels
            this.foregroundPixels = new byte[EnergyBitmapHeight];
            for (int i = 0; i < this.foregroundPixels.Length; ++i)
            {
                this.foregroundPixels[i] = 0xff;
            }
            for (int i = 0; i < this.lineVolumePixels.Length; ++i)
            {
                this.lineVolumePixels[i] = 0x55;
            }
            for (int i = 0; i < this.lineVolumePixelsIsSpeaking.Length; ++i)
            {
                this.lineVolumePixelsIsSpeaking[i] = 0x11;
            }
            for (int i = 0; i < this.lineVolumePixelsSoft.Length; ++i)
            {
                this.lineVolumePixelsSoft[i] = 0x22;
            }
            for (int i = 0; i < this.lineVolumePixelsLoud.Length; ++i)
            {
                this.lineVolumePixelsLoud[i] = 0x33;
            }

            //    this.kinectImage.Source = this.energyBitmap;
            // CompositionTarget.Rendering += this.UpdateEnergy;



            if (this.reader != null)
            {
                // Subscribe to new audio frame arrived events
                this.reader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        public void close()
        {
            // CompositionTarget.Rendering -= this.UpdateEnergy;

            if (this.reader != null)
            {
                // AudioBeamFrameReader is IDisposable
                this.reader.Dispose();
                this.reader = null;
            }


        }

        /// <summary>
        /// Handles the audio frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void Reader_FrameArrived(object sender, AudioBeamFrameArrivedEventArgs e)
        {
            AudioBeamFrameReference frameReference = e.FrameReference;

            try
            {
                AudioBeamFrameList frameList = frameReference.AcquireBeamFrames();

                if (frameList != null)
                {
                    // AudioBeamFrameList is IDisposable
                    using (frameList)
                    {
                        // Only one audio beam is supported. Get the sub frame list for this beam
                        IReadOnlyList<AudioBeamSubFrame> subFrameList = frameList[0].SubFrames;

                        // Loop over all sub frames, extract audio buffer and beam information
                        foreach (AudioBeamSubFrame subFrame in subFrameList)
                        {

                            subFrame.CopyFrameDataToArray(this.audioBuffer);


                            for (int i = 0; i < this.audioBuffer.Length; i += BytesPerSample)
                            {
                                // Extract the 32-bit IEEE float sample from the byte array
                                float audioSample = BitConverter.ToSingle(this.audioBuffer, i);

                                this.accumulatedSquareSum += audioSample * audioSample;
                                ++this.accumulatedSampleCount;

                                if (this.accumulatedSampleCount < SamplesPerColumn)
                                {
                                    continue;
                                }

                                float meanSquare = this.accumulatedSquareSum / SamplesPerColumn;

                                if (meanSquare > 1.0f)
                                {
                                    //    // A loud audio source right next to the sensor may result in mean square values
                                    //    // greater than 1.0. Cap it at 1.0f for display purposes.
                                    meanSquare = 1.0f;
                                }

                                // Calculate energy in dB, in the range [MinEnergy, 0], where MinEnergy < 0
                                float energy = MinEnergy;

                                if (meanSquare > 0)
                                {
                                    energy = (float)(10.0 * Math.Log10(meanSquare));
                                }

                                lock (this.energyLock)
                                {
                                    // Normalize values to the range [0, 1] for display
                                    this.energy[this.energyIndex] = (MinEnergy - energy) / MinEnergy;
                                    //  this.energy[this.energyIndex] = energy;
                                    this.energyIndex = (this.energyIndex + 1) % this.energy.Length;
                                    ++this.newEnergyAvailable;
                                }

                                this.accumulatedSquareSum = 0;
                                this.accumulatedSampleCount = 0;
                            }
                            analyzeAudio();
                        }


                    }
                }
            }
            catch (Exception)
            {
                // Ignore if the frame is no longer available
            }
        }

        public void analyzeAudio()
        {
   
    
           float averageVolumeTemp = 0;

            int x = 0;
            for (int i = 0; i < energy.Length; i++)
            {

                averageVolumeTemp = averageVolumeTemp + energy[i];
                    x++;
               

            }
            averageVolume = averageVolumeTemp / x;
        }

     }
  

}
