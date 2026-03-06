using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;


namespace DataLossPrevention
{
    public class Function1
    {
        public static string azstoragestring = Environment.GetEnvironmentVariable("WEBSITE_CONTENTAZUREFILECONNECTIONSTRING");
        public static string AzureADAppClientId = Environment.GetEnvironmentVariable("clientID");
        public static string AzureADAppClientSecret = Environment.GetEnvironmentVariable("clientSecret");
        public static string AzureSentinelLogName = Environment.GetEnvironmentVariable("customLogName");
        public static string ContentType = Environment.GetEnvironmentVariable("contentTypes");
        public static string LogAnalyticsWorkspaceId = Environment.GetEnvironmentVariable("workspaceID");
        public static string LogAnalyticsPrimaryKey = Environment.GetEnvironmentVariable("workspaceKey");
        public static string PublisherIdentifier = Environment.GetEnvironmentVariable("publisher");
        public static string TenantDomain = Environment.GetEnvironmentVariable("domain");
        public static string TenantId = Environment.GetEnvironmentVariable("tenantGuid");
        public static int Timeout = int.Parse(Environment.GetEnvironmentVariable("Timeout"));

        [FunctionName("Function1")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed start at: {DateTime.Now}");

            // Timer for timeout
            Stopwatch internalTimer = Stopwatch.StartNew();

            // Get the current universal time in the default string format
            DateTime currentUTCtime = DateTime.UtcNow;
            log.LogInformation("Current Time: " + currentUTCtime.ToString("yyyy-MM-ddTHH:mm:ss"));

            // add last run time to blob file to ensure no missed packages
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(azstoragestring);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("lastlog");

            bool containerExists = container.ExistsAsync().GetAwaiter().GetResult();
            if (!containerExists)
            {
                container.CreateIfNotExistsAsync().GetAwaiter().GetResult();

                DateTime startTime = currentUTCtime;
                UploadDateTimeToBlob(container, "lastlog.log", startTime);
            }
            else
            {
                DateTime startTime = DownloadDateTimeFromBlob(container, "lastlog.log");
                DateTime endTime = startTime.AddMinutes(1);

                while (endTime <= currentUTCtime)
                {
                    log.LogInformation("Start Time: " + startTime.ToString("yyyy-MM-ddTHH:mm:ss") + " End Time: " + endTime.ToString("yyyy-MM-ddTHH:mm:ss"));
                    log.LogInformation("Fetching O365 data");

                    Task<bool> result = GetO365Data(startTime, endTime, log);
                    log.LogInformation($"Result:  {result}");

                    if (await result == true)
                    {
                        UploadDateTimeToBlob(container, "lastlog.log", endTime);
                        startTime = endTime;
                        endTime = endTime.AddMinutes(1);
                    }
                    else
                    {
                        break;
                    }

                    if (internalTimer.Elapsed.TotalSeconds >= Timeout)
                    {
                        log.LogInformation($"Timeout! Function has been running for {internalTimer.Elapsed.TotalSeconds} seconds. Timeout of {Timeout} reached.");
                        break;
                    }
                }
            }

            log.LogInformation($"C# Timer trigger function executed end at: {DateTime.Now}");
        }


        public static string BuildSignature(string message, string secret)
        {
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = Convert.FromBase64String(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hash = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hash);
            }
        }

        public static Task PostLogAnalyticsData(string signature, string date, string json, ILogger log)
        {
            try
            {
                string url = "https://" + LogAnalyticsWorkspaceId + ".ods.opinsights.azure.com/api/logs?api-version=2016-04-01";

                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Log-Type", AzureSentinelLogName);
                client.DefaultRequestHeaders.Add("Authorization", signature);
                client.DefaultRequestHeaders.Add("x-ms-date", date);
                //client.DefaultRequestHeaders.Add("time-generated-field", TimeStampField);

                HttpContent httpContent = new StringContent(json, Encoding.UTF8);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                Task<HttpResponseMessage> response = client.PostAsync(new Uri(url), httpContent);
                
                HttpContent responseContent = response.Result.Content;
                string result = responseContent.ReadAsStringAsync().Result;
            }
            catch (Exception excep)
            {
                log.LogInformation($"Error: PostLogAnalyticsData request returned the status: {excep.Message}");
            }

            return Task.CompletedTask;
        }

