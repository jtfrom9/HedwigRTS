using System;
using UnityEngine;

namespace Hedwig.RTSCore
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class SerializableRequiredAttribute : PropertyAttribute
    {
        public SerializableRequiredAttribute() { }
    }
}