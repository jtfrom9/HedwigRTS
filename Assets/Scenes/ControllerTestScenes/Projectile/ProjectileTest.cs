#nullable enable

using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using DG.Tweening;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;
using UniRx;
using UniRx.Triggers;
using UnityExtensions;

using Hedwig.RTSCore.Model;
using Hedwig.RTSCore.Impl;

namespace Hedwig.RTSCore.Test
{
    public class ProjectileTest : LifetimeScope
    {
        [SerializeField, InspectInline]
        UnitObject? defaultUnitObject;

        [SerializeField, InspectInline]
        UnitManagerObject? unitManagerObject;

        [SerializeField, InspectInline]
        GlobalVisualizersObject? globalVisualizersObject;

        [SerializeField, InspectInline]
        List<ProjectileObject> projectileObjects = new List<ProjectileObject>();

        [SerializeField]
        TextMeshProUGUI? textMesh;

        readonly List<IProjectile> liveProjectiles = new List<IProjectile>();

        [Inject] readonly IUnitManager? enemyManager;
        [Inject] readonly ILauncher? launcher;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Setup(timeManager: null,
                launcherController: ControllerBase.Find<ILauncherController>(),
                unit: defaultUnitObject,
                unitManager: unitManagerObject,
                visualizers: globalVisualizersObject,
                projectiles: projectileObjects);
        }

        void Start()
        {
            if (enemyManager == null) return;
            if (defaultUnitObject == null) return;
            if (launcher == null) return;

            enemyManager.AutoRegisterUnitsInScene(defaultUnitObject);
            if (textMesh == null) return;

            foreach(var projectile in projectileObjects)
                setupDebug(projectile);
            setupUI(textMesh, launcher);

            var enemySelection = new ReactiveSelection<IUnit>(enemyManager.Units);
            enemySelection.OnPrevChanged.Subscribe(enemy => {
                (enemy as ISelectable)?.Select(false);
            }).AddTo(this);
            enemySelection.OnCurrentChanged.Subscribe(enemy =>
            {
                (enemy as ISelectable)?.Select(true);
                launcher.SetTarget(enemy.Controller);
            }).AddTo(this);
            enemySelection.Select(0);

            var shotProjectiles = projectileObjects.Where(p => p.Type == ProjectileType.Fire).ToList();
            var projectileSelection = new Selection<ProjectileObject>(shotProjectiles);
            CancellationTokenSource? cts = null;
            projectileSelection.OnCurrentChanged
             .Subscribe(async projectile =>
            {
                // projectileFactory = projectile;
                cts?.Cancel();
                cts = new CancellationTokenSource();
                await launcher.SetProjectileAsync(projectile, cancellationToken: cts.Token);
                cts.Dispose();
                cts = null;
            }).AddTo(this);
            projectileSelection.Select(0);

            this.UpdateAsObservable().Subscribe(_ =>
            {
                var enemy = enemySelection.Current as IUnit;
                if (enemy == null) return;
                _update(launcher, enemy, enemySelection, projectileSelection);
            }).AddTo(this);

            launcher.CanFire.Subscribe(can =>
            {
                if (can)
                {
                    if (launcher.ProjectileData!.Type == ProjectileType.Fire)
                    {
                        launcher.Fire();
                    }
                }
            }).AddTo(this);
        }

        void OnApplicationQuit()
        {
            Debug.Log($"Quit: frame:{Time.frameCount}");
            // launcher?.Dispose();
        }

