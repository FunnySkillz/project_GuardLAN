# GuardLAN Pi-hole Integration

GuardLAN can import DNS query history from Pi-hole into the backend `dns_queries` table.

The importer is disabled by default. Enable it through ASP.NET Core configuration for the API and worker:

```json
{
  "PiHole": {
    "Enabled": true,
    "BaseUrl": "http://pi.hole",
    "ApplicationPassword": "",
    "QueriesPath": "/api/queries",
    "LookbackMinutes": 60,
    "MaxQueries": 1000,
    "VerifyTls": true
  },
  "DnsIngestion": {
    "IntervalSeconds": 300
  }
}
```

Use environment variables for real credentials:

```powershell
$env:PiHole__Enabled = "true"
$env:PiHole__BaseUrl = "http://pi.hole"
$env:PiHole__ApplicationPassword = "<your app password>"
```

The API exposes a manual import endpoint:

```text
POST http://localhost:5232/api/dns/import/pihole
```

The worker also runs the same import on `DnsIngestion:IntervalSeconds`.

Imported records are normalized before storage:

* Duplicate records are skipped by client IP, domain and timestamp.
* Device matching uses the DNS client IP against known GuardLAN devices.
* Allowed and blocked queries are preserved.
* Invalid rows and unsupported response shapes are skipped.

The connector uses Pi-hole's session authentication endpoint when `ApplicationPassword` is configured and sends the returned session ID as `X-FTL-SID`.

Next validation step: test against a live Pi-hole instance and adjust `QueriesPath` if the local Pi-hole API documentation exposes a version-specific query path.
