using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Honeypots
{
    /// <summary>
    /// Business rules for the Honeypots domain.
    /// Encapsulates complex domain logic and validation.
    /// </summary>

    /// <summary>
    /// Rule: Honeypot name must be unique within organization.
    /// </summary>
    public class HoneypotNameUniquenessRule : IBusinessRule
    {
        private readonly Honeypot _honeypot;
        private readonly IEnumerable<Honeypot> _existingHoneypots;

        public HoneypotNameUniquenessRule(Honeypot honeypot, IEnumerable<Honeypot> existingHoneypots)
        {
            _honeypot = honeypot;
            _existingHoneypots = existingHoneypots;
        }

        public bool IsSatisfied()
        {
            return !_existingHoneypots.Any(h => 
                h.Id != _honeypot.Id && 
                h.Name.Equals(_honeypot.Name, StringComparison.OrdinalIgnoreCase));
        }

        public Error Error => Error.Custom(
            "Honeypot.NameMustBeUnique",
            "A honeypot with this name already exists in the organization.");
    }

    /// <summary>
    /// Rule: Cannot deploy honeypot if subscription is inactive.
    /// </summary>
    public class HoneypotDeploymentSubscriptionRule : IBusinessRule
    {
        private readonly string _subscriptionStatus;

        public HoneypotDeploymentSubscriptionRule(string subscriptionStatus)
        {
            _subscriptionStatus = subscriptionStatus;
        }

        public bool IsSatisfied()
        {
            // Assuming subscription status is "Active" when deployable
            return _subscriptionStatus.Equals("Active", StringComparison.OrdinalIgnoreCase);
        }

        public Error Error => HoneypotErrors.CannotDeployInactiveSubscription;
    }

    /// <summary>
    /// Rule: Honeypot quota must not be exceeded.
    /// </summary>
    public class HoneypotQuotaRule : IBusinessRule
    {
        private readonly int _currentCount;
        private readonly int _maxAllowed;

        public HoneypotQuotaRule(int currentCount, int maxAllowed)
        {
            _currentCount = currentCount;
            _maxAllowed = maxAllowed;
        }

        public bool IsSatisfied()
        {
            return _currentCount < _maxAllowed;
        }

        public Error Error => HoneypotErrors.MaxHoneypotsReached;
    }

    /// <summary>
    /// Rule: Honeypot can only be deployed if external service is available.
    /// </summary>
    public class ExternalServiceAvailabilityRule : IBusinessRule
    {
        private readonly bool _isAvailable;

        public ExternalServiceAvailabilityRule(bool isAvailable)
        {
            _isAvailable = isAvailable;
        }

        public bool IsSatisfied()
        {
            return _isAvailable;
        }

        public Error Error => HoneypotErrors.ExternalServiceUnavailable;
    }

    /// <summary>
    /// Rule: Honeypot status transition must be valid.
    /// </summary>
    public class HoneypotStatusTransitionRule : IBusinessRule
    {
        private readonly HoneypotStatus _currentStatus;
        private readonly HoneypotStatus _requestedStatus;

        public HoneypotStatusTransitionRule(HoneypotStatus currentStatus, HoneypotStatus requestedStatus)
        {
            _currentStatus = currentStatus;
            _requestedStatus = requestedStatus;
        }

        public bool IsSatisfied()
        {
            return _currentStatus switch
            {
                HoneypotStatus.Provisioning => _requestedStatus == HoneypotStatus.Active || _requestedStatus == HoneypotStatus.Error,
                HoneypotStatus.Active => _requestedStatus == HoneypotStatus.Paused || _requestedStatus == HoneypotStatus.Error || _requestedStatus == HoneypotStatus.Terminated,
                HoneypotStatus.Paused => _requestedStatus == HoneypotStatus.Active || _requestedStatus == HoneypotStatus.Terminated,
                HoneypotStatus.Inactive => _requestedStatus == HoneypotStatus.Active || _requestedStatus == HoneypotStatus.Terminated,
                HoneypotStatus.Error => _requestedStatus == HoneypotStatus.Terminated || _requestedStatus == HoneypotStatus.Active,
                HoneypotStatus.Terminated => false,
                HoneypotStatus.Retired => false,
                _ => false
            };
        }

        public Error Error => HoneypotErrors.InvalidStatusTransition;
    }

    /// <summary>
    /// Rule: Honeypot must be linked to external service before marking as deployed.
    /// </summary>
    public class HoneypotExternalServiceLinkingRule : IBusinessRule
    {
        private readonly ExternalServiceReference? _externalService;
        private readonly HoneypotNetworkInfo? _networkInfo;

        public HoneypotExternalServiceLinkingRule(ExternalServiceReference? externalService, HoneypotNetworkInfo? networkInfo)
        {
            _externalService = externalService;
            _networkInfo = networkInfo;
        }

        public bool IsSatisfied()
        {
            return _externalService is not null && _networkInfo is not null;
        }

        public Error Error => HoneypotErrors.ExternalServiceNotLinked;
    }

    /// <summary>
    /// Rule: Port must be available (not in use by another honeypot).
    /// </summary>
    public class HoneypotPortUniquenessRule : IBusinessRule
    {
        private readonly int _port;
        private readonly string? _ipAddress;
        private readonly IEnumerable<Honeypot> _existingHoneypots;

        public HoneypotPortUniquenessRule(int port, string? ipAddress, IEnumerable<Honeypot> existingHoneypots)
        {
            _port = port;
            _ipAddress = ipAddress;
            _existingHoneypots = existingHoneypots;
        }

        public bool IsSatisfied()
        {
            if (string.IsNullOrEmpty(_ipAddress))
                return true; // Port uniqueness only checked when IP is known

            return !_existingHoneypots.Any(h =>
                h.NetworkInfo?.Port == _port &&
                h.NetworkInfo?.IpAddress == _ipAddress &&
                h.Status != HoneypotStatus.Terminated);
        }

        public Error Error => Error.Custom(
            "Honeypot.PortAlreadyInUse",
            "The specified port is already in use by another honeypot on this IP address.");
    }

    /// <summary>
    /// Rule: Storage quota must not be exceeded.
    /// </summary>
    public class HoneypotStorageQuotaRule : IBusinessRule
    {
        private readonly decimal _currentStorageGb;
        private readonly decimal _maxStorageGb;

        public HoneypotStorageQuotaRule(decimal currentStorageGb, decimal maxStorageGb)
        {
            _currentStorageGb = currentStorageGb;
            _maxStorageGb = maxStorageGb;
        }

        public bool IsSatisfied()
        {
            return _currentStorageGb <= _maxStorageGb;
        }

        public Error Error => HoneypotErrors.StorageQuotaExceeded;
    }

    /// <summary>
    /// Rule: Honeypot cannot be updated if provisioning.
    /// </summary>
    public class HoneypotNotProvisioningRule : IBusinessRule
    {
        private readonly HoneypotStatus _status;

        public HoneypotNotProvisioningRule(HoneypotStatus status)
        {
            _status = status;
        }

        public bool IsSatisfied()
        {
            return _status != HoneypotStatus.Provisioning;
        }

        public Error Error => HoneypotErrors.CannotUpdateDeployingHoneypot;
    }

    /// <summary>
    /// Rule: Honeypot cannot be updated if terminated.
    /// </summary>
    public class HoneypotNotTerminatedRule : IBusinessRule
    {
        private readonly HoneypotStatus _status;

        public HoneypotNotTerminatedRule(HoneypotStatus status)
        {
            _status = status;
        }

        public bool IsSatisfied()
        {
            return _status != HoneypotStatus.Terminated;
        }

        public Error Error => HoneypotErrors.CannotUpdateTerminatedHoneypot;
    }

    /// <summary>
    /// Rule: Honeypot health must be acceptable for operation.
    /// </summary>
    public class HoneypotHealthStatusRule : IBusinessRule
    {
        private readonly HoneypotHealthStatus _health;

        public HoneypotHealthStatusRule(HoneypotHealthStatus health)
        {
            _health = health;
        }

        public bool IsSatisfied()
        {
            return _health != HoneypotHealthStatus.Unhealthy;
        }

        public Error Error => HoneypotErrors.HealthCheckFailed;
    }

    /// <summary>
    /// Rule: Configuration must be valid for deployment.
    /// </summary>
    public class HoneypotConfigurationValidationRule : IBusinessRule
    {
        private readonly HoneypotConfiguration _config;

        public HoneypotConfigurationValidationRule(HoneypotConfiguration config)
        {
            _config = config;
        }

        public bool IsSatisfied()
        {
            if (_config is null)
                return false;

            if (_config.Port <= 0 || _config.Port > 65535)
                return false;

            if (_config.RetentionDays <= 0 || _config.RetentionDays > 2555)
                return false;

            return true;
        }

        public Error Error => HoneypotErrors.InvalidConfiguration;
    }

    /// <summary>
    /// Business rule: Organization cannot exceed maximum honeypot limit.
    /// </summary>
    public class MaxHoneypotsPerOrganizationRule : IBusinessRule
    {
        private readonly int _currentCount;
        private readonly int _maxAllowed;

        public MaxHoneypotsPerOrganizationRule(int currentCount, int maxAllowed)
        {
            _currentCount = currentCount;
            _maxAllowed = maxAllowed;
        }

        public bool IsSatisfied() => _currentCount < _maxAllowed;

        public Error Error => Error.Custom(
            "Honeypot.QuotaExceeded",
            $"Cannot create honeypot. Organization has reached maximum limit of {_maxAllowed}. " +
            $"Current count: {_currentCount}.");
    }
}