        void _update(ILauncher launcher, IUnit enemy, Selection<IUnit> enemySelection, Selection<ProjectileObject> configSelection)
        {
            if(Input.GetKeyDown(KeyCode.RightArrow))
            {
                enemySelection.Next();
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                enemySelection.Prev();
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                configSelection.Next();
            }
            if(Input.GetKeyDown(KeyCode.UpArrow))
            {
                configSelection.Prev();
            }
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                foreach(var projectile in liveProjectiles) {
                    projectile.Dispose();
                }
            }
        }

        void setupDebug(ProjectileObject projectileFactory)
        {
            projectileFactory.OnCreated.Subscribe(projectile =>
            {
                int willHitFrame = 0;
                var stopwatch = new System.Diagnostics.Stopwatch();

                liveProjectiles.Add(projectile);

                projectile.OnStarted.Subscribe(_ =>
                {
                    Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} Started");
                    stopwatch.Start();
                }).AddTo(this);

                projectile.OnEnded.Subscribe(_ =>
                {
                    stopwatch.Stop();
                    Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} Ended @({projectile.Controller.Transform.Position}, elapsed:{stopwatch.ElapsedMilliseconds}ms");
                }).AddTo(this);

                projectile.OnDestroy.Subscribe(_ =>
                {
                    Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} Destroy reson:{projectile.EndReason}");
                    liveProjectiles.Remove(projectile);
                }).AddTo(this);

                projectile.Controller.OnEvent.Subscribe(e => {
                    var lateCount = (willHitFrame == 0) ? "-" : (Time.frameCount - willHitFrame).ToString();
                    var transform = projectile.Controller.Transform;
                    switch(e.Type) {
                        case ProjectileEventType.BeforeMove:
                            Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} BeforeMove @{transform.Position} to {e.To}");
                            break;
                        case ProjectileEventType.AfterMove:
                            Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} AfterMove @{transform.Position} (late:{lateCount})");
                            break;
                        case ProjectileEventType.BeforeLastMove:
                            Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} BeforeLastMove @{transform.Position} -> {e.WillHit!.Value.point} (late:{lateCount})");
                            break;
                        case ProjectileEventType.AfterLastMove:
                            Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} AfterLastMove @{transform.Position} (late:{lateCount})");
                            break;
                        case ProjectileEventType.WillHit:
                            Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} WillHit @{transform.Position} ray: {e.Ray}, maxDist: {e.MaxDistance!.Value} point: {e.WillHit!.Value.point} distance: {e.WillHit!.Value.distance}");
                            willHitFrame = Time.frameCount;
                            break;
                        case ProjectileEventType.Trigger:
                            Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} Trigger with <{e.Collider!.gameObject.name}> @{transform.Position} (late:{lateCount})");
                            break;
                        case ProjectileEventType.OnKill:
                            Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} OnKill (late:{lateCount})");
                            break;
                        case ProjectileEventType.OnComplete:
                            Debug.Log($"[{GetHashCode():x}] frame:{Time.frameCount} OnComplete (late:{lateCount})");
                            break;
                        case ProjectileEventType.OnPause:
                            Debug.Log($"[{GetHashCode():x}] frame:{Time.frameCount} OnPause (late:{lateCount})");
                            break;
                    }
                }).AddTo(this);

                projectile.Controller.Transform.OnPositionChanged.Subscribe(pos => {
                    if (pos.z > 1)
                    {
                        Debug.LogWarning($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} {pos}");
                        Debug.Break();
                        projectile.Dispose();
                    }
                }).AddTo(this);

            }).AddTo(this);
        }

        struct Info {
            public string config;
            public ITransformProvider? target;
            public bool canfire;
            public float recastRatio;
            public int elapsed;
            public int maxTime;
        }
        Info info;

        void updateText()
        {
            if(textMesh==null) return;

            string status = (info.canfire) ? " ** Ready **" :
            $" ** Recasting: {info.recastRatio} ({info.elapsed} / {info.maxTime}) ** ";

            textMesh.text = @$"
Weapon: {info.config}
Target: {info.target}
{status}
            ";
        }

        void setupUI(TextMeshProUGUI textMesh, ILauncher launcher)
        {
            launcher.OnProjectileChanged.Where(config => config!=null).Subscribe(config => {
                info.config = config!.Name;
                updateText();
            }).AddTo(this);
            launcher.OnTargetChanged.Where(target => target != null).Subscribe(target => {
                info.target = target!;
                updateText();
            }).AddTo(this);
            launcher.CanFire.Subscribe(can =>
            {
                info.canfire = can;
                updateText();
            }).AddTo(this);
            launcher.OnRecastTimeUpdated.Subscribe(ratio =>
            {
                info.recastRatio = ratio;
                info.elapsed = (int)(ratio * launcher.ProjectileData!.RecastTime /(float)100);
                info.maxTime = launcher.ProjectileData!.RecastTime;
                updateText();
            }).AddTo(this);
        }
    }
}