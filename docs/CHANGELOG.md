# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.3] - 2026-08-24

### Updated

- Updated NuGet packages.

## [1.1.2] - 2026-08-17

### Updated

- Updated NuGet packages.

## [1.1.1] - 2026-08-04

### Updated

- Updated NuGet packages (Cirreum spine 4.2.0 wave: `Cirreum.Contracts` 4.2.0 / `Cirreum.Domain` 4.2.0 and current patch releases).

## [1.1.0] - 2026-07-29

### Added

- **Configurable credentials for identity-based authentication** via the nested `Credential` block
  from `Cirreum.ServiceProvider` 1.1.0 (`Mode`: `Default` / `ManagedIdentity` / `Developer`, plus
  optional `IdentityId` selecting a user-assigned managed identity). The endpoint path
  previously hardcoded `new DefaultAzureCredential()` with no options — no tenant pinning, no
  identity selection.
- `Identifier` on the instance settings resolves as the Entra tenant, forwarded to every
  tenant-aware credential.
- A `Credential` block alongside a key-based connection string fails at startup with
  `InvalidOperationException` — identity configuration cannot apply to key authentication.
- An unrecognized `CredentialMode` value fails at startup instead of silently degrading to the
  default chain.

### Updated

- Updated NuGet packages.

## [1.0.20] - 2026-07-20

### Updated

- Updated NuGet packages.

## [1.0.19] - 2026-07-19

### Updated

- Updated NuGet packages.

## [1.0.18] - 2026-07-04

### Updated

- Updated NuGet packages.

## [1.0.17] - 2026-07-04

### Updated

- Updated NuGet packages.

## [1.0.16] - 2026-05-07

### Updated

- Updated NuGet packages.

## [1.0.15] - 2026-05-01

### Updated

- Updated NuGet packages.
