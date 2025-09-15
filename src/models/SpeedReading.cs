using System;

namespace NetworkUsage.Contracts
{
    /// <summary>
    /// Immutable representation of a speed measurement with automatic unit scaling
    /// Based on data-model.md specification - value object with auto-scaling display
    /// </summary>
    public readonly struct SpeedReading : IEquatable<SpeedReading>, IComparable<SpeedReading>
    {
        /// <summary>
        /// Raw speed in bytes per second
        /// </summary>
        public double BytesPerSecond { get; }

        /// <summary>
        /// Scaled value for display (e.g., 1.5 for 1.5 MB/s)
        /// </summary>
        public double DisplayValue { get; }

        /// <summary>
        /// Unit for display (Bytes, KB, MB, GB)
        /// </summary>
        public SpeedUnit DisplayUnit { get; }

        /// <summary>
        /// Ready-to-display string (e.g., "1.5 MB/s")
        /// </summary>
        public string FormattedString { get; }

        /// <summary>
        /// Constructor with raw bytes per second - calculates auto-scaling
        /// </summary>
        /// <param name="bytesPerSecond">Raw speed in bytes per second</param>
        private SpeedReading(double bytesPerSecond)
        {
            if (bytesPerSecond < 0)
                throw new ArgumentOutOfRangeException(nameof(bytesPerSecond), "Speed cannot be negative");

            BytesPerSecond = bytesPerSecond;
            
            // Calculate auto-scaled display values
            var (displayValue, unit) = CalculateAutoScale(bytesPerSecond);
            DisplayValue = displayValue;
            DisplayUnit = unit;
            FormattedString = FormatSpeed(displayValue, unit);
        }

        #region Factory Methods

        /// <summary>
        /// Creates SpeedReading from bytes per second with auto-scaling
        /// </summary>
        /// <param name="bytesPerSecond">Raw speed in bytes per second</param>
        /// <returns>SpeedReading with auto-scaled display values</returns>
        public static SpeedReading FromBytesPerSecond(double bytesPerSecond)
        {
            return new SpeedReading(bytesPerSecond);
        }

        /// <summary>
        /// Creates zero speed reading
        /// </summary>
        /// <returns>SpeedReading representing zero speed</returns>
        public static SpeedReading Zero => new(0);

        /// <summary>
        /// Creates SpeedReading from kilobytes per second
        /// </summary>
        public static SpeedReading FromKilobytesPerSecond(double kilobytesPerSecond)
        {
            return new SpeedReading(kilobytesPerSecond * 1024);
        }

        /// <summary>
        /// Creates SpeedReading from megabytes per second
        /// </summary>
        public static SpeedReading FromMegabytesPerSecond(double megabytesPerSecond)
        {
            return new SpeedReading(megabytesPerSecond * 1024 * 1024);
        }

        /// <summary>
        /// Creates SpeedReading from gigabytes per second
        /// </summary>
        public static SpeedReading FromGigabytesPerSecond(double gigabytesPerSecond)
        {
            return new SpeedReading(gigabytesPerSecond * 1024 * 1024 * 1024);
        }

        #endregion

        #region Auto-Scaling Logic

        /// <summary>
        /// Calculate auto-scaled display value and unit
        /// B/s → KB/s → MB/s → GB/s based on magnitude
        /// </summary>
        private static (double value, SpeedUnit unit) CalculateAutoScale(double bytesPerSecond)
        {
            const double kb = 1024;
            const double mb = kb * 1024;
            const double gb = mb * 1024;

            return bytesPerSecond switch
            {
                >= gb => (bytesPerSecond / gb, SpeedUnit.Gigabytes),
                >= mb => (bytesPerSecond / mb, SpeedUnit.Megabytes),
                >= kb => (bytesPerSecond / kb, SpeedUnit.Kilobytes),
                _ => (bytesPerSecond, SpeedUnit.Bytes)
            };
        }

        /// <summary>
        /// Format speed value with appropriate unit suffix
        /// </summary>
        private static string FormatSpeed(double value, SpeedUnit unit)
        {
            var unitSuffix = unit switch
            {
                SpeedUnit.Bytes => "B/s",
                SpeedUnit.Kilobytes => "KB/s", 
                SpeedUnit.Megabytes => "MB/s",
                SpeedUnit.Gigabytes => "GB/s",
                _ => "?/s"
            };

            // Format with appropriate precision based on magnitude
            var formatString = unit switch
            {
                SpeedUnit.Bytes => "F0", // No decimal places for bytes
                SpeedUnit.Kilobytes => value >= 100 ? "F0" : "F1", // 1 decimal for < 100 KB/s
                SpeedUnit.Megabytes => value >= 100 ? "F0" : "F1", // 1 decimal for < 100 MB/s
                SpeedUnit.Gigabytes => "F2", // 2 decimals for GB/s (precision important)
                _ => "F1"
            };

            return $"{value.ToString(formatString)} {unitSuffix}";
        }

        #endregion

        #region Unit Conversion Methods

        /// <summary>
        /// Get value in kilobytes per second
        /// </summary>
        public double ToKilobytesPerSecond() => BytesPerSecond / 1024;

        /// <summary>
        /// Get value in megabytes per second
        /// </summary>
        public double ToMegabytesPerSecond() => BytesPerSecond / (1024 * 1024);

        /// <summary>
        /// Get value in gigabytes per second
        /// </summary>
        public double ToGigabytesPerSecond() => BytesPerSecond / (1024 * 1024 * 1024);

        /// <summary>
        /// Get value in bits per second
        /// </summary>
        public double ToBitsPerSecond() => BytesPerSecond * 8;

        #endregion

        #region Value Object Equality and Comparison

        /// <summary>
        /// Compare based on BytesPerSecond value
        /// </summary>
        public int CompareTo(SpeedReading other)
        {
            return BytesPerSecond.CompareTo(other.BytesPerSecond);
        }

        /// <summary>
        /// Value-based equality comparison
        /// </summary>
        public bool Equals(SpeedReading other)
        {
            // Use small tolerance for double comparison
            return Math.Abs(BytesPerSecond - other.BytesPerSecond) < 0.001;
        }

        public override bool Equals(object? obj)
        {
            return obj is SpeedReading other && Equals(other);
        }

        public override int GetHashCode()
        {
            // Round to avoid floating point precision issues in hash
            return Math.Round(BytesPerSecond, 3).GetHashCode();
        }

        public static bool operator ==(SpeedReading left, SpeedReading right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SpeedReading left, SpeedReading right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(SpeedReading left, SpeedReading right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(SpeedReading left, SpeedReading right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(SpeedReading left, SpeedReading right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(SpeedReading left, SpeedReading right)
        {
            return left.CompareTo(right) >= 0;
        }

        #endregion

        #region Arithmetic Operations

        /// <summary>
        /// Add two speed readings
        /// </summary>
        public static SpeedReading operator +(SpeedReading left, SpeedReading right)
        {
            return FromBytesPerSecond(left.BytesPerSecond + right.BytesPerSecond);
        }

        /// <summary>
        /// Subtract two speed readings
        /// </summary>
        public static SpeedReading operator -(SpeedReading left, SpeedReading right)
        {
            var result = left.BytesPerSecond - right.BytesPerSecond;
            return FromBytesPerSecond(Math.Max(0, result)); // Prevent negative speeds
        }

        /// <summary>
        /// Multiply speed reading by scalar
        /// </summary>
        public static SpeedReading operator *(SpeedReading speed, double multiplier)
        {
            if (multiplier < 0) 
                throw new ArgumentOutOfRangeException(nameof(multiplier), "Multiplier cannot be negative");
            return FromBytesPerSecond(speed.BytesPerSecond * multiplier);
        }

        /// <summary>
        /// Multiply speed reading by scalar (reverse order)
        /// </summary>
        public static SpeedReading operator *(double multiplier, SpeedReading speed)
        {
            return speed * multiplier;
        }

        /// <summary>
        /// Divide speed reading by scalar
        /// </summary>
        public static SpeedReading operator /(SpeedReading speed, double divisor)
        {
            if (divisor <= 0) 
                throw new ArgumentOutOfRangeException(nameof(divisor), "Divisor must be positive");
            return FromBytesPerSecond(speed.BytesPerSecond / divisor);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if this speed is considered "fast" (>= 1 MB/s)
        /// </summary>
        public bool IsFast => BytesPerSecond >= 1024 * 1024;

        /// <summary>
        /// Check if this speed is considered "slow" (< 1 KB/s)
        /// </summary>
        public bool IsSlow => BytesPerSecond < 1024;

        /// <summary>
        /// Get percentage of another speed reading
        /// </summary>
        public double GetPercentageOf(SpeedReading other)
        {
            if (other.BytesPerSecond == 0) return 0;
            return (BytesPerSecond / other.BytesPerSecond) * 100;
        }

        /// <summary>
        /// Calculate average with another speed reading
        /// </summary>
        public SpeedReading Average(SpeedReading other)
        {
            return FromBytesPerSecond((BytesPerSecond + other.BytesPerSecond) / 2);
        }

        /// <summary>
        /// Get custom formatted string with specific unit
        /// </summary>
        public string ToStringWithUnit(SpeedUnit unit)
        {
            var value = unit switch
            {
                SpeedUnit.Bytes => BytesPerSecond,
                SpeedUnit.Kilobytes => ToKilobytesPerSecond(),
                SpeedUnit.Megabytes => ToMegabytesPerSecond(),
                SpeedUnit.Gigabytes => ToGigabytesPerSecond(),
                _ => BytesPerSecond
            };

            return FormatSpeed(value, unit);
        }

        #endregion

        /// <summary>
        /// Returns FormattedString (e.g., "1.5 MB/s")
        /// </summary>
        public override string ToString()
        {
            return FormattedString;
        }

        /// <summary>
        /// Create a detailed string representation for debugging
        /// </summary>
        public string ToDetailedString()
        {
            return $"SpeedReading: {FormattedString} ({BytesPerSecond:F1} B/s raw)";
        }
    }
}