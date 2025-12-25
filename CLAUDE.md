# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 10.0 library that provides Azure Communication Services email functionality for the Cirreum communications framework. It implements the `IEmailService` interface and provides:

- Single email sending with validation and retry logic
- Bulk email sending with parallel processing
- Health check integration
- Hosting extensions for dependency injection
- Azure AD and connection string authentication
- Attachment handling (inline/attachment disposition)
- Comprehensive error handling with exponential backoff
- Async operation status polling

## Commands

### Build
```bash
dotnet restore Cirreum.Communications.Email.Azure.slnx
dotnet build Cirreum.Communications.Email.Azure.slnx --configuration Release --no-restore
```

### Pack (for NuGet)
```bash
dotnet pack Cirreum.Communications.Email.Azure.slnx --configuration Release --no-build --output ./artifacts
```

### Solution Structure
- Use `Cirreum.Communications.Email.Azure.slnx` for all dotnet commands (not traditional .sln)
- Main project: `src/Cirreum.Communications.Email.Azure/Cirreum.Communications.Email.Azure.csproj`

## Architecture

### Core Components
- **AzureEmailService**: Main service implementing `IEmailService` with async operations and parallel bulk sending
- **AzureEmailRegistrar**: Handles service registration and configuration
- **Configuration classes**: Settings for instances, bulk operations, and health checks
- **HostingExtensions**: Extension methods for `IHostApplicationBuilder` registration

### Key Features
- **Bulk sending**: Parallel processing (no native batch API like SendGrid)
- **Async operations**: All sends return operation IDs requiring status polling
- **Authentication**: Connection string or Azure AD/Managed Identity support
- **Retry mechanism**: Exponential backoff for rate limits and failures
- **Validation**: Comprehensive email validation before sending
- **Health checks**: Built-in Azure Communication Services health check support

### Dependencies
- Azure.Communication.Email 1.1.0
- Azure.Identity 1.14.1 (for Azure AD authentication)
- Cirreum.Communications.Email 1.0.108 (provides base interfaces/types)
- Cirreum.ServiceProvider 1.0.5

### Configuration Structure
- Global configuration via `AzureEmailSettings` 
- Instance-specific via `AzureEmailInstanceSettings`
- Bulk operation settings in `AzureEmailBulkSettings`
- Health check options in `AzureEmailHealthCheckOptions`

### Azure Communication Services Limitations
- Default rate limits: Custom domains 30/min, 100/hour; Azure managed 5/min, 10/hour
- 50 recipients max per email
- 10MB total message size (including attachments after Base64 encoding)
- No built-in template system (external templating required)
- Async operations require polling for completion status

The service follows the Cirreum Foundation Framework conventions with layered configuration, dependency injection patterns, and comprehensive error handling adapted for Azure Communication Services' async operational model.