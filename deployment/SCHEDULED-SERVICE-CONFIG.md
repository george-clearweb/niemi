# Scheduled Order Service Configuration

## Overview

The Niemi API includes a **scheduled background service** that automatically processes orders and sends them to Rule.io daily.

## How It Works

### Default Behavior (DISABLED for Safety)

The scheduled service is **DISABLED by default** in both development and production to prevent accidental data sending.

### What It Does When Enabled

1. **Runs Daily at Specific Time**: Executes at configured hour (default: 8:00 AM CET)
2. **Processes Previous Day's Orders**: 
   - Fetches orders from the previous day (midnight to midnight)
   - Filters for: `KON` status (confirmed orders)
   - Filters for: `Private` customers only
   - Only includes orders with email OR phone number
3. **Sends to Rule.io**: Automatically sends subscriber data to Rule.io CRM
4. **Consistent Schedule**: Always runs at the same time regardless of restarts

## Configuration

### appsettings.json / appsettings.Production.json

```json
{
  "ScheduledOrderService": {
    "Enabled": false,                              // Set to true to enable
    "ScheduledHour": 8,                            // Hour to run (0-23)
    "TimeZone": "Central European Standard Time"   // Timezone for scheduling
  }
}
```

### Configuration Options

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Enabled` | boolean | `false` | Enable/disable the scheduled service |
| `ScheduledHour` | integer | `8` | Hour of day to run (0-23, in specified timezone) |
| `TimeZone` | string | `"Central European Standard Time"` | Timezone ID for scheduling |

### Common Timezone IDs

- **Central European Time**: `"Central European Standard Time"` (CET/CEST)
- **UTC**: `"UTC"`
- **Eastern Time (US)**: `"Eastern Standard Time"`
- **Pacific Time (US)**: `"Pacific Standard Time"`

**Important**: The service calculates the next run time based on the configured timezone, so it will run at 8:00 AM CET regardless of server time or restarts.

## Safety Features

### Why It's Disabled by Default

- ✅ **Prevents accidental data sending** during setup
- ✅ **Allows testing** without affecting production Rule.io
- ✅ **Gives you control** over when automation starts

### Logging

The service logs all actions:
- Service enabled/disabled status on startup
- Orders found and filtered
- Success/failure of Rule.io submissions
- Any errors encountered

## How to Enable

### Development Environment

1. Edit `appsettings.Development.json`:
```json
{
  "ScheduledOrderService": {
    "Enabled": true,
    "ScheduledHour": 8,
    "TimeZone": "Central European Standard Time"
  }
}
```

2. Restart the application
3. Check logs to confirm:
   - `ScheduledOrderService is ENABLED. Will run daily at 8:00 Central European Standard Time`
   - `Next scheduled run in X hours Y minutes at 2024-10-07 08:00:00`

### Production Environment (NIEMUB01)

1. SSH into server:
```bash
ssh george@NIEMUB01
```

2. Edit production settings:
```bash
sudo nano /var/www/niemi-api/appsettings.Production.json
```

3. Update the configuration:
```json
{
  "ScheduledOrderService": {
    "Enabled": true,
    "ScheduledHour": 8,
    "TimeZone": "Central European Standard Time"
  }
}
```

4. Restart the service:
```bash
sudo systemctl restart niemi-api
```

5. Verify in logs:
```bash
sudo journalctl -u niemi-api -f | grep ScheduledOrderService
```

You should see:
```
ScheduledOrderService configured: Enabled=True, ScheduledTime=8:00 Central European Standard Time
ScheduledOrderService is ENABLED. Will run daily at 8:00 Central European Standard Time
Next scheduled run in 13 hours 23 minutes at 2024-10-07 08:00:00
```

## Manual Trigger

You can manually trigger the scheduled processing without waiting:

### Via API Endpoint

```bash
# From server
curl -X POST http://localhost/scheduled/process-daily-orders

