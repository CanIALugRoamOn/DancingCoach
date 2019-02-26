using Accord.Audio;
using Accord.Audio.Formats;
using System;
using System.Media;
using System.IO;
using Accord.Audio.Filters;

namespace DancingTrainer
{
    class AudioManager
    {

        private string pathToSong;
        public string pathToBeat;
        SoundPlayer audioPlayer;
        SoundPlayer beatPlayer;
        WaveDecoder wavDec;
        // Kinect Reader Object

        public AudioManager(string pathToBeat)
        {
            //this.pathToSong = pathToSong;
            this.pathToBeat = pathToBeat;
        }

        [System.ComponentModel.Browsable(false)]
        public IntPtr HDL { get; }



        public Signal readWav(string path)
        {
            Signal audio;
            wavDec = new WaveDecoder(path);
            audio = wavDec.Decode();
            //Accord.Audio.Filters.MonoFilter mf = new Accord.Audio.Filters.MonoFilter();

            //audio = mf.Apply(audio);

            return audio;                    
        }

        public Signal streamToSignal(Stream st)
        {
            Signal sig;
            WaveDecoder dec = new WaveDecoder(st);
            sig = dec.Decode();
            return sig;
        }

        public byte[] floatToByteArray(float[] f)
        {
            var byteArr = new byte[f.Length * sizeof(float)];
            Buffer.BlockCopy(f, 0, byteArr, 0, byteArr.Length);
            return byteArr;
        }

        public Stream signalToStream(Signal sig)
        {   
            Stream s;
            byte[] b = floatToByteArray(sig.ToFloat());
            s = new MemoryStream(b,true);
            return s;
        }

        public Signal addSignals(Signal s1, Signal s2)
        {
            AddFilter ad = new AddFilter(s2);
            return ad.Apply(s1);
        }

        /*
        public Stream addStreams(Stream st1, Stream st2)
        {
            Stream st_new;
            return st_new;
        }
        */

        public void pauseWav()
        {
            audioPlayer.Stop();
            
        }

        public void playWav(string path)
        {
            audioPlayer = new SoundPlayer(path);
            audioPlayer.Play(); 
            
        }  

        private void syncBeatWithKinect()
        {

        }

    }
}
