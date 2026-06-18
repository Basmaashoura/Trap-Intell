using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Honeypots
{
    /// <summary>
    /// Value objects for the Honeypots domain.
    /// </summary>

    /// <summary>
    /// Represents honeypot configuration settings.
    /// </summary>
    public record HoneypotConfiguration
    {
        public int Port { get; }
        public string? Credentials { get; }
        public LogCaptureLevel CaptureLevel { get; }
        public int? MaxConnections { get; }
        public bool RecordPayload { get; }
        public int RetentionDays { get; }
        public Dictionary<string, string>? CustomSettings { get; }

        public HoneypotConfiguration(
            int port,
            LogCaptureLevel captureLevel = LogCaptureLevel.Standard,
            string? credentials = null,
            int? maxConnections = null,
            bool recordPayload = true,
            int retentionDays = 90,
            Dictionary<string, string>? customSettings = null)
        {
            if (port <= 0 || port > 65535)
                throw new ArgumentException("Port must be between 1 and 65535.", nameof(port));

            if (retentionDays <= 0 || retentionDays > 2555)
                throw new ArgumentException("Retention days must be between 1 and 2555.", nameof(retentionDays));

            if (maxConnections.HasValue && maxConnections <= 0)
                throw new ArgumentException("Max connections must be greater than 0.", nameof(maxConnections));

            Port = port;
            Credentials = credentials;
            CaptureLevel = captureLevel;
            MaxConnections = maxConnections;
            RecordPayload = recordPayload;
            RetentionDays = retentionDays;
            CustomSettings = customSettings ?? new Dictionary<string, string>();
        }

        public static Result<HoneypotConfiguration> Create(
            int port,
            LogCaptureLevel captureLevel = LogCaptureLevel.Standard,
            string? credentials = null,
            int? maxConnections = null,
            bool recordPayload = true,
            int retentionDays = 90,
            Dictionary<string, string>? customSettings = null)
        {
            try
            {
                var config = new HoneypotConfiguration(
                    port, captureLevel, credentials, maxConnections, recordPayload, retentionDays, customSettings);
                return Result.Success(config);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<HoneypotConfiguration>(
                    Error.Custom("Honeypot.InvalidConfiguration", ex.Message));
            }
        }
    }

    /// <summary>
    /// Represents health metrics of a honeypot.
    /// </summary>
    public record HoneypotHealth
    {
        public HoneypotHealthStatus Status { get; }
        public DateTime? LastHeartbeat { get; }
        public decimal CpuUsagePercent { get; }
        public decimal MemoryUsagePercent { get; }
        public decimal DiskUsagePercent { get; }
        public int ActiveConnections { get; }
        public long StorageUsedBytes { get; }
        public int FailedConnectionAttempts { get; }

        public HoneypotHealth(
            HoneypotHealthStatus status = HoneypotHealthStatus.Unknown,
            DateTime? lastHeartbeat = null,
            decimal cpuUsagePercent = 0,
            decimal memoryUsagePercent = 0,
            decimal diskUsagePercent = 0,
            int activeConnections = 0,
            long storageUsedBytes = 0,
            int failedConnectionAttempts = 0)
        {
            if (cpuUsagePercent < 0 || cpuUsagePercent > 100)
                throw new ArgumentException("CPU usage must be between 0 and 100.", nameof(cpuUsagePercent));

            if (memoryUsagePercent < 0 || memoryUsagePercent > 100)
                throw new ArgumentException("Memory usage must be between 0 and 100.", nameof(memoryUsagePercent));

            if (diskUsagePercent < 0 || diskUsagePercent > 100)
                throw new ArgumentException("Disk usage must be between 0 and 100.", nameof(diskUsagePercent));

            Status = status;
            LastHeartbeat = lastHeartbeat;
            CpuUsagePercent = cpuUsagePercent;
            MemoryUsagePercent = memoryUsagePercent;
            DiskUsagePercent = diskUsagePercent;
            ActiveConnections = activeConnections;
            StorageUsedBytes = storageUsedBytes;
            FailedConnectionAttempts = failedConnectionAttempts;
        }

        public bool IsHealthy => Status == HoneypotHealthStatus.Healthy;
        public bool IsDegraded => Status == HoneypotHealthStatus.Degraded;
        public bool IsUnhealthy => Status == HoneypotHealthStatus.Unhealthy;

        public decimal StorageUsedGb => StorageUsedBytes / (1024m * 1024m * 1024m);
    }

    /// <summary>
    /// Represents network location of a honeypot.
    /// </summary>
    public record HoneypotNetworkInfo
    {
        public string IpAddress { get; }
        public int Port { get; }
        public string? Hostname { get; }
        public string? MacAddress { get; }
        public string? NetworkInterface { get; }

        public HoneypotNetworkInfo(
            string ipAddress,
            int port,
            string? hostname = null,
            string? macAddress = null,
            string? networkInterface = null)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("IP address cannot be empty.", nameof(ipAddress));

            if (port <= 0 || port > 65535)
                throw new ArgumentException("Port must be between 1 and 65535.", nameof(port));

            IpAddress = ipAddress;
            Port = port;
            Hostname = hostname;
            MacAddress = macAddress;
            NetworkInterface = networkInterface;
        }

        public string FullAddress => $"{IpAddress}:{Port}";
    }

    /// <summary>
    /// Represents external service reference for the honeypot.
    /// </summary>
    public record ExternalServiceReference
    {
        public string ServiceId { get; }
        public string ServiceName { get; }
        public string ApiEndpoint { get; }
        public DateTime LinkedAt { get; }
        public string? ServiceVersion { get; }

        public ExternalServiceReference(
            string serviceId,
            string serviceName,
            string apiEndpoint,
            string? serviceVersion = null)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
                throw new ArgumentException("Service ID cannot be empty.", nameof(serviceId));

            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("Service name cannot be empty.", nameof(serviceName));

            if (string.IsNullOrWhiteSpace(apiEndpoint))
                throw new ArgumentException("API endpoint cannot be empty.", nameof(apiEndpoint));

            ServiceId = serviceId;
            ServiceName = serviceName;
            ApiEndpoint = apiEndpoint;
            LinkedAt = DateTime.UtcNow;
            ServiceVersion = serviceVersion;
        }
    }

    /// <summary>
    /// Represents honeypot statistics.
    /// </summary>
    public record HoneypotStatistics
    {
        public int TotalEventsCapture { get; init; }
        public int CriticalEvents { get; init; }
        public int HighSeverityEvents { get; init; }
        public int MediumSeverityEvents { get; init; }
        public int LowSeverityEvents { get; init; }
        public int UniqueSourceIps { get; init; }
        public int FailedAuthenticationAttempts { get; init; }
        public int SuccessfulConnectionAttempts { get; init; }
        public DateTime? FirstEventTime { get; init; }
        public DateTime? LastEventTime { get; init; }

        // Private constructor for EF Core
        private HoneypotStatistics() { }

        public HoneypotStatistics(
            int totalEventsCapture = 0,
            int criticalEvents = 0,
            int highSeverityEvents = 0,
            int mediumSeverityEvents = 0,
            int lowSeverityEvents = 0,
            int uniqueSourceIps = 0,
            int failedAuthenticationAttempts = 0,
            int successfulConnectionAttempts = 0,
            DateTime? firstEventTime = null,
            DateTime? lastEventTime = null)
        {
            TotalEventsCapture = totalEventsCapture;
            CriticalEvents = criticalEvents;
            HighSeverityEvents = highSeverityEvents;
            MediumSeverityEvents = mediumSeverityEvents;
            LowSeverityEvents = lowSeverityEvents;
            UniqueSourceIps = uniqueSourceIps;
            FailedAuthenticationAttempts = failedAuthenticationAttempts;
            SuccessfulConnectionAttempts = successfulConnectionAttempts;
            FirstEventTime = firstEventTime;
            LastEventTime = lastEventTime;
        }

        public int TotalAttackEvents => CriticalEvents + HighSeverityEvents + MediumSeverityEvents + LowSeverityEvents;
        public decimal AverageEventsPerDay
        {
            get
            {
                if (FirstEventTime is null || LastEventTime is null)
                    return 0;

                var days = (LastEventTime.Value - FirstEventTime.Value).TotalDays;
                return days > 0 ? TotalEventsCapture / (decimal)days : 0;
            }
        }
    }
}
