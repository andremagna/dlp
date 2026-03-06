using System;
using System.Collections.Generic;

namespace DataLossPrevention
{
    // EXCHASNGE - TEAMS - SHAREPOINT - ONDRIVE
    public class DlpRootDataModel
    {
        public string CreationTime { get; set; }
        public string Id { get; set; }
        public string Operation { get; set; }
        public string OrganizationId { get; set; }
        public int RecordType { get; set; }
        public string UserKey { get; set; }
        public int UserType { get; set; }
        public int Version { get; set; }
        public string Workload { get; set; }
        public string ObjectId { get; set; }
        public string UserId { get; set; }
        public string IncidentId { get; set; }
        public string ClientIP { get; set; }
        public bool SensitiveInfoDetectionIsIncluded { get; set; }
        public DlpPolicyName DlpPolicyName { get; set; }
        public DlpPolicyRuleName DlpPolicyRuleName { get; set; }
        public DlpPolicySeverity DlpPolicySeverity { get; set; }
        public DlpPolicyActions DlpPolicyActions { get; set; }
        public DlpPolicySensitiveInformationDetailsList DlpPolicySensitiveInformationDetailsList { get; set; }
        public DlpPolicySensitiveInformationDetectedDetailsList DlpPolicySensitiveInformationDetectedDetailsList { get; set; }
        public List<string> AssociatedAdminUnits { get; set; }
        public List<PolicyDetail> PolicyDetails { get; set; }
        public ExchangeMetaData ExchangeMetaData { get; set; }
        public SharePointMetaData SharePointMetaData { get; set; }
        public EndpointMetaData EndpointMetaData { get; set; }
        public GraphUsersAzureADModel GraphUsersAzureADModel { get; set; }
        public List<Rule> Rules { get; set; }
    }

    public class Rule
    {
        public List<string> ActionParameters { get; set; }
        public List<string> Actions { get; set; }
        public ConditionsMatched ConditionsMatched { get; set; }
        public string ManagementRuleId { get; set; }
        public string RuleId { get; set; }
        public string RuleMode { get; set; }
        public string RuleName { get; set; }
        public string Severity { get; set; }
    }

    public class ConditionsMatched
    {
        public bool ConditionMatchedInNewScheme { get; set; }
        public List<OtherCondition> OtherConditions { get; set; }
        public List<SensitiveInformation> SensitiveInformation { get; set; }
    }

    public class OtherCondition
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class SensitiveInformation
    {
        public string ClassifierType { get; set; }
        public int Confidence { get; set; }
        public int Count { get; set; }
        public int UniqueCount { get; set; }
        public List<SensitiveInformationDetailedClassificationAttribute> SensitiveInformationDetailedClassificationAttributes { get; set; }
        public SensitiveInformationDetections SensitiveInformationDetections { get; set; }
        public string SensitiveInformationTypeName { get; set; }
        public string SensitiveType { get; set; }
        public bool TruncatedFlag { get; set; }
    }

    public class SensitiveInformationDetailedClassificationAttribute
    {
        public int Confidence { get; set; }
        public int Count { get; set; }
        public bool IsMatch { get; set; }
    }

    public class SensitiveInformationDetections
    {
        public List<DetectedValue> DetectedValues { get; set; }
        public bool ResultsTruncated { get; set; }
    }

    public class DetectedValue
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class ExchangeMetaData
    {
        public int FileSize { get; set; }
        public string From { get; set; }
        public string MessageID { get; set; }
        public int RecipientCount { get; set; }
        public string Sent { get; set; }
        public string Subject { get; set; }
        public List<string> To { get; set; }
    }

    public class SharePointMetaData
    {
        public string FileID { get; set; }
        public string FileName { get; set; }
        public string FileOwner { get; set; }
        public string FilePathUrl { get; set; }
        public int FileSize { get; set; }
        public string From { get; set; }
        public bool IsViewableByExternalUsers { get; set; }
        public bool IsVisibleOnlyToOdbOwner { get; set; }
        public string ItemCreationTime { get; set; }
        public string ItemLastModifiedTime { get; set; }
        public string ItemLastSharedTime { get; set; }
        public List<object> SensitivityLabelIds { get; set; }
        public List<object> SharedBy { get; set; }
        public List<object> SiteAdmin { get; set; }
        public string SiteCollectionGuid { get; set; }
        public string SiteCollectionUrl { get; set; }
        public string UniqueID { get; set; }
    }

