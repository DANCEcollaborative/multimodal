﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Visualization.Collections;

    /// <summary>
    /// Represents an object used to read streams.
    /// </summary>
    public interface IStreamReader : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether this reader has been canceled.
        /// </summary>
        bool IsCanceled { get; }

        /// <summary>
        /// Gets a list of outstanding read requests.
        /// </summary>
        IReadOnlyList<ReadRequest> ReadRequests { get; }

        /// <summary>
        /// Gets the stream adapter type.
        /// </summary>
        Type StreamAdapterType { get; }

        /// <summary>
        /// Gets the stream name.
        /// </summary>
        string StreamName { get; }

        /// <summary>
        /// Gets a value indicating whether the stream reader currently has any instant stream readers.
        /// </summary>
        bool HasInstantStreamReaders { get; }

        /// <summary>
        /// Cancels this reader.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Completes the any read requests identified by the matching start and end times.
        /// </summary>
        /// <param name="startTime">Start time of read requests to complete.</param>
        /// <param name="endTime">End time of read requests to complete.</param>
        void CompleteReadRequest(DateTime startTime, DateTime endTime);

        /// <summary>
        /// Dispatches read data to clients of this reader. Called by <see cref="DataStoreReader"/> on the UI thread to populate data cache.
        /// </summary>
        void DispatchData();

        /// <summary>
        /// Open stream given a reader.
        /// </summary>
        /// <param name="reader">Reader to open stream with.</param>
        /// <param name="useIndex">Indicates reader should read the stream index.</param>
        void OpenStream(ISimpleReader reader, bool useIndex);

        /// <summary>
        /// Registers an instant data target to be notified when new data for a stream is available.
        /// </summary>
        /// <typeparam name="TTarget">The type of data the instant visualization object consumes.</typeparam>
        /// <param name="target">An instant data target that specifies the stream binding, the cursor epsilon, and the callback to call when new data is available.</param>
        /// <param name="viewRange">The initial time range over which data is expected.</param>
        void RegisterInstantDataTarget<TTarget>(InstantDataTarget target, TimeInterval viewRange);

        /// <summary>
        /// Unregisters an instant data target from data notification.
        /// </summary>
        /// <param name="registrationToken">The registration token that the target was given when it was initially registered.</param>
        void UnregisterInstantDataTarget(Guid registrationToken);

        /// <summary>
        /// Updates the cursor epsilon for an instant data target.  Changes to cursor epsilon will
        /// impact which data is served to the instant visualization object for a given cursor time.
        /// </summary>
        /// <param name="registrationToken">The registration token that the target was given when it was initially registered.</param>
        /// <param name="epsilon">A relative time interval specifying the window around a message time that may be considered a match.</param>
        void UpdateInstantDataTargetEpsilon(Guid registrationToken, RelativeTimeInterval epsilon);

        /// <summary>
        /// Reads instant data from the stream at the given cursor time and notifies all registered instant visualization objects of the new data.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="cursorTime">The currenttime at the cursor..</param>
        void ReadInstantData(ISimpleReader reader, DateTime cursorTime);

        /// <summary>
        /// Notifies the data store reader that the range of data that may be of interest to instant data targets has changed.
        /// </summary>
        /// <param name="viewRange">The new view range.</param>
        void OnInstantViewRangeChanged(TimeInterval viewRange);

        /// <summary>
        /// Creates a view of the indices identified by the matching start and end times and asychronously fills it in.
        /// </summary>
        /// <param name="startTime">Start time of indices to read.</param>
        /// <param name="endTime">End time of indices to read.</param>
        /// <returns>Observable view of indices.</returns>
        ObservableKeyedCache<DateTime, IndexEntry>.ObservableKeyedView ReadIndex(DateTime startTime, DateTime endTime);

        /// <summary>
        /// Creates a view of the messages identified by the matching parameters and asynchronously fills it in.
        /// View mode can be one of three values:
        ///     Fixed - fixed range based on start and end times
        ///     TailCount - sliding dynamic range that includes the tail of the underlying data based on quantity
        ///     TailRange - sliding dynamic range that includes the tail of the underlying data based on function.
        /// </summary>
        /// <typeparam name="TItem">The type of the message to read.</typeparam>
        /// <param name="viewMode">Mode the view will be created in.</param>
        /// <param name="startTime">Start time of messages to read.</param>
        /// <param name="endTime">End time of messages to read.</param>
        /// <param name="tailCount">Number of messages to included in tail.</param>
        /// <param name="tailRange">Function to determine range included in tail.</param>
        /// <returns>Observable view of data.</returns>
        ObservableKeyedCache<DateTime, Message<TItem>>.ObservableKeyedView ReadStream<TItem>(
            ObservableKeyedCache<DateTime, Message<TItem>>.ObservableKeyedView.ViewMode viewMode,
            DateTime startTime,
            DateTime endTime,
            uint tailCount,
            Func<DateTime, DateTime> tailRange);
    }
}