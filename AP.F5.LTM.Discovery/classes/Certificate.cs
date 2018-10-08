using AP.F5.LTM.Discovery.Classes;
using iControl;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using System;

namespace AP.F5.LTM.Discovery.classes
{
    public class Certificate
    {

        // Certificate SCOM Object
        public CreatableEnterpriseManagementObject SCOM_Object;

        // Default Constructor
        public Certificate(ManagementKeyCertificateCertificateInformation_v2 certInfo, SyncFailoverGroup dg, string PartitionName)
        {

            string FullName = certInfo.certificate.cert_info.id;
            string ShortName = FullName.Substring(FullName.LastIndexOf("/") + 1);
            string Partition = FullName.Substring(1, FullName.LastIndexOf("/") - 1);

            // Get Type
            string CertType = "";
            switch (certInfo.certificate.cert_type)
            {
                case ManagementKeyCertificateCertificateType.CTYPE_CA_SIGNED_YES:
                    CertType = "Certificate Signed by CA";
                    break;
                case ManagementKeyCertificateCertificateType.CTYPE_CA_SIGNED_NO:
                    CertType = "Certificate NOT Signed by CA";
                    break;
                default:
                    CertType = "Unknown";
                    break;
            }

            // Get Key Type
            string KeyType = "";
            switch (certInfo.certificate.key_type)
            {
                case ManagementKeyCertificateKeyType.KTYPE_RSA_PRIVATE:
                    KeyType = "RSA Private Key";
                    break;
                case ManagementKeyCertificateKeyType.KTYPE_RSA_PUBLIC:
                    KeyType = "RSA Public Key";
                    break;
                case ManagementKeyCertificateKeyType.KTYPE_DSA_PRIVATE:
                    KeyType = "DSA Private Key";
                    break;
                case ManagementKeyCertificateKeyType.KTYPE_DSA_PUBLIC:
                    KeyType = "DSA Public Key";
                    break;
                case ManagementKeyCertificateKeyType.KTYPE_EC_PRIVATE:
                    KeyType = "EC Private Key";
                    break;
                case ManagementKeyCertificateKeyType.KTYPE_EC_PUBLIC:
                    KeyType = "EC Public Key";
                    break;
                default:
                    KeyType = "Unknown";
                    break;
            }

            // Get Expiration in Days
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime certExpiry = epoch.AddSeconds(certInfo.certificate.expiration_date);
            int ExpirationDays = (certExpiry - DateTime.Now).Days;


            // Create Certificate Management Pack Class
            ManagementPackClass mpc_Certificate = SCOM.GetManagementPackClass("AP.F5.LTM.Certificate");
            // Create New NetworkInterface Object
            SCOM_Object = new CreatableEnterpriseManagementObject(SCOM.m_managementGroup, mpc_Certificate);

            // Create Root Entity Class & Key Property
            ManagementPackClass mpc_Entity = SCOM.GetManagementPackClass("System.Entity");
            ManagementPackProperty mpp_EntityDisplayName = mpc_Entity.PropertyCollection["DisplayName"];
            SCOM_Object[mpp_EntityDisplayName].Value = ShortName;

            // Create SyncFailoverGroup Management Pack Class & Key Property
            ManagementPackClass mpc_SyncFailoverGroup = SCOM.GetManagementPackClass("AP.F5.LTM.SyncFailoverGroup");
            ManagementPackProperty mpp_SyncFailoverGroupKey = mpc_SyncFailoverGroup.PropertyCollection["Key"];
            SCOM_Object[mpp_SyncFailoverGroupKey].Value = dg.Key;

            // Create Partition Management Pack Class & Key Property
            ManagementPackClass mpc_Partition = SCOM.GetManagementPackClass("AP.F5.LTM.Partition");
            ManagementPackProperty mpp_PartitionName = mpc_Partition.PropertyCollection["Name"];
            SCOM_Object[mpp_PartitionName].Value = PartitionName;

            // Set Properties of Certificate
            // FullName (Key Property) 
            ManagementPackProperty mpp_CertificateFullName = mpc_Certificate.PropertyCollection["FullName"];
            SCOM_Object[mpp_CertificateFullName].Value = FullName;
            ManagementPackProperty mpp_CertificateName = mpc_Certificate.PropertyCollection["Name"];
            SCOM_Object[mpp_CertificateName].Value = ShortName;
            ManagementPackProperty mpp_CertificateEmail = mpc_Certificate.PropertyCollection["eMail"];
            SCOM_Object[mpp_CertificateEmail].Value = certInfo.certificate.cert_info.email;
            ManagementPackProperty mpp_CertificateFileName = mpc_Certificate.PropertyCollection["FileName"];
            SCOM_Object[mpp_CertificateFileName].Value = certInfo.file_name;
            ManagementPackProperty mpp_CertificateCertType = mpc_Certificate.PropertyCollection["CertType"];
            SCOM_Object[mpp_CertificateCertType].Value = CertType;
            ManagementPackProperty mpp_CertificateKeyType = mpc_Certificate.PropertyCollection["KeyType"];
            SCOM_Object[mpp_CertificateKeyType].Value = KeyType;
            ManagementPackProperty mpp_CertificateBitLength = mpc_Certificate.PropertyCollection["BitLength"];
            SCOM_Object[mpp_CertificateBitLength].Value = certInfo.certificate.bit_length;
            ManagementPackProperty mpp_CertificateVersion = mpc_Certificate.PropertyCollection["Version"];
            SCOM_Object[mpp_CertificateVersion].Value = certInfo.certificate.version;
            ManagementPackProperty mpp_CertificateSerialNumber = mpc_Certificate.PropertyCollection["SerialNumber"];
            SCOM_Object[mpp_CertificateSerialNumber].Value = certInfo.certificate.serial_number;
            ManagementPackProperty mpp_CertificateExpirationDate = mpc_Certificate.PropertyCollection["ExpirationDate"];
            SCOM_Object[mpp_CertificateExpirationDate].Value = certInfo.certificate.expiration_string;
            ManagementPackProperty mpp_CertificateExpirationDays = mpc_Certificate.PropertyCollection["ExpirationDays"];
            SCOM_Object[mpp_CertificateExpirationDays].Value = ExpirationDays;
            ManagementPackProperty mpp_CertificateDeviceGroup = mpc_Certificate.PropertyCollection["DeviceGroup"];
            SCOM_Object[mpp_CertificateDeviceGroup].Value = dg.Name;
            ManagementPackProperty mpp_CertificatePartition = mpc_Certificate.PropertyCollection["Partition"];
            SCOM_Object[mpp_CertificatePartition].Value = Partition;

            // Get Issuer Info
            ManagementPackProperty mpp_CertificateIssuerCommon = mpc_Certificate.PropertyCollection["IssuerCommonName"];
            SCOM_Object[mpp_CertificateIssuerCommon].Value = certInfo.certificate.issuer.common_name;
            ManagementPackProperty mpp_CertificateIssuerCountry = mpc_Certificate.PropertyCollection["IssuerCountryName"];
            SCOM_Object[mpp_CertificateIssuerCountry].Value = certInfo.certificate.issuer.country_name;
            ManagementPackProperty mpp_CertificateIssuerDivision = mpc_Certificate.PropertyCollection["IssuerDivisionName"];
            SCOM_Object[mpp_CertificateIssuerDivision].Value = certInfo.certificate.issuer.division_name;
            ManagementPackProperty mpp_CertificateIssuerLocality = mpc_Certificate.PropertyCollection["IssuerLocalityName"];
            SCOM_Object[mpp_CertificateIssuerLocality].Value = certInfo.certificate.issuer.locality_name;
            ManagementPackProperty mpp_CertificateIssuerOrganisation = mpc_Certificate.PropertyCollection["IssuerOrganisationName"];
            SCOM_Object[mpp_CertificateIssuerOrganisation].Value = certInfo.certificate.issuer.organization_name;
            ManagementPackProperty mpp_CertificateIssuerState = mpc_Certificate.PropertyCollection["IssuerStateName"];
            SCOM_Object[mpp_CertificateIssuerState].Value = certInfo.certificate.issuer.state_name;

            // Get Subject Info
            ManagementPackProperty mpp_CertificateSubjectCommon = mpc_Certificate.PropertyCollection["SubjectCommonName"];
            SCOM_Object[mpp_CertificateSubjectCommon].Value = certInfo.certificate.subject.common_name;
            ManagementPackProperty mpp_CertificateSubjectCountry = mpc_Certificate.PropertyCollection["SubjectCountryName"];
            SCOM_Object[mpp_CertificateSubjectCountry].Value = certInfo.certificate.subject.country_name;
            ManagementPackProperty mpp_CertificateSubjectDivision = mpc_Certificate.PropertyCollection["SubjectDivisionName"];
            SCOM_Object[mpp_CertificateSubjectDivision].Value = certInfo.certificate.subject.division_name;
            ManagementPackProperty mpp_CertificateSubjectLocality = mpc_Certificate.PropertyCollection["SubjectLocalityName"];
            SCOM_Object[mpp_CertificateSubjectLocality].Value = certInfo.certificate.subject.locality_name;
            ManagementPackProperty mpp_CertificateSubjectOrganisation = mpc_Certificate.PropertyCollection["SubjectOrganisationName"];
            SCOM_Object[mpp_CertificateSubjectOrganisation].Value = certInfo.certificate.subject.organization_name;
            ManagementPackProperty mpp_CertificateSubjectState = mpc_Certificate.PropertyCollection["SubjectStateName"];
            SCOM_Object[mpp_CertificateSubjectState].Value = certInfo.certificate.subject.state_name;

        }
    }
}
