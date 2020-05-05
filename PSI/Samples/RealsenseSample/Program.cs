namespace RealsenseSample
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media;
    using Microsoft.Psi.RealSense.Windows;

    class Program
    {
        private const string ApplicationName = "RealsenseSample";
        static void Main(string[] args)
        {
            // Flag to exit the application
            bool exit = false;

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
                Microsoft.Psi.Data.Exporter store = Store.Create(pipeline, ApplicationName, pathToStore);

                // Create our webcam
                RealSenseSensor sensor = new RealSenseSensor(pipeline);

                var color = sensor.ColorImage.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;
                var depth = sensor.DepthImage.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;

                // Create the AudioCapture component to capture audio from the default device in 16 kHz 1-channel
                
                // Attach the webcam's image output to the store. We will write the images to the store as compressed JPEGs.
                Store.Write(color, "ColorImage", store, true, DeliveryPolicy.LatestMessage);
                Store.Write(depth, "DepthImage", store, true, DeliveryPolicy.LatestMessage);

                // Attach the audio input to the store
                // Store.Write(audioInput.Out, "Audio", store, true, DeliveryPolicy.LatestMessage);

                // Run the pipeline
                pipeline.RunAsync();

                Console.WriteLine("Press any key to finish recording");
                Console.ReadKey();
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
