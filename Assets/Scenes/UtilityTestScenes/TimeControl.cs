using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UniRx.Triggers;

static class TweenExtension
{
    public static void Forget(this Tween tween)
    {
        tween.ToUniTask().Forget();
    }
}

public class TimeControl : MonoBehaviour
{
    [SerializeField]
    Transform cube0;

    [SerializeField]
    Transform cube1;
    [SerializeField]
    Transform cube2;

    void Start()
    {
        tween(cube0).Forget();
        tween2(cube1).ToUniTask().Forget();
        navmesh(cube2).Forget();
    }

    async UniTask navmesh(Transform cube) {
        var agent = cube.gameObject.GetComponent<NavMeshAgent>();
        var mr = cube.gameObject.GetComponent<MeshRenderer>();
        mr.material.color = Color.green;

        agent.ObserveEveryValueChanged(agent => agent.velocity).Subscribe(v => {
            Debug.Log($"v = {v.magnitude}");
        }).AddTo(this);


        agent.speed = 10;
        agent.acceleration = 10000;
        agent.SetDestination(new Vector3(cube.transform.position.x, 0, 20));

        await UniTask.Delay(1000);
        agent.speed = 0;
        // agent.enabled = false;
        // var velocity = agent.velocity;
        // // var path = agent.path;
        // // agent.speed = 1;
        agent.isStopped = true;
        mr.material.color = Color.yellow;
        Debug.Log("agent: stop");

        await UniTask.Delay(1000);
        // agent.enabled = true;
        // agent.ResetPath();
        // agent.velocity = velocity;
        // agent.SetDestination(new Vector3(cube.transform.position.x, 0, 20));
        agent.speed = 10;
        agent.isStopped = false;
        Debug.Log("agent: start");
    }

    async UniTask tween(Transform cube)
    {
        var mr = cube.gameObject.GetComponent<MeshRenderer>();
        var count = 0;
        var elapsed = 0f;
        var t = cube.DOMove(Vector3.forward * 20, 2)
            .SetEase(Ease.Linear)
            // .OnUpdate(() =>
            // {
            //     Debug.Log($"[{count}] {Time.deltaTime}");
            //     elapsed += Time.deltaTime;
            //     count++;
            // })
            .OnComplete(() =>
            {
                Debug.Log($"complete. elapsed: {elapsed}");
            });
        var list = DOTween.TweensByTarget(cube);

        await UniTask.Delay(500);
        mr.material.color = Color.red;
        // t.Pause().Forget();
        foreach (var tw in list)
        {
            tw.timeScale = 0.1f;
        }

        await UniTask.Delay(1000);
        mr.material.color = Color.blue;
        // t.Play().Forget();
        foreach (var tw in list)
        {
            tw.timeScale = 1f;
        }
    }


    IEnumerator tween2(Transform cube)
    {
        cube.DOLocalPath(
                new[]
                {
                new Vector3(4f, -1.2f, 0f),
                new Vector3(10f, 0f, 0f),
                new Vector3(5, 1.5f, 0),
                },
                2f, PathType.CatmullRom)
            .SetOptions(true);
        cube.DOLocalRotate(new Vector3(0f, 720f, 0), 2f,
            RotateMode.FastBeyond360);

        // ターゲットにひもづくTweenリストを保持する
        var tweenList = DOTween.TweensByTarget(cube);

        yield return new WaitForSeconds(0.5f);
        foreach (var tween in tweenList)
        {
            // タイムスケールを変更
            tween.timeScale = 0.05f;
        }
        yield return new WaitForSeconds(2f);
        foreach (var tween in tweenList)
        {
            // タイムスケールを元に戻す
            tween.timeScale = 1f;
        }
    }



}
