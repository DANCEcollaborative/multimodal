// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1118 // Parameter must not span multiple lines

namespace BazaarSpeechSample
{
    using System;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.CognitiveServices.Speech;
    using Microsoft.Psi.Communication;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Speech sample program.
    /// </summary>
    public static class Program
    {
        private const string AppName = "SpeechSample";

        private const string LogPath = @"..\..\..\Data\" + Program.AppName;

        // This field is required if using Azure Speech (option 2 in the sample) and must be a valid key which may
        // be obtained by signing up at https://azure.microsoft.com/en-us/try/cognitive-services/?api=speech-api.
        private static string azureSubscriptionKey = "abee363f8d89444998c5f35b6365ca38";
        private static string azureRegion = "eastus"; // the region to which the subscription is associated (e.g. "westus")

        private static CommunicationManager manager;

#pragma warning disable SA1203 // Constants should appear before fields
        private const string TopicToBazaar = "PSI_Bazaar_Text";
#pragma warning restore SA1203 // Constants should appear before fields
        private const string TopicToPython = "PSI_Python_Image";
        private const string TopicToNVBG = "PSI_NVBG_Location";
        private const string TopicToVHText = "PSI_VHT_Text";
        private const string TopicFromPython = "Python_PSI_Location";
        private const string TopicFromBazaar = "Bazaar_PSI_Text";

