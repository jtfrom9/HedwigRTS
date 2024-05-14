#nullable enable

using UnityEngine;

using VContainer;
using VContainer.Unity;
using UnityExtensions;

using Hedwig.RTSCore;
using Hedwig.RTSCore.Model;
using Cysharp.Threading.Tasks;

public class UnitBattle : LifetimeScope
{
    // Inject
    [SerializeField, InspectInline] EnemyManagerObject? enemyManagerObject;
    [SerializeField, InspectInline] EnemyObject? defaultEnemyObject;
    // [SerializeField, InspectInline] GlobalVisualizersObject? globalVisualizersObject;

    // [SerializeField, InspectInline] List<ProjectileObject> projectiles = new List<ProjectileObject>();
    // [SerializeField] List<Vector3> spawnPoints = new List<Vector3>();

    #pragma warning disable CS8618
    [Inject] IEnemyManager enemyManager;
    #pragma warning restore CS8618

    protected override void Configure(IContainerBuilder builder)
    {
        builder.SetupEnemyManager(enemyManagerObject);
        // builder.SetupVisualizer(globalVisualizersObject);
    }

    async void Start()
    {
        if (defaultEnemyObject == null)
        {
            Debug.LogError("Invalid");
            return;
        }
        Debug.Log($"enemyManager = {enemyManager}");
        enemyManager.Initialize(defaultEnemyObject);

        var e1 = enemyManager.Spawn(defaultEnemyObject, new Vector3(9, 3, 17));
        var e2 = enemyManager.Spawn(defaultEnemyObject, new Vector3(-15, 3, -6));

        await UniTask.NextFrame();
        Debug.Log(e1.Transform.Position);
        Debug.Log(e2.Transform.Position);

        UniTask.Create(async () => {
            for (int i = 0; i < 20; i++) {
                await UniTask.Delay(2000);
                var x = -4 + Random.Range(-5, 5);
                var z = 5 + Random.Range(-5, 5);
                e1.SetDestination(new Vector3(x, 0, z));
            }
         }).Forget();

        UniTask.Create(async () =>
        {
            while(true)
            {
                await UniTask.Delay(300);
                e2.SetDestination(e1.Transform!.Position);
            }
        }).Forget();
    }
}

