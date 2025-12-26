# Cirreum.Communications.Email.Azure

Azure Communication Services email functionality for the Cirreum communications framework. This implementation follows the Cirreum provider pattern, enabling seamless switching between email providers (SendGrid, Azure Communication Services, etc.) without code changes.

## Features

- **IEmailService Implementation**: Drop-in replacement for any Cirreum email provider
- **Single & Bulk Email Sending**: Individual emails and parallel bulk operations with retry logic
- **Flexible Authentication**: Connection string (key-based) or Managed Identity authentication
- **Async Operations**: Built-in polling for Azure Communication Services operation status
- **Health Checks**: Service-level health monitoring using the same IEmailService interface
- **Retry Logic**: Exponential backoff with jitter for rate limits and transient failures
- **Attachment Support**: Inline and regular attachments with proper disposition handling
- **Source-Generated Logging**: High-performance logging optimized for .NET 10
- **Configuration-Driven**: Support for both manual registration and appsettings-based configuration

## Installation

```bash
dotnet add package Cirreum.Communications.Email.Azure
```

## Quick Start

### Manual Registration

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Using connection string (key-based authentication)
builder.AddAzureEmailClient("primary", connectionJson, defaultFrom);

// Using endpoint (managed identity authentication)
builder.AddAzureEmailClient("primary", 
    endpoint: "https://myacs.communication.azure.com",
    defaultFrom: new EmailAddress("noreply@company.com", "Company Name"));

var host = builder.Build();
```

### Configuration-Based Registration (Recommended)

```csharp
// Register the provider registrar
builder.AddServiceProvider<AzureEmailRegistrar>();
```

**appsettings.json:**
```json
{
  "EmailProviders": {
    "Email.Azure": {
      "Instances": {
        "primary": {
          "Name": "production",
          "DefaultFrom": {
            "Address": "noreply@company.com",
            "Name": "Company Name"
          },
          "MaxRetries": 3,
          "BulkOptions": {
            "MaxConcurrency": 10,
            "MaxBatchSize": 50
          }
        }
      }
    }
  }
}
```

**KeyVault (ConnectionStrings-production):**
```json
// Option 1: Key-based authentication
{
  "ConnectionString": "endpoint=https://myacs.communication.azure.com/;accesskey=...",
  "DefaultFrom": {
    "Address": "noreply@company.com",
    "Name": "Company Name"
  }
}

// Option 2: Managed identity authentication
{
  "Endpoint": "https://myacs.communication.azure.com",
  "DefaultFrom": {
    "Address": "support@company.com", 
    "Name": "Support Team"
  }
}

// Option 3: Minimal (uses DefaultFrom from appsettings.json)
{
  "ConnectionString": "endpoint=https://myacs.communication.azure.com/;accesskey=..."
}
```

### Sending Emails

```csharp
var emailService = serviceProvider.GetRequiredKeyedService<IEmailService>("primary");

// Single email
var result = await emailService.SendEmailAsync(new EmailMessage {
    To = [new EmailAddress("user@example.com", "User Name")],
    Subject = "Hello from Azure Communication Services",
    HtmlContent = "<h1>Welcome!</h1><p>This email was sent via Azure Communication Services.</p>",
    TextContent = "Welcome! This email was sent via Azure Communication Services."
});

// Bulk emails - shared template
var results = await emailService.SendBulkEmailAsync(
    template: new EmailMessage {
        Subject = "Monthly Newsletter", 
        HtmlContent = "<h1>Newsletter</h1>"
    },
    recipients: recipientList
);

// Bulk emails - fully personalized
var results = await emailService.SendBulkEmailAsync(personalizedMessages);
```

## Authentication & Security

### Authentication Methods

The service supports two authentication methods, controlled by what the security administrator provides in KeyVault:

1. **Connection String (Key-Based)**: Traditional connection string with embedded access key
2. **Managed Identity**: Uses Azure AD authentication with DefaultAzureCredential

### Security Design

- **SysAdmin Controls Authentication**: Authentication method determined by what's provided in KeyVault
- **DevOps Controls Configuration**: Operational settings managed via appsettings.json
- **Environment Separation**: Different auth methods per environment (keys for dev, managed identity for prod)

## Azure Communication Services Limitations

- **Rate Limits**: 30 emails/min, 100 emails/hour for free tier (higher quotas available)
- **Recipients**: Maximum 50 recipients per email (To + CC + BCC combined)  
- **Message Size**: 10MB total including attachments (after Base64 encoding)
- **Async Operations**: All sends require polling for completion status
- **Templates**: No built-in template system (use external templating solutions)

## Health Checks

Health checks use the same `IEmailService` interface and can optionally send test emails:

```json
{
  "HealthOptions": {
    "SendTestEmail": true,
    "TestEmailRecipient": "healthcheck@company.com", 
    "WaitForTestEmailCompletion": true,
    "Timeout": "00:01:00"
  }
}
```

Health checks automatically register when using the configuration-based approach.

## Advanced Configuration

### Bulk Operations

```json
{
  "BulkOptions": {
    "MaxConcurrency": 10,
    "MaxBatchSize": 50,
    "BatchDelay": "00:00:01",
    "WaitForCompletion": true,
    "OperationTimeout": "00:05:00"
  }
}
```

### Global Headers and Tags

```json
{
  "GlobalHeaders": {
    "X-Source": "MyApplication",
    "X-Environment": "Production"
  },
  "GlobalTags": ["newsletter", "automated"]
}
```

## Provider Pattern Benefits

- **Vendor Independence**: Switch between SendGrid, Azure, AWS SES with just configuration changes
- **A/B Testing**: Use different providers for different message types or user segments
- **Multi-Tenant**: Different providers per tenant or environment
- **Gradual Migration**: Migrate from one provider to another without code changes

## Integration with Layer 5 Runtime Extensions

This provider integrates seamlessly with Cirreum's Layer 5 communications extensions:

```csharp
// Single registration for all communication providers
builder.AddCommunications();

// Configuration determines what's actually available
var emailService = sp.GetKeyedService<IEmailService>("marketing");  // Could be SendGrid
var backupEmail = sp.GetKeyedService<IEmailService>("backup");      // Could be Azure
```

## Dependencies

- .NET 10.0+
- Azure.Communication.Email 1.1.0
- Azure.Identity 1.14.1
- Cirreum.Communications.Email 1.0.108+
- Cirreum.ServiceProvider 1.0.5+

## License

MIT License - see [LICENSE](LICENSE) file for details.