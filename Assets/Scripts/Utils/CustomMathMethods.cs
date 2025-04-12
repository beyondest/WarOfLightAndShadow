using System.Collections.Generic;
using UnityEngine;

namespace SparFlame.Utils.Utils
{
    public static class CustomMathMethods
    {
        public static List<Vector2> GenerateCirclePoints(Vector2 center, float radius, int pointCount)
        {
            var points = new List<Vector2>();

            var angleStep = 360f / pointCount;

            for (var i = 0; i < pointCount; i++)
            {
                var angleInDegrees = i * angleStep;
                var angleInRadians = angleInDegrees * Mathf.Deg2Rad;

                var x = center.x + radius * Mathf.Cos(angleInRadians);
                var y = center.y + radius * Mathf.Sin(angleInRadians);

                var point = new Vector2(x, y);
                points.Add(point);
            }

            return points;
        }


        /// <summary>
        /// Returns an interval [lower, upper] with a range width of l, wrapping x, and the position of x in the interval is random
        /// </summary>
        public static (float lower, float upper) GenerateRandomBoundsAround(float x, float l,
            Unity.Mathematics.Random rng)
        {
            
            var t = rng.NextFloat(0f, 1f);
            var lower = x - l * t;
            var upper = lower + l;

            return (lower, upper);
        }
    }
}