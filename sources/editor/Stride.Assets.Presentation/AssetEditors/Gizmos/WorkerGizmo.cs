// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    [GizmoComponent(typeof(WorkerComponent), true)]
    public class WorkerGizmo : BillboardingGizmo<WorkerComponent>
    {
        public WorkerGizmo(EntityComponent component)
            : base(component, "Worker", GizmoResources.ScriptGizmo)
        {
        }
    }
    [GizmoComponent(typeof(UIComponent), true)]
    public class UIGizmo : BillboardingGizmo<UIComponent>
    {
        public UIGizmo(EntityComponent component)
            : base(component, "UI", GizmoResources.UIGizmo)
        {
        }
    }
}
