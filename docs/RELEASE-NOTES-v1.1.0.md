# Cirreum.Communications.Email.Azure 1.1.0 — Configurable Credentials for Identity-Based Authentication

## Why this release exists

The identity-authentication path constructed a bare `new DefaultAzureCredential()` — no tenant
pinning, no user-assigned identity selection on hosts with several, no deterministic mode for
production. This release adopts the credential vocabulary every Cirreum provider now shares
(`Cirreum.Providers` 1.3.0, surfaced by `Cirreum.ServiceProvider` 1.1.0).

## What's new

Configure `Endpoint` (without a connection string) and the provider authenticates with Entra.
The nested `Credential` block selects how:

```json
"Credential": { "Mode": "ManagedIdentity", "IdentityId": "<user-assigned-client-id>" }
```

- `Default` — the full `DefaultAzureCredential` chain; `IdentityId` pins its managed-identity leg
- `ManagedIdentity` — deterministic, no chain probing; omit `IdentityId` for system-assigned
- `Developer` — Visual Studio → Azure CLI → Azure PowerShell, as the signed-in developer

`Identifier` resolves as the Entra tenant, forwarded to every tenant-aware credential. Two guard
rails: a `Credential` block alongside a key-based connection string fails at startup, and an
unrecognized mode fails instead of silently using the default chain.

## Compatibility

Purely additive for existing configurations: no `Credential` block means the `Default` chain,
exactly as before (with tenant pinning now available via `Identifier`). Key-based connection
strings are untouched.

## See also

- [CHANGELOG](CHANGELOG.md)
- [Cirreum.Providers — Credential Configuration](https://github.com/cirreum/Cirreum.Providers#credential-configuration)