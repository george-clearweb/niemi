#!/bin/bash

# Niemi API Deployment Script for Ubuntu Server
# This script automates the deployment process

set -e  # Exit on error

# Configuration
APP_NAME="niemi-api"
APP_DIR="/var/www/niemi-api"
SERVICE_NAME="niemi-api.service"
NGINX_CONF="niemi-api"
APP_USER="www-data"
APP_GROUP="www-data"

echo "========================================="
echo "Niemi API Deployment Script"
echo "========================================="

# Check if running with sudo
if [ "$EUID" -ne 0 ]; then 
    echo "Please run with sudo: sudo ./deploy.sh"
    exit 1
fi

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Step 1: Check prerequisites
echo ""
echo "Step 1: Checking prerequisites..."

if ! command_exists dotnet; then
    echo "❌ .NET is not installed. Please install .NET 8.0 first."
    exit 1
fi

if ! command_exists nginx; then
    echo "❌ Nginx is not installed. Please install Nginx first."
    exit 1
fi

echo "✅ Prerequisites met"

# Step 2: Stop existing service if running
echo ""
echo "Step 2: Stopping existing service..."

if systemctl is-active --quiet $SERVICE_NAME; then
    systemctl stop $SERVICE_NAME
    echo "✅ Service stopped"
else
    echo "ℹ️  Service is not running"
fi

# Step 3: Backup existing deployment
echo ""
echo "Step 3: Creating backup..."

if [ -d "$APP_DIR" ]; then
    BACKUP_DIR="${APP_DIR}.backup.$(date +%Y%m%d_%H%M%S)"
    cp -r $APP_DIR $BACKUP_DIR
    echo "✅ Backup created at: $BACKUP_DIR"
else
    echo "ℹ️  No existing deployment to backup"
fi

# Step 4: Create application directory
echo ""
echo "Step 4: Setting up application directory..."

mkdir -p $APP_DIR
echo "✅ Directory created: $APP_DIR"

# Step 5: Copy application files
echo ""
echo "Step 5: Copying application files..."

if [ -d "./publish" ]; then
    cp -r ./publish/* $APP_DIR/
    echo "✅ Application files copied"
else
    echo "❌ ./publish directory not found. Please publish the application first."
    echo "   Run: dotnet publish -c Release -o ./publish"
    exit 1
fi

# Step 6: Set permissions
echo ""
echo "Step 6: Setting permissions..."

chown -R $APP_USER:$APP_GROUP $APP_DIR
chmod +x $APP_DIR/Niemi
if [ -f "$APP_DIR/appsettings.Production.json" ]; then
    chmod 600 $APP_DIR/appsettings.Production.json
fi
echo "✅ Permissions set"

# Step 7: Install systemd service
echo ""
echo "Step 7: Installing systemd service..."

if [ -f "./niemi-api.service" ]; then
    cp ./niemi-api.service /etc/systemd/system/$SERVICE_NAME
    systemctl daemon-reload
    systemctl enable $SERVICE_NAME
    echo "✅ Service installed and enabled"
else
    echo "❌ niemi-api.service file not found"
    exit 1
fi

# Step 8: Configure Nginx
echo ""
echo "Step 8: Configuring Nginx..."

if [ -f "./nginx-niemi-api.conf" ]; then
    cp ./nginx-niemi-api.conf /etc/nginx/sites-available/$NGINX_CONF
    
    # Create symbolic link if it doesn't exist
    if [ ! -L "/etc/nginx/sites-enabled/$NGINX_CONF" ]; then
        ln -s /etc/nginx/sites-available/$NGINX_CONF /etc/nginx/sites-enabled/
    fi
    
    # Test Nginx configuration
    nginx -t
    echo "✅ Nginx configured"
else
    echo "⚠️  nginx-niemi-api.conf file not found, skipping Nginx configuration"
fi

# Step 9: Start services
echo ""
echo "Step 9: Starting services..."

systemctl start $SERVICE_NAME
sleep 2

if systemctl is-active --quiet $SERVICE_NAME; then
    echo "✅ Application service started"
else
    echo "❌ Failed to start application service"
    echo "   Check logs: sudo journalctl -u $SERVICE_NAME -n 50"
    exit 1
fi

systemctl reload nginx
echo "✅ Nginx reloaded"

# Step 10: Verify deployment
echo ""
echo "Step 10: Verifying deployment..."

sleep 3

# Check if application is responding
if curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/database | grep -q "200\|404"; then
    echo "✅ Application is responding"
else
    echo "⚠️  Application may not be responding correctly"
    echo "   Check logs: sudo journalctl -u $SERVICE_NAME -f"
fi

# Final status
echo ""
echo "========================================="
echo "Deployment Complete!"
echo "========================================="
echo ""
echo "📊 Service Status:"
systemctl status $SERVICE_NAME --no-pager -l || true
echo ""
echo "📝 Useful Commands:"
echo "   View logs:        sudo journalctl -u $SERVICE_NAME -f"
echo "   Restart service:  sudo systemctl restart $SERVICE_NAME"
echo "   Stop service:     sudo systemctl stop $SERVICE_NAME"
echo "   Check status:     sudo systemctl status $SERVICE_NAME"
echo ""
echo "🌐 API should be accessible at:"
echo "   http://localhost/database"
echo "   http://<server-ip>/database"
echo ""
echo "⚠️  Don't forget to:"
echo "   1. Configure appsettings.Production.json with your connection strings"
echo "   2. Update Nginx server_name with your domain"
echo "   3. Install SSL certificate with: sudo certbot --nginx -d yourdomain.com"
echo ""

