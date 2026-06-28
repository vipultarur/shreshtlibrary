using System;
using System.Runtime.InteropServices;

namespace WebApplication1.Services
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
        DateTime IstNow { get; }
        TimeZoneInfo IstTimeZone { get; }
    }

    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;

        public TimeZoneInfo IstTimeZone
        {
            get
            {
                string tzName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                    ? "India Standard Time" 
                    : "Asia/Kolkata";
                return TimeZoneInfo.FindSystemTimeZoneById(tzName);
            }
        }

        public DateTime IstNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstTimeZone);
    }
}
