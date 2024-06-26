#nullable enable

using System;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace Hedwig.RTSCore.Controller
{
    public class BasicLauncherController : ControllerBase, ILauncherController
    {
        [SerializeField]
        Transform? mazzle;

        MeshRenderer? mazzleMeshRenderer;

        ITransform _mazzleTranform = new CachedTransform();
        IDisposable? _disposable;

        void OnDestroy()
        {
            this.clearHandler();
        }

        void clearHandler()
        {
            if (_disposable != null)
            {
                _disposable.Dispose();
                _disposable = null;
            }
        }

        void setupHandler(ITransformProvider? target)
        {
            if (target != null)
            {
                _disposable = target.Transform.OnPositionChanged.Subscribe(pos =>
                {
                    transform.LookAt(pos);
                }).AddTo(this);
                transform.LookAt(target.Transform.Position);
            }
        }

        #region ILauncher

        ITransform ILauncherController.Mazzle { get => _mazzleTranform; }

         void ILauncherController.Initialize(ILauncher launcher)
        {
            if (mazzle != null)
            {
                _mazzleTranform.Initialize(mazzle);
                mazzleMeshRenderer = mazzle.GetComponent<MeshRenderer>();
            }

            launcher.CanFire.Subscribe(v => {
                if(mazzleMeshRenderer!=null) {
                    mazzleMeshRenderer.material.color = (!v) ? Color.red : Color.white;
                }
            }).AddTo(this);

            launcher.OnTargetChanged.Subscribe(v =>
            {
                this.clearHandler();
                this.setupHandler(v);
            });
        }

        #endregion
    }
}