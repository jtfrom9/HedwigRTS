#nullable enable

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hedwig.RTSCore.Model
{
    public class TrajectoryLineMap: ITrajectoryLineMap
    {
        TrajectorySectionMap sectionMap;
        int index;

        public float FromFactor { get; private set; }
        public float ToFactor { get; private set; }
        public bool IsFirst { get => index == 0; }

        static Vector3 makeBezierCurvePoint(Vector3 start, Vector3 end, Vector3 control, float t)
        {
            Vector3 Q0 = Vector3.Lerp(start, control, t);
            Vector3 Q1 = Vector3.Lerp(control, end, t);
            Vector3 Q2 = Vector3.Lerp(Q0, Q1, t);
            return Q2;
        }

        static Vector3 makePoint(Vector3 start, Vector3 end, float factor, List<Vector2> controlPoints)
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

        static Vector3 getPoint(float factor, TrajectorySectionMap sectionMap)
        {
            return makePoint(sectionMap.From, sectionMap.To, factor, sectionMap.ControlPoints);
        }

        public Vector3 GetFromPoint()
        {
            return getPoint(FromFactor, sectionMap);
        }

        public Vector3 GetToPoint()
        {
            return getPoint(ToFactor, sectionMap);
        }

        public float GetAccelatedSpeed()
        {
            var factor = (float)index / (float)sectionMap.NumLines;
            return sectionMap.GetAccelatedSpeed(factor);
        }

        public (Vector3, Vector3) GetPoints() => (GetFromPoint(), GetToPoint());

        public override string ToString()
        {
            return $"TrajectoryLineMap([{index}] {FromFactor} - {ToFactor} (section: {sectionMap}))";
        }

        public TrajectoryLineMap(
            TrajectorySectionMap sectionMap,
            int index,
            float fromFactor,
            float totFactor)
        {
            this.sectionMap = sectionMap;
            this.index = index;
            this.FromFactor = fromFactor;
            this.ToFactor = totFactor;
        }
    }

    public class TrajectorySectionMap: ITrajectorySectionMap
    {
        TrajectoryMap parent;
        TrajectorySection section;
        int index;
        List<TrajectoryLineMap> _lineMaps = new List<TrajectoryLineMap>();
        List<TrajectoryLineMap> _dynamicLineMaps = new List<TrajectoryLineMap>();

        public Vector3 From { get; private set; }
        public Vector3 To { get; private set; }
        public Vector3 BaseEnd { get; private set; }
        public float Minfactor { get; private set; }
        public float Maxfactor { get; private set; }
        public List<Vector2> ControlPoints { get => section.controlPoints; }

        public Vector3 Direction { get => (this.To - this.From).normalized; }
        public float Distance { get => (this.To - this.From).magnitude; }

        public float AdjustMaxAngle { get => section.adjustMaxAngle; }
        public int NumLines { get => _lineMaps.Count; }
        public float Speed { get => parent.baseSpeed + AdditionalSpeed; }
        public float AdditionalSpeed { get => parent.baseSpeed * section.speedFactor; }

        float speedPower(float factor, int pow)
        {
            return AdditionalSpeed * Mathf.Pow(factor, pow);
        }

        public float GetAccelatedSpeed(float factor)
        {
            int pow = 0;
            switch (section.acceleration)
            {
                case TrajectoryAccelerationType.None:
                default:
                    break;
                case TrajectoryAccelerationType.Linear:
                    pow = 1;
                    break;
                case TrajectoryAccelerationType.Quad:
                    pow = 2;
                    break;
                case TrajectoryAccelerationType.Cubic:
                    pow = 3;
                    break;
                case TrajectoryAccelerationType.Quart:
                    pow = 4;
                    break;
            }
            return parent.baseSpeed + speedPower(factor, pow);
        }

        public bool IsCurve { get => section.IsCurve; }
        public bool IsFirst { get => index == 0; }
        public bool IsLast { get => Maxfactor == 1.0f; }
        public bool IsHoming { get => section.type == TrajectorySectionType.Chase; }

        public void Clear()
        {
            _lineMaps.Clear();
        }

        const float fixedTimestep = 0.02f;

        float getMinimumPointsPerFixedUpdate() {
            return Distance / (Speed * fixedTimestep);
        }

        void makeLines()
        {
            if (IsCurve || IsHoming || section.acceleration != TrajectoryAccelerationType.None)
            {
                var pointCount = (int)getMinimumPointsPerFixedUpdate();
                for (var i = 0; i < pointCount - 1; i++)
                {
                    var fromFactor = (float)i / (float)(pointCount - 1);
                    var toFactor = (float)(i + 1) / (float)(pointCount - 1);
                    if (i == pointCount - 1) { toFactor = Maxfactor; }
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

        public void AddDynamicLine(int index, float fromFactor, float totFactor)
        {
            _dynamicLineMaps.Add(new TrajectoryLineMap(this, index, fromFactor, totFactor));
        }

        public IEnumerable<ITrajectoryLineMap> Lines { get => _lineMaps; }

        public override string ToString()
        {
            return $"Section({index},{section.type},{Minfactor} - {Maxfactor})";
            //             return @$"TrajectoryMap([section:${index}]) type: {section.type}
            // factor: {minfactor} - {maxfactor}
            // points: {from} - {to} (baseEnd: {baseEnd})";
        }

        public TrajectorySectionMap(
            TrajectoryMap parent,
            TrajectorySection section,
            int index,
            Vector3 start, Vector3 baseEnd, Vector3 end, float minfacator, float maxfactor)
        {
            this.parent = parent;
            this.index = index;
            this.section = section;
            this.From = start;
            this.BaseEnd = baseEnd;
            this.To = end;
            this.Minfactor = minfacator;
            this.Maxfactor = maxfactor;
            makeLines();
        }
    }

    public class TrajectoryMap: ITrajectoryMap
    {
        TrajectoryObject _trajectory;
        float _baseSpeed;
        List<TrajectorySectionMap> _sectionMaps = new List<TrajectorySectionMap>();

        public IEnumerable<ITrajectorySectionMap> Sections { get => _sectionMaps; }
        public float baseSpeed { get => _baseSpeed; }

        public IEnumerable<ITrajectoryLineMap> Lines
        {
            get
            {
                foreach (var sectionMap in _sectionMaps)
                {
                    foreach (var lineMap in sectionMap.Lines)
                    {
                        yield return lineMap;
                    }
                }
            }
        }

        TrajectoryMap(in TrajectoryObject trajectory, float baseSpeed)
        {
            this._trajectory = trajectory;
            this._baseSpeed = baseSpeed;
        }



        public static TrajectoryMap Create(in TrajectoryObject trajectory, Vector3 globalFrom, Vector3 globalTo, float baseSpeed)
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
                    map._sectionMaps.Add(new TrajectorySectionMap(map, section, index,
                        from, baseTo, to,
                        minfactor, maxfactor));
                    from = to;
                }
            }
            else
            {
                map._sectionMaps.Add(new TrajectorySectionMap(
                    map,
                    new TrajectorySection()
                    {
                        factor = 1,
                        type = TrajectorySectionType.Instruction,
                        toOffset = {
                            type = TrajectoryOffsetType.Base,
                            value = 0
                        }
                    }, 0, globalFrom, globalTo, globalTo, 0f, 1f));
            }
            return map;
        }
    }
}