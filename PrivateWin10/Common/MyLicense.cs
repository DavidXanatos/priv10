using QLicense;
using System;
using System.ComponentModel;
using System.Xml.Serialization;


public class MyLicense : QLicense.LicenseEntity
{
    [DisplayName("License Name")]
    [Category("License Options")]        
    [XmlElement("LicenseName")]
    [ShowInLicenseInfo(true, "License Name", ShowInLicenseInfoAttribute.FormatType.String)]
    public string LicenseName { get; set; }

    [DisplayName("License Number")]
    [Category("License Options")]
    [XmlElement("LicenseNumber")]
    [ShowInLicenseInfo(true, "License Number", ShowInLicenseInfoAttribute.FormatType.String)]
    public int LicenseNumber { get; set; }

    [DisplayName("Commercial Use")]
    [Category("License Options")]
    [XmlElement("CommercialUse")]
    [ShowInLicenseInfo(true, "Commercial Use", ShowInLicenseInfoAttribute.FormatType.String)]
    public bool CommercialUse { get; set; }

    [DisplayName("Support Level")]
    [Category("License Options")]        
    [XmlElement("SupportLevel")]
    [ShowInLicenseInfo(true, "Support Level", ShowInLicenseInfoAttribute.FormatType.String)]
    public int SupportLevel { get; set; }

    [DisplayName("Expiration Date")]
    [Category("License Options")]        
    [XmlElement("ExpirationDate")]
    [ShowInLicenseInfo(true, "Expiration Date", ShowInLicenseInfoAttribute.FormatType.String)]
    public DateTime ExpirationDate { get; set; }

    public LicenseStatus LicenseStatus = LicenseStatus.UNDEFINED;

    private static int[] voidNumbers = { };

    public MyLicense()
    {
        //Initialize app name for the license
        this.AppName = "PrivateWin10";
    }

    public override LicenseStatus DoExtraValidation(out string validationMsg)
    {
        LicenseStatus _licStatus = LicenseStatus.UNDEFINED;
        validationMsg = string.Empty;

        switch (this.Type)
        {
            case LicenseTypes.Single:
                //For Single License, check whether UID is matched
                if (this.UID == LicenseHandler.GenerateUID(this.AppName))
                {
                    _licStatus = LicenseStatus.VALID;
                }
                else
                {
                    validationMsg = "The license is NOT for this copy!";
                    _licStatus = LicenseStatus.INVALID;                    
                }
                break;
            case LicenseTypes.Volume:
                //No UID checking for Volume License
                _licStatus = LicenseStatus.VALID;
                break;
            default:
                validationMsg = "Invalid license";
                _licStatus= LicenseStatus.INVALID;
                break;
        }

        if (_licStatus == LicenseStatus.VALID)
        {
            if (WasVoided())
            {
                validationMsg = "This license number has been voided!";
                _licStatus = LicenseStatus.INVALID;
            }
            else if (HasExpired())
            {
                validationMsg = "This license has expired!";
                _licStatus = LicenseStatus.INVALID;
            }
        }

        return _licStatus;
    }

    public bool HasExpired()
    {
        if (ExpirationDate.Year >= 1970)
        //if (ExpirationDate > CreateDateTime)
        {
            if (ExpirationDate < DateTime.Now)
                return true;
        }
        return false;
    }

    public bool WasVoided()
    {
        foreach (int voidNumber in voidNumbers)
        {
            if (LicenseNumber == voidNumber)
                return true;
        }
        return false;
    }

    public string GetUID()
    {
        return LicenseHandler.GenerateUID(this.AppName);
    }
}