# From Windows
Invoke-WebRequest -Method POST http://NIEMUB01/scheduled/process-daily-orders
```

### Response
```json
{
  "message": "Daily order processing completed successfully"
}
```

## What Data Gets Sent to Rule.io

For each qualifying order, the following subscriber data is sent:

### Contact Information
- Email address
- Phone number
- Language (sv)

### Customer Data
- Personal number (Personnr)
- First name (Förnamn)
- Last name (Efternamn)
- City (Stad)
- Birth date (Födelsedag)

### Order Data
- Order date (Datum)
- Order number (Doknr)
- Total price (Pris)
- Facility name (Anlaggning)
- Facility email (AnlaggningEpost)
- Facility phone (AnlaggningTfn)

### Vehicle Data
- Vehicle type (Fordonstyp)
- Brand (Marke)
- Model (Modell)
- Year (Modellar)
- License plate (Regnr)
- Job types/categories (Jobbtyp)

## Monitoring

### Check Service Status

```bash
# View real-time logs
sudo journalctl -u niemi-api -f

# Check last scheduled run
sudo journalctl -u niemi-api | grep "Processing orders for"

# Check Rule.io submissions
sudo journalctl -u niemi-api | grep "Rule.io"
```

### Expected Log Output

When running successfully:
```
ScheduledOrderService configured: Enabled=True, ScheduledTime=8:00 Central European Standard Time
ScheduledOrderService is ENABLED. Will run daily at 8:00 Central European Standard Time
Next scheduled run in 22 hours 45 minutes at 2024-10-07 08:00:00
Processing orders for 2024-10-06 (from 2024-10-06 00:00:00 to 2024-10-06 23:59:59)
Found 15 orders for 2024-10-06
Filtered to 12 orders with email or phone (from 15 total) for 2024-10-06
Sending 12 orders to Rule.io for 2024-10-06
Successfully sent 12 orders to Rule.io for 2024-10-06
```

## Troubleshooting

### Service Says It's Disabled

**Check**: `appsettings.Production.json` has `"Enabled": true`

```bash
cat /var/www/niemi-api/appsettings.Production.json | grep -A 3 ScheduledOrderService
```

### Service Enabled But Not Running

1. **Check logs** for errors:
```bash
sudo journalctl -u niemi-api -n 100 | grep -i error
```

2. **Verify Rule.io configuration** exists:
```bash
cat /var/www/niemi-api/appsettings.Production.json | grep -A 3 RuleIo
```

3. **Restart service**:
```bash
sudo systemctl restart niemi-api
```

### No Orders Being Sent

**Possible reasons**:
- No orders from previous day with `KON` status and `Private` customer type
- All orders missing both email and phone number
- Database connection issues

**Check logs** for filter counts:
```bash
sudo journalctl -u niemi-api | grep "Filtered to"
```

## Deployment Checklist

Before enabling in production:

- [ ] Verify Rule.io credentials are configured
- [ ] Verify database connections work
- [ ] Test with manual trigger first: `POST /scheduled/process-daily-orders`
- [ ] Check Rule.io receives test data correctly
- [ ] Review logs for any errors
- [ ] Set `"Enabled": true` in production settings
- [ ] Restart service
- [ ] Monitor logs for 24-48 hours

## Important Notes

⚠️ **Once enabled**, the service will automatically send customer data to Rule.io daily at 8:00 AM CET.

⚠️ **Ensure Rule.io is configured** before enabling to avoid errors.

⚠️ **Monitor logs** regularly to ensure successful operation.

✅ **The service runs even after server restarts** (systemd handles this automatically).

✅ **Consistent schedule**: The service will always run at 9:00 AM CET, regardless of:
   - Server restarts (recalculates next run time on startup)
   - Daylight saving time changes (timezone handles this automatically)
   - Manual application restarts

### What Happens on Restart

When the application or server restarts:
1. Service checks current time in CET
2. If before 8:00 AM today → schedules for 8:00 AM today
3. If after 8:00 AM today → schedules for 8:00 AM tomorrow
4. Logs show: "Next scheduled run in X hours Y minutes at [exact time]"

**Example**: If server restarts at 2:00 PM CET, the next run will be at 8:00 AM CET the next day (18 hours away).

## Configuration Timeline

### Current Status
- ✅ Code updated to use Rule.io (not HttpBin)
- ✅ Default: DISABLED in all environments
- ✅ Manual trigger available for testing

### Recommended Activation Process

1. **Development**: Keep disabled until ready to test
2. **Production Setup**: Deploy with disabled state
3. **Testing Phase**: Use manual trigger to test
4. **Verification**: Confirm Rule.io receives data correctly
5. **Go Live**: Set `Enabled: true` in production
6. **Monitor**: Watch logs for first 48 hours

