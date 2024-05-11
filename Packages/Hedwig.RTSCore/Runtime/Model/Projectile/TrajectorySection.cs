#nullable enable

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hedwig.RTSCore.Model
{
    public enum TrajectorySectionType
    {
        Instruction,
        Chase
    };

    public enum TrajectoryOffsetType
    {
        Last,
        Base
    };

    [Serializable]
    public struct TrajectoryPointOffset
    {
        public TrajectoryOffsetType type;
        public float value;

        public Vector3 ToPoint(Vector3 lastPoint, Vector3 basePoint)
        {
            switch (type)
            {
                case TrajectoryOffsetType.Last:
                    return lastPoint + Vector3.up * value;
                case TrajectoryOffsetType.Base:
                    return basePoint + Vector3.up * value;
            }
            throw new InvalidConditionException("invalid PointOffset");
        }
    }

    public enum TrajectoryAccelerationType
    {
        None,
        Linear,
        Quad,
        Cubic,
        Quart,
    }

    [Serializable]
    public class TrajectorySection
    {
        public string name = "";

        [SerializeField]
        [Min(1)]
        public int factor = 1;

        [SerializeField]
        public TrajectorySectionType type = TrajectorySectionType.Instruction;

        [SerializeField]
        public TrajectoryPointOffset toOffset;

        [SerializeField]
        public List<Vector2> controlPoints = new List<Vector2>();

        [SerializeField]
        public float speedFactor = 0;

        [SerializeField]
        public TrajectoryAccelerationType acceleration = TrajectoryAccelerationType.None;

        [SerializeField]
        public float adjustMaxAngle = 10;

        public bool IsCurve
        {
            get => controlPoints.Count > 0;
        }

        static public int getTotalFactor(IList<TrajectorySection> sections)
        {
            return sections.Select(data => data.factor).Sum();
        }

        static public int sumFactor(IList<TrajectorySection> sections, int index)
        {
            int sum = 0;
            for (var i = 0; i <= index && i < sections.Count; i++)
            {
                sum += sections[i].factor;
            }
            return sum;
        }

        static public int getSectionIndex(IList<TrajectorySection> sections, float factor)
        {
            var totalFactor = getTotalFactor(sections);
            for (var i = 0; i < sections.Count; i++)
            {
                var f = (float)sumFactor(sections, i) / (float)totalFactor;
                if (f > factor)
                    return i;
            }
            return sections.Count - 1;
        }

        static public (float, float) getSectionFactor(IList<TrajectorySection> sections, float factor)
        {
            var totalFactor = getTotalFactor(sections);
            float maxfactor = 0f;
            float minfactor = 1f;
            for (var i = 0; i < sections.Count; i++)
            {
                maxfactor = (float)sumFactor(sections, i) / (float)totalFactor;
                minfactor = (i > 0) ? (float)sumFactor(sections, i - 1) / (float)totalFactor : 0f;
                if (maxfactor > factor)
                {
                    return (minfactor, maxfactor);
                }
            }
            return (minfactor, maxfactor);
        }
    }
}