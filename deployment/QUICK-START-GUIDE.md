# 🚀 Quick Start Guide - Deploy Niemi API to Linux

This is a condensed guide to get you up and running quickly. For detailed instructions, see the full guides.

## Prerequisites

- Hyper-V enabled on your Windows machine
- Ubuntu Server 24.04 LTS ISO downloaded (recommended) or 22.04 LTS
- PowerShell (for deployment from Windows)

## Part 1: Create Ubuntu VM in Hyper-V (15 minutes)

### 1. Create Virtual Machine

1. Open **Hyper-V Manager**
2. **Action** > **New** > **Virtual Machine**
3. Configure:
   - Name: `Niemi-API-Server`
   - Generation: **2**
   - Memory: **8192 MB** (with Dynamic Memory)
   - Network: Select your virtual switch
   - Hard Disk: **100 GB** (dynamic)
   - Install from: Browse to Ubuntu Server ISO

4. **Before starting**: VM Settings > Security
   - Uncheck "Enable Secure Boot" OR change to "Microsoft UEFI"

5. **Start VM** and **Connect**

### 2. Install Ubuntu Server

Follow the installer:
- Language: **English**
- Network: **Configure static IP** (recommended) or DHCP
- Storage: **Use entire disk**
- Profile:
  - Name: `george` (or your name)
  - Server: `NIEMUB01`
  - Username: `george`
  - Password: [your-password]
- **✅ Install OpenSSH server**
- Skip featured snaps

Wait for installation, then **reboot**.

### 3. Quick Server Setup

SSH into your new server (NIEMUB01):
```bash
ssh george@<server-ip>
# Or if DNS is configured: ssh george@NIEMUB01
```

Run the quick setup script (or install manually):
```bash
# Download and run quick-start script (if you transferred it)
chmod +x quick-start.sh
sudo ./quick-start.sh
```

OR install manually:
```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install .NET 8.0 (for Ubuntu 24.04)
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0 aspnetcore-runtime-8.0

# Install Nginx
sudo apt install -y nginx

# Install Firebird client
sudo apt install -y firebird3.0-utils libfbclient2

# Configure firewall
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable

# Create app directory
sudo mkdir -p /var/www/niemi-api
sudo chown -R $USER:$USER /var/www/niemi-api
```

## Part 2: Deploy Your Application (10 minutes)

### Option A: Automated Deployment from Windows (Easiest)

On your **Windows machine**, open PowerShell in the project directory:

```powershell
# Navigate to project
cd "C:\Users\GeorgeJohnsson\OneDrive - Clearweb AB\Dokument\Rider\Niemi"

# Run deployment script
.\deployment\deploy-from-windows.ps1 -ServerIP <your-server-ip> -Username george
```

The script will:
- ✅ Publish the application
- ✅ Transfer files to server
- ✅ Configure and start services
- ✅ Set up Nginx

### Option B: Manual Deployment

**Step 1: Publish** (on Windows):
```powershell
cd "C:\Users\GeorgeJohnsson\OneDrive - Clearweb AB\Dokument\Rider\Niemi"
dotnet publish Niemi/Niemi.csproj -c Release -r linux-x64 --self-contained false -o .\publish
```

**Step 2: Transfer Files**:
```powershell
scp -r .\publish\* george@<server-ip>:/var/www/niemi-api/
scp -r .\deployment\* george@<server-ip>:/tmp/deployment/
```

**Step 3: Deploy on Server**:
```bash
ssh george@<server-ip>

# Run deployment script
cd /tmp/deployment
chmod +x deploy.sh
sudo ./deploy.sh
```

## Part 3: Configure & Verify (5 minutes)

### 1. Configure Production Settings

```bash
sudo nano /var/www/niemi-api/appsettings.Production.json
```

Update connection strings:
```json
{
  "ConnectionStrings": {
    "AZURE_SQL_CONNECTION_STRING": "Server=tcp:yourserver.database.windows.net,1433;Database=yourdb;User ID=user;Password=pass;Encrypt=True;",
    "FirebirdConnection": "User=SYSDBA;Password=pass;Database=/path/to/db;ServerType=0;Port=3050"
  }
}
```

Save and exit (Ctrl+X, Y, Enter)

### 2. Restart Service

```bash
sudo systemctl restart niemi-api
```

### 3. Verify It's Working

```bash
# Check service status
sudo systemctl status niemi-api

# Test API
curl http://localhost/database
```

### 4. Test from Your Windows Machine

Open browser or use PowerShell:
```powershell
Invoke-WebRequest http://<server-ip>/database
```

## Part 4: Optional - SSL Certificate (5 minutes)

### Update Nginx with Your Domain

```bash
sudo nano /etc/nginx/sites-available/niemi-api
```

Change `server_name` to your domain:
```nginx
server_name api.yourdomain.com;
```

### Install SSL Certificate

```bash
# Install Certbot
sudo apt install -y certbot python3-certbot-nginx

# Get certificate
sudo certbot --nginx -d api.yourdomain.com
```

Follow the prompts. Certbot will automatically configure HTTPS.

## 🎉 You're Done!

Your API is now running at:
- HTTP: `http://<server-ip>/`
- HTTPS: `https://api.yourdomain.com/` (if SSL configured)

### Useful Commands

```bash
# View real-time logs
sudo journalctl -u niemi-api -f

# Restart service
sudo systemctl restart niemi-api

# Check status
sudo systemctl status niemi-api

# Restart Nginx
sudo systemctl restart nginx
```

### API Endpoints to Test

- `GET /database` - List available databases
- `GET /ordhuv?fromDate=2024-01-01&toDate=2024-01-31` - Get orders
- `GET /categories` - Get keyword categories

## Troubleshooting

**Service won't start:**
```bash
sudo journalctl -u niemi-api -n 50
```

**Can't connect:**
```bash
# Check if service is listening
sudo netstat -tulpn | grep :5000

# Check Nginx
sudo nginx -t
sudo systemctl status nginx
```

**Database connection issues:**
- Verify connection strings in appsettings.Production.json
- Check network connectivity to external databases
- Ensure Firebird client is installed

## Need Help?

See detailed guides:
- `HYPER-V-SETUP.md` - Full Hyper-V setup instructions
- `DEPLOYMENT-GUIDE.md` - Complete deployment guide
- `README.md` - Overview and troubleshooting

