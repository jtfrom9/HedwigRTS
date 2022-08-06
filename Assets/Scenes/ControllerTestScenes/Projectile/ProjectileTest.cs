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

namespace Hedwig.RTSCore.Test
{
    public class ProjectileTest : LifetimeScope
    {
        [SerializeField, InspectInline]
        EnemyObject? defaultEnemyObject;

        [SerializeField, InspectInline]
        EnemyManagerObject? enemyManagerObject;

        [SerializeField, InspectInline]
        VisualizersObject? visualizersObject;

        [SerializeField, InspectInline]
        List<ProjectileObject> projectileObjects = new List<ProjectileObject>();

        [SerializeField]
        TextMeshProUGUI? textMesh;

        List<IProjectile> liveProjectiles = new List<IProjectile>();

        [Inject] IEnemyManager? enemyManager;
        [Inject] ILauncher? launcher;

        IProjectileFactory? projectileFactory;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance<EnemyManagerObject>(enemyManagerObject!);
            builder.RegisterInstance<VisualizersObject>(visualizersObject!)
                .AsImplementedInterfaces();
            builder.Register<IEnemyManager, EnemyManagerImpl>(Lifetime.Singleton);
            builder.Register<ILauncher, LauncherImpl>(Lifetime.Singleton);
            builder.RegisterInstance<ILauncherController>(ControllerBase.Find<ILauncherController>());
        }

        void Start()
        {
            if (enemyManager == null) return;
            if(defaultEnemyObject==null) return;

            enemyManager.Initialize(defaultEnemyObject);
            if (launcher == null) return;
            launcher.Initialize();
            if (textMesh == null) return;

            foreach(var projectile in projectileObjects)
                setupDebug(projectile);
            setupUI(textMesh, launcher);

            var enemySelection = new ReactiveSelection<IEnemy>(enemyManager.Enemies);
            enemySelection.OnPrevChanged.Subscribe(enemy => {
                (enemy as ISelectable)?.Select(false);
            }).AddTo(this);
            enemySelection.OnCurrentChanged.Subscribe(enemy =>
            {
                (enemy as ISelectable)?.Select(true);
                launcher.SetTarget(enemy.controller);
            }).AddTo(this);
            enemySelection.Select(0);

            var shotProjectiles = projectileObjects.Where(p => p.type == ProjectileType.Fire).ToList();
            var projectileSelection = new Selection<ProjectileObject>(shotProjectiles);
            CancellationTokenSource? cts = null;
            projectileSelection.OnCurrentChanged
             .Subscribe(async projectile =>
            {
                projectileFactory = projectile;
                cts?.Cancel();
                cts = new CancellationTokenSource();
                await launcher.SetProjectileAsync(projectile, cancellationToken: cts.Token);
                cts.Dispose();
                cts = null;
            }).AddTo(this);
            projectileSelection.Select(0);

            this.UpdateAsObservable().Subscribe(_ =>
            {
                var enemy = enemySelection.Current as IEnemy;
                if (enemy == null) return;
                _update(launcher, enemy, enemySelection, projectileSelection);
            }).AddTo(this);

            launcher.CanFire.Subscribe(can =>
            {
                if (can)
                {
                    if (launcher.projectileObject!.type == ProjectileType.Fire)
                    {
                        launcher.Fire();
                    }
                }
            }).AddTo(this);
        }

        void OnApplicationQuit() {
            Debug.Log($"Quit: frame:{Time.frameCount}");
            launcher?.Dispose();
        }

        void _update(ILauncher launcher, IEnemy enemy, Selection<IEnemy> enemySelection, Selection<ProjectileObject> configSelection)
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

        void setupDebug(IProjectileFactory projectileFactory)
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
                    Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} Ended @({projectile.controller.transform.Position}, elapsed:{stopwatch.ElapsedMilliseconds}ms");
                }).AddTo(this);

                projectile.OnDestroy.Subscribe(_ =>
                {
                    Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} Destroy reson:{projectile.EndReason}");
                    liveProjectiles.Remove(projectile);
                }).AddTo(this);

                projectile.controller.OnEvent.Subscribe(e => {
                    var lateCount = (willHitFrame == 0) ? "-" : (Time.frameCount - willHitFrame).ToString();
                    var transform = projectile.controller.transform;
                    switch(e.type) {
                        case Projectile.EventType.BeforeMove:
                            Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} BeforeMove @{transform.Position} to {e.to}");
                            break;
                        case Projectile.EventType.AfterMove:
                            Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} AfterMove @{transform.Position} (late:{lateCount})");
                            break;
                        case Projectile.EventType.BeforeLastMove:
                            Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} BeforeLastMove @{transform.Position} -> {e.willHit!.Value.point} (late:{lateCount})");
                            break;
                        case Projectile.EventType.AfterLastMove:
                            Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} AfterLastMove @{transform.Position} (late:{lateCount})");
                            break;
                        case Projectile.EventType.WillHit:
                            Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} WillHit @{transform.Position} ray: {e.ray}, maxDist: {e.maxDistance!.Value} point: {e.willHit!.Value.point} distance: {e.willHit!.Value.distance}");
                            willHitFrame = Time.frameCount;
                            break;
                        case Projectile.EventType.Trigger:
                            Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} Trigger with <{e.collider!.gameObject.name}> @{transform.Position} (late:{lateCount})");
                            break;
                        case Projectile.EventType.OnKill:
                            Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} OnKill (late:{lateCount})");
                            break;
                        case Projectile.EventType.OnComplete:
                            Debug.Log($"[{GetHashCode():x}] frame:{Time.frameCount} OnComplete (late:{lateCount})");
                            break;
                        case Projectile.EventType.OnPause:
                            Debug.Log($"[{GetHashCode():x}] frame:{Time.frameCount} OnPause (late:{lateCount})");
                            break;
                    }
                }).AddTo(this);

                projectile.controller.transform.OnPositionChanged.Subscribe(pos => {
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
            launcher.OnProjectilehanged.Where(config => config!=null).Subscribe(config => {
                info.config = config!.name;
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
                info.elapsed = (int)(ratio * launcher.projectileObject!.recastTime /(float)100);
                info.maxTime = launcher.projectileObject!.recastTime;
                updateText();
            }).AddTo(this);
        }
    }
}