using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace Assets.Scripts.Extensions
{
    /// <summary>
    /// Handy extension methods for Unity's Vector3 type
    /// </summary>
    public static class Vector3Extensions
    {
        /// <summary>
        /// Returns a tuple given a vector to allow serialisation and storage
        /// in the database.
        /// </summary>
        public static Tuple<float, float, float> ToTuple(this Vector3 vector)
        {
            return new Tuple<float, float, float>(vector.x, vector.y, vector.z);
        }
    }
}
