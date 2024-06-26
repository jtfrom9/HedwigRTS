#nullable enable

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UniRx;
using Cysharp.Threading.Tasks;

using Hedwig.RTSCore.Model;

public class TrajectoryTest
{
    static List<TrajectorySection>[] testPatterns = new[] {
            // 0
            new List<TrajectorySection>() {
                new TrajectorySection() { factor = 1 },
            },
            // 1
            new List<TrajectorySection>() {
                new TrajectorySection() { factor = 1 }, // 0 <= v < 0.5
                new TrajectorySection() { factor = 1 }, // 0.5 <= v <1, 1
            },
            // 2
            new List<TrajectorySection>() {
                new TrajectorySection() { factor = 1 },  // 0 <= v < 0.25
                new TrajectorySection() { factor = 1 },  // 0.25 <= v < 0.5
                new TrajectorySection() { factor = 2 },  // 0.5 <= v < 1, 1
            }
        };

    [TestCase(0, ExpectedResult = 1)]
    [TestCase(1, ExpectedResult = 2)]
    [TestCase(2, ExpectedResult = 4)]
    public int getTotalFactorTest(int index)
    {
        return TrajectorySection.getTotalFactor(testPatterns[index]);
    }

    [TestCase(0, 0, ExpectedResult = 1)]
    [TestCase(1, 0, ExpectedResult = 1)]
    [TestCase(1, 1, ExpectedResult = 2)]
    [TestCase(2, 0, ExpectedResult = 1)]
    [TestCase(2, 1, ExpectedResult = 2)]
    [TestCase(2, 2, ExpectedResult = 4)]
    public int sumFactorTest(int listIndex, int i)
    {
        return TrajectorySection.sumFactor(testPatterns[listIndex], i);
    }

    [TestCase(0, 0f, ExpectedResult = 0)]
    [TestCase(0, 1f, ExpectedResult = 0)]
    [TestCase(1, 0f, ExpectedResult = 0)]
    [TestCase(1, .49f, ExpectedResult = 0)]
    [TestCase(1, .5f, ExpectedResult = 1)]
    [TestCase(1, 1f, ExpectedResult = 1)]
    [TestCase(2, 0f, ExpectedResult = 0)]
    [TestCase(2, 0.24999f, ExpectedResult = 0)]
    [TestCase(2, 0.25f, ExpectedResult = 1)]
    [TestCase(2, 0.49999f, ExpectedResult = 1)]
    [TestCase(2, 0.5f, ExpectedResult = 2)]
    [TestCase(2, 0.999f, ExpectedResult = 2)]
    [TestCase(2, 1, ExpectedResult = 2)]
    public int? getSectionIndexTest(int listIndex, float factor)
    {
        var regions = testPatterns[listIndex];
        return TrajectorySection.getSectionIndex(regions, factor);
    }

    [TestCase(0, 0f, 0f, 1f)]
    [TestCase(0, .5f, 0f, 1f)]
    [TestCase(0, 1f, 0f, 1f)]
    [TestCase(1, 0f, 0f, 0.5f)]
    [TestCase(1, .49f, 0f, 0.5f)]
    [TestCase(1, .5f, .5f, 1f)]
    [TestCase(1, .99f, .5f, 1f)]
    [TestCase(1, 1f, .5f, 1f)]
    [TestCase(2, 0f, 0f, .25f)]
    [TestCase(2, .2499f, 0f, .25f)]
    [TestCase(2, .25f, .25f, .5f)]
    [TestCase(2, .499f, .25f, .5f)]
    [TestCase(2, .5f, .5f, 1f)]
    [TestCase(2, .999f, .5f, 1f)]
    [TestCase(2, 1f, .5f, 1f)]
    public void getSectionFactorTest(int listIndex, float factor, float expectedMin, float expectedMax)
    {
        var regions = testPatterns[listIndex];
        var (min, max) = TrajectorySection.getSectionFactor(regions, factor);
        Debug.Log($"{min}, {max}");
        Assert.That(min, Is.EqualTo(expectedMin).Within(float.Epsilon), "min");
        Assert.That(max, Is.EqualTo(expectedMax).Within(float.Epsilon), "max");
    }

