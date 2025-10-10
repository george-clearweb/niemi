# Niemi API Deployment Guide for Ubuntu Server

> ⭐ **For quick updates to an existing server, see [SIMPLE-DEPLOY.md](SIMPLE-DEPLOY.md)** - This guide is for detailed first-time setup and reference.

## Prerequisites
- Ubuntu Server 24.04 LTS or 22.04 LTS with .NET 8.0 installed (see HYPER-V-SETUP.md)
- SSH access to the server
- Application directory created at `/var/www/niemi-api`

## Step 1: Publish the Application

### 1.1 On Your Development Machine (Windows)

Open PowerShell in your project directory:

```powershell
# Navigate to project directory
cd "C:\Users\GeorgeJohnsson\OneDrive - Clearweb AB\Dokument\Rider\Niemi"

# Publish the application for Linux
dotnet publish Niemi/Niemi.csproj -c Release -r linux-x64 --self-contained false -o ./publish

# This creates a 'publish' folder with all necessary files
```

### 1.2 Transfer Files to Server

**Option A: Using SCP (from PowerShell)**
```powershell
scp -r ./publish/* george@<server-ip>:/var/www/niemi-api/
```

**Option B: Using WinSCP or FileZilla**
- Use WinSCP or FileZilla to transfer the `publish` folder contents to `/var/www/niemi-api/`

**Option C: Using Git (Alternative)**
- Push code to a Git repository
- Clone on the server and publish there

## Step 2: Configure Application Settings

### 2.1 SSH into the Server

```bash
ssh george@<server-ip>
```

### 2.2 Create Production Configuration File

```bash
cd /var/www/niemi-api

# Create appsettings.Production.json
nano appsettings.Production.json
```

Add the following (update with your actual values):

```json
{
  "ConnectionStrings": {
    "AZURE_SQL_CONNECTION_STRING": "Server=tcp:your-server.database.windows.net,1433;Database=your-db;User ID=your-user;Password=your-password;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
    "FirebirdConnection": "User=SYSDBA;Password=your-password;Database=your-database-path;ServerType=0;Port=3050"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Pagination": {
    "DefaultPageSize": 100,
    "MaxPageSize": 1000
  },
  "Cors": {
    "AllowedOrigins": [
      "https://yourdomain.com",
      "https://www.yourdomain.com"
    ]
  },
  "RuleIo": {
    "BaseUrl": "https://app.rule.io/api/v2",
    "BearerToken": "your-rule-io-bearer-token"
  },
  "ScheduledOrderService": {
    "Enabled": false,
    "ScheduledHour": 8,
    "TimeZone": "Central European Standard Time"
  }
}
```

**Important**: Keep `"Enabled": false` until you're ready to automatically send data to Rule.io daily!

Save and exit (Ctrl+X, Y, Enter)

### 2.3 Set File Permissions

```bash
chmod 600 appsettings.Production.json
chmod +x Niemi
sudo chown -R www-data:www-data /var/www/niemi-api
```

## Step 3: Create Systemd Service

```bash
sudo nano /etc/systemd/system/niemi-api.service
```

Copy the service configuration (see `niemi-api.service` file in deployment folder)

```bash
# Reload systemd
sudo systemctl daemon-reload

# Enable service to start on boot
sudo systemctl enable niemi-api

# Start the service
sudo systemctl start niemi-api

# Check status
sudo systemctl status niemi-api
```

## Step 4: Configure Nginx Reverse Proxy

```bash
sudo nano /etc/nginx/sites-available/niemi-api
```

Copy the Nginx configuration (see `nginx-niemi-api.conf` file in deployment folder)

```bash
# Create symbolic link to enable the site
sudo ln -s /etc/nginx/sites-available/niemi-api /etc/nginx/sites-enabled/

# Remove default site
sudo rm /etc/nginx/sites-enabled/default

# Test Nginx configuration
sudo nginx -t

# Restart Nginx
sudo systemctl restart nginx
```

## Step 5: Verify Deployment

### 5.1 Check if Service is Running

```bash
# Check service status
sudo systemctl status niemi-api

# Check logs
sudo journalctl -u niemi-api -f

# Check if port 5000 is listening
sudo netstat -tulpn | grep :5000
```

### 5.2 Test API Endpoints

```bash
# Test from server
curl http://localhost/database

# Test from your machine (replace <server-ip>)
curl http://<server-ip>/database
```

## Step 6: SSL Configuration (Optional but Recommended)

### 6.1 Install Certbot

```bash
sudo apt install -y certbot python3-certbot-nginx
```

### 6.2 Obtain SSL Certificate

```bash
# Replace with your actual domain
sudo certbot --nginx -d api.yourdomain.com
```

Follow the prompts. Certbot will automatically configure Nginx for HTTPS.

## Troubleshooting

### View Application Logs
```bash
# Real-time logs
sudo journalctl -u niemi-api -f

# Last 100 lines
sudo journalctl -u niemi-api -n 100

# Logs for today
sudo journalctl -u niemi-api --since today
```

### Restart Services
```bash
# Restart API
sudo systemctl restart niemi-api

# Restart Nginx
sudo systemctl restart nginx
```

### Check Port Bindings
```bash
sudo netstat -tulpn | grep :5000
sudo netstat -tulpn | grep :80
```

### Common Issues

**Issue**: Service fails to start
- Check logs: `sudo journalctl -u niemi-api -n 50`
- Verify appsettings.Production.json exists and is valid JSON
- Check file permissions

**Issue**: Can't connect to database
- Verify connection strings in appsettings.Production.json
- Check firewall rules allow outbound connections
- Test database connectivity from server

**Issue**: 502 Bad Gateway
- Ensure the application is running on port 5000
- Check Nginx configuration
- Check application logs

## Monitoring and Maintenance

### Update Application
```bash
# Stop service
sudo systemctl stop niemi-api

# Backup current version
sudo cp -r /var/www/niemi-api /var/www/niemi-api.backup

# Deploy new files (transfer from dev machine)
# ... upload new files ...

# Restore permissions
sudo chown -R www-data:www-data /var/www/niemi-api

# Start service
sudo systemctl start niemi-api
```

### Monitor Service Status
```bash
# Create monitoring script
nano ~/check-niemi.sh
```

Add:
```bash
#!/bin/bash
if systemctl is-active --quiet niemi-api; then
    echo "Service is running"
else
    echo "Service is down! Attempting restart..."
    sudo systemctl restart niemi-api
fi
```

```bash
chmod +x ~/check-niemi.sh

# Add to crontab for monitoring every 5 minutes
crontab -e
# Add: */5 * * * * /home/admin/check-niemi.sh
```

