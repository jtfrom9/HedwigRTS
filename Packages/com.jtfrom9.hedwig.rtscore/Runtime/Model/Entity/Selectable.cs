#nullable enable

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Hedwig.RTSCore
{
    public interface ISelectable
    {
        IReadOnlyReactiveProperty<bool> selected { get; }
        void Select(bool v);
    }
}