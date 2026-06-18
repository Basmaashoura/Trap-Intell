namespace Trap_Intel.Infrastructure.Persistence.SeedData;

/// <summary>
/// Contains all predefined GUIDs for seed data to ensure consistency and relationships
/// </summary>
public static class SeedDataIds
{
    // ==========================================
    // Organizations
    // ==========================================
    public static class Organizations
    {
        public static readonly Guid CyberShieldCorp = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid TechDefenders = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid SecureBank = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid HealthGuard = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid GovSecure = Guid.Parse("55555555-5555-5555-5555-555555555555");
    }

    // ==========================================
    // Plans
    // ==========================================
    public static class Plans
    {
        public static readonly Guid FreeTier = Guid.Parse("aaaa1111-1111-1111-1111-111111111111");
        public static readonly Guid Professional = Guid.Parse("aaaa2222-2222-2222-2222-222222222222");
        public static readonly Guid Enterprise = Guid.Parse("aaaa3333-3333-3333-3333-333333333333");
        public static readonly Guid Ultimate = Guid.Parse("aaaa4444-4444-4444-4444-444444444444");
    }

    // ==========================================
    // Users
    // ==========================================
    public static class Users
    {
        // CyberShield Corp Users
        public static readonly Guid AhmedAdmin = Guid.Parse("bbbb1111-1111-1111-1111-111111111111");
        public static readonly Guid SaraAnalyst = Guid.Parse("bbbb1111-1111-1111-1111-222222222222");
        public static readonly Guid MohamedSOC = Guid.Parse("bbbb1111-1111-1111-1111-333333333333");

        // TechDefenders Users
        public static readonly Guid JohnAdmin = Guid.Parse("bbbb2222-2222-2222-2222-111111111111");
        public static readonly Guid EmilyAnalyst = Guid.Parse("bbbb2222-2222-2222-2222-222222222222");

        // SecureBank Users
        public static readonly Guid DavidCISO = Guid.Parse("bbbb3333-3333-3333-3333-111111111111");
        public static readonly Guid LisaSecOps = Guid.Parse("bbbb3333-3333-3333-3333-222222222222");
        public static readonly Guid MarkIncident = Guid.Parse("bbbb3333-3333-3333-3333-333333333333");

        // HealthGuard Users
        public static readonly Guid DrKhaledAdmin = Guid.Parse("bbbb4444-4444-4444-4444-111111111111");

        // GovSecure Users
        public static readonly Guid ColonelAli = Guid.Parse("bbbb5555-5555-5555-5555-111111111111");
        public static readonly Guid MajorFatima = Guid.Parse("bbbb5555-5555-5555-5555-222222222222");
    }

    // ==========================================
    // Subscriptions
    // ==========================================
    public static class Subscriptions
    {
        public static readonly Guid CyberShieldSub = Guid.Parse("cccc1111-1111-1111-1111-111111111111");
        public static readonly Guid TechDefendersSub = Guid.Parse("cccc2222-2222-2222-2222-222222222222");
        public static readonly Guid SecureBankSub = Guid.Parse("cccc3333-3333-3333-3333-333333333333");
        public static readonly Guid HealthGuardSub = Guid.Parse("cccc4444-4444-4444-4444-444444444444");
        public static readonly Guid GovSecureSub = Guid.Parse("cccc5555-5555-5555-5555-555555555555");
    }

    // ==========================================
    // Honeypots
    // ==========================================
    public static class Honeypots
    {
        // CyberShield Honeypots
        public static readonly Guid SSHHoneypot1 = Guid.Parse("dddd1111-1111-1111-1111-111111111111");
        public static readonly Guid HTTPHoneypot1 = Guid.Parse("dddd1111-1111-1111-1111-222222222222");
        public static readonly Guid SMBHoneypot1 = Guid.Parse("dddd1111-1111-1111-1111-333333333333");

        // TechDefenders Honeypots
        public static readonly Guid RDPHoneypot1 = Guid.Parse("dddd2222-2222-2222-2222-111111111111");
        public static readonly Guid FTPHoneypot1 = Guid.Parse("dddd2222-2222-2222-2222-222222222222");

        // SecureBank Honeypots
        public static readonly Guid DatabaseHoneypot1 = Guid.Parse("dddd3333-3333-3333-3333-111111111111");
        public static readonly Guid APIHoneypot1 = Guid.Parse("dddd3333-3333-3333-3333-222222222222");
        public static readonly Guid SSHHoneypot2 = Guid.Parse("dddd3333-3333-3333-3333-333333333333");
        public static readonly Guid WebHoneypot1 = Guid.Parse("dddd3333-3333-3333-3333-444444444444");

        // HealthGuard Honeypots
        public static readonly Guid HL7Honeypot1 = Guid.Parse("dddd4444-4444-4444-4444-111111111111");

        // GovSecure Honeypots
        public static readonly Guid SCIFHoneypot1 = Guid.Parse("dddd5555-5555-5555-5555-111111111111");
        public static readonly Guid ClassifiedHoneypot1 = Guid.Parse("dddd5555-5555-5555-5555-222222222222");
        public static readonly Guid DNSHoneypot1 = Guid.Parse("dddd5555-5555-5555-5555-333333333333");
    }

