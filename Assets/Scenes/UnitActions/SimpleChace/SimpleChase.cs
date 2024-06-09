#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Cinemachine;

using VContainer;
using VContainer.Unity;
using UnityExtensions;
using UniRx;
using NaughtyAttributes;

using Hedwig.RTSCore;
using Hedwig.RTSCore.Model;
using Hedwig.RTSCore.InputObservable;

using Cysharp.Threading.Tasks;
using UniRx.Triggers;

public class SimpleChase : LifetimeScope
{
    // Inject
    [SerializeField, InspectInline, Required] UnitManagerObject? UnitManagerObject;
    [SerializeField, InspectInline, Required] UnitObject? playerObject;
    [SerializeField, InspectInline, Required] UnitObject? enemyObject;
    [SerializeField, InspectInline, Required] UnitObject? turretObject;
    [SerializeField, InspectInline, Required] GlobalVisualizersObject? globalVisualizersObject;
    [SerializeField, Required] InputObservableMouseHandler? inputObservableCusrorManager;
    [SerializeField, InspectInline] List<ProjectileObject>? projectileObjects;
    [SerializeField, Required] CinemachineFreeLook? freeLookCamera;

#pragma warning disable CS8618
    [Inject] IUnitManager enemyManager;
    [Inject] IGlobalVisualizerFactory globalVisualizerFactory;
    [Inject] IMouseOperation mouseOperation;
#pragma warning restore CS8618

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Setup(timeManager: null,
            launcherController: null,
            units: new List<UnitObject>() { playerObject!, enemyObject!, turretObject! },
            unitManager: UnitManagerObject,
            visualizers: globalVisualizersObject,
            projectiles: projectileObjects);
        builder.Setup(inputObservableCusrorManager);
    }

    void setupMouse(IMouseOperation mouseOperation, IGlobalVisualizerFactory globalVisualizerFactory, IUnit player, CinemachineFreeLook freeLookCamera, Transform target)
    {
        IPointIndicator? cursor = null;
        IPointIndicator? destination = null;
        Vector3 pos = default;

        bool cameraMode = false;
        // this.UpdateAsObservable().Subscribe(_ =>
        // {
        //     cameraMode = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        // }).AddTo(this);

        mouseOperation.OnMove.Where(_ => !cameraMode).Subscribe(e =>
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

        mouseOperation.OnLeftClick.Where(_ => !cameraMode).Subscribe(e =>
        {
            if (cameraMode) return;
            player.SetDestination(pos);
            if (destination == null)
            {
                destination = globalVisualizerFactory.CreatePointIndicator();
                destination.SetColor(Color.red);
            }
            destination.Move(pos);
        }).AddTo(this);

        // mouseOperation.OnLeftTrigger.Subscribe(v => {
        //     cameraMode = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && v;
        //     Debug.Log($"camera: {cameraMode}, {v}");
        // }).AddTo(this);

        this.UpdateAsObservable().Subscribe(_ =>
        {
            cameraMode = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
            // Debug.Log($"camera: {cameraMode}");
        }).AddTo(this);

        var hratio = 180f / Screen.width;
        var vratio = -1f / Screen.height;
        mouseOperation.OnMoveVec2.Where(_ => cameraMode).Subscribe(v =>
        {
            // Debug.Log($"cameraMove: {v.x}, {hratio * v.x}, cur = {freeLookCamera.m_XAxis.Value}");
            freeLookCamera.m_XAxis.Value += hratio * v.x;
            freeLookCamera.m_YAxis.Value += vratio * v.y;
        }).AddTo(this);

        freeLookCamera.Follow = target;
        freeLookCamera.LookAt = target;
    }

    async void Start()
    {
        if (playerObject == null || enemyObject == null || turretObject == null || globalVisualizerFactory == null || mouseOperation == null || freeLookCamera == null)
        {
            Debug.LogError("Invalid");
            return;
        }
        Debug.Log($"enemyManager = {enemyManager}");
        enemyManager.AutoRegisterUnitsInScene(enemyObject);

        var player = enemyManager.Spawn(playerObject, new Vector3(13.5f, 3, 10.5f), "Player", tag: "P");
        var enemy = enemyManager.Spawn(enemyObject, new Vector3(-10f, 3, -10f), "Enemy", tag: "E");
        var enemy2 = enemyManager.Spawn(enemyObject, new Vector3(-21f, 3, 15f), "Enemy2", tag: "E");
        enemyManager.AutoRegisterUnitsInScene(turretObject);

        bool run = true;
        var (pos, rot) = (Camera.main.transform.position, Camera.main.transform.rotation);
        player.Status.SkipLatestValueOnSubscribe().Subscribe(status => {
            if (status == UnitStatus.Dying)
            {
                Debug.Log("Dying!!");
            }
            if (status == UnitStatus.Dead)
            {
                Debug.Log("Dead!!");
                // Camera.main.ResetPosition(pos, rot);
                run = false;
            }
        }).AddTo(this);

        setupMouse(mouseOperation, globalVisualizerFactory, player, freeLookCamera, player.Transform.Raw);

        await UniTask.NextFrame();

        // Camera.main.Tracking(player.Transform, new Vector3(0, 15, -8), Vector3.right * 60, 1);

        await UniTask.Create(async () =>
        {
            int tick = 100;
            while (run)
            {
                foreach (var enemy in enemyManager.Units)
                {
                    enemy.ActionRunner.DoAction(tick);
                }
                await UniTask.Delay(tick);
            }
        });

        Debug.Log("Game End");
    }
}

