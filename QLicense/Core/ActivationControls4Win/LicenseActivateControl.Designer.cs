namespace QLicense.Windows.Controls
{
    partial class LicenseActivateControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LicenseActivateControl));
            this.grpbxLicense = new System.Windows.Forms.GroupBox();
            this.txtLicense = new System.Windows.Forms.TextBox();
            this.grpbxUID = new System.Windows.Forms.GroupBox();
            this.lblUIDTip = new System.Windows.Forms.Label();
            this.lnkCopy = new System.Windows.Forms.LinkLabel();
            this.txtUID = new System.Windows.Forms.TextBox();
            this.grpbxLicense.SuspendLayout();
            this.grpbxUID.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpbxLicense
            // 
            resources.ApplyResources(this.grpbxLicense, "grpbxLicense");
            this.grpbxLicense.Controls.Add(this.txtLicense);
            this.grpbxLicense.Name = "grpbxLicense";
            this.grpbxLicense.TabStop = false;
            // 
            // txtLicense
            // 
            resources.ApplyResources(this.txtLicense, "txtLicense");
            this.txtLicense.Name = "txtLicense";
            // 
            // grpbxUID
            // 
            resources.ApplyResources(this.grpbxUID, "grpbxUID");
            this.grpbxUID.Controls.Add(this.lblUIDTip);
            this.grpbxUID.Controls.Add(this.lnkCopy);
            this.grpbxUID.Controls.Add(this.txtUID);
            this.grpbxUID.Name = "grpbxUID";
            this.grpbxUID.TabStop = false;
            // 
            // lblUIDTip
            // 
            resources.ApplyResources(this.lblUIDTip, "lblUIDTip");
            this.lblUIDTip.Name = "lblUIDTip";
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
            // txtUID
            // 
            resources.ApplyResources(this.txtUID, "txtUID");
            this.txtUID.Name = "txtUID";
            this.txtUID.ReadOnly = true;
            // 
            // LicenseActivateControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grpbxLicense);
            this.Controls.Add(this.grpbxUID);
            this.Name = "LicenseActivateControl";
            this.grpbxLicense.ResumeLayout(false);
            this.grpbxLicense.PerformLayout();
            this.grpbxUID.ResumeLayout(false);
            this.grpbxUID.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpbxLicense;
        private System.Windows.Forms.TextBox txtLicense;
        private System.Windows.Forms.GroupBox grpbxUID;
        private System.Windows.Forms.Label lblUIDTip;
        private System.Windows.Forms.LinkLabel lnkCopy;
        private System.Windows.Forms.TextBox txtUID;
    }
}
