#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using DG.Tweening;
using Cysharp.Threading.Tasks;
using UniRx;
using UniRx.Triggers;

using Hedwig.RTSCore;

public class PointIndicatorTest : MonoBehaviour
{
    [SerializeField]
    PointIndicator? prefab;

    [SerializeField]
    Button? moveCameraButton;


    void Start()
    {
        showIndicators();
        setupUI();


    // this.UpdateAsObservable().Subscribe(_ => 
    //     {
    //         var obj = GameObject.Find("textObj");
    //         // obj.transform.LookAt(Camera.main.transform);
    //         obj.transform.LookAt(obj.transform.position - Camera.main.transform.position);
    //         // obj.transform.rotation = Camera.main.transform.rotation;
    //     });
    }

    void create(Vector3 pos, string text) {
        if(prefab==null) {
            if (prefab == null)
            {
                throw new InvalidConditionException("no indicator");
            }
        }
        var indicator = Instantiate(prefab, pos, Quaternion.identity);
        indicator.SetText(text);
        indicator.SetCamera(Camera.main);
        indicator.SetColor(Random.ColorHSV());
        // indicator.SetOffset(new Vector3(0, 1, 0));
    }

    void showIndicators()
    {
        var points = new Vector3[] {
            Vector3.zero,
            new Vector3(10, 0, 3),
            new Vector3(-10, 0, -3)
        };
        foreach(var p in points) {
            create(p, $"{p}");
        }
    }

    void goCameraWork() 
    {
        Camera.main.transform.DORotateAround(Vector3.zero, Vector3.up, 360, 5);
    }

    void setupUI()
    {
        moveCameraButton?.OnClickAsObservable().Subscribe(_ => {
            goCameraWork();
        }).AddTo(this);
    }
}
