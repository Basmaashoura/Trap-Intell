# Final Domain Review - Missing Components Analysis

## Executive Summary

After comprehensive review of the Trap-Intel domain layer, I've identified several missing components that would complete the domain model. This review considers the multi-tenancy architecture, Go honeypot integration, and enterprise requirements.

---

## Current Domain Structure ?

### Aggregates (16 Total)
| Aggregate | Status | Child Entities |
|-----------|--------|----------------|
| Organization | ? Complete | OrganizationAddress, OrganizationMetadata |
| User | ? Complete | - |
| Subscription | ? Complete | SubscriptionQuotaEntity, UsageSnapshot, MonthlyUsageSummary |
| Plan | ? Complete | - |
| Honeypot | ? Complete | - |
| AttackEvent | ? Complete | - |
| ThreatActor | ? Complete | ThreatActorIPEntity, ThreatActorTTPEntity, BehaviorPatternEntity, ThreatIntelNoteEntity |
| Alert | ? Complete | AlertActionEntity, AlertCommentEntity, AlertNotificationEntity, AlertEscalationEntity |
| AgentCommand | ? Complete | - |
| Invoice | ? Complete | - |
| PaymentMethod | ? Complete | - |
| AuditTrail | ? Complete | - |
| AIRecommendation | ? Complete | - |
| Report | ? Complete | - |
| ReportTemplate | ? Complete | TemplateSection |
| ReportExport | ? Complete | - |

---

## Missing Components Identified

### ?? CRITICAL - Missing Aggregates/Entities

#### 1. **Incident** (New Aggregate)
**Purpose:** Group related alerts into security incidents for coordinated response.

```
Incident
??? IncidentId
??? OrganizationId
??? Title
??? Description
??? Status (Open, Investigating, Contained, Resolved, Closed)
??? Severity
??? Priority
??? Classification (Malware, DataBreach, Intrusion, DDoS, etc.)
??? LeadInvestigatorUserId
??? OpenedAt
??? ClosedAt
??? LinkedAlertIds (List<Guid>)
??? LinkedThreatActorIds (List<Guid>)
??? AffectedHoneypotIds (List<Guid>)
??? Timeline (List<IncidentTimelineEntry>) - Child Entity
??? Findings (List<IncidentFinding>) - Child Entity
??? Artifacts (List<IncidentArtifact>) - Child Entity
??? Containment actions, response plan
```

**Why Needed:** Multiple alerts often relate to single coordinated attack. Need incident management for SOC workflow.

---

#### 2. **IOC (Indicator of Compromise)** (New Aggregate)
**Purpose:** Track and share threat indicators across the platform.

```
IOC
??? IOCId
??? OrganizationId
??? Type (IP, Domain, FileHash, URL, Email, Registry, etc.)
??? Value
??? Confidence (0-100)
??? ThreatType
??? Source (Internal, ExternalFeed, PartnerShared)
??? FirstSeen
??? LastSeen
??? ExpiresAt
??? IsActive
??? RelatedThreatActorIds
??? RelatedAttackEventIds
??? Tags
??? Metadata (JSON)
??? Enrichment data
```

**Why Needed:** Essential for threat intelligence. Share indicators across honeypots and with external feeds.

---

#### 3. **NotificationRule** (New Aggregate)
**Purpose:** Configurable notification rules per organization.

```
NotificationRule
??? RuleId
??? OrganizationId
??? Name
??? IsEnabled
??? TriggerConditions (List<RuleCondition>)
??? Channels (Email, Slack, SMS, Webhook)
??? Recipients (List<RecipientConfig>)
??? Schedule (immediate, batched, scheduled)
??? RateLimitConfig
??? CreatedByUserId
??? CreatedAt
??? UpdatedAt
```

**Why Needed:** Organizations need custom notification rules beyond default alert notifications.

---

#### 4. **Dashboard** / **DashboardWidget** (New Aggregate)
**Purpose:** Customizable dashboards per user/organization.

```
Dashboard
??? DashboardId
??? OrganizationId
??? UserId (owner)
??? Name
??? IsDefault
??? IsShared
??? Layout (JSON)
??? Widgets (List<DashboardWidget>) - Child Entity
?   ??? WidgetId
?   ??? WidgetType (Chart, Table, Counter, Map, etc.)
?   ??? Title
?   ??? DataSource
?   ??? Position (x, y, width, height)
?   ??? Configuration (JSON)
?   ??? RefreshInterval
??? CreatedAt/UpdatedAt
```

**Why Needed:** AIRecommendation references DashboardViewId. Users need personalized dashboards.

---

#### 5. **IntegrationConfig** (New Aggregate)
**Purpose:** External integrations (SIEM, SOAR, ticketing systems).