        public static async Task<string> GetMSOfficeAuthTokenAsync(ILogger log)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = "https://login.windows.net/" + TenantDomain + "/oauth2/token?api-version=1.0";
                client.Timeout = TimeSpan.FromMinutes(10);

                Dictionary<string, string> requestData = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "resource", "https://manage.office.com" },
                    { "client_id", AzureADAppClientId },
                    { "client_secret", AzureADAppClientSecret }
                };

                FormUrlEncodedContent content = new FormUrlEncodedContent(requestData);
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonContent = await response.Content.ReadAsStringAsync();

                    JsonDocument document = JsonDocument.Parse(jsonContent);
                    JsonElement root = document.RootElement;
                    string bearerToken = root.GetProperty("access_token").GetString();

                    return bearerToken;
                }
                else
                {
                    log.LogInformation($"Error: GetMSOfficeAuthTokenAsync request returned the status: {response.StatusCode}");
                    return "";
                }
            }
        }

        public static async Task<string> GetAvailableOfficeContentTypeAsync(string bearerTokenMicrosoftOffice, DateTime startTime, DateTime endTime, ILogger log)
        {
            using (HttpClient client = new HttpClient())
            {
                string baseUrl = "https://manage.office.com/api/v1.0/";
                string url = baseUrl + TenantId + "/activity/feed/subscriptions/content?contentType=" + ContentType + "&PublisherIdentifier=" + PublisherIdentifier + "&startTime=" + startTime + "&endTime=" + endTime;

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerTokenMicrosoftOffice);
                client.Timeout = TimeSpan.FromMinutes(10);

                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string jsonContent = await response.Content.ReadAsStringAsync();
                    return jsonContent;
                }
                else
                {
                    log.LogInformation($"Error: GetAvailableOfficeContentTypeAsync request returned the status: {response.StatusCode}");
                    return "";
                }
            }
        }

        public static async Task<string> GetOfficeContentTypeAsync(string bearerTokenMicrosoftOffice, string url, ILogger log)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerTokenMicrosoftOffice);
                client.Timeout = TimeSpan.FromMinutes(10);

                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string jsonContent = await response.Content.ReadAsStringAsync();
                    return jsonContent;
                }
                else
                {
                    log.LogInformation($"Error: GetOfficeContentTypeAsync request returned the status: {response.StatusCode}");
                    return "";
                }
            }
        }

        public static async Task<string> GetMSGraphAuthTokenAsync(ILogger log)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = "https://login.windows.net/" + TenantDomain + "/oauth2/token?api-version=1.0";
                client.Timeout = TimeSpan.FromMinutes(10);

                Dictionary<string, string> requestData = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "resource", "https://graph.microsoft.com" },
                    { "client_id", AzureADAppClientId },
                    { "client_secret", AzureADAppClientSecret }
                };

                FormUrlEncodedContent content = new FormUrlEncodedContent(requestData);
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonContent = await response.Content.ReadAsStringAsync();

                    JsonDocument document = JsonDocument.Parse(jsonContent);
                    JsonElement root = document.RootElement;
                    string bearerToken = root.GetProperty("access_token").GetString();

                    return bearerToken;
                }
                else
                {
                    log.LogInformation($"Error: GetMSGraphAuthTokenAsync request returned the status: {response.StatusCode}");
                    return "";
                }
            }
        }

        public static async Task<GraphUsersAzureADModel> GetUserDetailsAsync(string bearerTokenMicrosoftGraph, string mail, ILogger log)
        {
            using (HttpClient client = new HttpClient())
            {
                string query = "?$select=id,companyName,department,displayName,mail,onPremisesDistinguishedName&$filter=userPrincipalName";
                string url = "https://graph.microsoft.com/v1.0/users" + query + " eq '" + mail + "'";

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerTokenMicrosoftGraph);
                client.Timeout = TimeSpan.FromMinutes(10);

                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string jsonContent = await response.Content.ReadAsStringAsync();

                    var userRsponse = JsonConvert.DeserializeObject<dynamic>(jsonContent);
                    List<GraphUsersAzureADModel> userDetails = JsonConvert.DeserializeObject<List<GraphUsersAzureADModel>>(userRsponse.value.ToString());
                    if (userDetails.Count > 0)
                    {
                        GraphUsersAzureADModel user = userDetails[0];
                        return user;
                    }
                    else {
                        log.LogInformation($"GetUserDetailsAsync request returned the status: {response.StatusCode}");
                        return null; 
                    }
                }
                else
                {
                    log.LogInformation($"Error: GetUserDetailsAsync request returned the status: {response.StatusCode}");
                    return null;
                }
            }
        }

        public static async Task<bool> GetO365Data(DateTime startTime, DateTime endTime, ILogger log)
        {
            try
            {
                List<DlpRootDataModel> eventsList = new();
                List<DlpPolicySensitiveInformationDetails> myListDlpPolicySensitiveInformationDetails = new();
                List<DlpPolicySensitiveInformationDetectedDetails> myListDlpPolicySensitiveInformationDetectedDetails = new();

                string officeToken = await GetMSOfficeAuthTokenAsync(log);
                string availableOfficeContentType = await GetAvailableOfficeContentTypeAsync(officeToken, startTime, endTime, log);
                List<OfficeContentTypeDataModel> contentResult = JsonConvert.DeserializeObject<List<OfficeContentTypeDataModel>>(availableOfficeContentType);

                if (contentResult.Count() > 0)
                {
                    foreach (var obj in contentResult)
                    {
                        string dataContent = await GetOfficeContentTypeAsync(officeToken, obj.contentUri, log);
                        List<DlpRootDataModel> data = JsonConvert.DeserializeObject<List<DlpRootDataModel>>(dataContent);

                        foreach (var events in data)
                        {
                            if (events.Operation == "DlpRuleMatch" || events.Operation == "DlpRuleUndo" || events.Operation == "DlpInfo")
                            {
                                log.LogInformation("* 1st Step - Getting User Email *");
                                string mail = string.Empty;

                                if (events.Workload == "Exchange" || events.Workload == "MicrosoftTeams") { mail = events.ExchangeMetaData.From; }
                                else if (events.Workload == "SharePoint" || events.Workload == "OneDrive") { mail = events.SharePointMetaData.From; }
                                else { mail = events.UserId; }
                                if (string.IsNullOrEmpty(mail))
                                {
                                    log.LogInformation("mail is empty, setting it");
                                    mail = events.UserId;
                                }
                                log.LogInformation($"1st Step Completed: {mail}");

                                log.LogInformation("* 2nd Step - Getting Microsoft Graph Extra Info *");
                                if (!string.IsNullOrEmpty(mail))
                                {
                                    string graphToken = await GetMSGraphAuthTokenAsync(log);
                                    GraphUsersAzureADModel user = await GetUserDetailsAsync(graphToken, mail, log);
                                    if (user != null)
                                    {
                                        events.GraphUsersAzureADModel = new GraphUsersAzureADModel
                                        {
                                            Id = user.Id,
                                            UserPrincipalName = user.UserPrincipalName,
                                            DisplayName = user.DisplayName,
                                            Mail = user.Mail,
                                            CompanyName = user.CompanyName,
                                            Department = user.Department,
                                            OnPremisesDistinguishedName = user.OnPremisesDistinguishedName
                                        };
                                    }
                                }
                                log.LogInformation($"2nd Step Completed: {mail}");

                                log.LogInformation("3rd Step - Getting Office Extra Info *");
                                string policyName = events.PolicyDetails[0].PolicyName;
                                if (!string.IsNullOrEmpty(policyName))
                                {
                                    log.LogInformation($"Event: {policyName}");

                                    List<Rule> policyObject = events.PolicyDetails[0].Rules;

                                    string policySeverity = policyObject[0].Severity;
                                    string policyRuleName = policyObject[0].RuleName;
                                    List<string> policyActions = policyObject[0].Actions;

                                    events.DlpPolicyName = new DlpPolicyName() { PolicyName = policyName };
                                    events.DlpPolicySeverity = new DlpPolicySeverity() { PolicySeverity = policySeverity };
                                    events.DlpPolicyRuleName = new DlpPolicyRuleName() { PolicyRuleName = policyRuleName };
                                    events.DlpPolicyActions = new DlpPolicyActions() { PolicyActions = policyActions };

                                    List<SensitiveInformation> policySensitiveInformationTypeObject = new List<SensitiveInformation>();
                                    policySensitiveInformationTypeObject = policyObject[0].ConditionsMatched.SensitiveInformation;

                                    if (events.Workload != "Endpoint" && policySensitiveInformationTypeObject == null)
                                    {
                                        Console.WriteLine($"Classification Event: {policyName}");

                                        events.DlpPolicySensitiveInformationDetailsList = new DlpPolicySensitiveInformationDetailsList() { DlpPolicySensitiveInformationDetails = myListDlpPolicySensitiveInformationDetails };
                                        events.DlpPolicySensitiveInformationDetectedDetailsList = new DlpPolicySensitiveInformationDetectedDetailsList() { DlpPolicySensitiveInformationDetectedDetails = myListDlpPolicySensitiveInformationDetectedDetails };
                                    }
                                    else if (events.Workload == "Endpoint")
                                    {
                                        Console.WriteLine($"Endpoint Event: {policyName}");

                                        List<SensitiveInfoTypeData> endpointSensitiveInfoTypeDataObject = new List<SensitiveInfoTypeData>() { };
                                        endpointSensitiveInfoTypeDataObject = events.EndpointMetaData.SensitiveInfoTypeData;

                                        int detectedValuesTotalElement = 0;
                                        foreach (SensitiveInfoTypeData sit in endpointSensitiveInfoTypeDataObject)
                                        {
                                            List<DetectedValue> policySITDetectedValuesList = sit.SensitiveInformationDetectionsInfo.DetectedValues;
                                            detectedValuesTotalElement += policySITDetectedValuesList.Count;
                                        }
                                        Console.WriteLine($"detectedValuesTotalElement: {detectedValuesTotalElement}");

                                        int i = 0;
                                        foreach (SensitiveInfoTypeData sit in endpointSensitiveInfoTypeDataObject)
                                        {
                                            int policySITUniqueCount = sit.UniqueCount;
                                            int policySITCount = sit.Count;
                                            int policySITConfidence = sit.Confidence;
                                            string policySITName = sit.SensitiveInfoTypeName;

                                            if (policySITUniqueCount == 0) { policySITUniqueCount = policySITCount; } // # i.e. "Workload":"MicrosoftTeams" don't have UniqueCount field

                                            DlpPolicySensitiveInformationDetails dlpPolicySensitiveInformationDetails = new DlpPolicySensitiveInformationDetails()
                                            {
                                                UniqueCount = policySITUniqueCount,
                                                Count = policySITCount,
                                                Confidence = policySITConfidence,
                                                SensitiveInformationTypeName = policySITName
                                            };
                                            myListDlpPolicySensitiveInformationDetails.Add(dlpPolicySensitiveInformationDetails);

                                            //check for macOS
                                            if (detectedValuesTotalElement > 0) 
                                            {
                                                bool truncatedFlag = false;
                                                var policySITDetectedValuesList = endpointSensitiveInfoTypeDataObject.Select(x => x.SensitiveInformationDetectionsInfo.DetectedValues).ToList(); // response contains max of 100 DetectedValues items for SIT

                                                int detectedValue = policySITDetectedValuesList[i].Count();
                                                Console.WriteLine($"detectedValue: {detectedValue}");

                                                int maxElements = (detectedValue * 80 / detectedValuesTotalElement); // detected values weighed in a list of max 80 elements (LogAnalytics column maxSize 32KB) https://learn.microsoft.com/en-us/azure/azure-monitor/logs/data-collector-api?tabs=c-sharp#return-codes
                                                Console.WriteLine($"maxElements: {maxElements}");
                                                int weightedMaxElement = maxElements - 1; // # handles the rounding of the detected values weighed
                                                List<DetectedValue> policySITDetectedValuesPreviewList = policySITDetectedValuesList[i].Take(weightedMaxElement + 1).ToList();

                                                if (detectedValue > weightedMaxElement) { truncatedFlag = true; }

                                                DlpPolicySensitiveInformationDetectedDetails dlpPolicySensitiveInformationDetectedDetails = new DlpPolicySensitiveInformationDetectedDetails()
                                                {
                                                    SensitiveInformationTypeName = policySITName,
                                                    SensitiveInformationDetections = policySITDetectedValuesPreviewList,
                                                    TruncatedFlag = truncatedFlag
                                                };
                                                myListDlpPolicySensitiveInformationDetectedDetails.Add(dlpPolicySensitiveInformationDetectedDetails);

                                                i++;
                                            }
                                        }
                                        events.DlpPolicySensitiveInformationDetailsList = new DlpPolicySensitiveInformationDetailsList() { DlpPolicySensitiveInformationDetails = myListDlpPolicySensitiveInformationDetails };
                                        events.DlpPolicySensitiveInformationDetectedDetailsList = new DlpPolicySensitiveInformationDetectedDetailsList() { DlpPolicySensitiveInformationDetectedDetails = myListDlpPolicySensitiveInformationDetectedDetails };

                                        if (events.EndpointMetaData.EnforcementMode == 1)
                                        {
                                            policyActions = new List<string> { "GenerateAlert" };
                                        }
                                        else if (events.EndpointMetaData.EnforcementMode == 4)
                                        {
                                            policyActions = new List<string> { "GenerateAlert", "NotifyUser", "BlockAccess" };
                                        }
                                        else
                                        {
                                            string justification = events.EndpointMetaData.Justification;
                                            if (string.IsNullOrEmpty(justification))
                                            {
                                                policyActions = new List<string> { "GenerateAlert", "NotifyUser", "BlockAccess" };
                                            }
                                            else
                                            {
                                                policyActions = new List<string> { "GenerateAlert" };
                                                // In EXO, populate OverriddenActions with (BlockAccess and NotifyUser) while TSO (BlockAccess, NotifyUser, and NotifyUser)
                                            }
                                        }
                                        events.DlpPolicyActions = new DlpPolicyActions { PolicyActions = policyActions };

                                        // do not use in Pilot, otherwise EnforcementMode will be always 1
                                        //if (events.EndpointMetaData.EnforcementMode == 1)
                                        //{
                                        //    policySeverity = "Low";
                                        //}
                                        //else if (events.EndpointMetaData.EnforcementMode == 4)
                                        //{
                                        //    policySeverity = "High";
                                        //}
                                        //else
                                        //{
                                        //    if (policyRuleName.Contains("Low"))
                                        //    {
                                        //        policySeverity = "Low";
                                        //    }
                                        //    else if (policyRuleName.Contains("High"))
                                        //    {
                                        //        policySeverity = "High";
                                        //    }
                                        //    else
                                        //    {
                                        //        policySeverity = "Medium";
                                        //    }
                                        //}
                                        //events.DlpPolicySeverity = new DlpPolicySeverity { PolicySeverity = policySeverity };
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Other Event: {policyName}");

                                        int detectedValuesTotalElement = 0;
                                        foreach (SensitiveInformation sit in policySensitiveInformationTypeObject)
                                        {
                                            List<DetectedValue> policySITDetectedValuesList = sit.SensitiveInformationDetections.DetectedValues;
                                            detectedValuesTotalElement += policySITDetectedValuesList.Count;
                                        }
                                        Console.WriteLine($"detectedValuesTotalElement: {detectedValuesTotalElement}");

                                        int i = 0;
                                        foreach (SensitiveInformation sit in policySensitiveInformationTypeObject)
                                        {
                                            int policySITUniqueCount = sit.UniqueCount;
                                            int policySITCount = sit.Count;
                                            int policySITConfidence = sit.Confidence;
                                            string policySITName = sit.SensitiveInformationTypeName;

                                            if (policySITUniqueCount == 0) { policySITUniqueCount = policySITCount; } // # i.e. "Workload":"MicrosoftTeams" don't have UniqueCount field

                                            DlpPolicySensitiveInformationDetails dlpPolicySensitiveInformationDetails = new DlpPolicySensitiveInformationDetails()
                                            {
                                                UniqueCount = policySITUniqueCount,
                                                Count = policySITCount,
                                                Confidence = policySITConfidence,
                                                SensitiveInformationTypeName = policySITName
                                            };
                                            myListDlpPolicySensitiveInformationDetails.Add(dlpPolicySensitiveInformationDetails);

                                            bool truncatedFlag = false;
                                            var policySITDetectedValuesList = policySensitiveInformationTypeObject.Select(x => x.SensitiveInformationDetections.DetectedValues).ToList(); // response contains max of 100 DetectedValues items for SIT

                                            int detectedValue = policySITDetectedValuesList[i].Count();
                                            Console.WriteLine($"detectedValue: {detectedValue}");

                                            int maxElements = (detectedValue * 80 / detectedValuesTotalElement); // detected values weighed in a list of max 80 elements (LogAnalytics column maxSize 32KB) https://learn.microsoft.com/en-us/azure/azure-monitor/logs/data-collector-api?tabs=c-sharp#return-codes
                                            Console.WriteLine($"maxElements: {maxElements}");
                                            int weightedMaxElement = maxElements - 1; // # handles the rounding of the detected values weighed
                                            List<DetectedValue> policySITDetectedValuesPreviewList = policySITDetectedValuesList[i].Take(weightedMaxElement + 1).ToList();

                                            if (detectedValue > weightedMaxElement) { truncatedFlag = true; }

                                            DlpPolicySensitiveInformationDetectedDetails dlpPolicySensitiveInformationDetectedDetails = new DlpPolicySensitiveInformationDetectedDetails()
                                            {
                                                SensitiveInformationTypeName = policySITName,
                                                SensitiveInformationDetections = policySITDetectedValuesPreviewList,
                                                TruncatedFlag = truncatedFlag
                                            };
                                            myListDlpPolicySensitiveInformationDetectedDetails.Add(dlpPolicySensitiveInformationDetectedDetails);

                                            i++;
                                        }
                                        events.DlpPolicySensitiveInformationDetailsList = new DlpPolicySensitiveInformationDetailsList() { DlpPolicySensitiveInformationDetails = myListDlpPolicySensitiveInformationDetails };
                                        events.DlpPolicySensitiveInformationDetectedDetailsList = new DlpPolicySensitiveInformationDetectedDetailsList() { DlpPolicySensitiveInformationDetectedDetails = myListDlpPolicySensitiveInformationDetectedDetails };
                                    }
                                    Console.WriteLine($"3rd Step Completed: {mail}");
                                }

                                eventsList.Add(events);
                            }
                        }
                    }
                }
                else { log.LogInformation("NO available content"); }
                
                foreach (var e in eventsList)
                {
                    log.LogInformation("4th Step - Writing Events in Table *");
                    var datestring = DateTime.UtcNow.ToString("r");
                    string jsonString = System.Text.Json.JsonSerializer.Serialize(e);
                    var jsonBytes = Encoding.UTF8.GetBytes(jsonString);
                    string stringToHash = "POST\n" + jsonBytes.Length + "\napplication/json\n" + "x-ms-date:" + datestring + "\n/api/logs";
                    string hashedString = BuildSignature(stringToHash, LogAnalyticsPrimaryKey);
                    string signature = "SharedKey " + LogAnalyticsWorkspaceId + ":" + hashedString;

                    await PostLogAnalyticsData(signature, datestring, jsonString, log);
                    log.LogInformation($"WS: {LogAnalyticsWorkspaceId}, Table: {AzureSentinelLogName}, Creation Time: {e.CreationTime}, EventId: {e.Id}");
                    log.LogInformation($"4th Step Completed");
                }
                return true;
            }
            catch (Exception excep)
            {
                log.LogInformation($"Error GetO365Data: {excep.Message}");
                return false;
            }
        }

        private static void UploadDateTimeToBlob(CloudBlobContainer container, string blobName, DateTime dateTimeValue)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
            string dateTimeStr = dateTimeValue.ToString("yyyy-MM-ddTHH:mm:ss");
            blockBlob.UploadTextAsync(dateTimeStr).GetAwaiter().GetResult();
        }

        private static DateTime DownloadDateTimeFromBlob(CloudBlobContainer container, string blobName)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            if (blockBlob.ExistsAsync().GetAwaiter().GetResult())
            {
                string dateTimeStr = blockBlob.DownloadTextAsync().GetAwaiter().GetResult();
                return DateTime.Parse(dateTimeStr);
            }
            else
            {
                return DateTime.MinValue;
            }
        }
    }
}
