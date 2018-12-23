using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace QLicense
{
    /// <summary>
    /// This attribute defines whether the property of LicenseEntity object will be shown in LicenseInfoControl
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ShowInLicenseInfoAttribute : Attribute
    {
        public enum FormatType
        {
            String,
            Date,
            DateTime,
            EnumDescription,
        }

        protected bool _showInLicenseInfo = true;
        protected string _displayAs = string.Empty;
        protected FormatType _formatType = FormatType.String;

        public ShowInLicenseInfoAttribute()
        {
        }

        public ShowInLicenseInfoAttribute(bool showInLicenseInfo)
        {            
            if (showInLicenseInfo)
            {
                throw new Exception("When ShowInLicenseInfo is True, DisplayAs MUST have a value");
            }
            _showInLicenseInfo = showInLicenseInfo;
        }

        public ShowInLicenseInfoAttribute(bool showInLicenseInfo, string displayAs)
        {
            _showInLicenseInfo = showInLicenseInfo;
            _displayAs = displayAs;
        }
        public ShowInLicenseInfoAttribute(bool showInLicenseInfo, string displayAs, FormatType dataFormatType)
        {
            _showInLicenseInfo = showInLicenseInfo;
            _displayAs = displayAs;
            _formatType = dataFormatType;
        }

        public bool ShowInLicenseInfo
        {
            get
            {
                return _showInLicenseInfo;
            }
        }

        public string DisplayAs
        {
            get
            {
                return _displayAs;
            }
        }

        public FormatType DataFormatType
        {
            get
            {
                return _formatType;
            }
        }
    }


    public abstract class LicenseEntity
    {
        [Browsable(false)]
        [XmlIgnore]
        [ShowInLicenseInfo(false)]
        public string AppName { get; protected set; }

        [Browsable(false)]
        [XmlElement("UID")]
        [ShowInLicenseInfo(false)]
        public string UID { get; set; }

        [Browsable(false)]
        [XmlElement("Type")]
        [ShowInLicenseInfo(true, "Type", ShowInLicenseInfoAttribute.FormatType.EnumDescription)]
        public LicenseTypes Type { get; set; }

        [Browsable(false)]
        [XmlElement("CreateDateTime")]
        [ShowInLicenseInfo(true, "Creation Time", ShowInLicenseInfoAttribute.FormatType.DateTime)]
        public DateTime CreateDateTime { get; set; }

        /// <summary>
        /// For child class to do extra validation for those extended properties
        /// </summary>
        /// <param name="validationMsg"></param>
        /// <returns></returns>
        public abstract LicenseStatus DoExtraValidation(out string validationMsg);

    }
}
