#nullable enable

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using UniRx;
using UniRx.Triggers;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;
using DG.Tweening;

using Hedwig.RTSCore.Model;

namespace Hedwig.RTSCore.Test
{
    public class CursorTest : LifetimeScope
    {
        [SerializeField]
        UnitObject? defaultUnitObject;

        [SerializeField]
        UnitManagerObject? unitManagerObject;

        [SerializeField]
        GlobalVisualizersObject? globalVisualizersObject;

        [SerializeField]
        Button? goButton;

        [SerializeField]
        Button? resetButton;

        [SerializeField]
        TMP_Dropdown? dropdown;

        [SerializeField]
        GameObject? bulletPrefab;

        [Inject] IUnitManager? enemyManager;

        [SerializeField]
        Vector3 tower = new Vector3(10, 20, -10);

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Setup(timeManager: null,
                launcherController: null,
                unit: defaultUnitObject,
                unitManager: unitManagerObject,
                visualizers: globalVisualizersObject);
        }

        void Start()
        {
            if (enemyManager == null || defaultUnitObject==null)
            {
                Debug.LogError($"enemyManager: {enemyManager}");
                return;
            }
            enemyManager.AutoRegisterUnitsInScene(defaultUnitObject);

            var selection = new ReactiveSelection<IUnit>(enemyManager.Units);
            selection.OnPrevChanged.Subscribe(e => { (e as ISelectable)?.Select(false); }).AddTo(this);
            selection.OnCurrentChanged.Subscribe(e => { (e as ISelectable)?.Select(true); }).AddTo(this);

            setupUI(selection, enemyManager);

            this.UpdateAsObservable().Subscribe(_ =>
            {
                update(selection, enemyManager);
            }).AddTo(this);
        }

        void resetCam()
        {
            Camera.main.ResetPosition(new Vector3(0, 1, -10), Quaternion.identity);
        }

        void trackCam(ISelectable? selectable)
        {
            var enemy = selectable as IUnit;
            if (enemy == null) return;

            if (towerView)
            {
                Camera.main.MoveWithLookAt(tower, enemy.Controller.Transform.Position, 1);
            }
            else
            {
                if (birdView)
                {
                    Camera.main.Tracking(enemy.Controller.Transform,
                        new Vector3(0, 10, -3),
                        new Vector3(80, 0, 0),
                        1);
                }
                else
                {
                    Camera.main.Tracking(enemy.Controller.Transform,
                        new Vector3(0, 3f, -3),
                        new Vector3(30, 0, 0),
                        1);
                }
            }
        }

        bool tracking { get => dropdown!.options[dropdown.value].text != "Init"; }
        bool birdView { get => dropdown!.options[dropdown.value].text == "Bird"; }
        bool towerView { get => dropdown!.options[dropdown.value].text == "Tower"; }

        void selectNext(Selection<IUnit> selection, IUnitManager enemyManager)
        {
            selection.Next();
            if(tracking) trackCam(selection.Current as ISelectable);
        }

        void selectPrev(Selection<IUnit> selection, IUnitManager enemyManager)
        {
            selection.Prev();
            if (tracking) trackCam(selection.Current as ISelectable);
        }

        void setupUI(Selection<IUnit> selection, IUnitManager enemyManager)
        {
            bool go = false;
            var tmp = goButton!.GetComponentInChildren<TextMeshProUGUI>();
            var ct = new CancellationTokenSource();

            goButton?.OnClickAsObservable().Subscribe(_ =>
            {
                go = !go;
                tmp!.text = (go) ? "Stop" : "Go";
                if (go) {
                    enemyManager.RandomWalk(-10f, 10f, 3000, ct.Token).Forget();
                } else
                {
                    ct.Cancel();
                    ct = new CancellationTokenSource();
                    enemyManager.StopAll();
                }
            }).AddTo(this);

            resetButton?.OnClickAsObservable().Subscribe(_ =>
            {
                foreach (var enemy in enemyManager.Units)
                {
                    enemy.ResetPos();
                }
            }).AddTo(this);

            if (dropdown != null)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData("Init"));
                dropdown.options.Add(new TMP_Dropdown.OptionData("Track"));
                dropdown.options.Add(new TMP_Dropdown.OptionData("Bird"));
                dropdown.options.Add(new TMP_Dropdown.OptionData("Tower"));
            }
            dropdown.ObserveEveryValueChanged(v => v!.value).Subscribe(v =>
            {
                Debug.Log(dropdown!.options[v].text);
                Debug.Log(tracking);
                if (tracking)
                {
                    trackCam(selection.Current as ISelectable);
                }
                else
                {
                    resetCam();
                }
            }).AddTo(this);
        }

        void shot(Selection<IUnit> selection)
        {
            if (!towerView) return;
            if (bulletPrefab == null) return;
            var e = selection.Current as IUnit;
            if (e == null) return;

            var go = Instantiate(bulletPrefab);
            go.transform.position = tower;

            go.transform.DOMove(e.Controller.Transform.Position, 3).OnComplete(() =>
            {
                Destroy(go);
            });

            var start = go.transform.position;
            var end = e.Controller.Transform.Position;
            var dir = end - start;
            Debug.Log(dir.magnitude);

            go.transform.DOPath(new Vector3[]{
                (start + end) / 2 + Vector3.up * 5 + Vector3.right * Random.Range(-1f, 1f),
                end
            }, 3, PathType.CatmullRom).SetEase(Ease.InQuart);
        }

        void aim(Selection<IUnit> selection)
        {
            if (!towerView) return;
            var e = selection.Current as IUnit;
            if (e == null) return;
            Debug.DrawLine(tower, e.Controller.Transform.Position, Color.red, 100);
        }

        void update(Selection<IUnit> selection, IUnitManager enemyManager)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                selectNext(selection, enemyManager);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                selectPrev(selection, enemyManager);
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                shot(selection);
            }
            aim(selection);
        }
    }
}
