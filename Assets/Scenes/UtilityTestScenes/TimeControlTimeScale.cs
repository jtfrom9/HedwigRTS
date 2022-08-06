using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UniRx.Triggers;

public class TimeControlTimeScale : MonoBehaviour
{
    [SerializeField] Transform cube0;
    [SerializeField] Transform cube1;

    void Start()
    {
        tween(cube0);
        navmesh(cube1);
        UniTask.Create(async () =>
        {
            await UniTask.Delay(1000);
            Time.timeScale = 0.1f;
            await UniTask.Delay(1000, ignoreTimeScale: true);
            Time.timeScale = 1f;
        }).Forget();
    }

    void navmesh(Transform cube)
    {
        var agent = cube.gameObject.GetComponent<NavMeshAgent>();
        agent.speed = 10;
        agent.acceleration = 10000;
        agent.SetDestination(new Vector3(cube.transform.position.x, 0, 20));
    }

    void tween(Transform cube)
    {
        cube.DOMove(Vector3.forward * 20, 2)
            .SetEase(Ease.Linear);
    }
}
