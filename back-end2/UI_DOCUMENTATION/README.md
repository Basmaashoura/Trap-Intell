# Trap-Intel UI Documentation

> **Frontend Development Guide for Trap-Intel Platform**  
> **Target Framework**: .NET 9 Backend with Modern Frontend Framework  
> **Last Updated**: December 2025

## ?? Table of Contents

- [Overview](#overview)
- [Documentation Structure](#documentation-structure)
- [Frontend API Reference](#frontend-api-reference)
- [Phase Specifications](#phase-specifications)
- [Domain Alignment](#domain-alignment)
- [Implementation Guide](#implementation-guide)
- [Quick Start](#quick-start)

---

## ?? Overview

This documentation provides comprehensive UI specifications for the Trap-Intel honeypot management platform. All specifications are fully aligned with the domain-driven design (DDD) architecture and include proper domain entity mappings, API endpoints, and business rule integration.

### Key Features

- **9 Complete UI Phases** covering all platform functionality
- **Domain-Aligned Architecture** with proper aggregate boundaries
- **Comprehensive API Specifications** with backend references
- **Real-time Integration** using domain events
- **Business Rule Compliance** with domain policies
- **Enterprise-Grade Design** patterns and practices

---

## ?? Documentation Structure

```
UI_DOCUMENTATION/
??? README.md                    # This file - main documentation index
??? PHASES/                     # Complete UI phase specifications
?   ??? PHASE_1_AUTHENTICATION_ONBOARDING.md
?   ??? PHASE_2_DASHBOARD.md
?   ??? PHASE_3_HONEYPOT_MANAGEMENT.md
?   ??? PHASE_4_ATTACK_EVENTS.md
?   ??? PHASE_5_ALERT_MANAGEMENT.md
?   ??? PHASE_6_THREAT_ACTORS.md
?   ??? PHASE_7_REPORTING_ANALYTICS.md
?   ??? PHASE_8_SETTINGS_CONFIGURATION.md
?   ??? PHASE_9_BILLING_SUBSCRIPTION.md
??? COMPONENTS/                 # Reusable component specifications
??? ASSETS/                     # UI assets and resources
??? GUIDELINES/                 # Design and development guidelines
?   ??? FRONTEND_API_ENDPOINT_REFERENCE.md
```

---

## ?? Frontend API Reference

- Detailed endpoint contract for frontend integration:
  - `UI_DOCUMENTATION/GUIDELINES/FRONTEND_API_ENDPOINT_REFERENCE.md`

This reference includes:
- grouped endpoints by module,
- auth and status-code expectations,
- pagination/filtering conventions,
- standards and implementation guidance.

---

## ?? Phase Specifications

### Phase Overview

| Phase | Feature Area | Domain Aggregates | Priority | Status |
|-------|--------------|-------------------|----------|--------|
| **Phase 1** | Authentication & Onboarding | User, Organization, OrganizationInvitation | Critical | ? Complete |
| **Phase 2** | Dashboard | DashboardView | Critical | ? Complete |
| **Phase 3** | Honeypot Management | Honeypot, AgentCommand | High | ? Complete |
| **Phase 4** | Attack Events | AttackEvent | High | ? Complete |
| **Phase 5** | Alert Management | Alert | High | ? Complete |
| **Phase 6** | Threat Actors | ThreatActor | Medium | ? Complete |
| **Phase 7** | Reporting & Analytics | Report, ReportTemplate, ReportExport | Medium | ? Complete |
| **Phase 8** | Settings & Configuration | User, Organization, ApiKey, Webhook | Medium | ? Complete |
| **Phase 9** | Billing & Subscription | Subscription, Invoice, PaymentMethod, Plan | Medium | ? Complete |

### Phase Details

#### ?? Phase 1: Authentication & Onboarding
- **Login/Registration** with organization approval workflow
- **Email verification** and password reset
- **2FA setup** and security features
- **Team invitations** and role management
- **Trial activation** and organization setup

#### ?? Phase 2: Dashboard
- **Customizable dashboards** with drag-and-drop widgets
- **Real-time KPI cards** and metrics
- **Interactive charts** and visualizations
- **Dashboard sharing** and collaboration
- **Event-driven updates** via WebSocket

#### ?? Phase 3: Honeypot Management
- **Honeypot deployment** wizard and configuration
- **Health monitoring** and status tracking
- **Agent command** execution and history
- **Configuration management** with validation
- **Multi-location deployment** support

#### ?? Phase 4: Attack Events
- **Real-time attack feed** with filtering
- **MITRE ATT&CK mapping** and threat analysis
- **Event correlation** and pattern detection
- **Forensic analysis** tools
- **Export and reporting** capabilities

#### ?? Phase 5: Alert Management
- **Intelligent alerting** with customizable rules
- **Escalation workflows** and assignments
- **Multi-channel notifications** (email, Slack, webhooks)
- **Alert correlation** and deduplication
- **Response tracking** and metrics

#### ?? Phase 6: Threat Actors
- **Threat actor profiling** and tracking
- **Behavioral analysis** and scoring
- **IOC management** and correlation
- **Threat intelligence** integration
- **Watchlist management** and alerts

#### ?? Phase 7: Reporting & Analytics
- **Pre-built report templates** and custom reports
- **Scheduled reporting** and distribution
- **Interactive analytics** with drill-down
- **Export capabilities** (PDF, CSV, JSON)
- **Executive summaries** and KPI tracking

#### ?? Phase 8: Settings & Configuration
- **User profile management** and preferences
- **Organization settings** and security policies
- **API key management** with scoped access
- **Webhook configuration** and testing
- **Integration management** (SIEM, Slack, etc.)

#### ?? Phase 9: Billing & Subscription
- **Subscription management** with plan comparison
- **Usage tracking** and quota monitoring
- **Payment method** management
- **Invoice history** and billing details
- **Upgrade/downgrade** workflows

---

## ?? Domain Alignment

### Domain-Driven Design Integration

All UI specifications are perfectly aligned with the backend domain model:

#### ? **Aggregate Boundaries Respected**
- UI screens map directly to domain aggregates
- No cross-aggregate data mutations in single transactions
- Proper event-driven communication between bounded contexts

#### ? **Domain Events Integration**
- Real-time UI updates driven by domain events
- Consistent state synchronization across components
- Event sourcing patterns for audit trails

#### ? **Business Rules Enforcement**
- Client-side validation mirrors domain validation
- Business logic delegated to domain services
- Policy-based access control and feature flags

#### ? **Value Objects & Enums**
- All UI components use proper domain enum values
- Type-safe data transfer with value object mapping
- Consistent terminology across UI and domain

### Domain Mapping Examples

```typescript
// UI State Management aligned with Domain
interface SubscriptionState {
  subscription: Subscription;
  status: SubscriptionStatus.Active | SubscriptionStatus.Trial | ...;
  usage: QuotaUsageSummary;
  plan: Plan;
}

// API Calls aligned with Domain Methods
const upgradeSubscription = async (planId: string) => {
  // Calls Subscription.ChangePlan() domain method
  return await api.post('/api/subscriptions/current/upgrade', { planId });
};
```

---

## ?? Implementation Guide

### Frontend Architecture Recommendations

#### **Technology Stack**
- **Frontend Framework**: React 18+ with TypeScript
- **State Management**: Redux Toolkit + RTK Query
- **UI Components**: Material-UI or Ant Design
- **Real-time**: Socket.IO client
- **Forms**: React Hook Form with Zod validation
- **Charts**: Chart.js or Recharts
- **Testing**: Jest + React Testing Library

#### **Project Structure**
```
src/
??? components/           # Reusable UI components
??? pages/               # Phase-based page components
??? store/               # Redux store and slices
??? services/            # API services and WebSocket
??? hooks/               # Custom React hooks
??? types/               # TypeScript type definitions
??? utils/               # Utility functions
??? constants/           # Domain enums and constants
```

#### **API Integration Pattern**
```typescript
// Domain-aligned API service
class SubscriptionApi {
  async getCurrentSubscription(): Promise<SubscriptionOverview> {
    // Maps to Subscription.GetQuotaUsageSummary()
    return this.get('/api/subscriptions/current');
  }
  
  async upgradePlan(planId: string): Promise<SubscriptionUpgrade> {
    // Maps to Subscription.ChangePlan()
    return this.post('/api/subscriptions/current/upgrade', { planId });
  }
}
```

#### **Real-time Integration**
```typescript
// WebSocket event handling aligned with domain events
useEffect(() => {
  socket.on('SubscriptionUsageUpdatedEvent', (event) => {
    dispatch(updateUsageMetrics(event.data));
  });
  
  socket.on('QuotaWarningEvent', (event) => {
    showNotification('Usage approaching limit', 'warning');
  });
}, []);
```

### Development Workflow

#### **Phase-by-Phase Implementation**
1. **Start with Phase 1** (Authentication) - Foundation
2. **Build Phase 2** (Dashboard) - Core UI framework
3. **Implement Core Features** (Phases 3-6) - Main functionality
4. **Add Business Features** (Phases 7-9) - Advanced capabilities

#### **Component Development**
1. **Read Phase Specification** thoroughly
2. **Map Domain Entities** to UI components
3. **Implement API Integration** using domain methods
4. **Add Real-time Features** with domain events
5. **Test Business Rules** and error handling

#### **Testing Strategy**
- **Unit Tests**: Component logic and API integration
- **Integration Tests**: Phase workflows and user journeys
- **E2E Tests**: Critical business processes
- **Domain Tests**: Business rule validation

---

## ?? Quick Start

### For Frontend Developers

1. **Review Domain Model**
   ```bash
   # Study the domain implementation
   cd Trap-Intel.Domain/
   # Understand aggregates, entities, and value objects
   ```

2. **Choose Your Phase**
   ```bash
   # Start with Phase 1 or your assigned phase
   open UI_DOCUMENTATION/PHASES/PHASE_1_AUTHENTICATION_ONBOARDING.md
   ```

3. **Set Up Development Environment**
   ```bash
   # Install dependencies
   npm install
   
   # Configure API endpoints
   cp .env.example .env.local
   ```

4. **Implement Phase Screens**
   - Follow the screen specifications exactly
   - Use provided API endpoints
   - Implement domain-aligned state management

### For Backend Developers

1. **Review API Specifications**
   - Each phase includes complete API endpoint definitions
   - Request/response models aligned with domain
   - Proper HTTP status codes and error handling

2. **Implement Controller Methods**
   ```csharp
   [HttpGet("/api/subscriptions/current")]
   public async Task<SubscriptionOverview> GetCurrentSubscription()
   {
       // Use domain methods as specified
       return await _subscriptionService.GetQuotaUsageSummary();
   }
   ```

3. **Add Real-time Features**
   ```csharp
   // Publish domain events for UI updates
   await _eventPublisher.PublishAsync(new SubscriptionUsageUpdatedEvent(...));
   ```

---

## ?? Additional Resources

### Domain Documentation
- **Domain Model**: `Trap-Intel.Domain/` - Core business logic
- **Infrastructure**: `Trap-Intel.Infrastructure/` - Data access and external services

### Design Resources
- **Color Palettes**: Professional blues and security-themed colors
- **Typography**: Enterprise-grade font recommendations
- **Icons**: Security and technology focused icon sets
- **Layout Patterns**: Enterprise application design patterns

### API Documentation
- **OpenAPI Specs**: Generated from controller specifications
- **Authentication**: JWT-based with role-based access control
- **Rate Limiting**: API quotas aligned with subscription plans
- **WebSocket Events**: Real-time update specifications

---

## ?? Contributing

### Documentation Updates
1. **Phase Modifications**: Update relevant phase specification
2. **Domain Changes**: Ensure alignment with domain model updates  
3. **API Changes**: Update endpoint specifications and examples
4. **Testing**: Add test scenarios for new features

### Review Process
1. **Domain Alignment Check**: Verify domain entity mapping
2. **API Consistency**: Ensure endpoint specifications match implementation
3. **Business Rule Validation**: Confirm UI behavior matches domain logic
4. **User Experience**: Review for usability and accessibility

---

## ?? Support

For questions about UI specifications or implementation guidance:

- **Technical Questions**: Review phase specifications and domain model
- **Domain Questions**: Consult domain aggregate documentation
- **API Questions**: Check endpoint specifications and backend implementation
- **Design Questions**: Follow enterprise application design patterns

---

**Happy Coding! ??**

> Build amazing user experiences that perfectly align with robust domain-driven architecture.