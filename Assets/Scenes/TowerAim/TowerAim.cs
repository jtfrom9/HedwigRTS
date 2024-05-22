#nullable enable

using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;
using UniRx;
using UniRx.Triggers;
using UnityExtensions;

using Hedwig.RTSCore;
using Hedwig.RTSCore.InputObservable;
using Hedwig.RTSCore.Usecase;
using Hedwig.RTSCore.Model;

public class TowerAim : LifetimeScope
{
    // UI
    [SerializeField] TextMeshProUGUI? textMesh;

    // Inject
    [SerializeField, InspectInline] UnitManagerObject? UnitManagerObject;
    [SerializeField, InspectInline] UnitObject? defaultUnitObject;
    [SerializeField, InspectInline] EnvironmentObject? environmentObject;
    [SerializeField, InspectInline] GlobalVisualizersObject? globalVisualizersObject;
    [SerializeField, InspectInline] List<ProjectileObject> projectiles = new List<ProjectileObject>();
    [SerializeField] InputObservableMouseHandler? inputObservableCusrorManager;
    [SerializeField] Transform? cameraTarget;
    [SerializeField] List<Vector3> spawnPoints = new List<Vector3>();
    [SerializeField] bool randomWalk = true;
    [SerializeField] int spawnCondition = 10;

#pragma warning disable CS8618
    [Inject] IUnitManager enemyManager;
    [Inject] IMouseOperation mouseOperation;
    [Inject] IGlobalVisualizerFactory globalVisualizerFactory;
    [Inject] ILauncher launcher;
    // [Inject] IEnvironment environment;
#pragma warning restore CS8618

    CompositeDisposable disposables = new CompositeDisposable();

    protected override void Configure(IContainerBuilder builder)
    {
        builder.SetupEnemyManager(UnitManagerObject);
        // builder.SetupEnvironment(environmentObject);
        builder.SetupVisualizer(globalVisualizersObject);
        builder.Setup(inputObservableCusrorManager);
        builder.SetupLauncher();
    }

    void Start()
    {
        if (enemyManager == null || defaultUnitObject==null) return;
        enemyManager.Initialize(defaultUnitObject);

        launcher.Initialize();

        var token = this.GetCancellationTokenOnDestroy();
        if (randomWalk)
        {
            enemyManager.RandomWalk(-10f, 10f, 3000, token).Forget();
        }else
        {
            var gameSenario = new GameSenario(enemyManager,
                defaultUnitObject,
                spawnPoints.ToArray(),
                Vector3.zero,
                spawnCondition);
            var cts = new CancellationTokenSource();
            gameSenario.Run(cts.Token).Forget();
        }

        var projectileSelection = new Selection<ProjectileObject>(projectiles);
        projectileSelection.OnCurrentChanged.Subscribe(p =>
        {
            launcher.SetProjectile(p);
        }).AddTo(this);

        setupKey(projectileSelection, launcher);

        launcher.OnProjectileChanged.Subscribe(p =>
        {
            showInfo(p);
        }).AddTo(this);

        setupMouse(mouseOperation, launcher, globalVisualizerFactory);

        projectileSelection.Select(projectileSelection.Index);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        disposables.Dispose();
    }

    void showInfo(IProjectileData? projectile)
    {
        if(textMesh==null) return;
        if (projectile != null)
        {
            textMesh.text = @$"
Name: {projectile.Name}
Type: {projectile.Type}
Speed: {projectile.BaseSpeed}
Distance: {projectile.Range}
";
        } else {
            textMesh.text = "";
        }
    }

    void setupKey(Selection<ProjectileObject> configSelection, ILauncher launcher)
    {
        this.UpdateAsObservable().Subscribe(_ =>
        {
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                configSelection.Next();
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                configSelection.Prev();
            }
        }).AddTo(this);
    }

    void setupMouse(IMouseOperation mouseOperation, ILauncher launcher, IGlobalVisualizerFactory globalVisualizerFactory)
    {
        IPointIndicator? cursor = null;
        mouseOperation.OnMove.Subscribe(e =>
        {
            switch (e.type)
            {
                case MouseMoveEventType.Enter:
                    if (cursor == null)
                    {
                        cursor = globalVisualizerFactory.CreatePointIndicator();
                        cursor.Move(e.position);
                        launcher.SetTarget(cursor);
                    }
                    break;
                case MouseMoveEventType.Over:
                    cursor?.Move(e.position);
                    break;
                case MouseMoveEventType.Exit:
                    if(cursor!=null) {
                        cursor.Dispose();
                        cursor = null;
                    }
                    break;
            }
        }).AddTo(this);

        mouseOperation.OnLeftClick.Subscribe(_ => {
            if (launcher.CanFire.Value)
            {
                launcher.Fire();
            }
            else
            {
                Debug.LogWarning("Cannot Fire Now");
            }
        }).AddTo(this);

        mouseOperation.OnLeftTrigger.Subscribe(trigger =>
        {
            if (trigger)
            {
                launcher.TriggerOn();
            }
            else
            {
                launcher.TriggerOff();
            }
        }).AddTo(this);

        if (this.cameraTarget != null)
        {
            var speed = 20f;
            this.UpdateAsObservable().Where(_ => cursor != null).Subscribe(_ =>
            {
                var diff = cursor!.Transform.Position - cameraTarget.position;
                if(diff.magnitude < 0.1f) {
                    cameraTarget.position = cursor!.Transform.Position;
                }else
                {
                    cameraTarget.position += diff.normalized * Time.deltaTime * speed;
                }
            }).AddTo(this);
        }
    }
}