```
IntegrationConfig
??? IntegrationId
??? OrganizationId
??? IntegrationType (Splunk, Sentinel, ServiceNow, Jira, etc.)
??? Name
??? IsEnabled
??? ConnectionConfig (encrypted)
??? AuthenticationMethod
??? SyncSettings
??? LastSyncAt
??? Status (Connected, Error, Disabled)
??? ErrorMessage
??? WebhookUrl (for incoming)
```

**Why Needed:** Enterprise customers need SIEM/SOAR integrations. Current domain doesn't support this.

---

### ?? IMPORTANT - Missing Child Entities

#### 6. **OrganizationMember** (Child of Organization)
**Purpose:** Track organization membership and roles explicitly.

```
OrganizationMember
??? MemberId
??? OrganizationId
??? UserId
??? Role (Owner, Admin, Analyst, Viewer)
??? JoinedAt
??? InvitedByUserId
??? Status (Active, Pending, Suspended)
??? Permissions (JSON or separate entity)
```

**Why Needed:** User belongs to Organization but membership details (invitation, role assignment) not tracked.

---

#### 7. **ApiKey** (Child of Organization or User)
**Purpose:** API access management for organizations.

```
ApiKey
??? ApiKeyId
??? OrganizationId
??? UserId (who created)
??? Name
??? KeyHash (never store plain)
??? Prefix (for identification)
??? Permissions (scopes)
??? RateLimit
??? ExpiresAt
??? LastUsedAt
??? IsActive
??? CreatedAt
??? RevokedAt
```

**Why Needed:** OrganizationSettings has `EnableApiAccess` but no API key management.

---

#### 8. **HoneypotDeployment** (Child of Honeypot)
**Purpose:** Track deployment history and configuration changes.

```
HoneypotDeployment
??? DeploymentId
??? HoneypotId
??? DeploymentType (Initial, Update, Restart, Migration)
??? RequestedByUserId
??? RequestedAt
??? StartedAt
??? CompletedAt
??? Status (Pending, InProgress, Completed, Failed)
??? Configuration (snapshot)
??? ErrorMessage
??? ExternalJobId (from Go service)
```

**Why Needed:** Track deployment history for compliance and debugging.

---

#### 9. **AttackSession** (Child of AttackEvent or separate)
**Purpose:** Group related attack events into sessions.

```
AttackSession
??? SessionId
??? HoneypotId
??? OrganizationId
??? SourceIP
??? StartedAt
??? EndedAt
??? EventCount
??? TotalDuration
??? Severity (highest)
??? AttackTypes (distinct)
??? LinkedEventIds
??? ThreatActorId
```

**Why Needed:** AttackEvent has SessionId but no proper entity to manage sessions.

---

### ?? IMPORTANT - Missing Value Objects

#### 10. **IPAddress** (Value Object)
```csharp
public record IPAddress
{
    public string Value { get; }
    public IPVersion Version { get; } // IPv4, IPv6
    public bool IsPrivate { get; }
    public bool IsReserved { get; }
    // Validation, parsing, comparison
}
```

---

#### 11. **GeoLocation** (Proper Value Object)
Current implementation may be incomplete. Needs:
```csharp
public record GeoLocation
{
    public double? Latitude { get; }
    public double? Longitude { get; }
    public string? Country { get; }
    public string? CountryCode { get; }
    public string? Region { get; }
    public string? City { get; }
    public string? PostalCode { get; }
    public string? ISP { get; }
    public string? ASN { get; }
    public string? Timezone { get; }
}
```

---

#### 12. **TimeRange** (Value Object)
```csharp
public record TimeRange
{
    public DateTime Start { get; }
    public DateTime End { get; }
    public TimeSpan Duration => End - Start;
    public bool Contains(DateTime dt) => dt >= Start && dt <= End;
    public bool Overlaps(TimeRange other);
}
```

---

#### 13. **SLAConfig** (Value Object for Alert escalation)
```csharp
public record SLAConfig
{
    public string Name { get; }
    public TimeSpan AcknowledgeWithin { get; }
    public TimeSpan ResolveWithin { get; }
    public AlertSeverity AppliesTo { get; }
    public EscalationLevel EscalateTo { get; }
}
```

---

### ?? NICE TO HAVE - Additional Components

#### 14. **Tag / Label System**
Generic tagging for all entities (honeypots, alerts, threat actors).

```
Tag
??? TagId
??? OrganizationId
??? Name
??? Color
??? Category
??? UsageCount
```

#### 15. **SavedSearch / Filter**
User-saved searches and filters.

