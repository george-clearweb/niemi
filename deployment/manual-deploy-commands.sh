#!/bin/bash
# Manual deployment commands for NIEMUB01
# Run these commands after SSH'ing into the server: ssh george@192.168.19.142

echo "Starting manual deployment..."

# Copy application files
sudo mkdir -p /var/www/niemi-api
sudo cp -r /tmp/niemi-publish/* /var/www/niemi-api/

# Set permissions
sudo chown -R www-data:www-data /var/www/niemi-api
sudo chmod +x /var/www/niemi-api/Niemi

# Install systemd service
sudo cp /tmp/niemi-deployment/niemi-api.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable niemi-api

# Configure Nginx
sudo cp /tmp/niemi-deployment/nginx-niemi-api.conf /etc/nginx/sites-available/niemi-api
sudo ln -sf /etc/nginx/sites-available/niemi-api /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx

# Create production config (you'll need to edit this with real values)
sudo nano /var/www/niemi-api/appsettings.Production.json

# Start the service
sudo systemctl start niemi-api
sudo systemctl status niemi-api

echo "Deployment complete! Check status above."