        /// <summary>
        /// Main entry point.
        /// </summary>
        public static void Main()
        {
            // The root folder under which data will be logged. This may be set to null.
            string outputLogPath = null;

            // The root folder from which previously logged audio data will be read as input. By default the
            // most recent session will be used. If set to null, live audio from the microphone will be used.
            string inputLogPath = null;

            // Flag to exit the application
            bool exit = false;

            while (!exit)
            {
                Console.WriteLine("================================================================================");
                Console.WriteLine("                               Psi and Bazaar Speech Sample");
                Console.WriteLine("================================================================================");
                Console.WriteLine("2) Speech-To-Text using Azure speech recognizer");
                Console.WriteLine("Q) QUIT");
                Console.Write("Enter selection: ");
                ConsoleKey key = Console.ReadKey().Key;
                Console.WriteLine();

                exit = false;
                switch (key)
                {
                    case ConsoleKey.D2:
                        // Azure speech service requires a valid subscription key
                        if (GetSubscriptionKey() && Initialize())
                        {
                            // Demonstrate the use of the AzureSpeechRecognizer component
                            RunAzureSpeech(outputLogPath, inputLogPath);
                        }

                        break;

                    case ConsoleKey.Q:
                        exit = true;
                        break;

                    default:
                        exit = false;
                        break;
                }
            }
        }

#pragma warning disable SA1400 // Access modifier should be declared
        static bool Initialize()
#pragma warning restore SA1400 // Access modifier should be declared
        {
            Console.Title = Program.AppName;
            if (!GetSubscriptionKey())
            {
                Console.WriteLine("Missing Subscription Key!");
                return false;
            }

            manager = new CommunicationManager();
            manager.subscribe(TopicFromBazaar, ProcessText);
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1121:Use built-in type alias", Justification = "<挂起>")]
        private static void ProcessText(String s)
        {
            if (s != null)
            {
                Console.WriteLine($"Send location message to VHT: multimodal:false;%;identity:someone;%;text:{s}");
                manager.SendText(TopicToVHText, $"multimodal:false;%;identity:someone;%;text:{s}");
            }
        }

#pragma warning disable SA1202 // Elements should be ordered by access
                              /// <summary>
                              /// Builds and runs a speech recognition pipeline using the Azure speech recognizer. Requires a valid Cognitive Services
                              /// subscription key. See https://docs.microsoft.com/en-us/azure/cognitive-services/cognitive-services-apis-create-account.
                              /// </summary>
                              /// <remarks>
                              /// If you are getting a <see cref="System.InvalidOperationException"/> with the message 'AzureSpeechRecognizer returned
                              /// OnConversationError with error code: LoginFailed. Original error text: Transport error', this most likely is due to
                              /// an invalid subscription key. Please check your Azure portal at https://portal.azure.com and ensure that you have
                              /// added a subscription to the Azure Speech API on your account.
                              /// </remarks>
                              /// <param name="outputLogPath">The path under which to write log data.</param>
                              /// <param name="inputLogPath">The path from which to read audio input data.</param>
        public static void RunAzureSpeech(string outputLogPath = null, string inputLogPath = null)
#pragma warning restore SA1202 // Elements should be ordered by access
        {
            // Create the pipeline object.
            using (Pipeline pipeline = Pipeline.Create())
            {
                // Use either live audio from the microphone or audio from a previously saved log
                IProducer<AudioBuffer> audioInput = null;
                if (inputLogPath != null)
                {
                    // Open the MicrophoneAudio stream from the last saved log
                    var store = Store.Open(pipeline, Program.AppName, inputLogPath);
                    audioInput = store.OpenStream<AudioBuffer>($"{Program.AppName}.MicrophoneAudio");
                }
                else
                {
                    // Create the AudioCapture component to capture audio from the default device in 16 kHz 1-channel
                    // PCM format as required by both the voice activity detector and speech recognition components.
                    audioInput = new AudioCapture(pipeline, new AudioCaptureConfiguration() { OutputFormat = WaveFormat.Create16kHz1Channel16BitPcm() });
                }

                // Perform voice activity detection using the voice activity detector component
                var vad = new SystemVoiceActivityDetector(pipeline);
                audioInput.PipeTo(vad);

                // Create Azure speech recognizer component
                var recognizer = new AzureSpeechRecognizer(pipeline, new AzureSpeechRecognizerConfiguration() { SubscriptionKey = Program.azureSubscriptionKey, Region = Program.azureRegion });

                // The input audio to the Azure speech recognizer needs to be annotated with a voice activity flag.
                // This can be constructed by using the Psi Join() operator to combine the audio and VAD streams.
                var annotatedAudio = audioInput.Join(vad);

                // Subscribe the recognizer to the annotated audio
                annotatedAudio.PipeTo(recognizer);

                // Partial and final speech recognition results are posted on the same stream. Here
                // we use Psi's Where() operator to filter out only the final recognition results.
                var finalResults = recognizer.Out.Where(result => result.IsFinal);

                // Print the recognized text of the final recognition result to the console.
                finalResults.Do(result =>
                {
                    manager.SendText("test", result.Text);
                    Console.WriteLine("Send text message to Bazaar:" + result.Text);
                    });

                // Create a data store to log the data to if necessary. A data store is necessary
                // only if output logging is enabled.
                var dataStore = CreateDataStore(pipeline, outputLogPath);

                // Register an event handler to catch pipeline errors
                pipeline.PipelineExceptionNotHandled += Pipeline_PipelineException;

                // Register an event handler to be notified when the pipeline completes
                pipeline.PipelineCompleted += Pipeline_PipelineCompleted;

                // Run the pipeline
                pipeline.RunAsync();

                // Azure speech transcribes speech to text
                Console.WriteLine("Say anything");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
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

        /// <summary>
        /// Create a data store to log stream data to. A data store may be persisted on disk (if outputLogPath is defined),
        /// or it may be an in-memory volatile store. The latter is only required if we are visualizing live data, and
        /// only if we are not already logging data to a persisted store.
        /// </summary>
        /// <param name="pipeline">The Psi pipeline associated with the store.</param>
        /// <param name="outputLogPath">The path to a folder in which a persistent store will be created.</param>
        /// <returns>The store Exporter object if a store was successfully created.</returns>
        private static Exporter CreateDataStore(Pipeline pipeline, string outputLogPath = null)
        {
            // If this is a persisted store, use the application name as the store name. Otherwise, generate
            // a unique temporary name for the volatile store only if we are visualizing live data.
            string dataStoreName = (outputLogPath != null) ? Program.AppName : null;

            // Create the store only if it is needed (logging to disk).
            return (dataStoreName != null) ? Store.Create(pipeline, dataStoreName, outputLogPath) : null;
        }

        /// <summary>
        /// Prompt user to enter Azure Speech subscription key from Cognitive Services. Or just set the AzureSubscriptionKey
        /// static member at the top of this file to avoid having to enter it each time. For more information on how to
        /// register for a subscription, see https://www.microsoft.com/cognitive-services/en-us/sign-up.
        /// </summary>
        /// <returns>
        /// True if <see cref="azureSubscriptionKey"/> contains a non-empty key (the key will not actually be
        /// authenticated until the first attempt to access the speech recognition service). False otherwise.
        /// </returns>
        private static bool GetSubscriptionKey()
        {
            Console.WriteLine("A cognitive services Azure Speech subscription key is required to use this. For more info, see 'https://docs.microsoft.com/en-us/azure/cognitive-services/cognitive-services-apis-create-account'");
            Console.Write("Enter subscription key");
            Console.Write(string.IsNullOrWhiteSpace(Program.azureSubscriptionKey) ? ": " : string.Format(" (current = {0}): ", Program.azureSubscriptionKey));

            // Read a new key or hit enter to keep using the current one (if any)
            string response = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(response))
            {
                Program.azureSubscriptionKey = response;
            }

            Console.Write("Enter region");
            Console.Write(string.IsNullOrWhiteSpace(Program.azureRegion) ? ": " : string.Format(" (current = {0}): ", Program.azureRegion));

            // Read a new key or hit enter to keep using the current one (if any)
            response = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(response))
            {
                Program.azureRegion = response;
            }

            return !string.IsNullOrWhiteSpace(Program.azureSubscriptionKey) && !string.IsNullOrWhiteSpace(Program.azureRegion);
        }
    }
}

#pragma warning restore SA1118 // Parameter must not span multiple lines
