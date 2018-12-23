using System;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.ComponentModel;

namespace QLicense.Windows.Controls
{
    public partial class LicenseInfoControl : UserControl
    {
        public string DateFormat { get; set; }

        public string DateTimeFormat { get; set; }

        public LicenseInfoControl()
        {
            InitializeComponent();
        }

        public void ShowTextOnly(string text)
        {
            txtLicInfo.Text = text.Trim();
        }

        public void ShowLicenseInfo(LicenseEntity license)
        {
            ShowLicenseInfo(license, string.Empty);
        }


        public void ShowLicenseInfo(LicenseEntity license, string additionalInfo)
        {
            try
            {
                StringBuilder _sb = new StringBuilder(512);

                Type _typeLic = license.GetType();
                PropertyInfo[] _props = _typeLic.GetProperties();

                object _value = null;
                string _formatedValue = string.Empty;
                foreach (PropertyInfo _p in _props)
                {
                    try
                    {
                        ShowInLicenseInfoAttribute _showAttr = (ShowInLicenseInfoAttribute)Attribute.GetCustomAttribute(_p, typeof(ShowInLicenseInfoAttribute));
                        if (_showAttr != null && _showAttr.ShowInLicenseInfo)
                        {
                            _value = _p.GetValue(license, null);
                            _sb.Append(_showAttr.DisplayAs);
                            _sb.Append(": ");

                            //Append value and apply the format   
                            if (_value != null)
                            {
                                switch (_showAttr.DataFormatType)
                                {
                                    case ShowInLicenseInfoAttribute.FormatType.String:
                                        _formatedValue = _value.ToString();
                                        break;
                                    case ShowInLicenseInfoAttribute.FormatType.Date:
                                        if (_p.PropertyType == typeof(DateTime) && !string.IsNullOrWhiteSpace(DateFormat))
                                        {
                                            _formatedValue = ((DateTime)_value).ToString(DateFormat);
                                        }
                                        else
                                        {
                                            _formatedValue = _value.ToString();
                                        }
                                        break;
                                    case ShowInLicenseInfoAttribute.FormatType.DateTime:
                                        if (_p.PropertyType == typeof(DateTime) && !string.IsNullOrWhiteSpace(DateTimeFormat))
                                        {
                                            _formatedValue = ((DateTime)_value).ToString(DateTimeFormat);
                                        }
                                        else
                                        {
                                            _formatedValue = _value.ToString();
                                        }
                                        break;
                                    case ShowInLicenseInfoAttribute.FormatType.EnumDescription:
                                        string _name = Enum.GetName(_p.PropertyType, _value);
                                        if (_name != null)
                                        {
                                            FieldInfo _fi = _p.PropertyType.GetField(_name);
                                            DescriptionAttribute _dna = (DescriptionAttribute)Attribute.GetCustomAttribute(_fi, typeof(DescriptionAttribute));
                                            if (_dna != null)
                                                _formatedValue = _dna.Description;
                                            else
                                                _formatedValue = _value.ToString();
                                        }
                                        else
                                        {
                                            _formatedValue = _value.ToString();
                                        }
                                        break;
                                }

                                _sb.Append(_formatedValue);
                            }

                            _sb.Append("\r\n");
                        }
                    }
                    catch
                    {
                        //Ignore exeption
                    }
                }


                if (string.IsNullOrWhiteSpace(additionalInfo))
                {
                    _sb.Append(additionalInfo.Trim());
                }

                txtLicInfo.Text = _sb.ToString();
            }
            catch (Exception ex)
            {
                txtLicInfo.Text = ex.Message;
            }
        }
    }
}
