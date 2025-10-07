#!/bin/bash

# Quick Start Script for Ubuntu Server Setup
# Run this script on a fresh Ubuntu Server 24.04 or 22.04 LTS installation

set -e

echo "========================================="
echo "Niemi API Server - Quick Setup"
echo "========================================="
echo ""

# Check if running with sudo
if [ "$EUID" -ne 0 ]; then 
    echo "Please run with sudo: sudo ./quick-start.sh"
    exit 1
fi

# Update system
echo "Step 1: Updating system..."
apt update
apt upgrade -y
apt autoremove -y
echo "✅ System updated"
echo ""

# Install .NET 8.0
echo "Step 2: Installing .NET 8.0..."
# Detect Ubuntu version
UBUNTU_VERSION=$(lsb_release -rs)
if [ "$UBUNTU_VERSION" = "24.04" ]; then
    wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
else
    wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
fi
dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
apt update
apt install -y dotnet-sdk-8.0 aspnetcore-runtime-8.0
echo "✅ .NET 8.0 installed"
echo ""

# Verify .NET installation
echo "Verifying .NET installation..."
dotnet --version
echo ""

# Install Nginx
echo "Step 3: Installing Nginx..."
apt install -y nginx
echo "✅ Nginx installed"
echo ""

# Install Firebird client
echo "Step 4: Installing Firebird client libraries..."
apt install -y firebird3.0-utils libfbclient2
echo "✅ Firebird client installed"
echo ""

# Install useful tools
echo "Step 5: Installing useful tools..."
apt install -y curl git unzip wget htop net-tools
echo "✅ Tools installed"
echo ""

# Configure firewall
echo "Step 6: Configuring firewall..."
ufw allow OpenSSH
ufw allow 'Nginx Full'
ufw --force enable
echo "✅ Firewall configured"
echo ""

# Create application directory
echo "Step 7: Creating application directory..."
mkdir -p /var/www/niemi-api
echo "✅ Application directory created at /var/www/niemi-api"
echo ""

# Set ownership
if [ -n "$SUDO_USER" ]; then
    chown -R $SUDO_USER:$SUDO_USER /var/www/niemi-api
    echo "✅ Ownership set to $SUDO_USER"
else
    echo "⚠️  Please set ownership manually: sudo chown -R \$USER:\$USER /var/www/niemi-api"
fi
echo ""

# Display system information
echo "========================================="
echo "Setup Complete!"
echo "========================================="
echo ""
echo "📊 System Information:"
echo "   OS:          $(lsb_release -d | cut -f2)"
echo "   .NET:        $(dotnet --version)"
echo "   Nginx:       $(nginx -v 2>&1 | cut -d'/' -f2)"
echo ""
echo "🌐 Network Information:"
echo "   Hostname:    $(hostname)"
echo "   IP Address:  $(hostname -I | awk '{print $1}')"
echo ""
echo "   SSH: ssh george@$(hostname -I | awk '{print $1}')"
echo "   Or:  ssh george@$(hostname)"
echo ""
echo "🔥 Firewall Status:"
ufw status
echo ""
echo "📁 Application Directory:"
echo "   /var/www/niemi-api"
echo ""
echo "📝 Next Steps:"
echo ""
echo "1. Transfer your published application to /var/www/niemi-api"
echo "   From your Windows machine, run:"
echo "   scp -r ./publish/* george@$(hostname -I | awk '{print $1}'):/var/www/niemi-api/"
echo ""
echo "2. Configure application settings:"
echo "   nano /var/www/niemi-api/appsettings.Production.json"
echo ""
echo "3. Run the deployment script:"
echo "   sudo ./deploy.sh"
echo ""
echo "4. (Optional) Install SSL certificate:"
echo "   sudo certbot --nginx -d yourdomain.com"
echo ""
echo "📚 For detailed instructions, see:"
echo "   cat DEPLOYMENT-GUIDE.md"
echo ""

