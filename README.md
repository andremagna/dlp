# DataLossPrevention – Sentinel Connector (Azure Function)

![C#](https://img.shields.io/badge/C%23-net6.0-blue)
![Azure Functions](https://img.shields.io/badge/Azure-Functions%20v4-orange)
![Office 365](https://img.shields.io/badge/API-Office%20365%20Management-green)
![Microsoft Graph](https://img.shields.io/badge/API-Microsoft%20Graph-blue)
![Status](https://img.shields.io/badge/Status-Production%20Ready-brightgreen)

---

## 📌 Overview

**DataLossPrevention** is an Azure Functions solution (.NET 6) that:

* Calls the **Office 365 Management Activity API** for DLP events every 5 minutes
* Enriches events with user data from **Microsoft Graph**
* Filters and maps DLP operations with custom fields: `DlpRuleMatch`, `DlpRuleUndo`, `DlpInfo`
* Forwards enriched events to **Azure Sentinel / Log Analytics** via HMAC-authenticated POST
* Persists execution state in **Azure Blob Storage** to prevent missed events

---

## 🏗️ Architecture

```text
Azure Function (Timer - every 5 min)
            ↓
Office 365 Management API
            ↓
Microsoft Graph API (user enrichment)
            ↓
Log Analytics / Azure Sentinel
            ↓
    Azure Blob Storage
```

---

## 📁 Repository Structure

```text
DataLossPrevention/
│
├── DataLossPrevention.sln
│
└── DataLossPrevention/
    ├── Function1.cs
    ├── DlpRootDataModel.cs
    ├── OfficeContentTypeDataModel.cs
    ├── host.json
    ├── local.settings.json
    └── Properties/
        └── launchSettings.json
```

---

## 📊 Data Sources

### Office 365 Management API

* DLP rule match events (`DlpRuleMatch`, `DlpRuleUndo`, `DlpInfo`)
* Workloads: **Exchange**, **SharePoint**, **OneDrive**, **MicrosoftTeams**, **Endpoint**
* Sensitive Information Type (SIT) details and detected values

### Microsoft Graph API

* User enrichment: `id`, `displayName`, `mail`, `department`, `companyName`, `onPremisesDistinguishedName`

---

## ⚙️ Configuration

File:

```
DataLossPrevention/local.settings.json
```

```json
{
  "Values": {
    "clientID": "<AZURE_AD_APP_CLIENT_ID>",
    "clientSecret": "<AZURE_AD_APP_CLIENT_SECRET>",
    "tenantGuid": "<TENANT_ID>",
    "domain": "<TENANT_DOMAIN>",
    "publisher": "<PUBLISHER_IDENTIFIER>",
    "contentTypes": "DLP.All",
    "workspaceID": "<LOG_ANALYTICS_WORKSPACE_ID>",
    "workspaceKey": "<LOG_ANALYTICS_PRIMARY_KEY>",
    "customLogName": "<SENTINEL_TABLE_NAME>",
    "Timeout": "540",
    "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING": "<AZURE_STORAGE_CONNECTION_STRING>"
  }
}
```

---

## 🔐 Authentication

* **Office 365 Management API** – OAuth2 client credentials (`manage.office.com`)
* **Microsoft Graph API** – OAuth2 client credentials (`graph.microsoft.com`)
* **Log Analytics** – HMAC-SHA256 shared key signature

---

## 🔑 Required Azure AD Permissions

| API | Permission |
|-----|-----------|
| Office 365 Management API | `ActivityFeed.Read` |
| Microsoft Graph | `User.Read.All` |

---

## ▶️ Execution

```bash
# Local development
cd DataLossPrevention
func start

# Deploy to Azure
az functionapp deployment source config-zip ...
```

---

## 🔄 ETL Workflow

1. **Timer fires** every 5 minutes
2. **State read** – Last processed timestamp from Azure Blob Storage
3. **O365 content** – Fetch available content for the time window
4. **Event filtering** – Keep only DLP rule match operations
5. **User enrichment** – Microsoft Graph call per event
6. **SIT extraction** – Sensitive Information Type details mapped per workload
7. **Log Analytics push** – HMAC-signed POST per event
8. **State write** – Update blob with new timestamp

---

## ⚡ Performance & Resilience

* Configurable execution **timeout** (env variable `Timeout` in seconds)
* Incremental polling with **1-minute sliding windows**
* Workload-aware payload mapping (Exchange, SharePoint, OneDrive, Teams, Endpoint)
* Weighted truncation of detected values to stay within Log Analytics column limits (32 KB)

---

## 📜 Logging

* Azure Functions built-in ILogger
* Execution steps logged at each steps
* Errors logged with exception messages

---

## 🔒 Security

* No secrets in code – all configuration via environment variables / App Settings
* HMAC-SHA256 signature for Log Analytics ingestion
* Azure AD OAuth2 client credentials flow

---

## 🚀 Roadmap

* Multi-tenant support
* Retry logic with exponential backoff
* Dead-letter queue for failed events
* Deployment via Bicep / ARM templates

---

## 📄 License

© 2026 - Andrea Magnaghi

---

## 👨‍💻 Version

```
1.0.0
```