    [Test]
    public void LinearLineTrajectoryTest()
    {
        var trajectory = ScriptableObject.CreateInstance<TrajectoryObject>();
        var start = Vector3.zero;
        var end = new Vector3(0, 0, 10);
        var map = trajectory.ToMap(start, end, 10);
        foreach (var sectionMap in map.Sections)
        {
            Debug.Log(sectionMap);
        }
        var lines = map.Lines.ToArray();
        Assert.That(lines.Length, Is.EqualTo(1));
        // Assert.That(lines.Length, Is.EqualTo(pointCount - 1));

        Vector3? dir = null;
        foreach (var line in lines)
        {
            var (sp, ep) = line.GetPoints();
            Debug.Log($"{sp} - {ep}");
            if (dir.HasValue)
            {
                Assert.That(ep - sp, Is.EqualTo(dir.Value));
            }
        }
    }

    [Test]
    public void TwoLinesTrajectoryTest()
    {
        var trajectory = ScriptableObject.CreateInstance<TrajectoryObject>();
        trajectory.sections = new List<TrajectorySection>() {
                new TrajectorySection() {
                    factor = 1,
                    type = TrajectorySectionType.Instruction,
                    toOffset = {
                        type = TrajectoryOffsetType.Base,
                        value = 20
                    }
                },
                new TrajectorySection() {
                    factor = 1,
                    type = TrajectorySectionType.Instruction,
                    toOffset = {
                        type = TrajectoryOffsetType.Base,
                        value = 0
                    }
                }
            };
        var start = Vector3.zero;
        var end = new Vector3(0, 0, 10);
        var map = trajectory.ToMap(start, end, 10);
        var sections = map.Sections.Cast<TrajectorySectionMap>().ToList();
        foreach (var sectionMap in map.Sections) Debug.Log(sectionMap);
        Assert.That(map.Sections.Count, Is.EqualTo(2));
        Assert.That(sections[0].From, Is.EqualTo(start));
        Assert.That(sections[0].To, Is.EqualTo(new Vector3(0, 20, 5)));
        Assert.That(sections[0].BaseEnd, Is.EqualTo(new Vector3(0, 0, 5)));
        Assert.That(sections[0].Minfactor, Is.EqualTo(0f).Within(float.Epsilon));
        Assert.That(sections[0].Maxfactor, Is.EqualTo(0.5f).Within(float.Epsilon));
        Assert.That(sections[1].From, Is.EqualTo(new Vector3(0, 20, 5)));
        Assert.That(sections[1].To, Is.EqualTo(end));
        Assert.That(sections[1].BaseEnd, Is.EqualTo(end));
        Assert.That(sections[1].Minfactor, Is.EqualTo(0.5f).Within(float.Epsilon));
        Assert.That(sections[1].Maxfactor, Is.EqualTo(1f).Within(float.Epsilon));

        var lines = map.Lines.ToArray();
        Assert.That(lines.Length, Is.EqualTo(2));

        Vector3 prev = start;
        foreach (var line in lines)
        {
            var (sp, ep) = line.GetPoints();
            Debug.Log($"{sp} - {ep}");
            Assert.That(sp, Is.EqualTo(prev));
            prev = ep;
        }
    }

