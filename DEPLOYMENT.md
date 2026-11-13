# Deployment Guide for Parking Rush

## AWS Deployment

### 1. AWS Infrastructure Setup

#### Required AWS Services
- EC2 for game servers
- S3 for asset storage
- CloudFront for CDN
- RDS for database
- Lambda for serverless functions
- CloudWatch for monitoring

#### Infrastructure as Code (Terraform)
```hcl
# main.tf
provider "aws" {
  region = "us-east-1"
}

resource "aws_instance" "game_server" {
  ami           = "ami-0c55b159cbfafe1f0"
  instance_type = "t2.medium"
  
  tags = {
    Name = "ParkingRush-GameServer"
  }
}

resource "aws_s3_bucket" "game_assets" {
  bucket = "parkingrush-assets"
  acl    = "private"
  
  versioning {
    enabled = true
  }
}

resource "aws_cloudfront_distribution" "cdn" {
  origin {
    domain_name = aws_s3_bucket.game_assets.bucket_regional_domain_name
    origin_id   = "S3-${aws_s3_bucket.game_assets.id}"
  }
  
  enabled = true
  default_root_object = "index.html"
}
```

### 2. CI/CD Pipeline Setup

#### GitHub Actions Workflow
```yaml
name: Deploy to AWS

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    
    - name: Configure AWS credentials
      uses: aws-actions/configure-aws-credentials@v1
      with:
        aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
        aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
        aws-region: us-east-1
    
    - name: Build Unity project
      uses: game-ci/unity-builder@v2
      with:
        unityVersion: 2022.3.0f1
        targetPlatform: WebGL
    
    - name: Deploy to S3
      run: |
        aws s3 sync ./Build s3://parkingrush-assets/ --delete
```

### 3. Database Setup

#### RDS Configuration
```sql
-- Create database
CREATE DATABASE parkingrush;

-- Create tables
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE,
    email VARCHAR(100) UNIQUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE game_progress (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id),
    level INTEGER,
    score INTEGER,
    completed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### 4. Monitoring Setup

#### CloudWatch Dashboard
```json
{
    "widgets": [
        {
            "type": "metric",
            "properties": {
                "metrics": [
                    ["AWS/EC2", "CPUUtilization", "InstanceId", "i-1234567890abcdef0"]
                ],
                "period": 300,
                "stat": "Average",
                "region": "us-east-1",
                "title": "EC2 CPU Utilization"
            }
        }
    ]
}
```

## Mobile Publishing

### 1. iOS Publishing

#### Requirements
1. Apple Developer Account ($99/year)
2. Xcode installed
3. iOS Developer Certificate
4. Provisioning Profile
5. App Store Connect Account

#### Build Process
1. Unity Build Settings:
   - Platform: iOS
   - Architecture: ARM64
   - Development Build: No
   - Scripting Backend: IL2CPP
   - Target SDK: Device SDK

2. Xcode Configuration:
```bash
# Export from Unity
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity -quit -batchmode -projectPath /path/to/project -executeMethod BuildScript.BuildiOS

# Archive in Xcode
xcodebuild -workspace ParkingRush.xcworkspace -scheme ParkingRush -configuration Release archive -archivePath ParkingRush.xcarchive

# Export IPA
xcodebuild -exportArchive -archivePath ParkingRush.xcarchive -exportOptionsPlist ExportOptions.plist -exportPath .
```

#### App Store Submission
1. Prepare App Store Assets:
   - App Icon (1024x1024)
   - Screenshots (6.5" and 5.5")
   - App Preview Videos
   - Description and Keywords
   - Privacy Policy URL

2. Submit through App Store Connect:
   - Create New App
   - Fill App Information
   - Upload Build
   - Submit for Review

### 2. Android Publishing

#### Requirements
1. Google Play Developer Account ($25 one-time)
2. Android Studio
3. Keystore file
4. Google Play Console access

#### Build Process
1. Unity Build Settings:
   - Platform: Android
   - Architecture: ARM64
   - Development Build: No
   - Scripting Backend: IL2CPP
   - Target API Level: 33
   - Minimum API Level: 24

2. Generate Keystore:
```bash
keytool -genkey -v -keystore parkingrush.keystore -alias parkingrush -keyalg RSA -keysize 2048 -validity 10000
```

3. Build APK/AAB:
```bash
# Export from Unity
/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity -quit -batchmode -projectPath /path/to/project -executeMethod BuildScript.BuildAndroid

# Sign APK
jarsigner -verbose -sigalg SHA1withRSA -digestalg SHA1 -keystore parkingrush.keystore app-release-unsigned.apk parkingrush

# Align APK
zipalign -v 4 app-release-unsigned.apk ParkingRush.apk
```

#### Play Store Submission
1. Prepare Store Assets:
   - App Icon (512x512)
   - Feature Graphic (1024x500)
   - Screenshots (Phone and Tablet)
   - Description and Keywords
   - Privacy Policy URL

2. Submit through Play Console:
   - Create New App
   - Fill Store Listing
   - Upload APK/AAB
   - Complete Content Rating
   - Submit for Review

## Post-Deployment

### 1. Monitoring
- Set up CloudWatch Alarms
- Monitor crash reports
- Track user engagement
- Analyze performance metrics

### 2. Updates
- Plan regular updates
- Test on all platforms
- Monitor user feedback
- Address issues promptly

### 3. Analytics
- Implement analytics tracking
- Monitor user behavior
- Track conversion rates
- Analyze retention

## Best Practices

### 1. Security
- Use HTTPS for all connections
- Implement proper authentication
- Secure API keys and secrets
- Regular security audits

### 2. Performance
- Optimize asset loading
- Implement caching
- Monitor memory usage
- Regular performance testing

### 3. User Experience
- Test on multiple devices
- Gather user feedback
- Monitor crash reports
- Regular updates and fixes

### 4. Compliance
- Follow platform guidelines
- Implement privacy policies
- Handle user data properly
- Regular compliance checks 