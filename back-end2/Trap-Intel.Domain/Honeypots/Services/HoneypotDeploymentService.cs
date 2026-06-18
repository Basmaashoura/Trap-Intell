using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Domain.Auditing.Services;

namespace Trap_Intel.Domain.Honeypots.Services
{
    /// <summary>
    /// Domain service for orchestrating honeypot deployments.
    /// 
    /// ?? WARNING: This service violates DDD layering principles and should be moved to Application Layer.
    /// It orchestrates multiple aggregates, external services, and persistence - these are Application Layer concerns.
    /// 
    /// TODO: Move to Trap-Intel.Application/Honeypots/Commands/DeployHoneypot/DeployHoneypotHandler.cs
    /// 
    /// Coordinates between:
    /// - Honeypot aggregate (domain model)
    /// - External honeypot service (REST API)
    /// - Subscription quotas (business rules)
    /// - Audit trail (compliance)
    /// 
    /// Workflow orchestration ONLY - all actual honeypot operations handled by external service.
    /// </summary>
    [Obsolete("This service should be moved to Application Layer. It orchestrates multiple concerns that belong in Application Layer handlers.")]
    public class HoneypotDeploymentService
    {
        private readonly IHoneypotRepository _honeypotRepository;
        private readonly IExternalHoneypotService _externalService;
        private readonly AuditService _auditService;
        private readonly IAuditTrailRepository _auditRepository;

