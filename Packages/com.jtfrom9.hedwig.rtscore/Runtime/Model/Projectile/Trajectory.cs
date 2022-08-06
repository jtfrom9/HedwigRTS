#nullable enable

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hedwig.RTSCore
{
    public interface ITrajectoryVisualizer
    {
        bool visible { get; }
        void SetStartTarget(ITransform? target);
        void SetEndTarget(ITransform? target);
        void SetProjectile(ProjectileObject? projectileObject);
        void Show(bool v);
    }

    [CreateAssetMenu(menuName = "Hedwig/Projectile/Trajectory", fileName = "Trajectory")]
    public class Trajectory : ScriptableObject
    {
        public List<Section> sections = new List<Section>();

        public enum SectionType
        {
            Instruction,
            Chase
        };

        public enum OffsetType
        {
            Last,
            Base
        };

        [Serializable]
        public struct PointOffset
        {
            public OffsetType type;
            public float value;

            public Vector3 ToPoint(Vector3 lastPoint, Vector3 basePoint)
            {
                switch (type)
                {
                    case OffsetType.Last:
                        return lastPoint + Vector3.up * value;
                    case OffsetType.Base:
                        return basePoint + Vector3.up * value;
                }
                throw new InvalidConditionException("invalid PointOffset");
            }
        }

        public enum AccelerationType
        {
            None,
            Linear,
            Quad,
            Cubic,
            Quart,
        }

        [Serializable]
        public class Section
        {
            public string name = "";

            [SerializeField]
            [Min(1)]
            public int factor = 1;

            [SerializeField]
            public SectionType type = SectionType.Instruction;

            [SerializeField]
            public PointOffset toOffset;

            [SerializeField]
            public List<Vector2> controlPoints = new List<Vector2>();

            [SerializeField]
            public float speedFactor = 0;

            [SerializeField]
            public AccelerationType acceleration = AccelerationType.None;

            [SerializeField]
            public float adjustMaxAngle = 10;

            public bool IsCurve
            {
                get => controlPoints.Count > 0;
            }
        }

        static public int getTotalFactor(IList<Section> sections)
        {
            return sections.Select(data => data.factor).Sum();
        }

        static public int sumFactor(IList<Section> sections, int index)
        {
            int sum = 0;
            for (var i = 0; i <= index && i < sections.Count; i++)
            {
                sum += sections[i].factor;
            }
            return sum;
        }

        static public int getSectionIndex(IList<Section> sections, float factor)
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

        static public (float, float) getSectionFactor(IList<Section> sections, float factor)
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

    public static class TrajectoryExtension
    {
        static public (float, float) GetSectionFactor(this Trajectory trajectory, int index)
        {
            var sections = trajectory.sections;
            var totalFactor = Trajectory.getTotalFactor(sections);
            var minFactor = (index == 0) ? 0f : (float)Trajectory.sumFactor(sections, index - 1);
            var maxFactor = (float)Trajectory.sumFactor(sections, index);
            return ((float)minFactor / (float)totalFactor, (float)maxFactor / (float)totalFactor);
        }

        static public Vector3[] MakePoints(this Trajectory trajectory, Vector3 from, Vector3 to, float baseSpeed)
        {
            var map = trajectory.ToMap(from, to, baseSpeed);
            var points = new List<Vector3>() { from };
            foreach (var line in map.Lines)
            {
                points.Add(line.GetToPoint());
            }
            return points.ToArray();
        }

        public static TrajectoryMap ToMap(this Trajectory trajectory, Vector3 from, Vector3 to, float baseSpeed)
        {
            return TrajectoryMap.Create(trajectory, from, to, baseSpeed);
        }
    }
}