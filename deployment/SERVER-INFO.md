# Niemi API Server Information

## Server Details

- **Server Name**: `NIEMUB01`
- **OS**: Ubuntu Server 24.04 LTS
- **Purpose**: Niemi API Production Server
- **Application**: .NET 8.0 Web API

## Network Configuration

- **Hostname**: NIEMUB01
- **IP Address**: [To be configured during setup]
- **SSH Port**: 22
- **HTTP Port**: 80
- **HTTPS Port**: 443 (if SSL configured)
- **Application Port**: 5000 (internal, proxied by Nginx)

## Directory Structure

```
/var/www/niemi-api/          # Application root
├── Niemi.dll                # Main application
├── appsettings.json         # Base configuration
├── appsettings.Production.json  # Production settings (sensitive)
└── [other application files]

/etc/nginx/sites-available/niemi-api  # Nginx configuration
/etc/systemd/system/niemi-api.service  # Service configuration
/var/log/nginx/                # Nginx logs
```

## Service Management

### Systemd Service
- **Service Name**: `niemi-api.service`
- **User**: `www-data`
- **Working Directory**: `/var/www/niemi-api`
- **Environment**: Production

### Common Commands
```bash
# Service control
sudo systemctl start niemi-api
sudo systemctl stop niemi-api
sudo systemctl restart niemi-api
sudo systemctl status niemi-api

# View logs
sudo journalctl -u niemi-api -f
sudo journalctl -u niemi-api -n 100

# Nginx control
sudo systemctl restart nginx
sudo nginx -t  # Test configuration
```

## Access Information

### SSH Access
```bash
ssh george@NIEMUB01
# Or using IP: ssh george@<server-ip>
```

### API Endpoints
- **Base URL**: `http://NIEMUB01` or `http://<server-ip>`
- **API Documentation**: `http://NIEMUB01/swagger` (Development only)
- **Health Check**: `http://NIEMUB01/database`

### Main Endpoints
- `GET /database` - List available database facilities
- `GET /ordhuv` - Get orders by date range
- `POST /ordhuv` - Get orders by license plates
- `GET /categories` - Get keyword categories
- `POST /rule-flow` - Process orders to Rule.io
- `POST /scheduled/process-daily-orders` - Manual trigger for scheduled processing

## Configuration Files

### Application Settings
**Location**: `/var/www/niemi-api/appsettings.Production.json`

Required configurations:
- Azure SQL connection string
- Firebird database connection
- Rule.io API credentials
- CORS allowed origins

### Nginx Configuration
**Location**: `/etc/nginx/sites-available/niemi-api`

Key settings:
- Upstream server: `localhost:5000`
- Server name: Update with your domain
- SSL certificates: Configure after domain setup

## Security

### Firewall Rules (UFW)
```bash
sudo ufw status

# Allowed services:
# - OpenSSH (port 22)
# - Nginx Full (ports 80, 443)
```

### File Permissions
```bash
# Application files
sudo chown -R www-data:www-data /var/www/niemi-api

# Sensitive configuration
sudo chmod 600 /var/www/niemi-api/appsettings.Production.json
```

## Monitoring

### Log Locations
- **Application Logs**: `sudo journalctl -u niemi-api`
- **Nginx Access**: `/var/log/nginx/niemi-api-access.log`
- **Nginx Error**: `/var/log/nginx/niemi-api-error.log`
- **System**: `/var/log/syslog`

### Health Checks
```bash
# Check if service is running
sudo systemctl is-active niemi-api

# Check port binding
sudo netstat -tulpn | grep :5000

# Test API locally
curl http://localhost/database

# Test from network
curl http://NIEMUB01/database
```

## Backup Strategy

### Application Backup
```bash
# Backup before updates
sudo cp -r /var/www/niemi-api /var/www/niemi-api.backup.$(date +%Y%m%d_%H%M%S)
```

### Configuration Backup
```bash
# Backup configs
sudo tar -czf ~/niemi-backup-$(date +%Y%m%d).tar.gz \
  /var/www/niemi-api/appsettings.Production.json \
  /etc/nginx/sites-available/niemi-api \
  /etc/systemd/system/niemi-api.service
```

## Update Procedure

1. **Stop service**: `sudo systemctl stop niemi-api`
2. **Backup current version**: See backup commands above
3. **Deploy new files**: Transfer and extract
4. **Restore permissions**: `sudo chown -R www-data:www-data /var/www/niemi-api`
5. **Start service**: `sudo systemctl start niemi-api`
6. **Verify**: `sudo systemctl status niemi-api`

## Troubleshooting

### Service Won't Start
```bash
# Check logs
sudo journalctl -u niemi-api -n 50

# Check file permissions
ls -la /var/www/niemi-api/

# Check configuration
cat /var/www/niemi-api/appsettings.Production.json
```

### Connection Issues
```bash
# Check if service is listening
sudo netstat -tulpn | grep :5000

# Check Nginx
sudo nginx -t
sudo systemctl status nginx

# Check firewall
sudo ufw status
```

### Database Connection Issues
- Verify connection strings in appsettings.Production.json
- Test network connectivity to Azure SQL
- Ensure Firebird client libraries are installed: `dpkg -l | grep firebird`

## Contact & Support

- **Server Location**: Hyper-V Virtual Machine
- **Administrator**: [Your Name]
- **Documentation**: See deployment folder for full guides
- **Emergency Access**: Use Hyper-V Console if SSH is unavailable

## Quick Reference

### SSH to Server
```bash
ssh george@NIEMUB01
```

### Deploy Update (from Windows)
```powershell
.\deployment\deploy-from-windows.ps1 -ServerIP NIEMUB01 -Username george
```

### View Logs
```bash
sudo journalctl -u niemi-api -f
```

### Restart Service
```bash
sudo systemctl restart niemi-api
```

