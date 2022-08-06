#nullable enable

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UniRx;
using Cysharp.Threading.Tasks;

using Hedwig.RTSCore;

public class SelectionTest
{
    [Test]
    public void ReactiveCollectionTrial()
    {
        var rcol = new ReactiveCollection<int>() { 0, 1, 2, 3, 4, 5 };

        rcol.ObserveAdd().Subscribe(e =>
        {
            Debug.Log($"Add: {e.Index}, {e.Value}");
            Debug.Log(string.Join(",", rcol));
        });

        rcol.ObserveRemove().Subscribe(e =>
        {
            Debug.Log($"Del: {e.Index}, {e.Value}");
            Debug.Log(string.Join(",", rcol));
        });

        rcol.Add(6);
        rcol.Add(99);
        rcol.Add(100);

        rcol.Remove(1);
        rcol.RemoveAt(7);
    }

    [TestCase(0, ExpectedResult = new int[] { 0, 0 })]
    [TestCase(9, ExpectedResult = new int[] { 0, 9 })]
    public int[] ReactiveSelectionTest_Sel(int sel)
    {
        var rcol = new ReactiveCollection<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var rs = new ReactiveSelection<int>(rcol);
        int cur = -1;
        int prev = -1;
        rs.OnCurrentChanged.Subscribe(v => cur = v);
        rs.OnPrevChanged.Subscribe(v => prev = v);
        rs.Select(sel);
        return new int[] { prev, cur };
    }

    [TestCase(0, ExpectedResult = new int[] { 0, 1 })]
    [TestCase(1, ExpectedResult = new int[] { 1, 2 })]
    [TestCase(2, ExpectedResult = new int[] { 2, 3 })]
    [TestCase(9, ExpectedResult = new int[] { 9, 0 })]
    public int[] ReactiveSelectionTest_Next(int sel)
    {
        var rcol = new ReactiveCollection<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var rs = new ReactiveSelection<int>(rcol);
        int cur = -1;
        int prev = -1;
        rs.OnCurrentChanged.Subscribe(v => cur = v);
        rs.OnPrevChanged.Subscribe(v => prev = v);
        rs.Select(sel);
        rs.Next();
        return new int[] { prev, cur };
    }

    [TestCase(0, ExpectedResult = new int[] { 0, 9 })]
    [TestCase(1, ExpectedResult = new int[] { 1, 0 })]
    [TestCase(2, ExpectedResult = new int[] { 2, 1 })]
    [TestCase(8, ExpectedResult = new int[] { 8, 7 })]
    [TestCase(9, ExpectedResult = new int[] { 9, 8 })]
    public int[] ReactiveSelectionTest_Prev(int sel)
    {
        var rcol = new ReactiveCollection<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var rs = new ReactiveSelection<int>(rcol);
        int cur = -1;
        int prev = -1;
        rs.OnCurrentChanged.Subscribe(v => cur = v);
        rs.OnPrevChanged.Subscribe(v => prev = v);
        rs.Select(sel);
        rs.Prev();
        return new int[] { prev, cur };
    }

    [TestCase(0, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(1, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(2, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(9, ExpectedResult = new int[] { -1, -1 })]
    public int[] ReactiveSelectionTest_Add(int sel)
    {
        var rcol = new ReactiveCollection<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var rs = new ReactiveSelection<int>(rcol);
        int cur = -1;
        int prev = -1;
        rs.Select(sel);
        rs.OnCurrentChanged.Subscribe(v => cur = v);
        rs.OnPrevChanged.Subscribe(v => prev = v);

        rcol.Add(10);
        return new int[] { prev, cur };
    }

    [TestCase(0, 0, ExpectedResult = new int[] { 0, 1 })]
    [TestCase(0, 1, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(0, 2, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(0, 3, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(0, 8, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(0, 9, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(1, 0, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(1, 1, ExpectedResult = new int[] { 1, 2 })]
    [TestCase(1, 2, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(1, 3, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(1, 4, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(1, 8, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(1, 9, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(2, 0, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(2, 1, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(2, 2, ExpectedResult = new int[] { 2, 3 })]
    [TestCase(2, 3, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(2, 4, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(2, 8, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(2, 9, ExpectedResult = new int[] { -1, -1 })]
    [TestCase(8, 9, ExpectedResult = new int[] { -1, -1 })]
    public int[] ReactiveSelectionTest_Remove(int sel, int index)
    {
        var rcol = new ReactiveCollection<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var rs = new ReactiveSelection<int>(rcol);
        int cur = -1;
        int prev = -1;
        rs.Select(sel);
        rs.OnCurrentChanged.Subscribe(v => cur = v);
        rs.OnPrevChanged.Subscribe(v => prev = v);

        rcol.RemoveAt(index);
        return new int[] { prev, cur };
    }

    [TestCase(0, 0, ExpectedResult = 1)]
    [TestCase(0, 1, ExpectedResult = 0)]
    [TestCase(0, 2, ExpectedResult = 0)]
    [TestCase(0, 3, ExpectedResult = 0)]
    [TestCase(0, 8, ExpectedResult = 0)]
    [TestCase(0, 9, ExpectedResult = 0)]
    [TestCase(1, 0, ExpectedResult = 2)]
    [TestCase(1, 1, ExpectedResult = 2)]
    [TestCase(1, 2, ExpectedResult = 1)]
    [TestCase(1, 3, ExpectedResult = 1)]
    [TestCase(1, 8, ExpectedResult = 1)]
    [TestCase(1, 9, ExpectedResult = 1)]
    [TestCase(2, 0, ExpectedResult = 3)]
    [TestCase(2, 1, ExpectedResult = 3)]
    [TestCase(2, 2, ExpectedResult = 3)]
    [TestCase(2, 3, ExpectedResult = 2)]
    [TestCase(2, 8, ExpectedResult = 2)]
    [TestCase(2, 9, ExpectedResult = 2)]
    [TestCase(8, 0, ExpectedResult = 9)]
    [TestCase(8, 1, ExpectedResult = 9)]
    [TestCase(8, 8, ExpectedResult = 9)]
    [TestCase(8, 9, ExpectedResult = 8)]
    [TestCase(9, 0, ExpectedResult = 10)]
    [TestCase(9, 9, ExpectedResult = 10)]
    public int ReactiveSelectionTest_Insert(int sel, int insert_index)
    {
        var rcol = new ReactiveCollection<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var rs = new ReactiveSelection<int>(rcol);
        rs.Select(sel);
        var cur = rs.Current;
        rcol.Insert(insert_index, 10);
        Assert.That(rs.Current, Is.EqualTo(cur));
        return rs.Index;
    }


    // [TestCase(0, 0, ExpectedResult = new int[] { 0, 1 })]
    // [TestCase(0, 1, ExpectedResult = new int[] { -1, -1 })]
    // public int[] ReactiveSelectionTest_Insert(int sel, int insert_index)
    // {
    //     var rcol = new ReactiveCollection<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    //     var rs = new ReactiveSelection<int>(rcol);
    //     int cur = -1;
    //     int prev = -1;
    //     rs.Select(sel);
    //     rs.OnCurrentChanged.Subscribe(v => cur = v);
    //     rs.OnPrevChanged.Subscribe(v => prev = v);

    //     rcol.Insert(insert_index, 10);
    //     return new int[] { prev, cur };
    // }
}

