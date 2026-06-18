using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Trap_Intel.Infrastructure.Persistence.SeedData.Seeders;

/// <summary>
/// Seeds subscription plans data
/// </summary>
public sealed class PlanSeeder : BaseSeeder
{
    public PlanSeeder(ILogger<PlanSeeder> logger) : base(logger) { }

    public override int Order => 1;
    public override string EntityName => "Plans";

    public override async Task<bool> ShouldSeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        return !await context.Plans.AnyAsync(cancellationToken);
    }

    public override async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await ShouldSeedAsync(context, cancellationToken))
        {
            LogSkipped("Plans already exist");
            return;
        }

        LogSeeding("Seeding subscription plans...");

        var sql = @"
INSERT INTO trapintel.plans (
    id, name, description, type, support_level, support_response_time_minutes, 
    support_includes_dedicated_manager, compliance_level, compliance_certifications,
    compliance_auditing_included, customization_level, is_active, created_at, updated_at,
    pricing, features,
    ai_threat_analysis, ai_automated_detection, ai_predictive_analytics, ai_custom_models,
    threat_intel_included, threat_intel_data_sources, threat_intel_update_hours,
    quota_max_honeypots, quota_max_storage_gb, quota_max_api_calls, quota_max_users,
    quota_max_events_retained, quota_data_retention_days, quota_max_reports, quota_max_webhooks,
    quota_max_api_keys, quota_hard_limit_enforced, quota_overage_honeypot_rate,
    quota_overage_storage_rate, quota_overage_api_rate
) VALUES
-- Free Tier: Perfect for evaluation and small teams
('aaaa1111-1111-1111-1111-111111111111', 'Free Tier', 
 'Perfect for small teams getting started with honeypot technology. Includes basic monitoring and limited honeypots.', 
 'Free', 'Basic', 0, false, 'None', '[]', false, 'None', true, NOW(), NOW(), 
 '{{""Monthly"":{{""Amount"":0,""Currency"":""USD"",""SetupFee"":0}},""Annually"":{{""Amount"":0,""Currency"":""USD"",""SetupFee"":0}}}}', 
 '[{{""Code"":""honeypot_ssh"",""Name"":""SSH Honeypot"",""Description"":""Deploy SSH honeypots"",""Category"":""Honeypots"",""IsEnabled"":true,""LimitValue"":2,""LimitUnit"":""instances"",""IsPremium"":false,""SortOrder"":1}},{{""Code"":""alerts_email"",""Name"":""Email Alerts"",""Description"":""Receive email alerts"",""Category"":""Alerting"",""IsEnabled"":true,""LimitValue"":null,""LimitUnit"":null,""IsPremium"":false,""SortOrder"":2}},{{""Code"":""api_access"",""Name"":""API Access"",""Description"":""Programmatic access"",""Category"":""API"",""IsEnabled"":true,""LimitValue"":1000,""LimitUnit"":""calls/month"",""IsPremium"":false,""SortOrder"":3}}]', 
 false, false, false, false, false, null, 24, 
 2, 1.0, 1000, 3, 10000, 30, 5, 2, 2, true, 0, 0, 0),

-- Professional: For growing security teams
('aaaa2222-2222-2222-2222-222222222222', 'Professional', 
 'For growing security teams needing advanced threat detection and AI-powered analysis.', 
 'Paid', 'Priority', 480, false, 'GDPR', '[""GDPR""]', false, 'Basic', true, NOW(), NOW(), 
 '{{""Monthly"":{{""Amount"":499,""Currency"":""USD"",""SetupFee"":0}},""Annually"":{{""Amount"":4990,""Currency"":""USD"",""SetupFee"":0}}}}', 
 '[{{""Code"":""honeypot_database"",""Name"":""Database Honeypot"",""Description"":""Deploy database honeypots"",""Category"":""Honeypots"",""IsEnabled"":true,""LimitValue"":10,""LimitUnit"":""instances"",""IsPremium"":false,""SortOrder"":1}},{{""Code"":""ai_threat_analysis"",""Name"":""AI Threat Analysis"",""Description"":""ML-powered threat analysis"",""Category"":""AI"",""IsEnabled"":true,""LimitValue"":null,""LimitUnit"":null,""IsPremium"":true,""SortOrder"":2}},{{""Code"":""threat_feeds"",""Name"":""Threat Feeds"",""Description"":""External threat feed integrations"",""Category"":""ThreatIntelligence"",""IsEnabled"":true,""LimitValue"":2,""LimitUnit"":""sources"",""IsPremium"":true,""SortOrder"":3}},{{""Code"":""reports_basic"",""Name"":""Basic Reports"",""Description"":""Generate standard reports"",""Category"":""Reporting"",""IsEnabled"":true,""LimitValue"":50,""LimitUnit"":""per month"",""IsPremium"":false,""SortOrder"":4}}]', 
 true, true, false, false, true, '[""OSINT"", ""VirusTotal""]', 12, 
 10, 50.0, 50000, 10, 100000, 90, 50, 10, 10, false, 25.00, 0.10, 0.001),

