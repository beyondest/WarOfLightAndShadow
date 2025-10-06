using System;
using Unity.Entities;

namespace SparFlame.Components.General
{
    public enum WaitType
    {
        None = 0,
        Personalize = 1,
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
        public int day;
        public int month;
        public int year;
        public float deltaHour;
        public float totalHours;
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
            28, // Feb (不考虑闰年)
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
            WorldTimeData result = new WorldTimeData();

            // ---- 年份 ----
            int year = (int)(totalHours / HoursPerYear) + 1;
            totalHours %= HoursPerYear;

            // ---- 月份 ----
            int month = 1;
            for (int i = 0; i < DaysInMonth.Length; i++)
            {
                double monthHours = DaysInMonth[i] * HoursPerDay;
                if (totalHours >= monthHours)
                {
                    totalHours -= monthHours;
                    month++;
                }
                else break;
            }

            // ---- 天数 ----
            int day = (int)(totalHours / HoursPerDay) + 1;
            totalHours %= HoursPerDay;

            // ---- 小时 ----
            float hour = (float)totalHours;

            result.year = year;
            result.month = month;
            result.day = day;
            result.hour = hour;
            return result;
        }

        /// <summary>
        /// 把 WorldTimeData 转换为绝对小时
        /// </summary>
        public static float GetTotalHoursFromWorldTimeData(WorldTimeData time)
        {
            var totalHours = 0f;

            // 累加年份（按平年算）
            totalHours += (time.year - 1) * DaysPerYear * HoursPerDay;

            // 累加月份
            for (int m = 1; m < time.month; m++)
            {
                totalHours += DaysInMonth[m - 1] * HoursPerDay;
            }

            // 累加天数
            totalHours += (time.day - 1) * HoursPerDay;

            // 累加小时
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
            WorldTimeData result = currentWorldTime;

            // 1. 年
            result.year += waitYears;

            // 2. 月
            result.month += waitMonths;
            while (result.month > DaysInMonth.Length)
            {
                result.month -= DaysInMonth.Length;
                result.year++;
            }

            // 3. 日
            result.day += waitDays;
            while (true)
            {
                int daysInCurrentMonth = DaysInMonth[result.month - 1]; // 月份从1开始
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

            // 4. 小时
            result.hour += waitHours;
            while (result.hour >= HoursPerDay)
            {
                result.hour -= HoursPerDay;
                result.day++;

                int daysInCurrentMonth = DaysInMonth[result.month - 1];
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