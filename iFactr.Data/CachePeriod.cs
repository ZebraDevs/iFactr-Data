using System;

namespace iFactr.Data
{

    /// <summary>
    /// Contains DateTime values for the most frequently used cache expiration periods.
    /// </summary>
    /// <remarks>
    /// The <c>CachePeriod </c>class is a utility class that calculates <c>DateTime </c>differentials based on the current UTC date/time.
    /// </remarks>
    public static class CachePeriod
    {
        /// <summary>
        /// Sets the cache period to expire immediately.
        /// </summary>
        /// <returns>The current UTC time to expire a cached item immediately.</returns>
        public static DateTime Expired()
        {
            return DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the cache period to expire in one hour.
        /// </summary>
        /// <returns>The current UTC time plus one hour.</returns>
        public static DateTime OneHour()
        {
            return DateTime.UtcNow.AddHours( 1 );
        }

        /// <summary>
        /// Sets the cache period to expire in the number of hours specified.
        /// </summary>
        /// <returns>The current UTC time plus the number of hours in the argument provided.</returns>
        public static DateTime Hours(int hours)
        {
            return DateTime.UtcNow.AddHours(hours);
        }

        /// <summary>
        /// Sets the cache period to expire in one day.
        /// </summary>
        /// <returns>The current UTC time plus one day.</returns>
        public static DateTime OneDay()
        {
            return DateTime.UtcNow.AddDays(1);
        }

        /// <summary>
        /// Sets the cache period to expire in the number of days specified.
        /// </summary>
        /// <returns>The current UTC time plus the number of days in the argument provided.</returns>
        public static DateTime Days(int days)
        {
            return DateTime.UtcNow.AddDays(days);
        }

        /// <summary>
        /// Sets the cache period to expire in one week.
        /// </summary>
        /// <returns>The current UTC time plus one week.</returns>
        public static DateTime OneWeek()
        {
            return DateTime.UtcNow.AddDays(7);
        }

        /// <summary>
        /// Sets the cache period to expire in the number of weeks specified.
        /// </summary>
        /// <returns>The current UTC time plus the number of weeks in the argument provided.</returns>
        public static DateTime Weeks(int weeks)
        {
            return DateTime.UtcNow.AddDays(weeks * 7);
        }

        /// <summary>
        /// Sets the cache period to expire in one month.
        /// </summary>
        /// <returns>The current UTC time plus one month.</returns>
        public static DateTime OneMonth()
        {
            return DateTime.UtcNow.AddMonths(1);
        }

        /// <summary>
        /// Sets the cache period to expire in the number of months specified.
        /// </summary>
        /// <returns>The current UTC time plus the number of months in the argument provided.</returns>
        public static DateTime Months(int months)
        {
            return DateTime.UtcNow.AddMonths(months);
        }

        /// <summary>
        /// Sets the cache period to expire in one year.
        /// </summary>
        /// <returns>The current UTC time plus one year.</returns>
        public static DateTime OneYear()
        {
            return DateTime.UtcNow.AddYears(1);
        }

        /// <summary>
        /// Sets the cache period to expire in the number of years specified.
        /// </summary>
        /// <returns>The current UTC time plus the number of years in the argument provided.</returns>
        public static DateTime Years(int years)
        {
            return DateTime.Now.AddYears( years );
        }

        /// <summary>
        /// Sets the cache period to the default value.
        /// </summary>
        /// <returns>The current UTC time plus one month.</returns>
        public static DateTime Default()
        {
            return CachePeriod.OneMonth();
        }
    }
}
