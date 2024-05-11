#nullable enable

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Projectile/Trajectory", fileName = "Trajectory")]
    public class TrajectoryObject : ScriptableObject
    {
        public List<TrajectorySection> sections = new List<TrajectorySection>();
    }

    public static class TrajectoryExtension
    {
        static public (float, float) GetSectionFactor(this TrajectoryObject trajectory, int index)
        {
            var sections = trajectory.sections;
            var totalFactor = TrajectorySection.getTotalFactor(sections);
            var minFactor = (index == 0) ? 0f : (float)TrajectorySection.sumFactor(sections, index - 1);
            var maxFactor = (float)TrajectorySection.sumFactor(sections, index);
            return ((float)minFactor / (float)totalFactor, (float)maxFactor / (float)totalFactor);
        }

        static public Vector3[] MakePoints(this TrajectoryObject trajectory, Vector3 from, Vector3 to, float baseSpeed)
        {
            var map = trajectory.ToMap(from, to, baseSpeed);
            var points = new List<Vector3>() { from };
            foreach (var line in map.Lines)
            {
                points.Add(line.GetToPoint());
            }
            return points.ToArray();
        }

        public static TrajectoryMap ToMap(this TrajectoryObject trajectory, Vector3 from, Vector3 to, float baseSpeed)
        {
            return TrajectoryMap.Create(trajectory, from, to, baseSpeed);
        }
    }
}