    // ==========================================
    // Threat Actors
    // ==========================================
    public static class ThreatActors
    {
        public static readonly Guid APT28_FancyBear = Guid.Parse("eeee1111-1111-1111-1111-111111111111");
        public static readonly Guid APT29_CozyBear = Guid.Parse("eeee2222-2222-2222-2222-222222222222");
        public static readonly Guid Lazarus_Group = Guid.Parse("eeee3333-3333-3333-3333-333333333333");
        public static readonly Guid DarkSide_Ransomware = Guid.Parse("eeee4444-4444-4444-4444-444444444444");
        public static readonly Guid ScriptKiddie_Unknown = Guid.Parse("eeee5555-5555-5555-5555-555555555555");
        public static readonly Guid APT41_WinntiGroup = Guid.Parse("eeee6666-6666-6666-6666-666666666666");
    }

    // ==========================================
    // Attack Events (Sample IDs - more will be generated)
    // ==========================================
    public static class AttackEvents
    {
        public static readonly Guid BruteForceSSH1 = Guid.Parse("ffff1111-1111-1111-1111-111111111111");
        public static readonly Guid SQLInjection1 = Guid.Parse("ffff2222-2222-2222-2222-222222222222");
        public static readonly Guid RDPExploit1 = Guid.Parse("ffff3333-3333-3333-3333-333333333333");
        public static readonly Guid MalwareUpload1 = Guid.Parse("ffff4444-4444-4444-4444-444444444444");
        public static readonly Guid PortScan1 = Guid.Parse("ffff5555-5555-5555-5555-555555555555");
        public static readonly Guid CredentialStuffing1 = Guid.Parse("ffff6666-6666-6666-6666-666666666666");
    }

    // ==========================================
    // Alerts
    // ==========================================
    public static class Alerts
    {
        public static readonly Guid CriticalBruteForce = Guid.Parse("aaaa1111-aaaa-1111-aaaa-111111111111");
        public static readonly Guid HighSQLInjection = Guid.Parse("aaaa2222-aaaa-2222-aaaa-222222222222");
        public static readonly Guid MediumPortScan = Guid.Parse("aaaa3333-aaaa-3333-aaaa-333333333333");
        public static readonly Guid CriticalRansomware = Guid.Parse("aaaa4444-aaaa-4444-aaaa-444444444444");
        public static readonly Guid HighAPTActivity = Guid.Parse("aaaa5555-aaaa-5555-aaaa-555555555555");
    }

    // ==========================================
    // API Keys
    // ==========================================
    public static class ApiKeys
    {
        public static readonly Guid CyberShieldProdKey = Guid.Parse("1111aaaa-1111-aaaa-1111-aaaaaaaaaaaa");
        public static readonly Guid SecureBankIntegration = Guid.Parse("2222aaaa-2222-aaaa-2222-aaaaaaaaaaaa");
        public static readonly Guid GovSecureClassified = Guid.Parse("3333aaaa-3333-aaaa-3333-aaaaaaaaaaaa");
    }

    // ==========================================
    // Webhooks
    // ==========================================
    public static class Webhooks
    {
        public static readonly Guid SlackAlerts = Guid.Parse("1111bbbb-1111-bbbb-1111-bbbbbbbbbbbb");
        public static readonly Guid PagerDutyIncidents = Guid.Parse("2222bbbb-2222-bbbb-2222-bbbbbbbbbbbb");
        public static readonly Guid SIEMIntegration = Guid.Parse("3333bbbb-3333-bbbb-3333-bbbbbbbbbbbb");
    }

    // ==========================================
    // Reports & Templates
    // ==========================================
    public static class Reports
    {
        public static readonly Guid WeeklyThreatReport = Guid.Parse("1111cccc-1111-cccc-1111-cccccccccccc");
        public static readonly Guid MonthlyExecutiveSummary = Guid.Parse("2222cccc-2222-cccc-2222-cccccccccccc");
        public static readonly Guid IncidentAnalysis = Guid.Parse("3333cccc-3333-cccc-3333-cccccccccccc");
    }

    public static class ReportTemplates
    {
        public static readonly Guid ThreatAnalysisTemplate = Guid.Parse("1111dddd-1111-dddd-1111-dddddddddddd");
        public static readonly Guid ExecutiveBriefTemplate = Guid.Parse("2222dddd-2222-dddd-2222-dddddddddddd");
        public static readonly Guid ComplianceReportTemplate = Guid.Parse("3333dddd-3333-dddd-3333-dddddddddddd");
    }

    // ==========================================
    // Dashboards
    // ==========================================
    public static class Dashboards
    {
        public static readonly Guid SOCOverview = Guid.Parse("1111eeee-1111-eeee-1111-eeeeeeeeeeee");
        public static readonly Guid ThreatIntelDashboard = Guid.Parse("2222eeee-2222-eeee-2222-eeeeeeeeeeee");
        public static readonly Guid ExecutiveDashboard = Guid.Parse("3333eeee-3333-eeee-3333-eeeeeeeeeeee");
        public static readonly Guid HoneypotMonitor = Guid.Parse("4444eeee-4444-eeee-4444-eeeeeeeeeeee");
        public static readonly Guid ComplianceDashboard = Guid.Parse("5555eeee-5555-eeee-5555-eeeeeeeeeeee");
    }

