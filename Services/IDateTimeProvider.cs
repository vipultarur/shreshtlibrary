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
        private static readonly TimeZoneInfo _istTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "India Standard Time" : "Asia/Kolkata");

        public DateTime UtcNow => DateTime.UtcNow;

        public TimeZoneInfo IstTimeZone => _istTimeZone;

        public DateTime IstNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _istTimeZone);
    }
}