        public HoneypotDeploymentService(
            IHoneypotRepository honeypotRepository,
            IExternalHoneypotService externalService,
            AuditService auditService,
            IAuditTrailRepository auditRepository)
        {
            _honeypotRepository = honeypotRepository ?? throw new ArgumentNullException(nameof(honeypotRepository));
            _externalService = externalService ?? throw new ArgumentNullException(nameof(externalService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        }

        /// <summary>
        /// Deploy a honeypot to the external service.
        /// 
        /// Workflow:
        /// 1. Validate organization and subscription exist
        /// 2. Validate quota not exceeded
        /// 3. Validate external service available
        /// 4. Create honeypot aggregate (Status: Provisioning)
        /// 5. Request deployment from external service
        /// 6. If success: Link to external service, mark as Active
        /// 7. If failure: Mark as Error, log failure
        /// 8. Audit the deployment
        /// 
        /// Returns the deployed honeypot or error.
        /// </summary>
        public async Task<Result<Honeypot>> DeployHoneypotAsync(
            Guid organizationId,
            Guid subscriptionId,
            string name,
            HoneypotType type,
            HoneypotConfiguration configuration,
            HoneypotDeploymentLocation deploymentLocation,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            // Validation
            if (organizationId == Guid.Empty)
                return Result.Failure<Honeypot>(HoneypotErrors.InvalidOrganizationId);

            if (subscriptionId == Guid.Empty)
                return Result.Failure<Honeypot>(HoneypotErrors.InvalidSubscriptionId);

            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure<Honeypot>(HoneypotErrors.InvalidName);

            if (configuration is null)
                return Result.Failure<Honeypot>(HoneypotErrors.InvalidConfiguration);

            try
            {
                // Step 1: Validate external service is available
                var healthCheck = await _externalService.IsHealthyAsync(cancellationToken);
                if (healthCheck.IsFailure)
                    return Result.Failure<Honeypot>(HoneypotErrors.ExternalServiceUnavailable);

                // Step 2: Create honeypot aggregate (Status: Provisioning)
                var honeypotResult = Honeypot.Create(
                    organizationId,
                    subscriptionId,
                    name,
                    type,
                    configuration,
                    deploymentLocation);

                if (honeypotResult.IsFailure)
                    return honeypotResult;

                var honeypot = honeypotResult.Value;

                // Step 3: Request deployment from external service
                var deploymentRequest = new ExternalHoneypotDeploymentRequest
                {
                    Name = honeypot.Name,
                    Type = honeypot.Type,
                    OrganizationId = organizationId.ToString(),
                    Configuration = configuration,
                    Metadata = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "organization_id", organizationId.ToString() },
                        { "honeypot_id", honeypot.Id.ToString() },
                        { "deployment_time", DateTime.UtcNow.ToString("O") }
                    }
                };

                var deploymentResult = await _externalService.DeployAsync(
                    deploymentRequest,
                    cancellationToken);

                // Step 4a: Handle deployment success
                if (deploymentResult.IsSuccess)
                {
                    var deploymentResponse = deploymentResult.Value;

                    // Link to external service
                    var externalServiceRef = new ExternalServiceReference(
                        serviceId: deploymentResponse.ExternalHoneypotId,
                        serviceName: "Go-Honeypot-Service",
                        apiEndpoint: "external-service-api",
                        serviceVersion: deploymentResponse.ServiceVersion);

                    var linkResult = honeypot.LinkExternalService(externalServiceRef);
                    if (linkResult.IsFailure)
                        return Result.Failure<Honeypot>(linkResult.Errors);

                    // Update network info
                    var networkInfo = new HoneypotNetworkInfo(
                        ipAddress: deploymentResponse.IpAddress,
                        port: deploymentResponse.Port,
                        hostname: $"honeypot-{honeypot.Type.ToString().ToLower()}-{honeypot.Id.ToString().Substring(0, 8)}.local");

                    var networkResult = honeypot.UpdateNetworkInfo(networkInfo);
                    if (networkResult.IsFailure)
                        return Result.Failure<Honeypot>(networkResult.Errors);

                    // Mark as deployed
                    var deployedResult = honeypot.MarkAsDeployed();
                    if (deployedResult.IsFailure)
                        return Result.Failure<Honeypot>(deployedResult.Errors);

                    // Persist honeypot
                    await _honeypotRepository.AddAsync(honeypot, cancellationToken);

                    // Audit deployment success
                    var auditResult = _auditService.CreateSystemAuditLog(
                        organizationId,
                        AuditResourceType.HoneyPot,
                        honeypot.Id,
                        AuditAction.Create,
                        $"Honeypot deployed successfully. Type: {type}, Name: {name}, External ID: {deploymentResponse.ExternalHoneypotId}, IP: {deploymentResponse.IpAddress}:{deploymentResponse.Port}");

                    if (auditResult.IsSuccess)
                    {
                        await _auditRepository.AddAsync(auditResult.Value);
                    }

                    return Result.Success(honeypot);
                }
                else
                {
                    // Step 4b: Handle deployment failure
                    var errorMessage = string.Join(", ", deploymentResult.Errors.Select(e => e.Message));
                    
                    var failureResult = honeypot.MarkDeploymentFailed(errorMessage);
                    if (failureResult.IsFailure)
                        return Result.Failure<Honeypot>(failureResult.Errors);

                    await _honeypotRepository.AddAsync(honeypot, cancellationToken);

                    // Audit deployment failure
                    var auditFailureResult = _auditService.CreateFailureAuditLog(
                        organizationId,
                        userId,
                        AuditResourceType.HoneyPot,
                        honeypot.Id,
                        AuditAction.Create,
                        $"Honeypot deployment failed: {errorMessage}");

                    if (auditFailureResult.IsSuccess)
                    {
                        await _auditRepository.AddAsync(auditFailureResult.Value);
                    }

                    return Result.Failure<Honeypot>(HoneypotErrors.DeploymentFailed);
                }
            }
            catch (Exception ex)
            {
                // Log unexpected errors
                var auditExceptionResult = _auditService.CreateFailureAuditLog(
                    organizationId,
                    userId,
                    AuditResourceType.HoneyPot,
                    Guid.Empty,
                    AuditAction.Create,
                    $"Unexpected error during honeypot deployment: {ex.Message}");

                if (auditExceptionResult.IsSuccess)
                {
                    await _auditRepository.AddAsync(auditExceptionResult.Value);
                }

                return Result.Failure<Honeypot>(
                    Error.Custom("Honeypot.DeploymentException", $"Unexpected deployment error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Terminate a deployed honeypot.
        /// 
        /// Workflow:
        /// 1. Get honeypot from repository
        /// 2. Validate honeypot exists
        /// 3. Call external service to terminate
        /// 4. Update honeypot status to Terminated
        /// 5. Persist changes
        /// 6. Audit termination
        /// </summary>
        public async Task<Result> TerminateHoneypotAsync(
            Guid honeypotId,
            Guid userId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Step 1: Get honeypot
                var honeypot = await _honeypotRepository.GetByIdAsync(honeypotId, cancellationToken);
                if (honeypot is null)
                    return Result.Failure(HoneypotErrors.NotFound);

                // Step 2: Get external service ID
                if (honeypot.ExternalService is null)
                    return Result.Failure(HoneypotErrors.ExternalServiceNotLinked);

                // Step 3: Call external service to terminate
                var terminationResult = await _externalService.TerminateAsync(
                    honeypot.ExternalService.ServiceId,
                    cancellationToken);

                if (terminationResult.IsFailure)
                {
                    // Log termination failure but continue to mark as terminated in our system
                    var auditFailureResult = _auditService.CreateFailureAuditLog(
                        honeypot.OrganizationId,
                        userId,
                        AuditResourceType.HoneyPot,
                        honeypotId,
                        AuditAction.Delete,
                        $"Failed to terminate honeypot on external service: {string.Join(", ", terminationResult.Errors.Select(e => e.Message))}");

                    if (auditFailureResult.IsSuccess)
                    {
                        await _auditRepository.AddAsync(auditFailureResult.Value);
                    }
                }

                // Step 4: Update honeypot status
                var terminateResult = honeypot.Terminate(reason);
                if (terminateResult.IsFailure)
                    return terminateResult;

                // Step 5: Persist changes
                await _honeypotRepository.UpdateAsync(honeypot, cancellationToken);

                // Step 6: Audit termination
                var auditResult = _auditService.CreateSystemAuditLog(
                    honeypot.OrganizationId,
                    AuditResourceType.HoneyPot,
                    honeypotId,
                    AuditAction.Delete,
                    $"Honeypot terminated. Reason: {reason}");

                if (auditResult.IsSuccess)
                {
                    await _auditRepository.AddAsync(auditResult.Value);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(
                    Error.Custom("Honeypot.TerminationException", $"Unexpected termination error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Pause honeypot (stop capturing but keep deployed).
        /// </summary>
        public async Task<Result> PauseHoneypotAsync(
            Guid honeypotId,
            Guid userId,
            string? reason,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var honeypot = await _honeypotRepository.GetByIdAsync(honeypotId, cancellationToken);
                if (honeypot is null)
                    return Result.Failure(HoneypotErrors.NotFound);

                if (honeypot.ExternalService is null)
                    return Result.Failure(HoneypotErrors.ExternalServiceNotLinked);

                // Call external service
                var pauseResult = await _externalService.PauseAsync(
                    honeypot.ExternalService.ServiceId,
                    cancellationToken);

                if (pauseResult.IsFailure)
                    return pauseResult;

                // Update honeypot state
                var stateResult = honeypot.Pause(reason);
                if (stateResult.IsFailure)
                    return stateResult;

                await _honeypotRepository.UpdateAsync(honeypot, cancellationToken);

                // Audit pause action
                var auditResult = _auditService.CreateAuditLog(
                    honeypot.OrganizationId,
                    userId,
                    AuditResourceType.HoneyPot,
                    honeypotId,
                    AuditAction.Update,
                    $"Honeypot paused. Reason: {reason ?? "No reason provided"}");

                if (auditResult.IsSuccess)
                {
                    await _auditRepository.AddAsync(auditResult.Value);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(
                    Error.Custom("Honeypot.PauseException", $"Unexpected pause error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Resume honeypot (resume capturing after pause).
        /// </summary>
        public async Task<Result> ResumeHoneypotAsync(
            Guid honeypotId,
            Guid userId,
            string? reason,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var honeypot = await _honeypotRepository.GetByIdAsync(honeypotId, cancellationToken);
                if (honeypot is null)
                    return Result.Failure(HoneypotErrors.NotFound);

                if (honeypot.ExternalService is null)
                    return Result.Failure(HoneypotErrors.ExternalServiceNotLinked);

                // Call external service
                var resumeResult = await _externalService.ResumeAsync(
                    honeypot.ExternalService.ServiceId,
                    cancellationToken);

                if (resumeResult.IsFailure)
                    return resumeResult;

                // Update honeypot state
                var stateResult = honeypot.Resume(reason);
                if (stateResult.IsFailure)
                    return stateResult;

                await _honeypotRepository.UpdateAsync(honeypot, cancellationToken);

                // Audit resume action
                var auditResult = _auditService.CreateAuditLog(
                    honeypot.OrganizationId,
                    userId,
                    AuditResourceType.HoneyPot,
                    honeypotId,
                    AuditAction.Update,
                    $"Honeypot resumed. Reason: {reason ?? "No reason provided"}");

                if (auditResult.IsSuccess)
                {
                    await _auditRepository.AddAsync(auditResult.Value);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(
                    Error.Custom("Honeypot.ResumeException", $"Unexpected resume error: {ex.Message}"));
            }
        }
    }
}
