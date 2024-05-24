#nullable enable

using UnityEngine;

using VContainer;
using VContainer.Unity;
using UnityExtensions;
using UniRx;

using Hedwig.RTSCore;
using Hedwig.RTSCore.Model;
using Hedwig.RTSCore.InputObservable;

using Cysharp.Threading.Tasks;

public class UnitBattle : LifetimeScope
{
    // Inject
    [SerializeField, InspectInline] UnitManagerObject? UnitManagerObject;
    [SerializeField, InspectInline] UnitObject? playerObject;
    [SerializeField, InspectInline] UnitObject? UnitObject;
    [SerializeField, InspectInline] GlobalVisualizersObject? globalVisualizersObject;
    [SerializeField] InputObservableMouseHandler? inputObservableCusrorManager;

    // [SerializeField, InspectInline] List<ProjectileObject> projectiles = new List<ProjectileObject>();
    // [SerializeField] List<Vector3> spawnPoints = new List<Vector3>();

#pragma warning disable CS8618
    [Inject] IUnitManager enemyManager;
    [Inject] IGlobalVisualizerFactory globalVisualizerFactory;
    [Inject] IMouseOperation mouseOperation;
#pragma warning restore CS8618

    protected override void Configure(IContainerBuilder builder)
    {
        builder.SetupUnitManager(UnitManagerObject);
        builder.SetupVisualizer(globalVisualizersObject);
        builder.Setup(inputObservableCusrorManager);
    }

    void setupMouse(IMouseOperation mouseOperation, IGlobalVisualizerFactory globalVisualizerFactory, IUnit player)
    {
        IPointIndicator? cursor = null;
        IPointIndicator? destination = null;
        Vector3 pos = default;
        mouseOperation.OnMove.Subscribe(e =>
        {
            switch (e.type)
            {
                case MouseMoveEventType.Enter:
                    Debug.Log("Enter");
                    if (cursor == null)
                    {
                        cursor = globalVisualizerFactory.CreatePointIndicator();
                        cursor.SetColor(Color.cyan);
                        cursor.Move(e.position);
                        pos = e.position;
                    }
                    break;
                case MouseMoveEventType.Over:
                    cursor?.Move(e.position);
                    pos = e.position;
                    break;
                case MouseMoveEventType.Exit:
                    Debug.Log("Exit");
                    if (cursor != null)
                    {
                        cursor.Dispose();
                        cursor = null;
                    }
                    break;
            }
        }).AddTo(this);

        mouseOperation.OnLeftClick.Subscribe(e =>
        {
            player.SetDestination(pos);
            if (destination == null)
            {
                destination = globalVisualizerFactory.CreatePointIndicator();
                destination.SetColor(Color.red);
            }
            destination.Move(pos);
        }).AddTo(this);
    }

    async void Start()
    {
        if (playerObject == null || UnitObject == null || globalVisualizerFactory == null || mouseOperation == null)
        {
            Debug.LogError("Invalid");
            return;
        }
        Debug.Log($"enemyManager = {enemyManager}");
        enemyManager.Initialize(UnitObject);

        var player = enemyManager.Spawn(playerObject, new Vector3(13.5f, 3, 10.5f), "Player");
        var enemy = enemyManager.Spawn(UnitObject, new Vector3(-10f, 3, -10f), "Enemy");

        setupMouse(mouseOperation, globalVisualizerFactory, player);

        await UniTask.NextFrame();

        UniTask.Create(async () =>
        {
            int tick = 100;
            while (true)
            {
                foreach (var enemy in enemyManager.Units)
                {
                    enemy.ActionRunner.DoAction(tick);
                }
                await UniTask.Delay(tick);
            }
        }).Forget();
    }
}

