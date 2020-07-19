// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Stride.Core.Collections;
using Stride.Core.MicroThreading;
using Stride.Core.Diagnostics;
using Stride.Games;
using Stride.Core;

namespace Stride.Engine.Processors
{
    /// <summary>
    /// The worker system handles worker component scheduling in a game.
    /// </summary>
    public sealed class WorkerSystem : GameSystemBase
    {
        private const long UpdateBit = 1L << 32;

        internal static readonly Logger Log = GlobalLogger.GetLogger(nameof(WorkerSystem));

        /// <summary>
        /// Contains all currently executing scripts.
        /// </summary>
        private readonly HashSet<WorkerComponent> registeredWorkers = new HashSet<WorkerComponent>();
        private readonly HashSet<WorkerComponent> updateWorkers = new HashSet<WorkerComponent>();
        private readonly List<WorkerComponent> updateWorkersLastFrame = new List<WorkerComponent>();
        private readonly HashSet<WorkerComponent> addedWorkers = new HashSet<WorkerComponent>();
        private readonly List<WorkerComponent> addedWorkersLastFrom = new List<WorkerComponent>();

        /// <summary>
        /// Gets the scheduler.
        /// </summary>
        /// <value>The scheduler.</value>
        public Scheduler Scheduler { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameSystemBase" /> class.
        /// </summary>
        /// <param name="registry">The registry.</param>
        /// <remarks>The GameSystem is expecting the following services to be registered: <see cref="IGame" /> and <see cref="ContentManager" />.</remarks>
        public WorkerSystem(IServiceRegistry registry) : base(registry)
        {
            Enabled = true;
            Scheduler = new Scheduler();
            Scheduler.ActionException += SchedulerActionException;
        }

        protected override void Destroy()
        {
            Scheduler.ActionException -= SchedulerActionException;
            Scheduler = null;

            Services.RemoveService<WorkerSystem>();

            base.Destroy();
        }

        public override void Update(GameTime gameTime)
        {
            // Seperate collection so that workers added this frame don't affect us.
            addedWorkersLastFrom.AddRange(addedWorkers);
            addedWorkers.Clear();
            updateWorkersLastFrame.AddRange(updateWorkers);

            // Schedule 
            foreach (WorkerComponent worker in addedWorkersLastFrom)
            {
                if (worker is IStart)
                {
                    Scheduler.Schedule(worker.StartSchedulerNode, ScheduleMode.Last);
                }
                else if (worker is AsyncWorker asyncWorker)
                {
                    asyncWorker.MicroThread = AddTask(asyncWorker.Execute, asyncWorker.Priority & UpdateBit);
                    asyncWorker.MicroThread.ProfilingKey = asyncWorker.ProfilingKey;
                }
            }

            // Schedule existing workers: IUpdate.Update()
            foreach (WorkerComponent worker in updateWorkersLastFrame)
            {
                // Update priority.
                worker.UpdateSchedulerNode.Value.Priority = worker.Priority | UpdateBit;
                // Schedule.
                Scheduler.Schedule(worker.UpdateSchedulerNode, ScheduleMode.Last);
            }

            // Run current micro threads.
            Scheduler.Run();

            // Remove the start schedualer node after it has been executed.
            foreach (WorkerComponent worker in addedWorkersLastFrom)
            {
                worker.StartSchedulerNode = null;
                worker.IsLiveReloading = false;
            }

            addedWorkersLastFrom.Clear();
            updateWorkersLastFrame.Clear();
        }

        /// <summary>
        /// Allows to wait for next frame.
        /// </summary>
        /// <returns>ChannelMicroThreadAwaiter&lt;System.Int32&gt;.</returns>
        public ChannelMicroThreadAwaiter<int> NextFrame()
        {
            return Scheduler.NextFrame();
        }

        /// <summary>
        /// Adds the specified micro thread function.
        /// </summary>
        /// <param name="microThreadFunction">The micro thread function.</param>
        /// <returns>MicroThread.</returns>
        public MicroThread AddTask(Func<Task> microThreadFunction, long priority = 0)
        {
            var microThread = Scheduler.Create();
            microThread.Priority = priority;
            microThread.Start(microThreadFunction);
            return microThread;
        }

        /// <summary>
        /// Add the provided worker to the worker system.
        /// </summary>
        /// <param name="worker">The worker to add.</param>
        public void Add(WorkerComponent worker)
        {
            worker.Initialize(Services);
            registeredWorkers.Add(worker);
            addedWorkers.Add(worker);

            if (worker is AsyncWorker async)
            {
                // AsyncWorkers shouldn't support IStart or IUpdate.
                return;
            }

            if (worker is IStart start)
            {
                worker.StartSchedulerNode = Scheduler.Create(start.Start, worker.Priority, worker, worker.ProfilingKey);
            }

            if (worker is IUpdate update)
            {
                worker.UpdateSchedulerNode = Scheduler.Create(update.Update, worker.Priority & UpdateBit, worker);
                updateWorkers.Add(worker);
            }
        }

        public void Remove(WorkerComponent worker)
        {
            // Make sure it's not registered in any pending list
            bool startWasPending = addedWorkers.Remove(worker);
            bool wasRegistered = registeredWorkers.Remove(worker);

            if (!startWasPending && wasRegistered)
            {
                // Cancel scripts that were already started
                try
                {
                    if (worker is ICancel cancel)
                        cancel.Cancel();
                    worker.Collector.Dispose();
                }
                catch (Exception e)
                {
                    HandleSynchronousException(worker, e);
                }

                if (worker is AsyncWorker asyncWorker)
                    asyncWorker.MicroThread.Cancel();
            }

            // Remove script from the scheduler, in case it was removed during scheduler execution
            if (worker is IUpdate)
            {
                updateWorkers.Remove(worker);
            }

            if (worker is IStart)
            {
                if (worker.StartSchedulerNode != null)
                {
                    Scheduler?.Unschedule(worker.StartSchedulerNode);
                    worker.StartSchedulerNode = null;
                }
            }
        }

        /// <summary>
        /// Called by a live scripting debugger to notify the WorkerSystem about reloaded scripts.
        /// </summary>
        /// <param name="oldWorker">The old script</param>
        /// <param name="newWorker">The new script</param>
        public void LiveReload(WorkerComponent oldWorker, WorkerComponent newWorker)
        {
            // Set live reloading mode for the rest of it's lifetime
            oldWorker.IsLiveReloading = true;

            // Set live reloading mode until after being started
            newWorker.IsLiveReloading = true;
        }

        private void SchedulerActionException(Scheduler scheduler, SchedulerEntry schedulerEntry, Exception e)
        {
            HandleSynchronousException((WorkerComponent)schedulerEntry.Token, e);
        }

        private void HandleSynchronousException(WorkerComponent worker, Exception e)
        {
            Log.Error("Unexpected exception while executing a worker.", e);

            // Only crash if live scripting debugger is not listening
            if (Scheduler.PropagateExceptions)
                ExceptionDispatchInfo.Capture(e).Throw();

            // Remove worker from all lists
            if (worker is IUpdate)
            {
                updateWorkers.Remove(worker);
            }

            registeredWorkers.Remove(worker);
        }

        private class PriorityWorkerComparer : IComparer<WorkerComponent>
        {
            public static readonly PriorityWorkerComparer Default = new PriorityWorkerComparer();

            public int Compare(WorkerComponent x, WorkerComponent y)
            {
                return x.Priority.CompareTo(y.Priority);
            }
        }
    }
}
