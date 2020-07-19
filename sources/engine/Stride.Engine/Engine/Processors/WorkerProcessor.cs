// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Engine.Processors
{
    /// <summary>
    /// Manage workers.
    /// </summary>
    public sealed class WorkerProcessor : EntityProcessor<WorkerComponent>
    {
        private WorkerSystem workerSystem;

        public WorkerProcessor()
        {
            // Worker processor always running before others
            Order = -100000;
        }

        protected internal override void OnSystemAdd()
        {
            workerSystem = Services.GetService<WorkerSystem>();
        }

        /// <inheritdoc/>
        protected override void OnEntityComponentAdding(Entity entity, WorkerComponent component, WorkerComponent associatedData)
        {
            // Add current list of workers
            workerSystem.Add(component);
        }

        /// <inheritdoc/>
        protected override void OnEntityComponentRemoved(Entity entity, WorkerComponent component, WorkerComponent associatedData)
        {
            workerSystem.Remove(component);

        }
    }
}
