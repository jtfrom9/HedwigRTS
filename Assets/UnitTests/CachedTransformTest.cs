#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using UniRx;
using UniRx.Triggers;

using Hedwig.RTSCore;

public class CachedTransformTest
{
    public class Dummy : MonoBehaviour
    {
    }

    GameObject makeGameObject()
    {
        var go = new GameObject("Hoge");
        go.AddComponent<Dummy>();
        return go;
    }

    [UnityTest]
    public IEnumerator TestCachedPosition0() => UniTask.ToCoroutine(async () =>
    {
        var go = makeGameObject();

        var pos = go.transform.position;
        var ct = go.CachedTransform();
        await UniTask.NextFrame();

        Assert.AreEqual(pos, ct.Position);
    });

    [UnityTest]
    public IEnumerator TestCachedPosition1() => UniTask.ToCoroutine(async () =>
    {
        var go = makeGameObject();
        var ct = go.CachedTransform();

        int count = 0;
        Vector3 pos = Vector3.zero;
        ct.OnPositionChanged.Subscribe(p =>
        {
            count++;
            pos = p;
        });

        go.transform.Translate(1, 0, 0);
        Assert.AreEqual(0, count);
        Assert.AreEqual(Vector3.zero, pos);

        await UniTask.NextFrame();
        Assert.AreEqual(1, count);
        Assert.AreEqual(new Vector3(1, 0, 0), pos);
    });
}
