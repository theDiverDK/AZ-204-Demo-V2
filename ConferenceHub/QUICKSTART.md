# ConferenceHub - Quick Start Guide

## 🚀 Running Locally (5 minutes)

### Prerequisites
- .NET 9.0 SDK installed
- VS Code or Visual Studio (optional)

### Steps
```powershell
# Navigate to project
cd "C:\Users\Admin\AZ-204 demo\ConferenceHub"

# Run the application
dotnet run
```

### Access Points
- **Home Page**: http://localhost:5053
- **Sessions**: http://localhost:5053/sessions
- **Organizer**: http://localhost:5053/organizer

## 📋 Demo Walkthrough (10 minutes)

### 1. Home Page Tour
- Show the landing page with three main features
- Explain the conference concept

### 2. Browse Sessions
- Click "View Sessions" or navigate to `/sessions`
- Show 8 pre-loaded sessions
- Point out session details: speaker, time, room, capacity

### 3. Register for a Session
- Click "View Details & Register" on any session
- Fill in registration form (Name + Email)
- Submit registration
- Show success message and updated capacity bar

### 4. Organizer Dashboard
- Navigate to `/organizer`
- Show list of all sessions with registration counts
- Click "View Registrations" to see who registered

### 5. Create New Session
- Click "Create New Session"
- Fill in the form:
  - Title: "Introduction to Azure DevOps"
  - Speaker: "Your Name"
  - Description: "Learn CI/CD with Azure"
  - Start/End Time: Select future date/time
  - Room: "Conference Room B"
  - Capacity: 50
- Save and see it appear in both organizer and public views

### 6. Edit a Session
- Click "Edit" on any session
- Change capacity or other details
- Save and verify changes

### 7. Delete a Session
- Click "Delete" on a session
- Show confirmation page with warning
- Confirm deletion

## 🎯 Key Learning Points for AZ-204

### Part 1 Topics Covered
✅ **ASP.NET Core Web App**
- MVC pattern
- Dependency Injection
- Razor Pages
- Tag Helpers

✅ **Data Management**
- JSON file storage
- In-memory collections
- Async/await patterns

✅ **Preparation for Azure**
- Service-based architecture (IDataService)
- Configuration patterns
- Separation of concerns

### What Makes This Azure-Ready?
1. **Stateless Design** - Ready for scale-out
2. **Service Layer** - Easy to swap implementations
3. **Async Operations** - Non-blocking I/O
4. **Configuration-Based** - Environment-aware settings

## 📊 Test Scenarios

### Scenario 1: Capacity Management
1. Register 100 attendees for "Building Cloud-Native Applications" (capacity: 100)
2. Try to register one more - should see "session is full" error

### Scenario 2: Data Persistence
1. Create a new session in Organizer
2. Stop the app (Ctrl+C)
3. Restart the app
4. Verify the new session still appears (saved to JSON)
5. Note: Registrations are in-memory only (will be lost)

### Scenario 3: Concurrent Access
1. Open multiple browser windows
2. Register different attendees for the same session
3. Verify capacity increments correctly

## 🔄 Evolution Plan (Future Learning Paths)

### Learning Path 2: Azure Storage
- [ ] Store session data in Azure Table Storage
- [ ] Upload speaker slides to Blob Storage
- [ ] Use Queue Storage for email notifications

### Learning Path 3: Azure Functions
- [ ] Background job for sending confirmation emails
- [ ] Timer trigger for session reminders

### Learning Path 4: Cosmos DB
- [ ] Replace in-memory registrations with Cosmos DB
- [ ] Global distribution support

### Learning Path 5: Security & Identity
- [ ] Azure AD B2C for attendee authentication
- [ ] Key Vault for connection strings
- [ ] Managed Identity for Azure services

### Learning Path 6: Monitoring
- [ ] Application Insights integration
- [ ] Custom telemetry for registrations
- [ ] Availability tests

### Learning Path 7: API Management
- [ ] Create REST API for mobile app
- [ ] API Management gateway
- [ ] Rate limiting and policies

### Learning Path 8: Messaging
- [ ] Service Bus for session updates
- [ ] Event Grid for real-time notifications

### Learning Path 9: Caching
- [ ] Redis Cache for session data
- [ ] CDN for static assets

### Learning Path 10: Containers
- [ ] Dockerize the application
- [ ] Deploy to Azure Container Instances
- [ ] Azure Kubernetes Service

### Learning Path 11: Advanced Features
- [ ] Cognitive Services for feedback analysis
- [ ] Logic Apps for workflow automation
- [ ] Azure SignalR for live updates

## 💡 Teaching Tips

### For Instructors
1. **Start Simple**: Just show the running app first
2. **Code Review**: Walk through key files (Program.cs, DataService.cs, Controllers)
3. **Azure Connection**: Explain how each component maps to Azure services
4. **Live Demo**: Deploy to Azure during class (takes ~5 minutes)

### Common Questions
**Q: Why JSON files instead of a database?**
A: We'll migrate to Azure Table Storage and Cosmos DB in learning paths 2 & 4. This shows the progression.

**Q: Why in-memory registrations?**
A: Demonstrates the limitation of local state. We'll fix this with Cosmos DB.

**Q: How does this scale?**
A: Currently doesn't (file writes, in-memory state). Each learning path removes these limitations.

## 🐛 Troubleshooting

### Port Already in Use
```powershell
# Change port in launchSettings.json or use:
dotnet run --urls "http://localhost:5555"
```

### JSON File Not Found
```powershell
# Ensure Data/sessions.json exists
# The app creates it if missing, but might be empty
```

### Bootstrap Icons Not Showing
- Check internet connection (loaded from CDN)
- Icons are optional, app works without them

## 📝 Modifications for Different Scenarios

### For a Different Conference Type
Edit `Data/sessions.json`:
- Medical Conference: Change sessions to medical topics
- DevOps Conference: Focus on CI/CD, automation topics
- Data Science Conference: ML, AI, data pipeline sessions

### For Different Business Logic
- Add speaker bio/photo support
- Add session ratings
- Add prerequisites for sessions
- Add multi-track support
- Add session conflicts detection

## 🎓 Assessment Ideas

### Beginner
- Add a "Contact" page
- Change the theme colors
- Add a footer with conference date

### Intermediate  
- Add session search functionality
- Add filtering by room or speaker
- Add session export to CSV

### Advanced
- Add speaker profile pages
- Add session feedback/ratings
- Add session prerequisites checking
- Add conflict detection (can't register for overlapping sessions)

---
**Ready to deploy to Azure? See DEPLOYMENT.md**

## Azure DevOps Pipeline (Infrastructure + App)
- Pipeline: `ConferenceHub/azure-pipelines.yml`
- Bicep: `ConferenceHub/infra/main.bicep`
- Required variables: `azureSubscription`, `resourceGroupName`, `location`, `appServicePlanName`, `webAppName`, `appServicePlanSku`, `appRuntime`
- Notes: Ensure `webAppName` is globally unique; the pipeline builds and zip-deploys the app after infra provisioning.
