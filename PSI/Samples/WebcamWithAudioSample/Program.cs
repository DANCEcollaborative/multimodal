// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Samples.WebcamWithAudioSample
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Communication;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media;

    /// <summary>
    /// Webcam with audio sample program.
    /// </summary>
    public class Program
    {
        private const string ApplicationName = "WebcamWithAudioSample";

        private static readonly object SendLock = new object();

        private static DateTime frameTime = new DateTime(0);

        private static CommunicationManager manager;

        private static volatile bool sending = false;

        /// <summary>
        /// Main entry point.
        /// </summary>
        public static void Main()
        {
            // Flag to exit the application
            bool exit = false;
            manager = new CommunicationManager();

            while (!exit)
            {
                Console.WriteLine("================================================================================");
                Console.WriteLine("                               Psi Web Camera + Audio Sample");
                Console.WriteLine("================================================================================");
                Console.WriteLine("1) Start Recording. Please any key to finish recording");
                Console.WriteLine("Q) QUIT");
                Console.Write("Enter selection: ");
                ConsoleKey key = Console.ReadKey().Key;
                Console.WriteLine();

                exit = false;
                switch (key)
                {
                    case ConsoleKey.D1:
                        // Record video+audio to a store in the user's MyVideos folder
                        RecordAudioVideo(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
                        break;
                    case ConsoleKey.Q:
                        exit = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Builds and runs a webcam pipeline and records the data to a Psi store.
        /// </summary>
        /// <param name="pathToStore">The path to directory where store should be saved.</param>
        public static void RecordAudioVideo(string pathToStore)
        {
            // Create the pipeline object.
            using (Pipeline pipeline = Pipeline.Create())
            {
                // Register an event handler to catch pipeline errors
                pipeline.PipelineExceptionNotHandled += Pipeline_PipelineException;

                // Register an event handler to be notified when the pipeline completes
                pipeline.PipelineCompleted += Pipeline_PipelineCompleted;

                // Create store
                Data.Exporter store = Store.Create(pipeline, ApplicationName, pathToStore);

                // Create our webcam
                MediaCapture webcam = new MediaCapture(pipeline, 1280, 720, 30, true);
                webcam.Out.Do(SendImage);

                // Create the AudioCapture component to capture audio from the default device in 16 kHz 1-channel
                IProducer<AudioBuffer> audioInput = new AudioCapture(pipeline, new AudioCaptureConfiguration() { OutputFormat = WaveFormat.Create16kHz1Channel16BitPcm() });

                var images = webcam.Out.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;

                // Attach the webcam's image output to the store. We will write the images to the store as compressed JPEGs.
                // Store.Write(images, "Image", store, true, DeliveryPolicy.LatestMessage);

                // Attach the audio input to the store
                // Store.Write(audioInput.Out, "Audio", store, true, DeliveryPolicy.LatestMessage);

                // Run the pipeline
                pipeline.RunAsync();

                Console.WriteLine("Press any key to finish recording");
                Console.ReadKey();
            }
        }

        private static void SendImage(Shared<Image> image, Envelope envelope)
        {
            // manager.SendText("test", "New image received" + image.ToString());
            Image rawData = image.Resource;
            Task task = new Task(() =>
            {
                lock (SendLock)
                {
                    try
                    {
                        rawData = rawData.Scale(0.4f, 0.4f, SamplingMode.Bilinear).Resource;
                        manager.SendImage("testRTSP", rawData);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                    }

                    sending = false;
                }
            });
            if (!sending && envelope.OriginatingTime.CompareTo(frameTime) > 0)
            {
                Console.WriteLine("sending");
                sending = true;
                frameTime = envelope.OriginatingTime;
                task.Start();
            }
        }

        /// <summary>
        /// Event handler for the <see cref="Pipeline.PipelineCompleted"/> event.
        /// </summary>
        /// <param name="sender">The sender which raised the event.</param>
        /// <param name="e">The pipeline completion event arguments.</param>
        private static void Pipeline_PipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            Console.WriteLine("Pipeline execution completed with {0} errors", e.Errors.Count);
        }

        /// <summary>
        /// Event handler for the <see cref="Pipeline.PipelineExceptionNotHandled"/> event.
        /// </summary>
        /// <param name="sender">The sender which raised the event.</param>
        /// <param name="e">The pipeline exception event arguments.</param>
        private static void Pipeline_PipelineException(object sender, PipelineExceptionNotHandledEventArgs e)
        {
            Console.WriteLine(e.Exception);
        }
    }
}
