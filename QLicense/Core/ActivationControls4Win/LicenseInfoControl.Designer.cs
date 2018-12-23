namespace QLicense.Windows.Controls
{
    partial class LicenseInfoControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LicenseInfoControl));
            this.grpbxLicInfo = new System.Windows.Forms.GroupBox();
            this.txtLicInfo = new System.Windows.Forms.TextBox();
            this.grpbxLicInfo.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpbxLicInfo
            // 
            resources.ApplyResources(this.grpbxLicInfo, "grpbxLicInfo");
            this.grpbxLicInfo.Controls.Add(this.txtLicInfo);
            this.grpbxLicInfo.Name = "grpbxLicInfo";
            this.grpbxLicInfo.TabStop = false;
            // 
            // txtLicInfo
            // 
            resources.ApplyResources(this.txtLicInfo, "txtLicInfo");
            this.txtLicInfo.Name = "txtLicInfo";
            this.txtLicInfo.ReadOnly = true;
            // 
            // LicenseInfoControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grpbxLicInfo);
            this.Name = "LicenseInfoControl";
            this.grpbxLicInfo.ResumeLayout(false);
            this.grpbxLicInfo.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpbxLicInfo;
        private System.Windows.Forms.TextBox txtLicInfo;
    }
}
