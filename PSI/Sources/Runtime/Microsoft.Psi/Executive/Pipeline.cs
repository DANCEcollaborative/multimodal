﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Executive;
    using Microsoft.Psi.Scheduling;

    /// <summary>
    /// Represents a graph of components and controls scheduling and message passing.
    /// </summary>
    public class Pipeline : IDisposable
    {
        private static int lastStreamId = 0;
        private static int lastReceiverId = -1;
        private static int lastElementId = -1;
        private static int lastPipelineId = -1;

        private readonly int id;
        private readonly string name;

        /// <summary>
        /// This event becomes set once the pipeline is done.
        /// </summary>
        private readonly ManualResetEvent completed = new ManualResetEvent(false);

        /// <summary>
        /// This event becomes set when the first source component is done.
        /// </summary>
        private readonly ManualResetEvent anyCompleted = new ManualResetEvent(false);

        private readonly KeyValueStore configStore = new KeyValueStore();

        private readonly DeliveryPolicy deliveryPolicy;

        /// <summary>
        /// If set, indicates that the pipeline is in replay mode.
        /// </summary>
        private ReplayDescriptor replayDescriptor;

        private TimeInterval proposedTimeInterval;

        private TimeInterval proposedOriginatingTimeInterval;

        /// <summary>
        /// The list of components.
        /// </summary>
        private ConcurrentQueue<PipelineElement> components = new ConcurrentQueue<PipelineElement>();

        /// <summary>
        /// The list of completable components.
        /// </summary>
        private List<PipelineElement> completableComponents = new List<PipelineElement>();

        private bool finiteSourcePreviouslyCompleted = false;

        private Scheduler scheduler;

        // the context on which message delivery is scheduled
        private SchedulerContext schedulerContext;

        // the context used exclusively for activating components
        private SchedulerContext activationContext;

        private List<Exception> errors = new List<Exception>();

        private State state;

        private bool enableExceptionHandling;

        private Emitter<PipelineDiagnostics> diagnosticsEmitter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pipeline"/> class.
        /// </summary>
        /// <param name="name">Pipeline name.</param>
        /// <param name="deliveryPolicy">Pipeline-level delivery policy.</param>
        /// <param name="threadCount">Number of threads.</param>
        /// <param name="allowSchedulingOnExternalThreads">Whether to allow scheduling on external threads.</param>
        /// <param name="enableDiagnostics">Whether to enable collecting and publishing diagnostics information on the Pipeline.Diagnostics stream.</param>
        /// <param name="diagnosticsConfig">Optional diagnostics configuration information.</param>
        public Pipeline(string name, DeliveryPolicy deliveryPolicy, int threadCount, bool allowSchedulingOnExternalThreads, bool enableDiagnostics = false, DiagnosticsConfiguration diagnosticsConfig = null)
            : this(name, deliveryPolicy, enableDiagnostics ? new DiagnosticsCollector() : null, diagnosticsConfig)
        {
            this.scheduler = new Scheduler(this.ErrorHandler, threadCount, allowSchedulingOnExternalThreads, name);
            this.schedulerContext = new SchedulerContext();

            // stop the scheduler when the main pipeline completes
            this.PipelineCompleted += (sender, args) => this.scheduler.Stop(args.AbandonedPendingWorkitems);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pipeline"/> class.
        /// </summary>
        /// <param name="name">Pipeline name.</param>
        /// <param name="deliveryPolicy">Pipeline-level delivery policy.</param>
        /// <param name="scheduler">Scheduler to be used.</param>
        /// <param name="schedulerContext">The scheduler context.</param>
        /// <param name="diagnosticsCollector">Collector with which to gather diagnostic information.</param>
        /// <param name="diagnosticsConfig">Optional diagnostics configuration information.</param>
        internal Pipeline(string name, DeliveryPolicy deliveryPolicy, Scheduler scheduler, SchedulerContext schedulerContext, DiagnosticsCollector diagnosticsCollector, DiagnosticsConfiguration diagnosticsConfig)
            : this(name, deliveryPolicy, diagnosticsCollector, diagnosticsConfig)
        {
            this.scheduler = scheduler;
            this.schedulerContext = schedulerContext;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pipeline"/> class.
        /// </summary>
        /// <param name="name">Pipeline name.</param>
        /// <param name="deliveryPolicy">Pipeline-level delivery policy.</param>
        /// <param name="diagnosticsCollector">Collector with which to gather diagnostic information.</param>
        /// <param name="diagnosticsConfig">Optional diagnostics configuration information.</param>
        private Pipeline(string name, DeliveryPolicy deliveryPolicy, DiagnosticsCollector diagnosticsCollector, DiagnosticsConfiguration diagnosticsConfig)
        {
            this.id = Interlocked.Increment(ref lastPipelineId);
            this.name = name ?? "default";
            this.deliveryPolicy = deliveryPolicy ?? DeliveryPolicy.Unlimited;
            this.enableExceptionHandling = false;
            this.LatestSourceCompletionTime = DateTime.MinValue;
            this.FinalOriginatingTime = DateTime.MinValue;
            this.state = State.Initial;
            this.activationContext = new SchedulerContext();
            this.DiagnosticsCollector = diagnosticsCollector;
            this.DiagnosticsConfiguration = diagnosticsConfig ?? DiagnosticsConfiguration.Default;
            this.DiagnosticsCollector?.PipelineCreate(this);
            if (!(this is Subpipeline))
            {
                this.Diagnostics = new DiagnosticsSampler(this, this.DiagnosticsCollector, this.DiagnosticsConfiguration).Diagnostics;
            }
        }

        /// <summary>
        /// Event that is raised when the pipeline starts running.
        /// </summary>
        public event EventHandler<PipelineRunEventArgs> PipelineRun;

        /// <summary>
        /// Event that is raised upon pipeline completion.
        /// </summary>
        public event EventHandler<PipelineCompletedEventArgs> PipelineCompleted;

        /// <summary>
        /// Event that is raised upon component completion.
        /// </summary>
        public event EventHandler<ComponentCompletedEventArgs> ComponentCompleted;

        /// <summary>
        /// Event that is raised when one or more unhandled exceptions occur in the pipeline. If a handler is attached
        /// to this event, any unhandled exceptions during pipeline execution will not be thrown, and will instead
        /// be handled by the attached handler. If no handler is attached, unhandled exceptions will be thrown within
        /// the execution context in which the exception occurred if the pipeline was run asynchronously via one of the
        /// RunAsync methods. This could cause the application to terminate abruptly. If the pipeline was run synchronously
        /// via one of the Run methods, an AggregateException will be thrown from the Run method (which may be caught).
        /// </summary>
        public event EventHandler<PipelineExceptionNotHandledEventArgs> PipelineExceptionNotHandled;

        /// <summary>
        /// Enumeration of pipeline states.
        /// </summary>
        private enum State
        {
            Initial,
            Starting,
            Running,
            Stopping,
            Completed,
        }

        /// <summary>
        /// Gets pipeline ID.
        /// </summary>
        public int Id => this.id;

        /// <summary>
        /// Gets pipeline name.
        /// </summary>
        public string Name => this.name;

        /// <summary>
        /// Gets replay descriptor.
        /// </summary>
        public ReplayDescriptor ReplayDescriptor => this.replayDescriptor;

        /// <summary>
        /// Gets emitter producing diagnostics information (must be enabled when running pipeline).
        /// </summary>
        public Emitter<PipelineDiagnostics> Diagnostics
        {
            get
            {
                if (this is Subpipeline)
                {
                    throw new InvalidOperationException("Diagnostics is not supported directly on Subpipelines.");
                }

                return this.diagnosticsEmitter;
            }

            private set
            {
                this.diagnosticsEmitter = value;
            }
        }

        /// <summary>
        /// Gets the pipeline-level delivery policy.
        /// </summary>
        public DeliveryPolicy DeliveryPolicy => this.deliveryPolicy;

        internal bool IsInitial => this.state == State.Initial;

        internal bool IsStarting => this.state == State.Starting;

        internal bool IsRunning => this.state == State.Running;

        internal bool IsStopping => this.state == State.Stopping;

        internal bool IsCompleted => this.state == State.Completed;

        internal Scheduler Scheduler => this.scheduler;

        internal SchedulerContext ActivationContext => this.activationContext;

        internal SchedulerContext SchedulerContext => this.schedulerContext;

        internal Clock Clock => this.scheduler.Clock;

        internal KeyValueStore ConfigurationStore => this.configStore;

        internal ConcurrentQueue<PipelineElement> Components => this.components;

        internal DiagnosticsCollector DiagnosticsCollector { get; set; }

        internal DiagnosticsConfiguration DiagnosticsConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the completion time of the latest completed source component.
        /// </summary>
        protected DateTime LatestSourceCompletionTime { get; set; }

        /// <summary>
        /// Gets or sets originating time of final message scheduled.
        /// </summary>
        protected DateTime FinalOriginatingTime { get; set; }

        /// <summary>
        /// Gets an <see cref="AutoResetEvent"/> that signals when there are no remaining completable components.
        /// </summary>
        /// <remarks>
        /// This is an <see cref="AutoResetEvent"/> rather than a <see cref="ManualResetEvent"/> as we need the
        /// event to trigger one and only one action when signalled.
        /// </remarks>
        protected AutoResetEvent NoRemainingCompletableComponents { get; } = new AutoResetEvent(false);

        /// <summary>
        /// Create pipeline.
        /// </summary>
        /// <param name="name">Pipeline name.</param>
        /// <param name="deliveryPolicy">Pipeline-level delivery policy.</param>
        /// <param name="threadCount">Number of threads.</param>
        /// <param name="allowSchedulingOnExternalThreads">Whether to allow scheduling on external threads.</param>
        /// <param name="enableDiagnostics">Whether to enable collecting and publishing diagnostics information on the Pipeline.Diagnostics stream.</param>
        /// <param name="diagnosticsConfig">Optional diagnostics configuration information.</param>
        /// <returns>Created pipeline.</returns>
        public static Pipeline Create(string name = null, DeliveryPolicy deliveryPolicy = null, int threadCount = 0, bool allowSchedulingOnExternalThreads = false, bool enableDiagnostics = false, DiagnosticsConfiguration diagnosticsConfig = null)
        {
            return new Pipeline(name, deliveryPolicy, threadCount, allowSchedulingOnExternalThreads, enableDiagnostics, diagnosticsConfig);
        }

        /// <summary>
        /// Create pipeline.
        /// </summary>
        /// <param name="name">Pipeline name.</param>
        /// <param name="enableDiagnostics">Whether to enable collecting and publishing diagnostics information on the Pipeline.Diagnostics stream.</param>
        /// <param name="diagnosticsConfig">Optional diagnostics configuration information.</param>
        /// <returns>Created pipeline.</returns>
        public static Pipeline Create(string name, bool enableDiagnostics, DiagnosticsConfiguration diagnosticsConfig = null)
        {
            return Create(name, null, 0, false, enableDiagnostics, diagnosticsConfig);
        }

        /// <summary>
        /// Create pipeline.
        /// </summary>
        /// <param name="enableDiagnostics">Whether to enable collecting and publishing diagnostics information on the Pipeline.Diagnostics stream.</param>
        /// <param name="diagnosticsConfig">Optional diagnostics configuration information.</param>
        /// <returns>Created pipeline.</returns>
        public static Pipeline Create(bool enableDiagnostics, DiagnosticsConfiguration diagnosticsConfig = null)
        {
            return Create(null, enableDiagnostics, diagnosticsConfig);
        }

        /// <summary>
        /// Propose replay time.
        /// </summary>
        /// <param name="activeInterval">Active time interval.</param>
        /// <param name="originatingTimeInterval">Originating time interval.</param>
        public virtual void ProposeReplayTime(TimeInterval activeInterval, TimeInterval originatingTimeInterval)
        {
            if (!activeInterval.LeftEndpoint.Bounded)
            {
                throw new ArgumentException(nameof(activeInterval), "Replay time intervals must have a valid start time.");
            }

            if (!originatingTimeInterval.LeftEndpoint.Bounded)
            {
                throw new ArgumentException(nameof(originatingTimeInterval), "Replay time intervals must have a valid start time.");
            }

            this.proposedTimeInterval = (this.proposedTimeInterval == null) ? activeInterval : TimeInterval.Coverage(new[] { this.proposedTimeInterval, activeInterval });
            this.proposedOriginatingTimeInterval = (this.proposedOriginatingTimeInterval == null) ? originatingTimeInterval : TimeInterval.Coverage(new[] { this.proposedOriginatingTimeInterval, originatingTimeInterval });
        }

        /// <summary>
        /// Creates an input receiver associated with the specified component object.
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this receiver.</typeparam>
        /// <param name="owner">The component that owns the receiver. This is usually the state object that the receiver operates on.
        /// The receivers associated with the same owner are never executed concurrently.</param>
        /// <param name="action">The action to execute when a message is delivered to this receiver.</param>
        /// <param name="name">The debug name of the receiver.</param>
        /// <param name="autoClone">If true, the receiver will clone the message before passing it to the action, which is then responsible for recycling it as needed (using receiver.Recycle).</param>
        /// <returns>A new receiver.</returns>
        public Receiver<T> CreateReceiver<T>(object owner, Action<T, Envelope> action, string name, bool autoClone = false)
        {
            return this.CreateReceiver<T>(owner, m => action(m.Data, m.Envelope), name, autoClone);
        }

        /// <summary>
        /// Creates an input receiver associated with the specified component object.
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this receiver.</typeparam>
        /// <param name="owner">The component that owns the receiver. This is usually the state object that the receiver operates on.
        /// The receivers associated with the same owner are never executed concurrently.</param>
        /// <param name="action">The action to execute when a message is delivered to this receiver.</param>
        /// <param name="name">The debug name of the receiver.</param>
        /// <param name="autoClone">If true, the receiver will clone the message before passing it to the action, which is then responsible for recycling it as needed (using receiver.Recycle).</param>
        /// <returns>A new receiver.</returns>
        public Receiver<T> CreateReceiver<T>(object owner, Action<T> action, string name, bool autoClone = false)
        {
            return this.CreateReceiver<T>(owner, m => action(m.Data), name, autoClone);
        }

        /// <summary>
        /// Creates an input receiver associated with the specified component object.
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this receiver.</typeparam>
        /// <param name="owner">The component that owns the receiver. This is usually the state object that the receiver operates on.
        /// The receivers associated with the same owner are never executed concurrently.</param>
        /// <param name="action">The action to execute when a message is delivered to this receiver.</param>
        /// <param name="name">The debug name of the receiver.</param>
        /// <param name="autoClone">If true, the receiver will clone the message before passing it to the action, which is then responsible for recycling it as needed (using receiver.Recycle).</param>
        /// <returns>A new receiver.</returns>
        public Receiver<T> CreateReceiver<T>(object owner, Action<Message<T>> action, string name, bool autoClone = false)
        {
            PipelineElement node = this.GetOrCreateNode(owner);
            var receiver = new Receiver<T>(Interlocked.Increment(ref lastReceiverId), name, node, owner, action, node.SyncContext, this, autoClone);
            node.AddInput(name, receiver);
            return receiver;
        }

        /// <summary>
        /// Creates an input receiver associated with the specified component object, connected to an async message processing function.
        /// The expected signature of the message processing delegate is: <code>async void Receive(<typeparamref name="T"/> message, Envelope env);</code>
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this receiver.</typeparam>
        /// <param name="owner">The component that owns the receiver. This is usually the state object that the receiver operates on.
        /// The receivers associated with the same owner are never executed concurrently.</param>
        /// <param name="action">The action to execute when a message is delivered to this receiver.</param>
        /// <param name="name">The debug name of the receiver.</param>
        /// <param name="autoClone">If true, the receiver will clone the message before passing it to the action, which is then responsible for recycling it as needed (using receiver.Recycle).</param>
        /// <returns>A new receiver.</returns>
        public Receiver<T> CreateAsyncReceiver<T>(object owner, Func<T, Envelope, Task> action, string name, bool autoClone = false)
        {
            return this.CreateReceiver<T>(owner, m => action(m.Data, m.Envelope).Wait(), name, autoClone);
        }

        /// <summary>
        /// Creates an input receiver associated with the specified component object, connected to an async message processing function.
        /// The expected signature of the message processing delegate is: <code>async void Receive(<typeparamref name="T"/> message);</code>
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this receiver.</typeparam>
        /// <param name="owner">The component that owns the receiver. This is usually the state object that the receiver operates on.
        /// The receivers associated with the same owner are never executed concurrently.</param>
        /// <param name="action">The action to execute when a message is delivered to this receiver.</param>
        /// <param name="name">The debug name of the receiver.</param>
        /// <param name="autoClone">If true, the receiver will clone the message before passing it to the action, which is then responsible for recycling it as needed (using receiver.Recycle).</param>
        /// <returns>A new receiver.</returns>
        public Receiver<T> CreateAsyncReceiver<T>(object owner, Func<T, Task> action, string name, bool autoClone = false)
        {
            return this.CreateReceiver<T>(owner, m => action(m.Data).Wait(), name, autoClone);
        }

        /// <summary>
        /// Creates an input receiver associated with the specified component object, connected to an async message processing function.
        /// The expected signature of the message processing delegate is: <code>async void Receive(Message{<typeparamref name="T"/>} message);</code>
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this receiver.</typeparam>
        /// <param name="owner">The component that owns the receiver. This is usually the state object that the receiver operates on.
        /// The receivers associated with the same owner are never executed concurrently.</param>
        /// <param name="action">The action to execute when a message is delivered to this receiver.</param>
        /// <param name="name">The debug name of the receiver.</param>
        /// <param name="autoClone">If true, the receiver will clone the message before passing it to the action, which is then responsible for recycling it as needed (using receiver.Recycle).</param>
        /// <returns>A new receiver.</returns>
        public Receiver<T> CreateAsyncReceiver<T>(object owner, Func<Message<T>, Task> action, string name, bool autoClone = false)
        {
            return this.CreateReceiver<T>(owner, m => action(m).Wait(), name, autoClone);
        }

        /// <summary>
        /// Create emitter.
        /// </summary>
        /// <typeparam name="T">Type of emitted messages.</typeparam>
        /// <param name="owner">Owner of emitter.</param>
        /// <param name="name">Name of emitter.</param>
        /// <returns>Created emitter.</returns>
        public Emitter<T> CreateEmitter<T>(object owner, string name)
        {
            return this.CreateEmitterWithFixedStreamId<T>(owner, name, Interlocked.Increment(ref lastStreamId));
        }

        /// <summary>
        /// Wait for all components to complete.
        /// </summary>
        /// <param name="millisecondsTimeout">Timeout (milliseconds).</param>
        /// <returns>Success.</returns>
        public bool WaitAll(int millisecondsTimeout = Timeout.Infinite)
        {
            return this.completed.WaitOne(millisecondsTimeout);
        }

        /// <summary>
        /// Wait for any component to complete.
        /// </summary>
        /// <param name="millisecondsTimeout">Timeout (milliseconds).</param>
        /// <returns>Success.</returns>
        public bool WaitAny(int millisecondsTimeout = Timeout.Infinite)
        {
            return this.anyCompleted.WaitOne(millisecondsTimeout);
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            this.Dispose(false);
            this.DiagnosticsCollector?.PipelineDisposed(this);
        }

        /// <summary>
        /// Run pipeline (synchronously).
        /// </summary>
        /// <param name="descriptor">Replay descriptor.</param>
        public void Run(ReplayDescriptor descriptor)
        {
            this.enableExceptionHandling = true; // suppress exceptions while running
            this.RunAsync(descriptor);
            this.WaitAll();
            this.enableExceptionHandling = false;

            // throw any exceptions if running synchronously and there is no PipelineException handler
            this.ThrowIfError();
        }

        /// <summary>
        /// Run pipeline (synchronously).
        /// </summary>
        /// <param name="replayInterval">Time interval within which to replay.</param>
        /// <param name="useOriginatingTime">Whether to use originating time.</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock.</param>
        /// <param name="replaySpeedFactor">Speed factor at which to replay (e.g. 2 for double speed, 0.5 for half speed).</param>
        public void Run(TimeInterval replayInterval = null, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1)
        {
            this.Run(new ReplayDescriptor(replayInterval, useOriginatingTime, enforceReplayClock, replaySpeedFactor));
        }

        /// <summary>
        /// Run pipeline (synchronously).
        /// </summary>
        /// <param name="replayStartTime">Time at which to start replaying.</param>
        /// <param name="useOriginatingTime">Whether to use originating time.</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock.</param>
        /// <param name="replaySpeedFactor">Speed factor at which to replay (e.g. 2 for double speed, 0.5 for half speed).</param>
        public void Run(DateTime replayStartTime, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1)
        {
            this.Run(new ReplayDescriptor(replayStartTime, DateTime.MaxValue, useOriginatingTime, enforceReplayClock, replaySpeedFactor));
        }

        /// <summary>
        /// Run pipeline (synchronously).
        /// </summary>
        /// <param name="replayStartTime">Time at which to start replaying.</param>
        /// <param name="replayEndTime">Time at which to end replaying.</param>
        /// <param name="useOriginatingTime">Whether to use originating time.</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock.</param>
        /// <param name="replaySpeedFactor">Speed factor at which to replay (e.g. 2 for double speed, 0.5 for half speed).</param>
        public void Run(DateTime replayStartTime, DateTime replayEndTime, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1)
        {
            this.Run(new ReplayDescriptor(replayStartTime, replayEndTime, useOriginatingTime, enforceReplayClock, replaySpeedFactor));
        }

        /// <summary>
        /// Run pipeline (synchronously).
        /// </summary>
        /// <param name="duration">Duration (time span) to replay.</param>
        public void Run(TimeSpan duration)
        {
            this.enableExceptionHandling = true; // suppress exceptions while running
            this.RunAsync();
            if (!this.WaitAll((int)duration.TotalMilliseconds))
            {
                this.Stop(this.GetCurrentTime());
            }

            this.enableExceptionHandling = false;

            // throw any exceptions if running synchronously and there is no PipelineException handler
            this.ThrowIfError();
        }

        /// <summary>
        /// Run pipeline (asynchronously).
        /// </summary>
        /// <param name="replayInterval">Time interval within which to replay.</param>
        /// <param name="useOriginatingTime">Whether to use originating time.</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock.</param>
        /// <param name="replaySpeedFactor">Speed factor at which to replay (e.g. 2 for double speed, 0.5 for half speed).</param>
        /// <returns>Disposable used to terminate pipeline.</returns>
        public IDisposable RunAsync(TimeInterval replayInterval = null, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1)
        {
            return this.RunAsync(new ReplayDescriptor(replayInterval, useOriginatingTime, enforceReplayClock, replaySpeedFactor));
        }

        /// <summary>
        /// Run pipeline (asynchronously).
        /// </summary>
        /// <param name="replayStartTime">Time at which to start replaying.</param>
        /// <param name="useOriginatingTime">Whether to use originating time.</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock.</param>
        /// <param name="replaySpeedFactor">Speed factor at which to replay (e.g. 2 for double speed, 0.5 for half speed).</param>
        /// <returns>Disposable used to terminate pipeline.</returns>
        public IDisposable RunAsync(DateTime replayStartTime, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1)
        {
            return this.RunAsync(new ReplayDescriptor(replayStartTime, DateTime.MaxValue, useOriginatingTime, enforceReplayClock, replaySpeedFactor));
        }

        /// <summary>
        /// Run pipeline (asynchronously).
        /// </summary>
        /// <param name="replayStartTime">Time at which to start replaying.</param>
        /// <param name="replayEndTime">Time at which to end replaying.</param>
        /// <param name="useOriginatingTime">Whether to use originating time.</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock.</param>
        /// <param name="replaySpeedFactor">Speed factor at which to replay (e.g. 2 for double speed, 0.5 for half speed).</param>
        /// <returns>Disposable used to terminate pipeline.</returns>
        public IDisposable RunAsync(DateTime replayStartTime, DateTime replayEndTime, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1)
        {
            return this.RunAsync(new ReplayDescriptor(replayStartTime, replayEndTime, useOriginatingTime, enforceReplayClock, replaySpeedFactor));
        }

        /// <summary>
        /// Run pipeline (asynchronously).
        /// </summary>
        /// <param name="descriptor">Replay descriptor.</param>
        /// <returns>Disposable used to terminate pipeline.</returns>
        public virtual IDisposable RunAsync(ReplayDescriptor descriptor)
        {
            return this.RunAsync(descriptor, null);
        }

        /// <summary>
        /// Get current clock time.
        /// </summary>
        /// <returns>Current clock time.</returns>
        public DateTime GetCurrentTime()
        {
            return this.Clock.GetCurrentTime();
        }

        /// <summary>
        /// Get current time, given elapsed ticks.
        /// </summary>
        /// <param name="ticksFromSystemBoot">Ticks elapsed since system boot.</param>
        /// <returns>Current time.</returns>
        public DateTime GetCurrentTimeFromElapsedTicks(long ticksFromSystemBoot)
        {
            return this.Clock.GetTimeFromElapsedTicks(ticksFromSystemBoot);
        }

        /// <summary>
        /// Convert virtual duration to real time.
        /// </summary>
        /// <param name="duration">Duration to convert.</param>
        /// <returns>Converted time span.</returns>
        public TimeSpan ConvertToRealTime(TimeSpan duration)
        {
            return this.Clock.ToRealTime(duration);
        }

        /// <summary>
        /// Convert virtual datetime to real time.
        /// </summary>
        /// <param name="time">Datetime to convert.</param>
        /// <returns>Converted datetime.</returns>
        public DateTime ConvertToRealTime(DateTime time)
        {
            return this.Clock.ToRealTime(time);
        }

        /// <summary>
        /// Convert real timespan to virtual.
        /// </summary>
        /// <param name="duration">Duration to convert.</param>
        /// <returns>Converted time span.</returns>
        public TimeSpan ConvertFromRealTime(TimeSpan duration)
        {
            return this.Clock.ToVirtualTime(duration);
        }

        /// <summary>
        /// Convert real datetime to virtual.
        /// </summary>
        /// <param name="time">Datetime to convert.</param>
        /// <returns>Converted datetime.</returns>
        public DateTime ConvertFromRealTime(DateTime time)
        {
            return this.Clock.ToVirtualTime(time);
        }

        internal Emitter<T> CreateEmitterWithFixedStreamId<T>(object owner, string name, int streamId)
        {
            PipelineElement node = this.GetOrCreateNode(owner);
            var emitter = new Emitter<T>(streamId, owner, node.SyncContext, this);
            node.AddOutput(name, emitter);
            return emitter;
        }

        /// <summary>
        /// Stops the pipeline and removes all connectivity (pipes).
        /// </summary>
        /// <param name="abandonPendingWorkItems">Abandons pending work items.</param>
        internal void Dispose(bool abandonPendingWorkItems)
        {
            if (this.components == null)
            {
                // we never started or we've been already disposed
                return;
            }

            this.Stop(this.GetCurrentTime(), abandonPendingWorkItems);
            this.DisposeComponents();
            this.components = null;
        }

        internal void AddComponent(PipelineElement pe)
        {
            pe.Initialize(this);
            if (pe.StateObject != this)
            {
                this.components.Enqueue(pe);
            }
        }

        /// <summary>
        /// Notify pipeline of component completion along with originating time of final message.
        /// </summary>
        /// <param name="component">Component which has completed.</param>
        /// <param name="finalOriginatingTime">Originating time of final message.</param>
        internal virtual void NotifyCompletionTime(PipelineElement component, DateTime finalOriginatingTime)
        {
            this.CompleteComponent(component, finalOriginatingTime);

            if (this.NoRemainingCompletableComponents.WaitOne(0))
            {
                // stop the pipeline once all finite sources have completed
                if (this.finiteSourcePreviouslyCompleted)
                {
                    ThreadPool.QueueUserWorkItem(_ => this.Stop(this.LatestSourceCompletionTime));
                }
            }
        }

        /// <summary>
        /// Mark the component as completed and update the final message originating time of the pipeline.
        /// </summary>
        /// <param name="component">Component which has completed.</param>
        /// <param name="finalOriginatingTime">Originating time of final message.</param>
        internal void CompleteComponent(PipelineElement component, DateTime finalOriginatingTime)
        {
            if (!component.IsSource)
            {
                return;
            }

            // MaxValue is special; meaning the component was *never* a finite source
            // we simply remove it from the list and continue.
            //
            // An example of such a component is the Subpipeline, which is declared as
            // a finite source, but only truly is if it contains a finite source (which
            // can't be known until runtime).
            var finiteCompletion = finalOriginatingTime != DateTime.MaxValue;

            bool lastRemainingCompletable = false;

            lock (this.completableComponents)
            {
                if (!this.completableComponents.Contains(component))
                {
                    // the component was already removed (e.g. because the pipeline is stopping)
                    return;
                }

                this.completableComponents.Remove(component);
                lastRemainingCompletable = this.completableComponents.Count == 0;

                if (finiteCompletion && finalOriginatingTime > this.LatestSourceCompletionTime)
                {
                    this.LatestSourceCompletionTime = finalOriginatingTime;
                }
            }

            if (finiteCompletion)
            {
                this.anyCompleted.Set();
                this.ComponentCompleted?.Invoke(this, new ComponentCompletedEventArgs(component.Name, finalOriginatingTime));
                this.finiteSourcePreviouslyCompleted = true;
            }

            if (lastRemainingCompletable)
            {
                // signal completion of all completable components
                this.NoRemainingCompletableComponents.Set();
            }
        }

        /// <summary>
        /// Run pipeline (asynchronously).
        /// </summary>
        /// <param name="descriptor">Replay descriptor.</param>
        /// <param name="clock">Clock to use (in the case of a shared scheduler - e.g. subpipeline).</param>
        /// <returns>Disposable used to terminate pipeline.</returns>
        internal IDisposable RunAsync(ReplayDescriptor descriptor, Clock clock)
        {
            this.state = State.Starting;
            descriptor = descriptor ?? ReplayDescriptor.ReplayAll;
            this.replayDescriptor = descriptor.Intersect(descriptor.UseOriginatingTime ? this.proposedOriginatingTimeInterval : this.proposedTimeInterval);

            this.completed.Reset();
            if (clock == null)
            {
                // this is the main pipeline (subpipelines inherit the parent clock)
                clock =
                    this.replayDescriptor.Interval.Left != DateTime.MinValue ?
                    new Clock(this.replayDescriptor.Start, 1 / this.replayDescriptor.ReplaySpeedFactor) :
                    new Clock(default(TimeSpan), 1 / this.replayDescriptor.ReplaySpeedFactor);

                // start the scheduler
                this.scheduler.Start(clock, this.replayDescriptor.EnforceReplayClock);
            }

            this.DiagnosticsCollector?.PipelineStart(this);

            // raise the event prior to starting the components
            this.PipelineRun?.Invoke(this, new PipelineRunEventArgs(this.Clock.GetCurrentTime()));

            this.state = State.Running;

            // keep track of completable source components
            foreach (var component in this.components)
            {
                if (component.IsSource)
                {
                    lock (this.completableComponents)
                    {
                        this.completableComponents.Add(component);
                    }
                }
            }

            // Start scheduling for component activation only - startup of source components will be scheduled, but
            // any other work (e.g. delivery of messages) will be deferred until the schedulerContext is started.
            this.scheduler.StartScheduling(this.activationContext);

            foreach (var component in this.components)
            {
                component.Activate();
            }

            // wait for component activation to finish
            this.scheduler.PauseForQuiescence(this.activationContext);

            // now start scheduling work on the main scheduler context
            this.scheduler.StartScheduling(this.schedulerContext);

            return this;
        }

        /// <summary>
        /// Signal pipeline completion.
        /// </summary>
        /// <param name="abandonPendingWorkitems">Abandons the pending work items.</param>
        internal void Complete(bool abandonPendingWorkitems)
        {
            this.PipelineCompleted?.Invoke(this, new PipelineCompletedEventArgs(this.Clock.GetCurrentTime(), abandonPendingWorkitems, this.errors));
            this.state = State.Completed;
        }

        /// <summary>
        /// Apply action to this pipeline and to all descendent Subpipelines.
        /// </summary>
        /// <param name="action">Action to apply.</param>
        internal void ForThisPipelineAndAllDescendentSubpipelines(Action<Pipeline> action)
        {
            action(this);
            foreach (var sub in this.components.Where(c => c.StateObject is Subpipeline && !c.IsFinalized).Select(c => c.StateObject as Subpipeline))
            {
                sub.ForThisPipelineAndAllDescendentSubpipelines(action);
            }
        }

        /// <summary>
        /// Pause pipeline (and all subpipeline) scheduler for quiescence.
        /// </summary>
        internal void PauseForQuiescence()
        {
            this.ForThisPipelineAndAllDescendentSubpipelines(p => p.scheduler.PauseForQuiescence(p.schedulerContext));
        }

        internal PipelineElement GetOrCreateNode(object component)
        {
            PipelineElement node = this.components.FirstOrDefault(c => c.StateObject == component);
            if (node == null)
            {
                var id = Interlocked.Increment(ref lastElementId);
                var name = component.GetType().Name;
                var fullName = $"{id}.{name}";
                node = new PipelineElement(id, fullName, component);
                this.AddComponent(node);

                if (this.IsRunning && node.IsSource && !(component is Subpipeline))
                {
                    throw new InvalidOperationException($"Source component added when pipeline already running. Consider using Subpipeline.");
                }

                this.DiagnosticsCollector?.PipelineElementCreate(this, node, component);
            }

            return node;
        }

        /// <summary>
        /// Stops the pipeline by disabling message passing between the pipeline components.
        /// The pipeline configuration is not changed and the pipeline can be restarted later.
        /// </summary>
        /// <param name="finalOriginatingTime">
        /// The final originating time of the pipeline. Delivery of messages with originating times
        /// later than the final originating time will no longer be guaranteed.
        /// </param>
        /// <param name="abandonPendingWorkitems">Abandons the pending work items.</param>
        internal virtual void Stop(DateTime finalOriginatingTime, bool abandonPendingWorkitems = false)
        {
            if (this.IsCompleted)
            {
                return;
            }

            if (this.IsStopping)
            {
                this.completed.WaitOne();
                return;
            }

            this.state = State.Stopping;

            // use the supplied final originating time, unless the pipeline has already seen a later source completion time
            this.NotifyPipelineFinalizing(finalOriginatingTime > this.LatestSourceCompletionTime ? finalOriginatingTime : this.LatestSourceCompletionTime);

            // deactivate all components, to disable the generation of new messages from source components
            int count;
            do
            {
                count = 0;
                this.ForThisPipelineAndAllDescendentSubpipelines(p => count += p.DeactivateComponents());
                this.PauseForQuiescence();
            }
            while (count > 0);

            // final call for components to post and cease
            this.FinalizeComponents();
            this.DiagnosticsCollector?.PipelineStopped(this);

            // block until all messages in the pipeline are fully processed
            this.Scheduler.StopScheduling(this.SchedulerContext);

            this.completed.Set();
            this.Complete(abandonPendingWorkitems);
        }

        /// <summary>
        /// Gather all nodes within this pipeline and recursively within subpipelines.
        /// </summary>
        /// <param name="pipeline">Pipeline (or Subpipeline) from which to gather nodes.</param>
        /// <returns>Active nodes.</returns>
        private static IEnumerable<PipelineElement> GatherActiveNodes(Pipeline pipeline)
        {
            foreach (var node in pipeline.components.Where(c => !c.IsFinalized))
            {
                if (node.StateObject is Subpipeline subpipeline)
                {
                    var subnodes = GatherActiveNodes(subpipeline);
                    if (subnodes.Count() > 0)
                    {
                        foreach (var sub in subnodes)
                        {
                            yield return sub;
                        }
                    }
                    else
                    {
                        yield return node; // include subpipeline itself once no more active children
                    }
                }
                else
                {
                    yield return node;
                }
            }
        }

        /// <summary>
        /// Determine whether a node is a Connector component.
        /// </summary>
        /// <param name="node">Element for which to determine whether it represents a Connector component.</param>
        /// <returns>Indication of whether the element represents a Connector component.</returns>
        private static bool IsConnector(PipelineElement node) => node.StateObject is IConnector;

        /// <summary>
        /// Pipeline-bridging Connectors create two nodes; one with inputs in one pipeline and one with outputs in the other.
        /// Here we attempt to find the input side, given a Connector node.
        /// </summary>
        /// <param name="node">Node (representing the output side of a Connector) for which to try to find the matching input node.</param>
        /// <param name="inputConnectors">Known nodes (with inputs) representing Connectors.</param>
        /// <param name="bridge">Populated with input side of Connector bridge if found.</param>
        /// <returns>Indication of whether a bridge has been found.</returns>
        private static bool TryGetConnectorBridge(PipelineElement node, Dictionary<object, PipelineElement> inputConnectors, out PipelineElement bridge)
        {
            bridge = null;
            return node.IsConnector && inputConnectors.TryGetValue(node.StateObject, out bridge);
        }

        /// <summary>
        /// Determine whether a node has *only* cyclic inputs (back to origin; not including upstream independent cycles).
        /// </summary>
        /// <param name="node">Node for which to determine whether it has only cyclic inputs.</param>
        /// <param name="origin">Node from which a potential cycle may originate.</param>
        /// <param name="emitterNodes">Mapping of emitter IDs to corresponding nodes.</param>
        /// <param name="inputConnectors">Known nodes (with inputs) representing Connectors.</param>
        /// <param name="visitedNodes">Used to mark visited nodes; preventing infinitely exploring cycles (upstream from the origin).</param>
        /// <returns>An indication of whether the node has only cyclic inputs.</returns>
        private static bool OnlyCyclicInputs(PipelineElement node, PipelineElement origin, Dictionary<int, PipelineElement> emitterNodes, Dictionary<object, PipelineElement> inputConnectors, HashSet<PipelineElement> visitedNodes)
        {
            if (node.IsFinalized)
            {
                return false;
            }

            if (node == origin)
            {
                return true; // cycle back to origin detected
            }

            if (visitedNodes.Contains(node))
            {
                return false; // upstream cycle
            }

            visitedNodes.Add(node);

            if (node.Inputs.Count > 0)
            {
                bool hasCycle = false; // begin assuming no cycles at all (e.g. no actually wired inputs)
                foreach (var receiver in node.Inputs)
                {
                    var emitter = receiver.Value.Source;
                    if (emitter != null)
                    {
                        PipelineElement parent;
                        if (emitterNodes.TryGetValue(emitter.Id, out parent))
                        {
                            if (OnlyCyclicInputs(parent, origin, emitterNodes, inputConnectors, visitedNodes))
                            {
                                hasCycle = true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }

                return hasCycle;
            }
            else
            {
                // no inputs? perhaps it's the output side of a Connector pair
                // try to get the input side and check it for cycles
                PipelineElement bridge;
                if (TryGetConnectorBridge(node, inputConnectors, out bridge))
                {
                    return OnlyCyclicInputs(bridge, origin, emitterNodes, inputConnectors, visitedNodes);
                }
            }

            return false;
        }

        /// <summary>
        /// Determine whether a node is eligible for finalization (has no inputs from unfinalized node or else only cycles back to self).
        /// </summary>
        /// <param name="node">Node for which to determine eligibility.</param>
        /// <param name="emitterNodes">Mapping of emitter IDs to corresponding nodes.</param>
        /// <param name="inputConnectors">Known nodes (with inputs) representing Connectors.</param>
        /// <param name="includeCycles">Whether to consider nodes that are members of a pure cycle to be finalizable.</param>
        /// <param name="onlyDirectCycles">Whether to consider only direct cycles (node is it's own parent).</param>
        /// <returns>An indication of eligibility for finalization.</returns>
        private static bool IsNodeFinalizable(PipelineElement node, Dictionary<int, PipelineElement> emitterNodes, Dictionary<object, PipelineElement> inputConnectors, bool includeCycles, bool onlyDirectCycles)
        {
            if (node.IsFinalized)
            {
                return false; // already done
            }

            if (node.Inputs.Count > 0)
            {
                foreach (var receiver in node.Inputs)
                {
                    var emitter = receiver.Value.Source;
                    if (emitter != null)
                    {
                        if (!includeCycles)
                        {
                            return false; // has active source (and irrelevant whether cyclic)
                        }

                        PipelineElement parent;
                        if (emitterNodes.TryGetValue(emitter.Id, out parent))
                        {
                            if (onlyDirectCycles)
                            {
                                if (parent != node)
                                {
                                    return false;
                                }
                            }
                            else if (!OnlyCyclicInputs(parent, node, emitterNodes, inputConnectors, new HashSet<PipelineElement>()))
                            {
                                return false; // has as least one active non-cyclic source
                            }
                        }
                        else
                        {
                            return false; // has inactive source but has not finished unsubscribing
                        }
                    }
                }
            }
            else
            {
                PipelineElement bridge;
                if (TryGetConnectorBridge(node, inputConnectors, out bridge))
                {
                    return IsNodeFinalizable(bridge, emitterNodes, inputConnectors, includeCycles, onlyDirectCycles);
                }
            }

            return true;
        }

        /// <summary>
        /// Deactivates all active components.
        /// </summary>
        /// <returns>Number of components deactivated.</returns>
        private int DeactivateComponents()
        {
            // to avoid deadlocks resulting from component calls to NotifyCompleted, copy the list before calling each source component
            PipelineElement[] components;
            lock (this.components)
            {
                components = this.components.ToArray();
            }

            var count = 0;
            foreach (var component in components)
            {
                if (component.IsActivated)
                {
                    component.Deactivate(this.FinalOriginatingTime);
                    count++;
                }
                else if (component.IsDeactivating)
                {
                    // continue to hold the pipeline open pending deactivation
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Finalize child components.
        /// </summary>
        private void FinalizeComponents()
        {
            IList<PipelineElement> nodes = GatherActiveNodes(this).ToList(); // all non-finalized node within pipeline and subpipelines
            while (nodes.Count() > 0)
            {
                // build emitter ID -> node and connector node mappings
                var emitterNodes = new Dictionary<int, PipelineElement>(); // used to traverse up emitter edge
                var inputConnectors = new Dictionary<object, PipelineElement>(); // used to find the input side of a pipeline-bridging Connector (two nodes; one in each pipeline)
                foreach (var node in nodes)
                {
                    foreach (var output in node.Outputs)
                    {
                        emitterNodes.Add(output.Value.Id, node);
                    }

                    if (node.Inputs.Count > 0 && IsConnector(node))
                    {
                        inputConnectors.Add(node.StateObject, node);
                    }
                }

                // finalize eligible nodes with no active receivers
                var finalizable = nodes.Where(n => IsNodeFinalizable(n, emitterNodes, inputConnectors, false, false));
                if (finalizable.Count() == 0)
                {
                    // try letting messages drain, then look for finalizable nodes again (there may have been closing messages in-flight)
                    this.PauseForQuiescence();
                    finalizable = nodes.Where(n => IsNodeFinalizable(n, emitterNodes, inputConnectors, false, false));
                }

                if (finalizable.Count() == 0)
                {
                    // try eliminating direct cycles first (nodes receiving only from themselves - these are completely safe)
                    finalizable = nodes.Where(n => IsNodeFinalizable(n, emitterNodes, inputConnectors, true, true));
                }

                if (finalizable.Count() == 0)
                {
                    // try eliminating indirect cycles (nodes indirectly from themselves - these finalize in arbitrary order!)
                    finalizable = nodes.Where(n => IsNodeFinalizable(n, emitterNodes, inputConnectors, true, false));
#if DEBUG
                    if (finalizable.Count() > 0)
                    {
                        Debug.WriteLine("FINALIZING INDIRECT CYCLES (UNSAFE ARBITRARY FINALIZATION ORDER)");
                        foreach (var node in finalizable)
                        {
                            Debug.WriteLine($"  FINALIZING {node.Name} {node.StateObject} {node.StateObject.GetType()}");
                        }
                    }
#endif
                }

                if (finalizable.Count() == 0)
                {
                    // finally, eliminate all remaining (cycles and nodes with only cycles upstream - these finalize in semi-arbitrary order!)
                    // finalize remaining nodes in order of number of active outputs (most to least; e.g. terminal nodes last) as a heuristic
                    finalizable = nodes.OrderBy(n => -n.Outputs.Where(o => o.Value.HasSubscribers).Count());
#if DEBUG
                    if (finalizable.Count() > 0)
                    {
                        Debug.WriteLine("ONLY SEPARATED CYCLES REMAINING (UNSAFE ARBITRARY FINALIZATION ORDER)");
                        foreach (var node in finalizable)
                        {
                            Debug.WriteLine($"  FINALIZING {node.Name} {node.StateObject} {node.StateObject.GetType()}");
                        }
                    }
#endif
                }

                foreach (var node in finalizable)
                {
                    node.Final(this.FinalOriginatingTime);
                }

                nodes = GatherActiveNodes(this).ToList(); // all non-finalized node within pipeline and subpipelines
            }
        }

        /// <summary>
        /// Error handler function.
        /// </summary>
        /// <param name="e">Exception to handle.</param>
        /// <returns>Whether exception handled.</returns>
        private bool ErrorHandler(Exception e)
        {
            lock (this.errors)
            {
                this.errors.Add(e);
                if (!this.IsStopping)
                {
                    ThreadPool.QueueUserWorkItem(_ => this.Stop(this.GetCurrentTime()));
                }
            }

            // raise the exception event
            this.PipelineExceptionNotHandled?.Invoke(this, new PipelineExceptionNotHandledEventArgs(e));

            // suppress the exception if there is a handler attached or enableExceptionHandling flag is set
            return this.PipelineExceptionNotHandled != null || this.enableExceptionHandling;
        }

        private void NotifyPipelineFinalizing(DateTime finalOriginatingTime)
        {
            this.FinalOriginatingTime = finalOriginatingTime;
            this.schedulerContext.FinalizeTime = finalOriginatingTime;

            // propagate the final originating time to all descendent subpipelines and their respective scheduler contexts
            foreach (var sub in this.components.Where(c => c.StateObject is Subpipeline && !c.IsFinalized).Select(c => c.StateObject as Subpipeline))
            {
                // if subpipeline is already stopping then don't override its final originating time
                if (!sub.IsStopping)
                {
                    sub.FinalOriginatingTime = finalOriginatingTime;
                    sub.schedulerContext.FinalizeTime = finalOriginatingTime;
                }
            }
        }

        private void DisposeComponents()
        {
            foreach (var component in this.components)
            {
                this.DiagnosticsCollector?.PipelineElementDisposed(this, component);
                component.Dispose();
            }
        }

        private void ThrowIfError()
        {
            if (this.PipelineExceptionNotHandled != null)
            {
                // if exception event is hooked, do not throw the exception
                return;
            }

            lock (this.errors)
            {
                if (this.errors.Count > 0)
                {
                    var error = new AggregateException($"Pipeline '{this.name}' was terminated because of one or more unexpected errors", this.errors);
                    this.errors.Clear();
                    throw error;
                }
            }
        }
    }
}
