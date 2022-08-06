#nullable enable

using System;
using UnityEngine;
using UniRx;

namespace Hedwig.RTSCore
{
    public interface ICharactor
    {
        IReadOnlyReactiveProperty<int> Health { get; }
    }
}