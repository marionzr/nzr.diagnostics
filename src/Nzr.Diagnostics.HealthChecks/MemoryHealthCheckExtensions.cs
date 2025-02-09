namespace Nzr.Diagnostics.HealthChecks;

/// <summary>
/// Extension method to convert the values from MB to Bytes
/// </summary>
internal static class MemoryHealthCheckExtensions
{
    private const long BytesPerMB = 1024L * 1024L;

    /// <summary>
    /// Converts the value from bytes to megabytes (MB) and rounds up to the nearest whole number.
    /// </summary>
    /// <param name="valueInBytes">The metric to be converted (in bytes)</param>
    /// <returns>The metric in megabytes (MB) rounded up</returns>
    public static long FromBytesToMegabytes(this long valueInBytes)
    {
        return Convert.ToInt64(Math.Ceiling((double)valueInBytes / BytesPerMB));
    }

    /// <summary>
    /// Converts the value from megabytes (MB) to bytes.
    /// </summary>
    /// <param name="valueInMegabytes">The metric to be converted (in megabytes)</param>
    /// <returns>The metric in bytes</returns>
    public static long FromMegabytesToBytes(this long valueInMegabytes)
    {
        return valueInMegabytes * BytesPerMB;
    }
}