    [Test]
    public void ParabollaTrajectoryTest_2Section()
    {
        var trajectory = ScriptableObject.CreateInstance<TrajectoryObject>();
        trajectory.sections = new List<TrajectorySection>() {
                new TrajectorySection() {
                    factor = 1,
                    type = TrajectorySectionType.Instruction,
                    toOffset = {
                        type = TrajectoryOffsetType.Base,
                        value = 20
                    },
                    controlPoints = new List<Vector2>() { new Vector2() { x = 0.5f, y = 30} }
                },
                new TrajectorySection() {
                    factor = 1,
                    type = TrajectorySectionType.Instruction,
                    toOffset = {
                        type = TrajectoryOffsetType.Base,
                        value = 0
                    },
                    controlPoints = new List<Vector2>() { new Vector2() { x = 0.5f, y = 10} }
                }
            };
        var start = Vector3.zero;
        var end = new Vector3(0, 0, 10);
        var map = trajectory.ToMap(start, end, 10);
        var sections = map.Sections.Cast<TrajectorySectionMap>().ToList();
        foreach (var sectionMap in map.Sections) Debug.Log(sectionMap);
        Assert.That(map.Sections.Count, Is.EqualTo(2));
        Assert.That(sections[0].From, Is.EqualTo(start));
        Assert.That(sections[0].To, Is.EqualTo(new Vector3(0, 20, 5)));
        Assert.That(sections[0].BaseEnd, Is.EqualTo(new Vector3(0, 0, 5)));
        Assert.That(sections[0].Minfactor, Is.EqualTo(0f).Within(float.Epsilon));
        Assert.That(sections[0].Maxfactor, Is.EqualTo(0.5f).Within(float.Epsilon));
        Assert.That(sections[1].From, Is.EqualTo(new Vector3(0, 20, 5)));
        Assert.That(sections[1].To, Is.EqualTo(end));
        Assert.That(sections[1].BaseEnd, Is.EqualTo(end));
        Assert.That(sections[1].Minfactor, Is.EqualTo(0.5f).Within(float.Epsilon));
        Assert.That(sections[1].Maxfactor, Is.EqualTo(1f).Within(float.Epsilon));

        var lines = map.Lines.ToArray();
        // Assert.That(lines.Length, Is.EqualTo(pointCount - 1));

        Vector3 prev = start;
        foreach (var line in lines)
        {
            Debug.Log(line);
            var (sp, ep) = line.GetPoints();
            Debug.Log($"{sp} - {ep}");
            Assert.That(sp, Is.EqualTo(prev));
            prev = ep;
        }
    }

    [Test]
    public void ParabollaTrajectoryTest_Control()
    {
        var trajectory = ScriptableObject.CreateInstance<TrajectoryObject>();
        trajectory.sections = new List<TrajectorySection>() {
                new TrajectorySection() {
                    factor = 1,
                    type = TrajectorySectionType.Instruction,
                    toOffset = {
                        type = TrajectoryOffsetType.Base,
                        value = 0
                    },
                    controlPoints = new List<Vector2>() { new Vector2() { x=0.5f, y = 30 } }
                }
            };
        var start = Vector3.zero;
        var end = new Vector3(0, 0, 10);
        var map = trajectory.ToMap(start, end, 10);
        var sections = map.Sections.Cast<TrajectorySectionMap>().ToList();
        foreach (var sectionMap in map.Sections) Debug.Log(sectionMap);
        Assert.That(map.Sections.Count, Is.EqualTo(1));
        Assert.That(sections[0].From, Is.EqualTo(start));
        Assert.That(sections[0].To, Is.EqualTo(new Vector3(0, 0, 10)));
        Assert.That(sections[0].BaseEnd, Is.EqualTo(new Vector3(0, 0, 10)));
        Assert.That(sections[0].Minfactor, Is.EqualTo(0f).Within(float.Epsilon));
        Assert.That(sections[0].Maxfactor, Is.EqualTo(1f).Within(float.Epsilon));

        var lines = map.Lines.ToArray();
        Vector3 prev = start;
        foreach (var line in lines)
        {
            Debug.Log(line);
            var (sp, ep) = line.GetPoints();
            Debug.Log($"{sp} - {ep}");
            Assert.That(sp, Is.EqualTo(prev));
            prev = ep;
        }
    }


    [TestCase(0, ExpectedResult = 1)]
    [TestCase(1, ExpectedResult = 2)]
    [TestCase(2, ExpectedResult = 3)]
    public int MakeMap(int patternIndex)
    {
        var trajectory = ScriptableObject.CreateInstance<TrajectoryObject>();
        trajectory.sections = testPatterns[patternIndex];

        var start = Vector3.zero;
        var end = new Vector3(0, 0, 10);
        var map = trajectory.ToMap(start, end, 10);
        return map.Sections.ToList().Count;
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void MakeMapFactor(int patternIndex)
    {
        var trajectory = ScriptableObject.CreateInstance<TrajectoryObject>();
        trajectory.sections = testPatterns[patternIndex];
        float min = 0;
        for (var i = 0; i < trajectory.sections.Count; i++)
        {
            var (minf, maxf) = trajectory.GetSectionFactor(i);
            Debug.Log($"{i} {minf} {maxf}");
            Assert.That(minf, Is.EqualTo(min).Within(float.Epsilon));
            min = maxf;
        }
    }
}