    public class PolicyDetail
    {
        public string PolicyId { get; set; }
        public string PolicyName { get; set; }
        public List<Rule> Rules { get; set; }
    }

    public class DlpPolicyName
    {
        public string PolicyName { get; set; }
    }
    public class DlpPolicyRuleName
    {
        public string PolicyRuleName { get; set; }
    }
    public class DlpPolicySeverity
    {
        public string PolicySeverity { get; set; }
    }
    public class DlpPolicyActions
    {
        public List<string> PolicyActions { get; set; }
    }

    public class DlpPolicySensitiveInformationDetails
    {
      
        public int Confidence { get; set; }
        public int Count { get; set; }
        public int UniqueCount { get; set; }
        public string SensitiveInformationTypeName { get; set; }
    }

    public class DlpPolicySensitiveInformationDetailsList
    {
        public List<DlpPolicySensitiveInformationDetails> DlpPolicySensitiveInformationDetails { get; set; }
    }

    public class DlpPolicySensitiveInformationDetectedDetailsList
    {
        public List<DlpPolicySensitiveInformationDetectedDetails> DlpPolicySensitiveInformationDetectedDetails { get; set; }
    }

    public class DlpPolicySensitiveInformationDetectedDetails
    {
        public List<DetectedValue> SensitiveInformationDetections { get; set; }
        public string SensitiveInformationTypeName { get; set; }
        public bool TruncatedFlag { get; set; }
    }

    public class GraphUsersAzureADModel
    {
        public List<string> BusinessPhones { get; set; }
        public string DisplayName { get; set; }
        public string GivenName { get; set; }
        public string JobTitle { get; set; }
        public string Mail { get; set; }
        public string MobilePhone { get; set; }
        public string CompanyName { get; set; }
        public string Department { get; set; }
        public string OnPremisesDistinguishedName { get; set; }
        public string OfficeLocation { get; set; }
        public string PreferredLanguage { get; set; }
        public string Surname { get; set; }
        public string UserPrincipalName { get; set; }
        public string Id { get; set; }
    }

    // ENDPOINT
    public class EndpointMetaData
    {
        public List<SensitiveInfoTypeData> SensitiveInfoTypeData { get; set; }
        public int SourceLocationType { get; set; }
        public int Platform { get; set; }
        public string Application { get; set; }
        public string FileExtension { get; set; }
        public string DeviceName { get; set; }
        public string MDATPDeviceId { get; set; }
        public string Sha1 { get; set; }
        public string Sha256 { get; set; }
        public int EnforcementMode { get; set; }
        public string Justification { get; set; }
        public string OriginatingDomain { get; set; }
        public string PreviousFileName { get; set; }
        public string TargetPrinterName { get; set; }
        public string TargetDomain { get; set; }
        public string TargetFilePath { get; set; }
        public int FileSize { get; set; }
        public string FileType { get; set; }
        public bool RMSEncrypted { get; set; }
        public bool Hidden { get; set; }
        public string EndpointOperation { get; set; }
        public string ParentArchiveHash { get; set; }
        public string GroupName { get; set; }
        public DlpAuditEventMetadata DlpAuditEventMetadata { get; set; }
        public bool JitTriggered { get; set; }
    }

    public class SensitiveInfoTypeData
    {
        public string SensitiveInfoTypeId { get; set; }
        public int Count { get; set; }
        public int Confidence { get; set; }
        public string SensitiveInfoTypeName { get; set; }
        public int UniqueCount { get; set; }
        public List<SensitiveInformationDetailedClassificationAttribute> SensitiveInformationDetailedClassificationAttributes { get; set; }
        public SensitiveInformationDetectionsInfo SensitiveInformationDetectionsInfo { get; set; }
        public string ClassifierType { get; set; }
        public string SensitiveTypeSource { get; set; }
    }
    public class SensitiveInformationDetectionsInfo
    {
        public List<DetectedValue> DetectedValues { get; set; }
    }

    public class DlpAuditEventMetadata
    {
        public string DlpPolicyMatchId { get; set; }
        public DateTime EvaluationTime { get; set; }
    }
}