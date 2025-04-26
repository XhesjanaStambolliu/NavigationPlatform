# Navigation Platform

A modern journey tracking application built with .NET 9 using Clean Architecture principles. The application allows users to track their journeys, share them with others, and earn badges for achieving distance goals.

## Overview

Navigation Platform is a comprehensive solution for recording, managing, and sharing travel journeys. The platform implements a rich domain model with proper domain events, follows best practices for clean architecture, and provides robust security through resource-based authorization.

### Key Technologies

- **.NET 9** - Latest framework version with enhanced performance
- **Clean Architecture** - Clear separation of concerns between layers
- **Entity Framework Core 7+** - Modern ORM for data persistence
- **MediatR** - For CQRS pattern implementation and in-process messaging
- **Auth0** - Secure authentication and authorization
- **SignalR** - Real-time notifications for journey updates
- **SQL Server** - Robust relational database
- **Outbox Pattern** - Reliable event publishing

## Architecture

The application follows a modular monolith architecture based on Clean Architecture principles, with clear separation of concerns:

- **Domain Layer** - Core business entities, value objects, and domain events
- **Application Layer** - Use cases, commands/queries, and business logic
- **Infrastructure Layer** - External concerns (persistence, authentication, etc.)
- **API Layer** - Controllers, middleware, and configuration

### Why Modular Monolith?

For this project, a modular monolith architecture was chosen over microservices for several reasons:

1. **Development Speed** - Faster initial development without distributed systems complexity
2. **Simplicity** - Easier to build, test, and deploy as a cohesive unit
3. **Team Size** - More efficient for smaller development teams
4. **Future Scalability** - Designed with clear boundaries for future microservice extraction
5. **Operational Overhead** - Lower infrastructure costs and simpler deployment pipeline

The key architectural principle here is "monolith-first" - starting with a well-structured monolith where bounded contexts are clearly defined, then evolving to microservices only when specific scaling needs arise.

## Authentication & Authorization

The platform implements a secure authentication and authorization mechanism using Auth0.

### Authentication Flow

- Uses Auth0 with Authorization Code Flow + PKCE for secure authentication
- Implements token refresh for seamless user experience
- JWT tokens validated with Auth0 public keys

### Authorization Model

- **Resource-based Authorization** - All resources (journeys, shares, etc.) are protected
- **Policy-based Authorization** - Using custom authorization handlers
- **Owner/Share Principle** - Users can only access journeys they own or have been shared with

## Domain Design

The domain model implements key Domain-Driven Design principles:

### Aggregate Roots and Entities

- **Journey** is the primary aggregate root
  - Controls access to child entities like JourneyShare, PublicLink, etc.
  - Enforces invariants and business rules
  - Raises domain events on state changes

### Value Objects

- **DistanceKm** is implemented as a proper value object
  - Immutable with equality based on value
  - Encapsulates validation (distance can't be negative)
  - Provides operations like addition and comparison

### Domain Events

Every significant change to the Journey aggregate root raises a corresponding domain event:

- **JourneyCreatedEvent** - When a new journey is recorded
- **JourneyUpdatedEvent** - When journey details are modified
- **JourneyDeletedEvent** - When a journey is deleted (soft delete)
- **DailyGoalAchievedEvent** - When a user reaches their daily distance goal

These events are:
1. Published immediately via MediatR for in-process handlers
2. Stored in the outbox for reliable processing

## CQRS & Event Flow

The application implements the Command Query Responsibility Segregation (CQRS) pattern:

### Command/Query Separation

- **Commands** - Change state (CreateJourney, UpdateJourney, etc.)
- **Queries** - Read data (GetJourney, GetJourneys, etc.)
- All commands and queries are handled via MediatR

### Event Processing

The platform uses the Outbox Pattern for reliable event publishing:

1. Domain events are captured when entities change
2. Events are persisted to the OutboxMessages table
3. A background service processes these events asynchronously
4. Events can trigger notifications, statistics updates, etc.

### Projections & Statistics

- Monthly user distance statistics are generated from journey data
- These read models are updated when journey events are processed
- Provides efficient querying for analytics and reporting

### Real-time Notifications

The application uses SignalR to provide real-time notifications:
- Journey updates are broadcast to users who favorited them
- The JourneyHub manages connection state and group membership
- Fallback mechanisms send notifications via outbox messaging when real-time delivery fails

## Persistence

The application uses Entity Framework Core 7+ for data persistence:

- **Code-First Approach** - Domain model drives database schema
- **Migrations** - Applied automatically on application startup
- **Soft Delete** - Records are marked as deleted rather than removed
- **Audit Trails** - Changes are tracked with creation/update timestamps

## Running the Application

### Prerequisites

- .NET 9.0 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022, JetBrains Rider, or VS Code
- Docker and Docker Compose (for observability tools)

### Setup

1. Clone the repository:
```
git clone [https://github.com/yourusername/NavigationPlatform.git](https://github.com/XhesjanaStambolliu/NavigationPlatform)
cd NavigationPlatform
```

2. Update the connection string in `appsettings.json` if needed.

3. Build and run the application:
```
cd src/NavigationPlatform.API
dotnet build
dotnet run
```

4. Navigate to `https://localhost:5001/swagger` to explore the API.

### Starting with Docker Compose

To run the application and all observability tools using Docker Compose:

```
cd docker-compose
docker-compose up -d
```

This will start:
- The Navigation Platform API
- SQL Server database
- Jaeger for distributed tracing
- Prometheus for metrics collection

## Observability

The application implements comprehensive observability features for monitoring, debugging, and performance analysis.

### Logging

- **Structured JSON Logs**: Using Serilog for structured logging
- **Correlation IDs**: Every request carries a unique correlation ID
- **Log Access**: 
  - Console logs in development
  - JSON file logs in `logs/navigation-platform-*.log`

### Distributed Tracing

- **OpenTelemetry**: For end-to-end tracing across the application
- **Jaeger UI**: Access the Jaeger UI at `http://localhost:16686`
  - View traces filtered by service, operation, tags, etc.
  - Analyze trace timelines and dependencies
  - Search by correlation ID to find specific request traces

### Metrics

- **Prometheus**: Collects and stores metrics
- **Metrics Endpoint**: Available at `http://localhost:5000/metrics`
- **Prometheus UI**: Access at `http://localhost:9090`
- **Key Metrics**:
  - HTTP request latency
  - Database query latency
  - Queue metrics (when message broker is implemented)

### Health Checks

Health check endpoints provide system status:
- **Liveness**: `http://localhost:5000/healthz` - Verifies the API is running
- **Readiness**: `http://localhost:5000/readyz` - Verifies dependencies are available

### Prometheus Alerts

Prometheus is configured with alerts for:
- **Queue Lag**: Triggers when message queue lag exceeds 100 messages for 5+ minutes
  - Alert rules are defined in `docker-compose/prometheus/alert.rules.yml`

## Key Features

- User journey tracking with various transport types
- Journey sharing and favoriting
- Public links for sharing journeys externally
- Daily distance badges for achievements
- Real-time notifications via SignalR
- Comprehensive authorization model

## Troubleshooting

If you encounter the error "A possible object cycle was detected" when creating entities with circular references, ensure that:

1. The JSON serialization in both `EventPublisher` and `OutboxProcessor` uses the `ReferenceHandler.Preserve` setting
2. The database has been updated with the latest migrations
3. All required services are properly registered in the DI container

## License

This project is licensed under the MIT License - see the LICENSE file for details. 
