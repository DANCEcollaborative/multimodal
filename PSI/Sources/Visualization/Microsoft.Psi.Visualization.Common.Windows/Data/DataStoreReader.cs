﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Collections;

    /// <summary>
    /// Represents an object used to read data stores. Reads data stores by reading their underlying streams.
    /// Attempts to batch reads through a data store where possible.
    /// </summary>
    public class DataStoreReader : IDisposable
    {
        private ISimpleReader simpleReader;
        private List<ExecutionContext> executionContexts;
        private List<IStreamReader> streamReaders;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataStoreReader"/> class.
        /// </summary>
        /// <param name="storeName">Store name to read.</param>
        /// <param name="storePath">Store path to read.</param>
        /// <param name="simpleReaderType">Simple reader type.</param>
        internal DataStoreReader(string storeName, string storePath, Type simpleReaderType)
        {
            this.simpleReader = (ISimpleReader)simpleReaderType.GetConstructor(new Type[] { }).Invoke(new object[] { });
            this.simpleReader.OpenStore(storeName, storePath);
            this.executionContexts = new List<ExecutionContext>();
            this.streamReaders = new List<IStreamReader>();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // cancel all stream readers
            this.streamReaders?.ForEach(sr => sr.Cancel());

            // dispose and clean up execution contexts
            this.executionContexts.ForEach(ec =>
            {
                try
                {
                    ec.ReadAllTokenSource.Cancel();
                    ec.ReadAllTask.Wait();
                }
                catch (AggregateException)
                {
                }

                ec.Reader.Dispose();
                ec.ReadAllTask.Dispose();
                ec.ReadAllTokenSource.Dispose();
            });

            this.executionContexts.Clear();
            this.executionContexts = null;

            // dispose all stream readers
            this.streamReaders?.ForEach(sr => sr.Dispose());
            this.streamReaders?.Clear();
            this.streamReaders = null;

            // dispose of simple reader
            this.simpleReader?.Dispose();
            this.simpleReader = null;
        }

        /// <summary>
        /// Registers an instant data target to be notified when new data for a stream is available.
        /// </summary>
        /// <typeparam name="TStreamData">The type of data in the stream.</typeparam>
        /// <typeparam name="TTarget">The type of data the instant visualization object requires.</typeparam>
        /// <param name="target">An instant data target that specifies the stream binding, the cursor epsilon, and the callback to call when new data is available.</param>
        /// <param name="viewRange">The initial time range over which data is expected.</param>
        internal void RegisterInstantDataTarget<TStreamData, TTarget>(InstantDataTarget target, TimeInterval viewRange)
        {
            // Get the stream reader.  Note that we don't care about the stream reader's stream adapter
            // because with instant data we always read raw data and adapt the stream later.
            IStreamReader streamReader = this.GetStreamReader<TStreamData>(target.StreamName, null, true);

            // Register the target with the stream reader
            streamReader.RegisterInstantDataTarget<TTarget>(target, viewRange);
        }

        /// <summary>
        /// Unregisters an instant visualization object from being notified when the current value of a stream changes.
        /// </summary>
        /// <param name="registrationToken">The registration token that the target was given when it was initially registered.</param>
        internal void UnregisterInstantDataTarget(Guid registrationToken)
        {
            foreach (IStreamReader streamReader in this.streamReaders)
            {
                streamReader.UnregisterInstantDataTarget(registrationToken);
            }
        }

        /// <summary>
        /// Updates the cursor epsilon for a registered instant visualization object.
        /// </summary>
        /// <param name="registrationToken">The registration token that the target was given when it was initially registered.</param>
        /// <param name="epsilon">A relative time interval specifying the window around a message time that may be considered a match.</param>
        internal void UpdateInstantDataTargetEpsilon(Guid registrationToken, RelativeTimeInterval epsilon)
        {
            foreach (IStreamReader streamReader in this.streamReaders)
            {
                streamReader.UpdateInstantDataTargetEpsilon(registrationToken, epsilon);
            }
        }

        /// <summary>
        /// Notifies the data store the the view range of instant data has changed.
        /// </summary>
        /// <param name="viewRange">The new view range of the navigator.</param>
        internal void OnInstantViewRangeChanged(TimeInterval viewRange)
        {
            foreach (IStreamReader streamReader in this.streamReaders.ToList())
            {
                if (streamReader.HasInstantStreamReaders)
                {
                    streamReader.OnInstantViewRangeChanged(viewRange);
                }
            }
        }

        /// <summary>
        /// Called to ask the reader to read the data for all instant streams.
        /// </summary>
        /// <param name="cursorTime">The time of the visualization container's cursor.</param>
        internal void ReadInstantData(DateTime cursorTime)
        {
            using (ISimpleReader reader = this.simpleReader.OpenNew())
            {
                foreach (IStreamReader streamReader in this.streamReaders.ToList())
                {
                    if (streamReader.HasInstantStreamReaders)
                    {
                        streamReader.ReadInstantData(reader, cursorTime);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a view of the messages identified by the matching parameters and asynchronously fills it in.
        /// View mode can be one of three values:
        ///     Fixed - fixed range based on start and end times
        ///     TailCount - sliding dynamic range that includes the tail of the underlying data based on quantity
        ///     TailRange - sliding dynamic range that includes the tail of the underlying data based on function.
        /// </summary>
        /// <typeparam name="T">The type of the message to read.</typeparam>
        /// <param name="streamBinding">The stream binding inidicating which stream to read from.</param>
        /// <param name="viewMode">Mode the view will be created in.</param>
        /// <param name="startTime">Start time of messages to read.</param>
        /// <param name="endTime">End time of messages to read.</param>
        /// <param name="tailCount">Number of messages to included in tail.</param>
        /// <param name="tailRange">Function to determine range included in tail.</param>
        /// <returns>Observable view of data.</returns>
        internal ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView ReadStream<T>(
            StreamBinding streamBinding,
            ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView.ViewMode viewMode,
            DateTime startTime,
            DateTime endTime,
            uint tailCount,
            Func<DateTime, DateTime> tailRange)
        {
            return this.GetStreamReader<T>(streamBinding.StreamName, streamBinding.StreamAdapter, true).ReadStream<T>(viewMode, startTime, endTime, tailCount, tailRange);
        }

        /// <summary>
        /// Periodically called by the <see cref="DataManager"/> on the UI thread to give data store readers time to process read requests.
        /// </summary>
        internal void Run()
        {
            lock (this.executionContexts)
            {
                // cleanup completed execution contexts
                var completedEcs = new List<ExecutionContext>();
                foreach (var ec in
                    this.executionContexts.Where(ec => ec.ReadAllTask != null && (ec.ReadAllTask.IsCanceled || ec.ReadAllTask.IsCompleted || ec.ReadAllTask.IsFaulted)))
                {
                    ec.Reader.Dispose();
                    ec.ReadAllTask.Dispose();
                    ec.ReadAllTokenSource.Dispose();
                    completedEcs.Add(ec);
                }

                // removed completed execution contexts
                completedEcs.ForEach(cec => this.executionContexts.Remove(cec));
            }

            lock (this.streamReaders)
            {
                IEnumerable<IGrouping<Tuple<DateTime, DateTime>, Tuple<ReadRequest, IStreamReader>>> groups = null;

                // NOTE: We might need to refactor this to avoid changing ReadRequests while we are enumerating over them.
                //       One approach might be adding a back pointer from the ReadRequest to the StreamReader so that the ReadRequest can lock the StreamReader
                // group StreamReaders by start and end time of read requests - a StreamReader can be included more than once if it has a disjointed read requests
                groups = this.streamReaders
                    .Select(sr => sr.ReadRequests.Select(rr => Tuple.Create(rr, sr)))
                    .SelectMany(rr2sr => rr2sr)
                    .GroupBy(rr2sr => Tuple.Create(rr2sr.Item1.StartTime, rr2sr.Item1.EndTime));

                // walk groups of matching start and end time read requests
                foreach (var group in groups)
                {
                    // setup execution context (needed for cleanup)
                    var readAllTokenSource = new CancellationTokenSource();
                    var reader = this.simpleReader.OpenNew();
                    ExecutionContext executionContext = new ExecutionContext() { Reader = reader, ReadAllTokenSource = readAllTokenSource };

                    // open each stream that has a match, and close read request
                    foreach (var rr2sr in group)
                    {
                        var streamReader = rr2sr.Item2;
                        streamReader.OpenStream(executionContext.Reader, rr2sr.Item1.ReadIndicesOnly);
                        streamReader.CompleteReadRequest(rr2sr.Item1.StartTime, rr2sr.Item1.EndTime);
                    }

                    // create new task
                    executionContext.ReadAllTask = Task.Factory.StartNew(() =>
                    {
                        // read all of the data
                        executionContext.Reader.ReadAll(new ReplayDescriptor(group.Key.Item1, group.Key.Item2, true), executionContext.ReadAllTokenSource.Token);
                    });

                    // save execution context for cleanup
                    lock (this.executionContexts)
                    {
                        this.executionContexts.Add(executionContext);
                    }
                }
            }
        }

        /// <summary>
        /// Periodically called by the <see cref="DataManager"/> on the UI thread to give data store readers time dispatch data from internal buffers to views.
        /// </summary>
        internal void DispatchData()
        {
            this.streamReaders.ForEach(sr => sr.DispatchData());
        }

        private IStreamReader GetStreamReader<T>(string streamName, IStreamAdapter streamAdapter, bool createIfNecessary)
        {
            var streamReader = this.streamReaders.Find(sr => sr.StreamName == streamName && sr.StreamAdapterType == streamAdapter?.GetType());

            if (streamReader == null)
            {
                if (createIfNecessary)
                {
                    streamReader = new StreamReader<T>(streamName, streamAdapter);
                    this.streamReaders.Add(streamReader);
                }
                else
                {
                    throw new ArgumentException("No stream reader exists for the stream binding.");
                }
            }

            return streamReader;
        }

        private struct ExecutionContext
        {
            public ISimpleReader Reader;
            public Task ReadAllTask;
            public CancellationTokenSource ReadAllTokenSource;
        }
    }
}
