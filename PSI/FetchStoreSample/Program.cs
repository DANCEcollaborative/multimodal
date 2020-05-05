using Microsoft.Psi;
using Microsoft.Psi.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Video.FFMPEG;
using Microsoft.Psi.Audio;

namespace FetchStoreSample
{
    class Program
    {
        static string AppName = "FetchStoreSample";

        static void Main(string[] args)
        {
            using (var pipeline = Pipeline.Create())
            {
                var data = Store.Open(pipeline, "WebcamWithAudioSample", "C:\\\\Users\\thisiswys\\Videos\\WebcamWithAudioSample.0001");
                var count = 0;
                foreach (var stream in data.AvailableStreams)
                {
                    Console.WriteLine($"ID: {stream.Id}");
                    Console.WriteLine($"Name: {stream.Name}");
                    Console.WriteLine($"TypeName: {stream.TypeName}");
                    Console.WriteLine($"MessageCount: {stream.MessageCount}");
                    Console.WriteLine($"AverageFrequency: {stream.AverageFrequency}");
                    Console.WriteLine($"AverageLatency: {stream.AverageLatency}");
                    Console.WriteLine($"AverageMessageSize: {stream.AverageMessageSize}");
                    Console.WriteLine($"FirstMessageOriginatingTime: {stream.FirstMessageOriginatingTime}");
                    Console.WriteLine($"LastMessageOriginatingTime: {stream.LastMessageOriginatingTime}");
                    Console.WriteLine($"IsClosed: {stream.IsClosed}");
                    Console.WriteLine($"IsIndexed: {stream.IsIndexed}");
                    Console.WriteLine($"IsPersisted: {stream.IsPersisted}");
                    Console.WriteLine($"IsPolymorphic: {stream.IsPolymorphic}");
                    if (stream.IsPolymorphic)
                    {
                        Console.WriteLine("RuntimeTypes:");
                        foreach (var type in stream.RuntimeTypes.Values)
                        {
                            Console.WriteLine(type);
                        }
                    }
                    count++;
                }
                VideoFileWriter writer = new VideoFileWriter();
                var video = data.OpenStream<Shared<EncodedImage>>("Image");
                writer.Open("C:\\\\Users\\thisiswys\\Videos\\a1.mp4", 1920, 1080, 30, VideoCodec.Default, 32500000, AudioCodec.MP3, 128000, 16000, 1);
                ImageSaver imgSaver = new ImageSaver(pipeline, writer);
                video.PipeTo(imgSaver);
                var audio = data.OpenStream<AudioBuffer>("Audio");
                AudioSaver audioWriter = new AudioSaver(pipeline, writer);
                audio.PipeTo(audioWriter);

                pipeline.Run();
                Console.WriteLine(imgSaver.number);
                writer.Close();
                Console.WriteLine($"Count: {count}");
                Console.ReadLine();
            }
        }
    }
}