```
SavedSearch
??? SearchId
??? OrganizationId
??? UserId
??? Name
??? EntityType (Alert, AttackEvent, ThreatActor, etc.)
??? FilterCriteria (JSON)
??? IsShared
??? CreatedAt
```

#### 16. **ExportJob**
Track long-running export operations.

```
ExportJob
??? JobId
??? OrganizationId
??? UserId
??? EntityType
??? Format (CSV, JSON, PDF)
??? Filters
??? Status (Pending, Processing, Completed, Failed)
??? FileUrl
??? ExpiresAt
??? CreatedAt
??? CompletedAt
```

---

## Missing Domain Events

### For Incident:
- `IncidentOpenedEvent`
- `IncidentClassifiedEvent`
- `IncidentEscalatedEvent`
- `IncidentContainedEvent`
- `IncidentResolvedEvent`
- `AlertLinkedToIncidentEvent`

### For IOC:
- `IOCCreatedEvent`
- `IOCEnrichedEvent`
- `IOCExpiredEvent`
- `IOCMatchedEvent` (when IOC matches new attack)

### For Integration:
- `IntegrationConnectedEvent`
- `IntegrationDisconnectedEvent`
- `IntegrationSyncCompletedEvent`
- `IntegrationSyncFailedEvent`

---

## Missing Domain Services

1. **IOCEnrichmentService** - Enrich IOCs with external data
2. **IncidentCorrelationService** - Auto-correlate alerts into incidents
3. **ThreatIntelSyncService** - Sync IOCs with external feeds
4. **SLAMonitoringService** - Monitor SLA breaches
5. **AttackSessionCorrelationService** - Group events into sessions

---

## Missing Repository Interfaces

```csharp
public interface IIncidentRepository : IRepository<Incident, Guid>
{
    Task<List<Incident>> GetOpenIncidentsByOrganizationAsync(Guid organizationId);
    Task<List<Incident>> GetIncidentsByThreatActorAsync(Guid threatActorId);
}

public interface IIOCRepository : IRepository<IOC, Guid>
{
    Task<IOC?> GetByValueAsync(string value, IOCType type);
    Task<List<IOC>> GetActiveIOCsAsync(Guid organizationId);
    Task<List<IOC>> SearchIOCsAsync(IOCSearchCriteria criteria);
}

public interface INotificationRuleRepository : IRepository<NotificationRule, Guid>
{
    Task<List<NotificationRule>> GetActiveRulesAsync(Guid organizationId);
    Task<List<NotificationRule>> GetRulesForTriggerAsync(string triggerType);
}

public interface IDashboardRepository : IRepository<Dashboard, Guid>
{
    Task<Dashboard?> GetDefaultDashboardAsync(Guid userId);
    Task<List<Dashboard>> GetSharedDashboardsAsync(Guid organizationId);
}
```

---

## Priority Recommendations

### Phase 3A - Critical (Implement First)
1. ? **Incident** aggregate - Core SOC workflow
2. ? **IOC** aggregate - Threat intelligence foundation
3. ? **AttackSession** entity - Better event correlation

### Phase 3B - Important
4. **OrganizationMember** entity - Proper membership tracking
5. **ApiKey** entity - API access management
6. **NotificationRule** aggregate - Custom notifications
7. **HoneypotDeployment** entity - Deployment tracking

### Phase 3C - Enhancement
8. **Dashboard/Widget** aggregate - User dashboards
9. **IntegrationConfig** aggregate - SIEM/SOAR integrations
10. **Tag system** - Generic tagging
11. **SavedSearch** - User saved filters

---

## Validation Checklist

### Current Gaps:
- [ ] No incident management (alerts are standalone)
- [ ] No IOC management (can't share indicators)
- [ ] No attack session correlation
- [ ] No custom notification rules
- [ ] No API key management
- [ ] No deployment history tracking
- [ ] No dashboard customization
- [ ] No external integrations support
- [ ] AttackEvent.SessionId exists but no Session entity
- [ ] AIRecommendation.DashboardViewId exists but no Dashboard entity

### What's Working Well:
- ? Multi-tenancy (OrganizationId on all entities)
- ? Rich threat actor profiling
- ? Comprehensive alert management
- ? Subscription/billing model
- ? Audit trail
- ? AI recommendations
- ? Reporting system
- ? Command/control for honeypots

---

## Conclusion

The domain is **80% complete** for a production honeypot platform. The missing 20% includes:

1. **Incident Management** - Critical for SOC operations
2. **IOC Management** - Essential for threat intelligence
3. **Session Correlation** - Better attack analysis
4. **Custom Notifications** - Enterprise requirement
5. **API Management** - Required for integrations
6. **Dashboard Customization** - User experience

Recommend implementing Phase 3A (Incident, IOC, Session) as the next priority.
