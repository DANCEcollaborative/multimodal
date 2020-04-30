using Accord.Video.FFMPEG;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Components;
using System;

namespace FetchStoreSample
{
    public class AudioSaver : SimpleConsumer<AudioBuffer>
    {
        private VideoFileWriter writer;
        private DateTime first;
        private bool isFirst;
        private DateTime last;
        private double totalDuration;

        public AudioSaver(Pipeline p, VideoFileWriter writer)
            : base(p)
        {
            this.writer = writer;
            this.last = new DateTime();
            this.first = new DateTime();
            this.isFirst = true;
            this.totalDuration = 0;
        }

        public override void Receive(Message<AudioBuffer> message)
        {
            var aud = message.Data;
            this.totalDuration += aud.Duration.TotalSeconds;
            if (this.isFirst)
            {
                this.isFirst = false;
                this.first = message.Time;
            }
            Console.WriteLine($"clip duration: {aud.Duration.TotalSeconds}, " +
                $"difference: {message.Time.Subtract(this.last).TotalSeconds}, " +
                $"total duration: {this.totalDuration}, " +
                $"total difference: {message.Time.Subtract(this.first).TotalSeconds}");
            this.last = message.Time;
            Console.WriteLine(aud.Data.Length);
            this.writer.WriteAudioFrame(aud.Data);
        }
    }
}