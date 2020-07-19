// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Stride.Engine;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.Assets.Presentation.ValueConverters
{
    public class WorkerToEntityAndWorker : OneWayValueConverter<WorkerToEntityAndWorker>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var worker = (WorkerComponent)value;
            return worker != null && worker.Entity != null ? string.Format("{0}.{1}", worker.Entity.Name, worker.GetType().Name) : "";
        }
    }
}