-- Enterprise: Full-featured solution
('aaaa3333-3333-3333-3333-333333333333', 'Enterprise', 
 'Full-featured solution for enterprise security operations with advanced compliance and analytics.', 
 'Paid', 'Priority', 60, true, 'SOC2', '[""SOC2"", ""GDPR"", ""ISO27001""]', true, 'Advanced', true, NOW(), NOW(), 
 '{{""Monthly"":{{""Amount"":1999,""Currency"":""USD"",""SetupFee"":0}},""Annually"":{{""Amount"":19990,""Currency"":""USD"",""SetupFee"":2500}}}}', 
 '[{{""Code"":""honeypot_custom"",""Name"":""Custom Honeypot"",""Description"":""Deploy custom protocol honeypots"",""Category"":""Honeypots"",""IsEnabled"":true,""LimitValue"":50,""LimitUnit"":""instances"",""IsPremium"":true,""SortOrder"":1}},{{""Code"":""ai_predictive"",""Name"":""Predictive Analytics"",""Description"":""Predict attack trends"",""Category"":""AI"",""IsEnabled"":true,""LimitValue"":null,""LimitUnit"":null,""IsPremium"":true,""SortOrder"":2}},{{""Code"":""compliance_soc2"",""Name"":""SOC2 Compliance"",""Description"":""SOC2 compliance controls"",""Category"":""Compliance"",""IsEnabled"":true,""LimitValue"":null,""LimitUnit"":null,""IsPremium"":true,""SortOrder"":3}},{{""Code"":""support_sla"",""Name"":""SLA (1-hour)"",""Description"":""Priority SLA support"",""Category"":""Support"",""IsEnabled"":true,""LimitValue"":1,""LimitUnit"":""hour response"",""IsPremium"":true,""SortOrder"":4}}]', 
 true, true, true, false, true, '[""OSINT"", ""VirusTotal"", ""AlienVault""]', 6, 
 50, 500.0, 500000, 50, 1000000, 365, 200, 50, 50, false, 20.00, 0.08, 0.0005),

-- Ultimate: Maximum protection
('aaaa4444-4444-4444-4444-444444444444', 'Ultimate', 
 'Maximum protection with dedicated support, custom AI models, and unlimited resources.', 
 'Paid', 'Dedicated', 15, true, 'Custom', '[""SOC2"", ""GDPR"", ""ISO27001"", ""HIPAA""]', true, 'Enterprise', true, NOW(), NOW(), 
 '{{""Monthly"":{{""Amount"":9999,""Currency"":""USD"",""SetupFee"":0}},""Annually"":{{""Amount"":99990,""Currency"":""USD"",""SetupFee"":10000}}}}', 
 '[{{""Code"":""honeypot_custom"",""Name"":""Custom Honeypot"",""Description"":""Unlimited custom honeypots"",""Category"":""Honeypots"",""IsEnabled"":true,""LimitValue"":null,""LimitUnit"":null,""IsPremium"":true,""SortOrder"":1}},{{""Code"":""ai_predictive"",""Name"":""Predictive Analytics"",""Description"":""Advanced predictive analytics"",""Category"":""AI"",""IsEnabled"":true,""LimitValue"":null,""LimitUnit"":null,""IsPremium"":true,""SortOrder"":2}},{{""Code"":""threat_feeds"",""Name"":""Threat Feeds"",""Description"":""Premium intelligence feeds"",""Category"":""ThreatIntelligence"",""IsEnabled"":true,""LimitValue"":5,""LimitUnit"":""sources"",""IsPremium"":true,""SortOrder"":3}},{{""Code"":""api_access"",""Name"":""API Access"",""Description"":""Enterprise API access"",""Category"":""API"",""IsEnabled"":true,""LimitValue"":null,""LimitUnit"":null,""IsPremium"":true,""SortOrder"":4}},{{""Code"":""compliance_hipaa"",""Name"":""HIPAA Compliance"",""Description"":""HIPAA compliance controls"",""Category"":""Compliance"",""IsEnabled"":true,""LimitValue"":null,""LimitUnit"":null,""IsPremium"":true,""SortOrder"":5}},{{""Code"":""support_csm"",""Name"":""Dedicated CSM"",""Description"":""Dedicated customer success manager"",""Category"":""Support"",""IsEnabled"":true,""LimitValue"":null,""LimitUnit"":null,""IsPremium"":true,""SortOrder"":6}}]', 
 true, true, true, true, true, '[""OSINT"", ""VirusTotal"", ""AlienVault"", ""MISP"", ""Custom Feeds""]', 1, 
 null, null, null, null, null, 730, null, null, null, false, 15.00, 0.05, 0.0001)
";

        await ExecuteSqlAsync(context, sql, cancellationToken);
        LogSeeded(4);
    }
}
