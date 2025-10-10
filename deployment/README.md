# Niemi API - Linux Deployment Package

This deployment package contains everything you need to deploy the Niemi API to an Ubuntu Server on Hyper-V.

## 📦 Package Contents

- **SIMPLE-DEPLOY.md** - ⭐ **Quick deployment guide with tested commands (USE THIS FOR UPDATES)**
- **HYPER-V-SETUP.md** - Complete guide to create and configure the Ubuntu VM in Hyper-V
- **DEPLOYMENT-GUIDE.md** - Step-by-step deployment instructions
- **QUICK-START-GUIDE.md** - Condensed setup guide for first-time deployment
- **niemi-api.service** - Systemd service configuration
- **nginx-niemi-api.conf** - Nginx reverse proxy configuration
- **deploy.sh** - Automated deployment script
- **quick-start.sh** - Quick setup script for initial server configuration

## 🚀 Quick Start

### **⭐ RECOMMENDED: Simple Deployment (For Updates)**

**If your server is already set up, use this:**

```bash
cat SIMPLE-DEPLOY.md
```

This guide has the exact tested commands for quick deployments.

---

### Option 1: First Time Setup

1. **Set up Hyper-V VM**
   ```bash
   # Follow the guide
   cat HYPER-V-SETUP.md
   ```

2. **Deploy Application**
   ```bash
   # Follow the guide
   cat DEPLOYMENT-GUIDE.md
   ```

### Option 2: Automated Deployment (Advanced)

1. **Publish the application** (on your Windows machine):
   ```powershell
   cd "C:\Users\GeorgeJohnsson\OneDrive - Clearweb AB\Dokument\Rider\Niemi"
   dotnet publish Niemi/Niemi.csproj -c Release -r linux-x64 --self-contained false -o ./publish
   ```

2. **Transfer files to server** (NIEMUB01):
   ```powershell
   # Transfer publish folder
   scp -r ./publish george@<server-ip>:/tmp/niemi-publish
   # Or: scp -r ./publish george@NIEMUB01:/tmp/niemi-publish
   
   # Transfer deployment scripts
   scp -r ./deployment george@<server-ip>:/tmp/niemi-deployment
   # Or: scp -r ./deployment george@NIEMUB01:/tmp/niemi-deployment
   ```

3. **Run deployment script** (on the server):
   ```bash
   ssh george@<server-ip>
   
   cd /tmp/niemi-deployment
   chmod +x deploy.sh
   sudo ./deploy.sh
   ```

## 📋 Prerequisites

### On Hyper-V Host
- Hyper-V enabled on Windows
- Ubuntu Server 24.04 LTS ISO downloaded (recommended) or 22.04 LTS
- At least 8GB RAM and 100GB disk space available

### On Ubuntu Server
- .NET 8.0 Runtime installed
- Nginx installed
- Firebird client libraries installed
- OpenSSH server running

## 🔧 Configuration Required

After deployment, you must configure:

1. **Connection Strings** in `/var/www/niemi-api/appsettings.Production.json`:
   - Azure SQL connection string
   - Firebird database connection
   - Rule.io credentials

2. **Scheduled Service** in `/var/www/niemi-api/appsettings.Production.json`:
   - Keep `"Enabled": false` until ready for production
   - See `SCHEDULED-SERVICE-CONFIG.md` for details

3. **Nginx Domain** in `/etc/nginx/sites-available/niemi-api`:
   - Update `server_name` with your domain

4. **SSL Certificate** (optional but recommended):
   ```bash
   sudo certbot --nginx -d api.yourdomain.com
   ```

## 🔍 Verification

After deployment, verify the API is running:

```bash
# Check service status
sudo systemctl status niemi-api

# Check API response
curl http://localhost/database

# View logs
sudo journalctl -u niemi-api -f
```

## 📊 Monitoring

### View Logs
```bash
# Real-time logs
sudo journalctl -u niemi-api -f

# Last 100 lines
sudo journalctl -u niemi-api -n 100
```

### Restart Services
```bash
# Restart API
sudo systemctl restart niemi-api

# Restart Nginx
sudo systemctl restart nginx
```

## 🐛 Troubleshooting

### Service won't start
```bash
# Check detailed logs
sudo journalctl -u niemi-api -n 100 --no-pager

# Check if port 5000 is in use
sudo netstat -tulpn | grep :5000

# Verify file permissions
ls -la /var/www/niemi-api/
```

### Can't connect to API
```bash
# Check if Nginx is running
sudo systemctl status nginx

# Test local connection
curl -v http://localhost:5000/database

# Check firewall
sudo ufw status
```

### Database connection issues
- Verify connection strings in appsettings.Production.json
- Check network connectivity to Azure SQL
- Ensure Firebird client libraries are installed

## 📚 Additional Resources

- [.NET on Linux](https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu)
- [Nginx Documentation](https://nginx.org/en/docs/)
- [Systemd Service Documentation](https://www.freedesktop.org/software/systemd/man/systemd.service.html)
- [Let's Encrypt SSL](https://letsencrypt.org/getting-started/)

## 🔐 Security Checklist

- [ ] Changed default SSH password
- [ ] Configured UFW firewall
- [ ] Installed SSL certificate
- [ ] Secured appsettings.Production.json permissions (600)
- [ ] Updated allowed origins in CORS settings
- [ ] Configured proper logging
- [ ] Set up automated backups

## 📞 Support

For issues or questions:
1. Check logs: `sudo journalctl -u niemi-api -f`
2. Review configuration files
3. Verify all prerequisites are met
4. Check network connectivity

## 📝 Update Procedure

To update the application:

```bash
# 1. Stop service
sudo systemctl stop niemi-api

# 2. Backup current version
sudo cp -r /var/www/niemi-api /var/www/niemi-api.backup.$(date +%Y%m%d)

# 3. Deploy new files (from your dev machine)
# ... publish and transfer files ...

# 4. Restore permissions
sudo chown -R www-data:www-data /var/www/niemi-api

# 5. Start service
sudo systemctl start niemi-api

# 6. Verify
sudo systemctl status niemi-api
```

