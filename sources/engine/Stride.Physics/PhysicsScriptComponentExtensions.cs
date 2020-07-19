// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;

namespace Stride.Physics
{
    /// <summary>
    /// Extension methods for the <see cref="WorkerComponent"/> related to phystics
    /// </summary>
    public static class PhysicsWorkerComponentExtensions
    {
        /// <summary>
        /// Gets the curent <see cref="Simulation"/>.
        /// </summary>
        /// <param name="workerComponent">The worker component to query physics from</param>
        /// <returns>The simulation object or null if there are no simulation running for the current scene.</returns>
        public static Simulation GetSimulation(this WorkerComponent workerComponent)
        {
            return workerComponent.SceneSystem.SceneInstance.GetProcessor<PhysicsProcessor>()?.Simulation;
        }
    }
}
