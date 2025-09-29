using System;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable AsyncVoidMethod

namespace SparFlame.UI.General
{
    public static class UIMathMethods
    {
        /// <summary>
        /// Get x min : y s format of seconds data
        /// </summary>
        /// <param name="totalSeconds"></param>
        /// <returns></returns>
        public static string FormatTimeFromSeconds(int totalSeconds)
        {
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            return $"{minutes} min : {seconds} s";
        }

        /// <summary>
        /// Get 00 : x min : y s format of seconds data
        /// </summary>
        /// <param name="totalSeconds"></param>
        /// <returns></returns>
        public static string FormatTimeFromSeconds2(int totalSeconds)
        {
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            var mid = minutes < 10 ? "0" + minutes : minutes.ToString();
            var last = seconds < 10 ? "0" + seconds : seconds.ToString();
            return $"00 : {mid}  : {last} s";
        }

        public static string FormatTimeFromHours(float totalHours)
        {
            var days = (int) totalHours / 24;
            var hours = math.ceil( totalHours % 24);
            return $"{days} days : {hours} h";
        }
        

        public static async void AnimateColorAsync(Image image, Color from, Color to, float duration)
        {
            var t = 0f;

            while (t < duration)
            {
                var progress = t / duration;
                image.color = Color.Lerp(from, to, progress);
                t += Time.deltaTime;
                await Task.Yield(); 
            }

            image.color = to;
        }
        
        /// <summary>
        /// Animated modify float
        /// </summary>
        /// <param name="getter">Current value</param>
        /// <param name="setter">Setter lambda function</param>
        /// <param name="duration">RepeatTime</param>
        /// <param name="curve"></param>
        /// <param name="to">If null, then use the curve value to set target value,
        /// else use the curve value between 0 and 1 to lerp target value</param>
        public static async void AnimateFloatOverCurveAsyc(Func<float> getter, Action<float> setter, 
            float duration, AnimationCurve curve,float? to = null)
        {
            if (to != null && !Mathf.Approximately(duration, 1f))
            {
                throw new ArgumentException(
                    "Duration should be 1 when to is not null, because you are using curve between 0 and 1" +
                    "to slerp the target value");
            }
            
            var from = getter();
            var time = 0f;
            while (time < duration)
            {
                var t = to != null ? time / duration : time;
                var curveValue = curve.Evaluate(t);
                float newValue;
                if (to != null)
                {
                     newValue = Mathf.Lerp(from, to.Value, curveValue);
                }
                else
                {
                    newValue = curve.Evaluate(t);
                }
                setter(newValue);
                time += Time.deltaTime;
                await Task.Yield(); 
            }

            if (to != null)
            {
                setter(to.Value);
            }
        }
    }
}