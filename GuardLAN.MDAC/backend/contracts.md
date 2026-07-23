# MDAC API Contract

## Register device

POST /api/mdac/register

Request body:

```json
{
  "deviceName": "Pixel 8"
}
```

Response:

```json
{
  "deviceId": "uuid",
  "status": "registered"
}
```

## Submit sync payload

POST /api/mdac/sync

Request body:

```json
{
  "deviceId": "uuid",
  "usage": {
    "appName": "GuardLAN",
    "foregroundSeconds": 120
  }
}
```

Response:

```json
{
  "status": "accepted"
}
```
