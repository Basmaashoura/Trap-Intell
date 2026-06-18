using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Plans;
using Sub = Trap_Intel.Domain.Subscriptions;

namespace Trap_Intel.Domain.Organizations
{
    /// <summary>
    /// Domain service that coordinates organization approval with automatic trial subscription creation.
    /// Handles the complex workflow of approving an organization and setting up a trial subscription.
    /// 
    /// This is a cross-aggregate operation:
    /// - Gets Organization aggregate from repository
    /// - Validates approval with business rules
    /// - Approves the organization
    /// - Gets trial plan from Plan repository
    /// - Creates trial Subscription aggregate
    /// - Saves both updated organization and new subscription
    /// </summary>
    public class OrganizationApprovalService
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly Sub.ISubscriptionRepository _subscriptionRepository;
        private readonly IPlanRepository _planRepository;

        public OrganizationApprovalService(
            IOrganizationRepository organizationRepository,
            Sub.ISubscriptionRepository subscriptionRepository,
            IPlanRepository planRepository)
        {
            _organizationRepository = organizationRepository ?? throw new ArgumentNullException(nameof(organizationRepository));
            _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
            _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
        }

        /// <summary>
        /// Approves a pending organization and creates an automatic trial subscription.
        /// 
        /// Workflow:
        /// 1. Gets organization from repository
        /// 2. Validates organization has required information
        /// 3. Checks if organization is in pending approval status
        /// 4. Approves the organization
        /// 5. Gets the trial plan
        /// 6. Creates trial subscription (if plan exists)
        /// 7. Saves both changes
        /// 
        /// Note: If trial plan doesn't exist, organization is still approved but without subscription.
        /// </summary>
        /// <param name="organizationId">The organization to approve</param>
        /// <param name="approvalNotes">Notes about the approval decision</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result indicating success or failure</returns>
        public async Task<Result> ApproveAsync(
            Guid organizationId,
            string? approvalNotes = null,
            CancellationToken cancellationToken = default)
        {
            // Validation
            if (organizationId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("OrganizationApproval.InvalidId", 
                        "Organization ID cannot be empty."));

            // Step 1: Get organization
            var org = await _organizationRepository.GetByIdAsync(
                organizationId, cancellationToken);

            if (org is null)
                return Result.Failure(OrganizationErrors.OrganizationNotFound);

            // Step 2: Validate organization has required information
            var approvalRule = new OrganizationApprovalRule(org);
            if (!approvalRule.IsSatisfied())
                return Result.Failure(approvalRule.Error);

            // Step 3: Approve organization
            org.Approve(approvalNotes);

            // Step 4: Get trial plan
            var trialPlans = await _planRepository.GetByTypeAsync(PlanType.Trial, cancellationToken);
            var trialPlan = trialPlans.FirstOrDefault();

            // Step 5: Create trial subscription if trial plan exists
            if (trialPlan is not null)
            {
                var subscriptionCreationService = new Sub.CreateSubscriptionService(_planRepository, _subscriptionRepository);

                _ = await subscriptionCreationService.CreateTrialAsync(
                    org.Id,
                    trialPlan.Id,
                    14,
                    cancellationToken);

                // If subscription creation fails, we still proceed with organization approval.
                // This preserves existing behavior while keeping subscription creation logic centralized.
            }

            // Step 6: Save changes
            // Save approved organization
            await _organizationRepository.UpdateAsync(org, cancellationToken);

            return Result.Success();
        }

        /// <summary>
        /// Approves organization without creating a trial subscription.
        /// Useful when you want to approve without auto-subscribing.
        /// </summary>
        public async Task<Result> ApproveWithoutTrialAsync(
            Guid organizationId,
            string? approvalNotes = null,
            CancellationToken cancellationToken = default)
        {
            if (organizationId == Guid.Empty)
                return Result.Failure(
                    Error.Custom("OrganizationApproval.InvalidId", 
                        "Organization ID cannot be empty."));

            var org = await _organizationRepository.GetByIdAsync(
                organizationId, cancellationToken);

            if (org is null)
                return Result.Failure(OrganizationErrors.OrganizationNotFound);

            // Validate
            var approvalRule = new OrganizationApprovalRule(org);
            if (!approvalRule.IsSatisfied())
                return Result.Failure(approvalRule.Error);

            // Approve
            org.Approve(approvalNotes);

            // Save
            await _organizationRepository.UpdateAsync(org, cancellationToken);

            return Result.Success();
        }
    }
}
