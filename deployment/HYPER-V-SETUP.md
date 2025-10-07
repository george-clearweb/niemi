# Hyper-V Ubuntu Server Setup Guide

## Step 1: Create the Hyper-V Virtual Machine

### 1.1 Download Ubuntu Server
1. Download **Ubuntu Server 24.04 LTS** ISO from: https://ubuntu.com/download/server
   - Recommended: 24.04 LTS (supported until 2029)
   - Alternative: 22.04 LTS (supported until 2027)
2. Save the ISO to your Hyper-V host machine

### 1.2 Create New Virtual Machine in Hyper-V Manager

1. Open **Hyper-V Manager**
2. Click **Action > New > Virtual Machine**
3. Follow the wizard:

   - **Name**: `Niemi-API-Server`
   - **Generation**: Generation 2
   - **Memory**: 8192 MB (8 GB)
     - ☑ Use Dynamic Memory
   - **Networking**: Select your virtual switch
   - **Virtual Hard Disk**: 
     - Create new: 100 GB
     - Location: Choose your preferred location
   - **Installation Options**: 
     - Install from bootable image file
     - Browse to the Ubuntu Server ISO

4. Click **Finish**

### 1.3 Configure VM Settings (Before First Boot)

1. Right-click the VM > **Settings**
2. **Security** tab:
   - ☐ Uncheck "Enable Secure Boot" (or change to Microsoft UEFI Certificate Authority)
3. **Processor**:
   - Virtual processors: 4
4. **Network Adapter**:
   - Note the MAC address (you'll need this for static IP)
5. Click **Apply** and **OK**

## Step 2: Install Ubuntu Server

1. **Start the VM** and **Connect**
2. Follow Ubuntu installation:
   - Language: English
   - Keyboard: Your layout
   - Network: Configure static IP (recommended) or DHCP
   - Storage: Use entire disk
   - Profile Setup:
     - Your name: `george` (or your preference)
     - Server name: `NIEMUB01`
     - Username: `george`
     - Password: **[Choose strong password]**
   - SSH Setup: **☑ Install OpenSSH server**
   - Featured Server Snaps: Skip (we'll install manually)

3. Wait for installation to complete
4. **Reboot** when prompted
5. Remove the ISO after reboot (VM Settings > DVD Drive > None)

## Step 3: Initial Server Configuration

### 3.1 SSH into the Server

```bash
ssh george@<server-ip>
```

### 3.2 Update System

```bash
sudo apt update
sudo apt upgrade -y
sudo apt autoremove -y
```

### 3.3 Install Required Dependencies

```bash
# Install .NET 8.0 SDK and Runtime
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt update
sudo apt install -y dotnet-sdk-8.0 aspnetcore-runtime-8.0

# Install Nginx
sudo apt install -y nginx

# Install Firebird client libraries (for FirebirdSql.Data.FirebirdClient)
sudo apt install -y firebird3.0-utils libfbclient2

# Install other useful tools
sudo apt install -y curl git unzip
```

### 3.4 Verify .NET Installation

```bash
dotnet --version
# Should show: 8.0.x
```

## Step 4: Configure Firewall

```bash
# Enable UFW firewall
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable

# Check status
sudo ufw status
```

## Step 5: Create Application Directory

```bash
# Create directory for the application
sudo mkdir -p /var/www/niemi-api
sudo chown -R $USER:$USER /var/www/niemi-api
```

## Next Steps

Continue with the deployment guide: `DEPLOYMENT-GUIDE.md`

