using System;
using Unity.Entities;

namespace SparFlame.Components.General
{
    public enum WaitType
    {
        None = 0,
        Personalize = 1,
        /// <summary>
        /// This function is not implemented yet
        /// </summary>
        UntilBattle = 2,
    }

    public struct GameTimeData : IComponentData
    {
        public float DeltaTime;
        public float ElapsedTime;
    }

    public struct GameTimeScale : IComponentData
    {
        public float Value;
    }


    [Serializable]
    public struct WorldTimeData : IComponentData
    {
        public float hour;
        public float deltaHour;
        public float totalHours;
        
        public int day;
        public int month;
        public int year;
    }

    public struct WaitInfo : IComponentData
    {
        public float TargetTotalHours;
        public WaitType WaitType;
    }



    public struct TimeUtils
    {
        private static readonly int[] DaysInMonth =
        {
            31, // Jan
            28, // Feb 
            31, // Mar
            30, // Apr
            31, // May
            30, // Jun
            31, // Jul
            31, // Aug
            30, // Sep
            31, // Oct
            30, // Nov
            31 // Dec
        };

        public const int HoursPerDay = 24;
        private const int DaysPerYear = 365;
        private const int HoursPerYear = DaysPerYear * HoursPerDay;

        public static WorldTimeData GetWorldTimeDataFromTotalHours(double totalHours)
        {
            var result = new WorldTimeData();

            // ---- Year ----
            var year = (int)(totalHours / HoursPerYear) + 1;
            totalHours %= HoursPerYear;

            // ---- month ----
            var month = 1;
            foreach (var t in DaysInMonth)
            {
                double monthHours = t * HoursPerDay;
                if (totalHours >= monthHours)
                {
                    totalHours -= monthHours;
                    month++;
                }
                else break;
            }

            // ---- day ----
            var day = (int)(totalHours / HoursPerDay) + 1;
            totalHours %= HoursPerDay;

            // ---- hour ----
            var hour = (float)totalHours;

            result.year = year;
            result.month = month;
            result.day = day;
            result.hour = hour;
            return result;
        }

        /// <summary>
        /// Turn WorldTimeData to Total Hours 
        /// </summary>
        public static float GetTotalHoursFromWorldTimeData(WorldTimeData time)
        {
            var totalHours = 0f;

            // Accumulate years
            totalHours += (time.year - 1) * DaysPerYear * HoursPerDay;

            // Accumulate months
            for (var m = 1; m < time.month; m++)
            {
                totalHours += DaysInMonth[m - 1] * HoursPerDay;
            }

            // Accumulate days
            totalHours += (time.day - 1) * HoursPerDay;

            // Accumulate hours
            totalHours += time.hour;

            return totalHours;
        }

        public static bool ShouldMonthAdd(int currentMonth, int currentDay)
        {
            return DaysInMonth[currentMonth - 1] < currentDay;
        }

        public static WorldTimeData GetWaitTargetWorldTime(
            WorldTimeData currentWorldTime,
            float waitHours, int waitDays, int waitMonths, int waitYears)
        {
            var result = currentWorldTime;

            result.year += waitYears;
            result.month += waitMonths;
            while (result.month > DaysInMonth.Length)
            {
                result.month -= DaysInMonth.Length;
                result.year++;
            }
            result.day += waitDays;
            while (true)
            {
                var daysInCurrentMonth = DaysInMonth[result.month - 1]; // 月份从1开始
                if (result.day > daysInCurrentMonth)
                {
                    result.day -= daysInCurrentMonth;
                    result.month++;
                    if (result.month > DaysInMonth.Length)
                    {
                        result.month = 1;
                        result.year++;
                    }
                }
                else break;
            }

            result.hour += waitHours;
            while (result.hour >= HoursPerDay)
            {
                result.hour -= HoursPerDay;
                result.day++;

                var daysInCurrentMonth = DaysInMonth[result.month - 1];
                if (result.day > daysInCurrentMonth)
                {
                    result.day = 1;
                    result.month++;
                    if (result.month > DaysInMonth.Length)
                    {
                        result.month = 1;
                        result.year++;
                    }
                }
            }

            return result;
        }

    }
}