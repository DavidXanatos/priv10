namespace QLicense.Windows.Controls
{
    partial class LicenseStringContainer
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LicenseStringContainer));
            this.grpbxLicStr = new System.Windows.Forms.GroupBox();
            this.lnkSaveToFile = new System.Windows.Forms.LinkLabel();
            this.lnkCopy = new System.Windows.Forms.LinkLabel();
            this.txtLicense = new System.Windows.Forms.TextBox();
            this.dlgSaveFile = new System.Windows.Forms.SaveFileDialog();
            this.grpbxLicStr.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpbxLicStr
            // 
            this.grpbxLicStr.Controls.Add(this.lnkSaveToFile);
            this.grpbxLicStr.Controls.Add(this.lnkCopy);
            this.grpbxLicStr.Controls.Add(this.txtLicense);
            resources.ApplyResources(this.grpbxLicStr, "grpbxLicStr");
            this.grpbxLicStr.Name = "grpbxLicStr";
            this.grpbxLicStr.TabStop = false;
            // 
            // lnkSaveToFile
            // 
            resources.ApplyResources(this.lnkSaveToFile, "lnkSaveToFile");
            this.lnkSaveToFile.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lnkSaveToFile.Name = "lnkSaveToFile";
            this.lnkSaveToFile.TabStop = true;
            this.lnkSaveToFile.VisitedLinkColor = System.Drawing.Color.Blue;
            this.lnkSaveToFile.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkSaveToFile_LinkClicked);
            // 
            // lnkCopy
            // 
            resources.ApplyResources(this.lnkCopy, "lnkCopy");
            this.lnkCopy.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lnkCopy.Name = "lnkCopy";
            this.lnkCopy.TabStop = true;
            this.lnkCopy.VisitedLinkColor = System.Drawing.Color.Blue;
            this.lnkCopy.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkCopy_LinkClicked);
            // 
            // txtLicense
            // 
            resources.ApplyResources(this.txtLicense, "txtLicense");
            this.txtLicense.Name = "txtLicense";
            this.txtLicense.ReadOnly = true;
            // 
            // dlgSaveFile
            // 
            this.dlgSaveFile.FileName = "License.lic";
            resources.ApplyResources(this.dlgSaveFile, "dlgSaveFile");
            // 
            // LicenseStringContainer
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grpbxLicStr);
            this.Name = "LicenseStringContainer";
            this.grpbxLicStr.ResumeLayout(false);
            this.grpbxLicStr.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpbxLicStr;
        private System.Windows.Forms.LinkLabel lnkSaveToFile;
        private System.Windows.Forms.LinkLabel lnkCopy;
        private System.Windows.Forms.TextBox txtLicense;
        private System.Windows.Forms.SaveFileDialog dlgSaveFile;
    }
}
