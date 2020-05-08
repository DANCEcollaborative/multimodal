﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    /// <summary>
    /// Merge one or more streams (T) into a single stream (Message{T}) interleaved in wall-clock time.
    /// </summary>
    /// <remarks>Messages are produced in the order they arrive, in wall-clock time; not necessarily in originating-time order.</remarks>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class Merge<T> : IProducer<Message<T>>
    {
        private readonly Pipeline pipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="Merge{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to which to attach.</param>
        public Merge(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            this.Out = pipeline.CreateEmitter<Message<T>>(this, nameof(this.Out));
        }

        /// <summary>
        /// Gets the output emitter.
        /// </summary>
        public Emitter<Message<T>> Out { get; }

        /// <summary>
        /// Add input receiver.
        /// </summary>
        /// <param name="name">The unique debug name of the receiver.</param>
        /// <returns>Receiver.</returns>
        public Receiver<T> AddInput(string name)
        {
            return this.pipeline.CreateReceiver<T>(this, this.Receive, name);
        }

        private void Receive(T message, Envelope e)
        {
            this.Out.Post(Message.Create(message, e), this.pipeline.GetCurrentTime());
        }
    }
}