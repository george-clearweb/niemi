# 🚀 Simple Deployment Guide - Update Niemi API

**Use this guide for quick deployments after the initial server setup is complete.**

This guide contains the exact commands that work, tested and verified.

---

## Prerequisites

- Server is already set up and running (NIEMUB01 at 192.168.19.142)
- You have SSH access to the server
- .NET 8.0 SDK installed on your Windows machine
- OpenSSH client installed on Windows

---

## Deployment Steps

### Step 1: Clean and Publish Locally

Open PowerShell in the project root directory:

```powershell
# Navigate to project (if not already there)
cd "C:\Users\GeorgeJohnsson\OneDrive - Clearweb AB\Dokument\Rider\Niemi"

# Remove old publish folder (if exists)
Remove-Item -Path .\publish -Recurse -Force -ErrorAction SilentlyContinue

# Publish application for Linux
dotnet publish .\Niemi -c Release -r linux-x64 --self-contained false -o .\publish
```

**Expected result:** Build succeeds and creates `.\publish` folder

---

### Step 2: Create Temp Directory on Server

```powershell
ssh george@192.168.19.142 "mkdir -p /tmp/niemi-publish"
```

**Expected result:** Directory created (or already exists)

---

### Step 3: Transfer Published Files

```powershell
scp -r .\publish\* george@192.168.19.142:/tmp/niemi-publish/
```

**You will be prompted for password.** Enter your server password.

**Expected result:** Files transferred successfully

---

### Step 4: SSH to Server

```powershell
ssh george@192.168.19.142
```

**You will be prompted for password.** Enter your server password.

**Expected result:** You're now connected to the server

---

### Step 5: Deploy on Server

Run these commands one by one on the server:

```bash
# Copy files to application directory
sudo cp -r /tmp/niemi-publish/* /var/www/niemi-api/

# Set correct ownership
sudo chown -R www-data:www-data /var/www/niemi-api

# Restart the API service
sudo systemctl restart niemi-api

# Check status
sudo systemctl status niemi-api
```

**Expected result:** 
- Service shows as "active (running)"
- Green status indicator
- "Application started" in logs

---

### Step 6: Verify Deployment

While still on the server:

```bash
# Test API locally
curl http://localhost/database

# View live logs (Ctrl+C to exit)
sudo journalctl -u niemi-api -f
```

**Expected result:** JSON response with database list

---

### Step 7: Exit Server

```bash
exit
```

---

### Step 8: Test from Windows

Back on your Windows machine:

```powershell
# Test API from Windows
curl http://192.168.19.142/database
```

**Expected result:** JSON response with database list

---

## Quick Reference Commands

### On Windows (PowerShell)

```powershell
# Full deployment in one go (copy-paste all)
cd "C:\Users\GeorgeJohnsson\OneDrive - Clearweb AB\Dokument\Rider\Niemi"
Remove-Item -Path .\publish -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish .\Niemi -c Release -r linux-x64 --self-contained false -o .\publish
ssh george@192.168.19.142 "mkdir -p /tmp/niemi-publish"
scp -r .\publish\* george@192.168.19.142:/tmp/niemi-publish/
ssh george@192.168.19.142
```

### On Server (after SSH)

```bash
# Deploy commands (copy-paste all)
sudo cp -r /tmp/niemi-publish/* /var/www/niemi-api/
sudo chown -R www-data:www-data /var/www/niemi-api
sudo systemctl restart niemi-api
sudo systemctl status niemi-api
```

---

## Troubleshooting

### "Could not resolve hostname"
- Use IP address instead: `192.168.19.142`
- Make sure server is running in Hyper-V

### "Permission denied (publickey)"
- You'll be prompted for password - this is normal
- Enter your server password when prompted

### Service fails to start
```bash
# View detailed error logs
sudo journalctl -u niemi-api -n 50 --no-pager
```

### Service is running but can't connect
```bash
# Check if port 5000 is listening
sudo netstat -tulpn | grep :5000

# Check Nginx status
sudo systemctl status nginx
```

---

## Git Workflow (Before Deploying)

Always commit and push your changes before deploying:

```powershell
git status
git add .
git commit -m "Your commit message"
git push
```

Then follow the deployment steps above.

---

## Notes

- **Server IP:** 192.168.19.142
- **Server Hostname:** NIEMUB01
- **Username:** george
- **Application Path:** /var/www/niemi-api
- **Service Name:** niemi-api
- **Log Command:** `sudo journalctl -u niemi-api -f`

---

## Related Documentation

- **Initial Setup:** See `QUICK-START-GUIDE.md` for first-time server setup
- **Configuration:** See `SCHEDULED-SERVICE-CONFIG.md` for scheduled service settings
- **Troubleshooting:** See `README.md` for detailed troubleshooting
- **Server Info:** See `SERVER-INFO.md` for server details

---

**Last Updated:** October 10, 2025

