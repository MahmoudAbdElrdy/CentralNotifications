# Central NotificationsService (SignalR + Push + DB) + Client API Example

This solution contains:
1) `BuildingBlocks.Notifications` (Class Library)
2) `NotificationsService` (Central service): runs SignalR hub + Push + SQL Server + Outbox
3) `Sample.ClientApi` (Any other project example): sends notifications to NotificationsService via HTTP

## Run NotificationsService
- Set connection string in `src/NotificationsService/appsettings.json`
- Run migrations:
```bash
cd src/NotificationsService
dotnet ef migrations add InitNotifications --context NotificationsDbContext
dotnet ef database update --context NotificationsDbContext
dotnet run
```

Hub endpoint:
- `/hubs/notifications`

HTTP API:
- `POST /api/notifications/send` (requires `X-Notifications-ApiKey`)
- `POST /api/push/register` (register a push token for a user; demo uses userId in body)

## Run Sample.ClientApi
- Configure:
  - `NotificationsService:BaseUrl`
  - `NotificationsService:ApiKey`
- Endpoint:
  - `POST /api/client/send/{userId}`
It will forward to NotificationsService.

## Notes
- Central service is the "correct" approach for multi-project setups.
- Other projects do NOT need to reference the building block at all (they can just call HTTP).
