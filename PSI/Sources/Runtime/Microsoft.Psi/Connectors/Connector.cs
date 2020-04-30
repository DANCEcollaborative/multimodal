﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    /// <summary>
    /// A pass-through component, that can relay messages from one pipeline to another and can be used when
    /// writing composite components via subpipelines. The composite component can create input and output
    /// connectors instead of receivers.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    public sealed class Connector<T> : IProducer<T>, IConsumer<T>, IConnector
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Connector{T}"/> class.
        /// </summary>
        /// <param name="from">The source pipeline.</param>
        /// <param name="to">The target pipeline.</param>
        /// <param name="name">The name of the connector.</param>
        public Connector(Pipeline from, Pipeline to, string name = null)
        {
            this.Out = to.CreateEmitter<T>(this, name ?? $"connector-{from.Name}->{to.Name}");
            this.In = from.CreateReceiver<T>(this, (m, e) => this.Out.Post(m, e.OriginatingTime), name);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Connector{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to create the connector in.</param>
        /// <param name="name">The name of the connector.</param>
        public Connector(Pipeline pipeline, string name = null)
            : this(pipeline, pipeline, name ?? $"connector-{pipeline.Name}")
        {
        }

        /// <summary>
        /// Gets the connector input.
        /// </summary>
        public Receiver<T> In { get; }

        /// <summary>
        /// Gets the connector output.
        /// </summary>
        public Emitter<T> Out { get; }
    }
}