    // ==========================================
    // Subscription Quotas
    // ==========================================
    public static class SubscriptionQuotas
    {
        public static readonly Guid CyberShieldQuota = Guid.Parse("1111ffff-1111-ffff-1111-ffffffffffff");
        public static readonly Guid TechDefendersQuota = Guid.Parse("2222ffff-2222-ffff-2222-ffffffffffff");
        public static readonly Guid SecureBankQuota = Guid.Parse("3333ffff-3333-ffff-3333-ffffffffffff");
        public static readonly Guid HealthGuardQuota = Guid.Parse("4444ffff-4444-ffff-4444-ffffffffffff");
        public static readonly Guid GovSecureQuota = Guid.Parse("5555ffff-5555-ffff-5555-ffffffffffff");
    }

    // ==========================================
    // Payment Methods
    // ==========================================
    public static class PaymentMethods
    {
        public static readonly Guid CyberShieldCard = Guid.Parse("1111aaab-1111-aaab-1111-aaabaaabaaab");
        public static readonly Guid TechDefendersCard = Guid.Parse("2222aaab-2222-aaab-2222-aaabaaabaaab");
        public static readonly Guid SecureBankWire = Guid.Parse("3333aaab-3333-aaab-3333-aaabaaabaaab");
        public static readonly Guid HealthGuardCard = Guid.Parse("4444aaab-4444-aaab-4444-aaabaaabaaab");
        public static readonly Guid GovSecurePO = Guid.Parse("5555aaab-5555-aaab-5555-aaabaaabaaab");
    }

    // ==========================================
    // Invoices
    // ==========================================
    public static class Invoices
    {
        public static readonly Guid CyberShieldInvoice1 = Guid.Parse("1111bbbc-1111-bbbc-1111-bbbcbbbcbbbc");
        public static readonly Guid CyberShieldInvoice2 = Guid.Parse("1111bbbc-1111-bbbc-1111-bbbcbbbcbbb2");
        public static readonly Guid TechDefendersInvoice1 = Guid.Parse("2222bbbc-2222-bbbc-2222-bbbcbbbcbbbc");
        public static readonly Guid SecureBankInvoice1 = Guid.Parse("3333bbbc-3333-bbbc-3333-bbbcbbbcbbbc");
        public static readonly Guid HealthGuardInvoice1 = Guid.Parse("4444bbbc-4444-bbbc-4444-bbbcbbbcbbbc");
        public static readonly Guid GovSecureInvoice1 = Guid.Parse("5555bbbc-5555-bbbc-5555-bbbcbbbcbbbc");
    }

    // ==========================================
    // Agent Commands
    // ==========================================
    public static class AgentCommands
    {
        public static readonly Guid RestartHoneypot1 = Guid.Parse("1111cccd-1111-cccd-1111-cccdcccdcccd");
        public static readonly Guid UpdateConfig1 = Guid.Parse("2222cccd-2222-cccd-2222-cccdcccdcccd");
        public static readonly Guid CollectLogs1 = Guid.Parse("3333cccd-3333-cccd-3333-cccdcccdcccd");
        public static readonly Guid DeployDecoy1 = Guid.Parse("4444cccd-4444-cccd-4444-cccdcccdcccd");
        public static readonly Guid HealthCheck1 = Guid.Parse("5555cccd-5555-cccd-5555-cccdcccdcccd");
    }

    // ==========================================
    // AI Recommendations
    // ==========================================
    public static class AIRecommendations
    {
        public static readonly Guid DeployMoreHoneypots = Guid.Parse("1111ddde-1111-ddde-1111-dddedddeddde");
        public static readonly Guid UpdateFirewallRules = Guid.Parse("2222ddde-2222-ddde-2222-dddedddeddde");
        public static readonly Guid InvestigateAPT = Guid.Parse("3333ddde-3333-ddde-3333-dddedddeddde");
        public static readonly Guid EnableMFA = Guid.Parse("4444ddde-4444-ddde-4444-dddedddeddde");
        public static readonly Guid PatchVulnerability = Guid.Parse("5555ddde-5555-ddde-5555-dddedddeddde");
    }

    // ==========================================
    // Audit Trails
    // ==========================================
    public static class AuditTrails
    {
        public static readonly Guid UserLogin1 = Guid.Parse("1111eeef-1111-eeef-1111-eeefeeefeeef");
        public static readonly Guid HoneypotCreated1 = Guid.Parse("2222eeef-2222-eeef-2222-eeefeeefeeef");
        public static readonly Guid AlertAcknowledged1 = Guid.Parse("3333eeef-3333-eeef-3333-eeefeeefeeef");
        public static readonly Guid ConfigChanged1 = Guid.Parse("4444eeef-4444-eeef-4444-eeefeeefeeef");
        public static readonly Guid ReportGenerated1 = Guid.Parse("5555eeef-5555-eeef-5555-eeefeeefeeef");
    }
}
