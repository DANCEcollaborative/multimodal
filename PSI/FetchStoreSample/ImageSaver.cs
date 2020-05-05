using Accord.Video.FFMPEG;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Imaging;
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace FetchStoreSample
{
    public class ImageSaver : SimpleConsumer<Shared<EncodedImage>>
    {
        public int number = 0;
        private DateTime firstTime;
        private bool first = true;
        private int lastFrameNumber;
        private Bitmap lastFrame;
        public VideoFileWriter writer;

        public ImageSaver(Pipeline p, VideoFileWriter writer)
            : base(p) 
        {
            this.number = 0;
            this.firstTime = new DateTime();
            this.first = true;
            this.writer = writer;
        }

        public override void Receive(Message<Shared<EncodedImage>> message)
        {
            //  Console.WriteLine("New Message!");
            var img = message.Data.Resource;
            Bitmap bitmap = (Bitmap)System.Drawing.Image.FromStream(img.GetStream());

            TimeSpan diff;
            if (first)
            {
                diff = new TimeSpan(0);
                this.firstTime = message.OriginatingTime;
                this.first = false;
                this.lastFrameNumber = 0;
                this.lastFrame = (Bitmap)System.Drawing.Image.FromStream(img.GetStream());
                Console.WriteLine($"0, 0");
            }
            else
            {
                diff = message.OriginatingTime.Subtract(this.firstTime);
                int thisFrameNumber = (int)Math.Round(diff.TotalMilliseconds / (1000.0 / 15));
                Console.WriteLine($"{diff.TotalSeconds}, {thisFrameNumber}");
                for (int i = this.lastFrameNumber + 1; i < thisFrameNumber; ++i)
                {
                    Console.WriteLine($"{i}, {this.lastFrame.Width}, {this.lastFrame.Height}");
                    writer.WriteVideoFrame(this.lastFrame);
                }
                this.lastFrameNumber = thisFrameNumber;
                this.lastFrame = (Bitmap)System.Drawing.Image.FromStream(img.GetStream());
            }
            
            writer.WriteVideoFrame(bitmap);
        }
    }
}
