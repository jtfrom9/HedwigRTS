#nullable enable

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hedwig.RTSCore
{
    public class TrajectoryLineMap
    {
        TrajectorySectionMap sectionMap;
        int index;
        public float fromFactor { get; private set; }
        public float toFactor { get; private set; }

        public bool IsFirst { get => index == 0; }

        static public Vector3 makeBezierCurvePoint(Vector3 start, Vector3 end, Vector3 control, float t)
        {
            Vector3 Q0 = Vector3.Lerp(start, control, t);
            Vector3 Q1 = Vector3.Lerp(control, end, t);
            Vector3 Q2 = Vector3.Lerp(Q0, Q1, t);
            return Q2;
        }

        static public Vector3 makePoint(Vector3 start, Vector3 end, float factor, List<Vector2> controlPoints)
        {
            if (controlPoints.Count > 0)
            {
                var cp = controlPoints[0];
                var control = Vector3.Lerp(start, end, cp.x) + Vector3.up * cp.y;
                return makeBezierCurvePoint(start, end, control, factor);
            }
            else
            {
                return Vector3.Lerp(start, end, factor);
            }
        }

        Vector3 getPoint(float factor, TrajectorySectionMap sectionMap)
        {
            return makePoint(sectionMap.from, sectionMap.to, factor, sectionMap.controlPoints);
        }

        public Vector3 GetFromPoint()
        {
            return getPoint(fromFactor, sectionMap);
        }

        public Vector3 GetToPoint()
        {
            return getPoint(toFactor, sectionMap);
        }

        public float GetAccelatedSpeed()
        {
            var factor = (float)index / (float)sectionMap.numLines;
            return sectionMap.GetAccelatedSpeed(factor);
        }

        public (Vector3, Vector3) GetPoints() => (GetFromPoint(), GetToPoint());

        public override string ToString()
        {
            return $"TrajectoryLineMap([{index}] {fromFactor} - {toFactor} (section: {sectionMap}))";
        }

        public TrajectoryLineMap(
            TrajectorySectionMap sectionMap,
            int index,
            float fromFactor,
            float totFactor)
        {
            this.sectionMap = sectionMap;
            this.index = index;
            this.fromFactor = fromFactor;
            this.toFactor = totFactor;
        }
    }

    public class TrajectorySectionMap
    {
        TrajectoryMap parent;
        Trajectory.Section section;
        int index;
        List<TrajectoryLineMap> _lineMaps = new List<TrajectoryLineMap>();
        List<TrajectoryLineMap> _dynamicLineMaps = new List<TrajectoryLineMap>();

        public Vector3 from { get; private set; }
        public Vector3 to { get; private set; }
        public Vector3 baseEnd { get; private set; }
        public float minfactor { get; private set; }
        public float maxfactor { get; private set; }
        public List<Vector2> controlPoints { get => section.controlPoints; }

        public Vector3 disrection { get => (this.to - this.from).normalized; }
        public float distance { get => (this.to - this.from).magnitude; }
        public float adjustMaxAngle { get => section.adjustMaxAngle; }
        public int numLines { get => _lineMaps.Count; }
        public float speed { get => parent.baseSpeed + additionalSpeed; }
        public float additionalSpeed { get => parent.baseSpeed * section.speedFactor; }

        float speedPower(float factor, int pow)
        {
            return additionalSpeed * Mathf.Pow(factor, pow);
        }

        public float GetAccelatedSpeed(float factor)
        {
            int pow = 0;
            switch (section.acceleration)
            {
                case Trajectory.AccelerationType.None:
                default:
                    break;
                case Trajectory.AccelerationType.Linear:
                    pow = 1;
                    break;
                case Trajectory.AccelerationType.Quad:
                    pow = 2;
                    break;
                case Trajectory.AccelerationType.Cubic:
                    pow = 3;
                    break;
                case Trajectory.AccelerationType.Quart:
                    pow = 4;
                    break;
            }
            return parent.baseSpeed + speedPower(factor, pow);
        }


        public bool IsCurve { get => section.IsCurve; }
        public bool IsFirst { get => index == 0; }
        public bool IsLast { get => maxfactor == 1.0f; }
        public bool IsHoming { get => section.type == Trajectory.SectionType.Chase; }

        public void Clear()
        {
            _lineMaps.Clear();
        }

        const float fixedTimestep = 0.02f;

        float getMinimumPointsPerFixedUpdate() {
            return distance / (speed * fixedTimestep);
        }

        void makeLines()
        {
            if (IsCurve || IsHoming || section.acceleration != Trajectory.AccelerationType.None)
            {
                var pointCount = (int)getMinimumPointsPerFixedUpdate();
                for (var i = 0; i < pointCount - 1; i++)
                {
                    var fromFactor = (float)i / (float)(pointCount - 1);
                    var toFactor = (float)(i + 1) / (float)(pointCount - 1);
                    if (i == pointCount - 1) { toFactor = maxfactor; }
                    _lineMaps.Add(new TrajectoryLineMap(
                        this,
                        i,
                        fromFactor,
                        toFactor));
                }
            }
            else
            {
                _lineMaps.Add(new TrajectoryLineMap(
                    this,
                    0,
                    0f,
                    1f));
            }
        }

        public void AddDynamic(TrajectoryLineMap line) {
            _dynamicLineMaps.Add(line);
        }

        public IReadOnlyList<TrajectoryLineMap> Lines { get => _lineMaps; }

        public override string ToString()
        {
            return $"Section({index},{section.type},{minfactor} - {maxfactor})";
            //             return @$"TrajectoryMap([section:${index}]) type: {section.type}
            // factor: {minfactor} - {maxfactor}
            // points: {from} - {to} (baseEnd: {baseEnd})";
        }

        public TrajectorySectionMap(
            TrajectoryMap parent,
            Trajectory.Section section,
            int index,
            Vector3 start, Vector3 baseEnd, Vector3 end, float minfacator, float maxfactor)
        {
            this.parent = parent;
            this.index = index;
            this.section = section;
            this.from = start;
            this.baseEnd = baseEnd;
            this.to = end;
            this.minfactor = minfacator;
            this.maxfactor = maxfactor;
            makeLines();
        }
    }

    public class TrajectoryMap
    {
        Trajectory _trajectory;
        float _baseSpeed;
        List<TrajectorySectionMap> _sectionMaps = new List<TrajectorySectionMap>();

        public IList<TrajectorySectionMap> Sections { get => _sectionMaps; }
        public float baseSpeed { get => _baseSpeed; }

        public IEnumerable<TrajectoryLineMap> Lines
        {
            get
            {
                foreach (var sectionMap in Sections)
                {
                    foreach (var lineMap in sectionMap.Lines)
                    {
                        yield return lineMap;
                    }
                }
            }
        }

        TrajectoryMap(in Trajectory trajectory, float baseSpeed)
        {
            this._trajectory = trajectory;
            this._baseSpeed = baseSpeed;
        }

        public static TrajectoryMap Create(in Trajectory trajectory, Vector3 globalFrom, Vector3 globalTo, float baseSpeed)
        {
            Vector3 from = globalFrom;
            var map = new TrajectoryMap(trajectory, baseSpeed);

            if (trajectory.sections.Count > 0)
            {
                foreach (var (section, index) in trajectory.sections.Select((section, index) => (section, index)))
                {
                    var (minfactor, maxfactor) = trajectory.GetSectionFactor(index);
                    var baseTo = Vector3.Lerp(globalFrom, globalTo, maxfactor);
                    var to = section.toOffset.ToPoint(baseTo.Y(from.y), baseTo);
                    map.Sections.Add(new TrajectorySectionMap(map, section, index,
                        from, baseTo, to,
                        minfactor, maxfactor));
                    from = to;
                }
            }
            else
            {
                map.Sections.Add(new TrajectorySectionMap(
                    map,
                    new Trajectory.Section()
                    {
                        factor = 1,
                        type = Trajectory.SectionType.Instruction,
                        toOffset = {
                            type = Trajectory.OffsetType.Base,
                            value = 0
                        }
                    }, 0, globalFrom, globalTo, globalTo, 0f, 1f));
            }
            return map;
        }
    }
}