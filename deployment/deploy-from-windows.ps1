# PowerShell Deployment Script for Niemi API
# Run this script from your Windows development machine to deploy to Ubuntu Server

param(
    [Parameter(Mandatory=$true)]
    [string]$ServerIP,
    
    [Parameter(Mandatory=$false)]
    [string]$Username = "george",
    
    [Parameter(Mandatory=$false)]
    [string]$ProjectPath = ".\Niemi"
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Niemi API - Windows to Linux Deployment" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check if dotnet is installed
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "❌ .NET SDK is not installed. Please install .NET 8.0 SDK first." -ForegroundColor Red
    exit 1
}

# Check if SCP is available (comes with OpenSSH client on Windows 10+)
if (-not (Get-Command scp -ErrorAction SilentlyContinue)) {
    Write-Host "❌ SCP is not available. Please install OpenSSH Client:" -ForegroundColor Red
    Write-Host "   Settings > Apps > Optional Features > OpenSSH Client" -ForegroundColor Yellow
    exit 1
}

# Step 1: Clean previous publish
Write-Host "Step 1: Cleaning previous publish..." -ForegroundColor Yellow
if (Test-Path ".\publish") {
    Remove-Item -Path ".\publish" -Recurse -Force
    Write-Host "✅ Cleaned previous publish" -ForegroundColor Green
}
Write-Host ""

# Step 2: Publish the application
Write-Host "Step 2: Publishing application for Linux..." -ForegroundColor Yellow
try {
    dotnet publish $ProjectPath -c Release -r linux-x64 --self-contained false -o .\publish
    Write-Host "✅ Application published successfully" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to publish application: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 3: Test SSH connection
Write-Host "Step 3: Testing SSH connection to $ServerIP..." -ForegroundColor Yellow
$testSSH = ssh -o ConnectTimeout=5 -o BatchMode=yes $Username@$ServerIP "echo ok" 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  SSH key authentication failed. You may need to enter password for subsequent operations." -ForegroundColor Yellow
    Write-Host "   Tip: Set up SSH key authentication for passwordless deployment:" -ForegroundColor Cyan
    Write-Host "   ssh-copy-id $Username@$ServerIP" -ForegroundColor Cyan
} else {
    Write-Host "✅ SSH connection successful" -ForegroundColor Green
}
Write-Host ""

# Step 4: Create temporary directory on server
Write-Host "Step 4: Preparing server directories..." -ForegroundColor Yellow
ssh $Username@$ServerIP "mkdir -p /tmp/niemi-publish /tmp/niemi-deployment"
Write-Host "✅ Server directories ready" -ForegroundColor Green
Write-Host ""

# Step 5: Transfer published files
Write-Host "Step 5: Transferring application files to server..." -ForegroundColor Yellow
Write-Host "   This may take a few moments..." -ForegroundColor Cyan
try {
    scp -r .\publish\* ${Username}@${ServerIP}:/tmp/niemi-publish/
    Write-Host "✅ Application files transferred" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to transfer application files: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 6: Transfer deployment scripts
Write-Host "Step 6: Transferring deployment scripts..." -ForegroundColor Yellow
try {
    scp -r .\deployment\* ${Username}@${ServerIP}:/tmp/niemi-deployment/
    Write-Host "✅ Deployment scripts transferred" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to transfer deployment scripts: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 7: Make scripts executable and move files
Write-Host "Step 7: Setting up deployment on server..." -ForegroundColor Yellow
ssh $Username@$ServerIP "chmod +x /tmp/niemi-deployment/*.sh"
Write-Host "✅ Scripts are now executable" -ForegroundColor Green
Write-Host ""

# Step 8: Run deployment script
Write-Host "Step 8: Running deployment script on server..." -ForegroundColor Yellow
Write-Host "   You may be prompted for sudo password..." -ForegroundColor Cyan
Write-Host ""

$deployCommand = @"
cd /tmp/niemi-deployment && \
sudo mkdir -p /var/www/niemi-api && \
sudo cp -r /tmp/niemi-publish/* /var/www/niemi-api/ && \
sudo chmod +x /tmp/niemi-deployment/deploy.sh && \
sudo /tmp/niemi-deployment/deploy.sh
"@

ssh -t $Username@$ServerIP $deployCommand

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=========================================" -ForegroundColor Green
    Write-Host "Deployment Complete!" -ForegroundColor Green
    Write-Host "=========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 Your API should be accessible at:" -ForegroundColor Cyan
    Write-Host "   http://$ServerIP/database" -ForegroundColor White
    Write-Host ""
    Write-Host "📝 Important Next Steps:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. Configure production settings:" -ForegroundColor White
    Write-Host "   ssh $Username@$ServerIP" -ForegroundColor Cyan
    Write-Host "   sudo nano /var/www/niemi-api/appsettings.Production.json" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "2. Update Nginx with your domain:" -ForegroundColor White
    Write-Host "   sudo nano /etc/nginx/sites-available/niemi-api" -ForegroundColor Cyan
    Write-Host "   sudo systemctl reload nginx" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "3. Install SSL certificate (recommended):" -ForegroundColor White
    Write-Host "   sudo certbot --nginx -d api.yourdomain.com" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "📚 Useful Commands:" -ForegroundColor Yellow
    Write-Host "   View logs:        ssh $Username@$ServerIP 'sudo journalctl -u niemi-api -f'" -ForegroundColor Cyan
    Write-Host "   Restart service:  ssh $Username@$ServerIP 'sudo systemctl restart niemi-api'" -ForegroundColor Cyan
    Write-Host "   Check status:     ssh $Username@$ServerIP 'sudo systemctl status niemi-api'" -ForegroundColor Cyan
    Write-Host ""
    
    # Test API endpoint
    Write-Host "🔍 Testing API endpoint..." -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest -Uri "http://$ServerIP/database" -UseBasicParsing -TimeoutSec 10 -ErrorAction SilentlyContinue
        Write-Host "✅ API is responding! (Status: $($response.StatusCode))" -ForegroundColor Green
    } catch {
        Write-Host "⚠️  Could not reach API endpoint. Please check the service status." -ForegroundColor Yellow
        Write-Host "   This is normal if you haven't configured the connection strings yet." -ForegroundColor Cyan
    }
    
} else {
    Write-Host ""
    Write-Host "❌ Deployment failed. Please check the error messages above." -ForegroundColor Red
    Write-Host ""
    Write-Host "📝 Troubleshooting:" -ForegroundColor Yellow
    Write-Host "   1. Check deployment logs: ssh $Username@$ServerIP 'sudo journalctl -u niemi-api -n 100'" -ForegroundColor Cyan
    Write-Host "   2. Verify server prerequisites are met (see HYPER-V-SETUP.md)" -ForegroundColor Cyan
    Write-Host "   3. Ensure you have sudo privileges on the server" -ForegroundColor Cyan
    exit 1
}

Write-Host ""
Write-Host "🎉 Thank you for using Niemi API deployment script!" -ForegroundColor Green
Write-Host ""

