namespace Trap_Intel.Domain.Commands.Enums;

/// <summary>
/// Type of command sent to Go honeypot agent.
/// Each command type has specific payload requirements.
/// </summary>
public enum AgentCommandType
{
    Unknown = 0,
    
    // Configuration Management
    UpdateConfiguration = 1,        // Update honeypot configuration
    ChangeLogLevel = 2,             // Change logging verbosity
    UpdateRetentionPolicy = 3,      // Update log retention settings
    
    // Security Operations
    BlockIP = 10,                   // Block specific IP address
    UnblockIP = 11,                 // Unblock previously blocked IP
    BlockIPRange = 12,              // Block IP range (CIDR)
    
    // Deception Management
    UpdateDeceptionAsset = 20,      // Update fake file/database
    AddDeceptionAsset = 21,         // Add new deception asset
    RemoveDeceptionAsset = 22,      // Remove deception asset
    UpdateHoneytoken = 23,          // Update honeytokens (fake credentials)
    
    // Agent Control
    RestartAgent = 30,              // Restart Go agent process
    StopAgent = 31,                 // Stop Go agent
    StartAgent = 32,                // Start Go agent
    UpdateAgent = 33,               // Update agent to new version
    
    // Data Management
    FlushLogs = 40,                 // Force immediate log sync
    ClearCache = 41,                // Clear agent cache
    ResetStatistics = 42,           // Reset statistics counters
    
    // Network Operations
    ChangeListeningPort = 50,       // Change listening port
    EnableSSL = 51,                 // Enable SSL/TLS
    DisableSSL = 52,                // Disable SSL/TLS
    
    // Health & Diagnostics
    RunDiagnostics = 60,            // Run diagnostic tests
    CollectMetrics = 61,            // Collect performance metrics
    GenerateReport = 62             // Generate status report
}

/// <summary>
/// Status of command execution.
/// Tracks the lifecycle of a command from creation to completion.
/// </summary>
public enum AgentCommandStatus
{
    Pending = 0,            // Created, not sent yet
    Queued = 1,             // Queued for delivery
    Sent = 2,               // Sent to agent via gRPC
    Acknowledged = 3,       // Agent acknowledged receipt
    InProgress = 4,         // Agent is executing command
    Completed = 5,          // Successfully completed
    Failed = 6,             // Execution failed
    Timeout = 7,            // Timed out waiting for response
    Cancelled = 8,          // Cancelled before execution
    PartialSuccess = 9      // Partially completed (e.g., some IPs blocked)
}

/// <summary>
/// Priority of command execution.
/// Higher priority commands are processed first.
/// </summary>
public enum CommandPriority
{
    Low = 0,        // Background tasks (diagnostics, reports)
    Normal = 1,     // Standard operations (config updates)
    High = 2,       // Security operations (block IP)
    Critical = 3    // Emergency operations (stop agent, security breach response)
}

/// <summary>
/// Delivery method for command.
/// </summary>
public enum CommandDeliveryMethod
{
    Immediate = 0,      // Send immediately via active gRPC connection
    Queued = 1,         // Queue for next heartbeat/poll
    Scheduled = 2       // Send at specific time
}
