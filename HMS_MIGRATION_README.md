# HMS Push Notification Migration

## Problem
The project had a dependency on `AGConnectAdmin` package (version 1.1.0) which is not available on NuGet.org. This package was used for Huawei Mobile Services (HMS) push notifications.

## Solution
Replaced the `AGConnectAdmin` package with a custom REST API implementation that directly calls Huawei's Push Kit API.

## Changes Made

### 1. Removed AGConnectAdmin Package
- Removed `AGConnectAdmin` from `BuildingBlocks.Notifications.csproj`
- Added `Microsoft.EntityFrameworkCore.SqlServer` package (version 8.0.4) that was missing

### 2. Created Custom HMS REST Client
Created `src\BuildingBlocks.Notifications\Push\HmsRestClient.cs` which provides:
- OAuth2 authentication with Huawei's API
- Access token caching with automatic expiration handling
- Message sending via Huawei's Push Kit REST API

### 3. Updated HMS Components
- **HmsMessagingFactory.cs**: Refactored to use custom `HmsMessagingClient` instead of AGConnectAdmin
- **HmsPushProvider.cs**: Updated to use new REST-based message format
- **NotificationsBuildingBlockDI.cs**: Updated service registration to use HttpClient-based `HmsRestClient`

## Configuration Required

The `HuaweiPushOptions` configuration now requires an `AppId` field in addition to the existing fields:

```json
{
  "HuaweiPush": {
    "LoginUri": "https://oauth-login.cloud.huawei.com",
    "ApiBaseUri": "https://push-api.cloud.huawei.com",
    "Apps": {
      "HuaweiUser": {
        "AppInstanceName": "user-app",
        "AppId": "your-app-id",
        "ClientId": "your-client-id",
        "ClientSecret": "your-client-secret"
      },
      "HuaweiDriver": {
        "AppInstanceName": "driver-app",
        "AppId": "your-app-id",
        "ClientId": "your-client-id",
        "ClientSecret": "your-client-secret"
      },
      "HuaweiMerchant": {
        "AppInstanceName": "merchant-app",
        "AppId": "your-app-id",
        "ClientId": "your-client-id",
        "ClientSecret": "your-client-secret"
      }
    }
  }
}
```

## Benefits
1. **No external dependencies**: The solution now uses only standard HTTP client
2. **Better control**: Direct access to the REST API provides more flexibility
3. **Token caching**: Automatic OAuth token management with expiration handling
4. **Transparency**: Full visibility into API requests and responses

## API Endpoints Used
- OAuth Token: `POST {LoginUri}/oauth2/v3/token`
- Send Message: `POST {ApiBaseUri}/v1/{appId}/messages:send`

## Notes
- The implementation uses `System.Text.Json` for serialization in the REST client
- The provider still uses `Newtonsoft.Json` for message data serialization (for consistency with existing code)
- Access tokens are cached with a 5-minute buffer before expiration to prevent edge cases
