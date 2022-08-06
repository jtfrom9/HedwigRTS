#nullable enable

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Hedwig.RTSCore
{
    public class Selection<T>
    {
        protected IReadOnlyList<T> list;
        protected int _index = -1;
        protected readonly ISubject<T> onCurrentChanged = new Subject<T>();
        protected readonly ISubject<T> onPrevChanged = new Subject<T>();

        public int Index { get => _index; }
        public T Current { get => this.list[_index]; }
        public IObservable<T> OnCurrentChanged { get => onCurrentChanged; }
        public IObservable<T> OnPrevChanged{ get => onPrevChanged; }

        public void Select(int index)
        {
            if (list.Count > 0 && index >= 0 && index < list.Count)
            {
                onPrevChanged.OnNext(Current);
                _index = index;
                onCurrentChanged.OnNext(Current);
            }
        }

        public void Next()
        {
            var cur = Index;
            var next = list.Count - 1 == cur ? 0 : cur + 1;
            Select(next);
        }

        public void Prev()
        {
            var cur = Index;
            var prev = cur == 0 ? list.Count - 1 : cur - 1;
            Select(prev);
        }

        public Selection(IReadOnlyList<T> list)
        {
            if(list.Count<=0) {
                throw new InvalidConditionException("no entries in list");
            }
            this.list = list;
            this._index = 0;
        }
    }

    public class ReactiveSelection<T> : Selection<T>, IDisposable
    {
        IReadOnlyReactiveCollection<T> reactiveList;
        CompositeDisposable disposables = new CompositeDisposable();

        public ReactiveSelection(IReadOnlyReactiveCollection<T> list) :
            base(list.ToList())
        {
            reactiveList = list;

            list.ObserveAdd().Subscribe(e =>
            {
                base.list = reactiveList.ToList();
                if (e.Index <= this._index)
                {
                    base._index++;
                }
            }).AddTo(disposables);

            list.ObserveRemove().Subscribe(e => {
                base.list = reactiveList.ToList();
                if(e.Index == this._index) {
                    onPrevChanged.OnNext(e.Value);
                    onCurrentChanged.OnNext(this.Current);
                }
            }).AddTo(disposables);
        }

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
