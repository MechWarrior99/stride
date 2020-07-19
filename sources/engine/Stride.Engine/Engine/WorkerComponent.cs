// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Audio;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization.Contents;
using Stride.Core.MicroThreading;
using Stride.Core.Collections;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Profiling;
using Stride.Rendering;
using Stride.Rendering.Sprites;
using Stride.Streaming;

namespace Stride.Engine
{
    [DataContract(Inherited = true)]
    [DefaultEntityComponentProcessor(typeof(WorkerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [Display(Expand = ExpandRule.Once)]
    [AllowMultipleComponents]
    [ComponentOrder(1000)]
    [ComponentCategory("Scripts")]
    public abstract class WorkerComponent : EntityComponent, ICollectorHolder
    {
        public const uint LiveScriptingMask = 128;

        internal PriorityQueueNode<SchedulerEntry> UpdateSchedulerNode;
        internal PriorityQueueNode<SchedulerEntry> StartSchedulerNode;

        /// <summary>
        /// The global profiling key for scripts. Activate/deactivate this key to activate/deactivate profiling for all your scripts.
        /// </summary>
        public static readonly ProfilingKey ScriptGlobalProfilingKey = new ProfilingKey("Script");

        private static readonly Dictionary<Type, ProfilingKey> ScriptToProfilingKey = new Dictionary<Type, ProfilingKey>();

        private ProfilingKey profilingKey;
        private Logger logger;
        private int priority;
        private ObjectCollector collector;
        private IGraphicsDeviceService graphicsDeviceService;

        /// <summary>
        /// Gets the profiling key to activate/deactivate profiling for the current script class.
        /// </summary>
        [DataMemberIgnore]
        public ProfilingKey ProfilingKey
        {
            get 
            {
                if (profilingKey != null)
                    return profilingKey;

                var scriptType = GetType();
                if (!ScriptToProfilingKey.TryGetValue(scriptType, out profilingKey))
                {
                    profilingKey = new ProfilingKey(ScriptGlobalProfilingKey, scriptType.FullName);
                    ScriptToProfilingKey[scriptType] = profilingKey;
                }

                return profilingKey;
            }
        }

        [DataMemberIgnore]
        protected Logger Log
        {
            get
            {
                if (logger != null)
                {
                    return logger;
                }

                var className = GetType().FullName;
                logger = GlobalLogger.GetLogger(className);
                return logger;
            }
        }

        /// <summary>
        /// The priority this script will be scheduled with (compared to other scripts).
        /// </summary>
        /// <userdoc>The execution priority for this script. It applies to async, sync and startup scripts. Lower values mean earlier execution.</userdoc>
        [DefaultValue(0)]
        [DataMember(10000)]
        public int Priority
        {
            get { return priority; }
            set { priority = value; OnPriorityUpdated(); }
        }

        /// <summary>
        /// The object collector associated with this script.
        /// </summary>
        [DataMemberIgnore]
        public ObjectCollector Collector
        {
            get
            {
                collector.EnsureValid();
                return collector;
            }
        }

        [DataMemberIgnore]
        public IServiceRegistry Services { get; private set; }

        [DataMemberIgnore]
        public IGame Game { get; private set; }

        [DataMemberIgnore]
        public ContentManager Content { get; private set; }

        [DataMemberIgnore]
        public SceneSystem SceneSystem { get; private set; }

        [DataMemberIgnore]
        public WorkerSystem WorkerSystem { get; private set; }

        [DataMemberIgnore]
        public EffectSystem EffectSystem { get; private set; }

        [DataMemberIgnore]
        public AudioSystem Audio { get; private set; }

        [DataMemberIgnore]
        public InputManager Input { get; private set; }

        [DataMemberIgnore]
        public SpriteAnimationSystem SpriteAnimation { get; private set; }

        [DataMemberIgnore]
        public GraphicsDevice GraphicsDevice => graphicsDeviceService?.GraphicsDevice;

        [DataMemberIgnore]
        public GameProfilingSystem GameProfiler { get; private set; }

        [DataMemberIgnore]
        public DebugTextSystem DebugText { get; private set; }

        /// <summary>
        /// Gets the streaming system.
        /// </summary>
        /// <value>The streaming system.</value>
        [DataMemberIgnore]
        public StreamingManager Streaming { get; private set; }

        /// <summary>
        /// Determines whether the script is currently undergoing live reloading.
        /// </summary>
        public bool IsLiveReloading { get; internal set; }

        public WorkerComponent()
        {
        }

        internal void Initialize(IServiceRegistry registry)
        {
            Services = registry;

            graphicsDeviceService = Services.GetSafeServiceAs<IGraphicsDeviceService>();

            Game = Services.GetSafeServiceAs<IGame>();
            Content = (ContentManager)Services.GetSafeServiceAs<IContentManager>();
            SceneSystem = Services.GetSafeServiceAs<SceneSystem>();
            WorkerSystem = Services.GetSafeServiceAs<WorkerSystem>();
            EffectSystem = Services.GetSafeServiceAs<EffectSystem>();
            Audio = Services.GetSafeServiceAs<AudioSystem>();
            Input = Services.GetSafeServiceAs<InputManager>();          
            SpriteAnimation = Services.GetSafeServiceAs<SpriteAnimationSystem>();
            Streaming = Services.GetSafeServiceAs<StreamingManager>();
            GameProfiler = Services.GetSafeServiceAs<GameProfilingSystem>();
            DebugText = Services.GetSafeServiceAs<DebugTextSystem>();
        }

        /// <summary>
        /// Internal helper function called when <see cref="Priority"/> is changed.
        /// </summary>
        protected internal virtual void OnPriorityUpdated()
        {
        }
    }
}
