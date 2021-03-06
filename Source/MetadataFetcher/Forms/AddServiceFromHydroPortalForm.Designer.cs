﻿namespace HydroDesktop.MetadataFetcher.Forms
{
	partial class AddServiceFromHydroPortalForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose ( bool disposing )
		{
			if ( disposing && (components != null) )
			{
				components.Dispose ();
			}
			base.Dispose ( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent ()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager ( typeof ( AddServiceFromHydroPortalForm ) );
			this.btnGetServices = new System.Windows.Forms.Button ();
			this.label1 = new System.Windows.Forms.Label ();
			this.txtUrl = new System.Windows.Forms.TextBox ();
			this.lblStatus = new System.Windows.Forms.Label ();
			this.SuspendLayout ();
			// 
			// btnGetServices
			// 
			this.btnGetServices.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnGetServices.Location = new System.Drawing.Point ( 214, 55 );
			this.btnGetServices.Name = "btnGetServices";
			this.btnGetServices.Size = new System.Drawing.Size ( 113, 23 );
			this.btnGetServices.TabIndex = 2;
			this.btnGetServices.Text = "&Get Services";
			this.btnGetServices.UseVisualStyleBackColor = true;
			this.btnGetServices.Click += new System.EventHandler ( this.btnGetServices_Click );
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point ( 12, 9 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size ( 102, 13 );
			this.label1.TabIndex = 1;
			this.label1.Text = "URL to HydroPortal:";
			// 
			// txtUrl
			// 
			this.txtUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.txtUrl.Location = new System.Drawing.Point ( 15, 25 );
			this.txtUrl.Name = "txtUrl";
			this.txtUrl.Size = new System.Drawing.Size ( 312, 20 );
			this.txtUrl.TabIndex = 1;
			// 
			// lblStatus
			// 
			this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lblStatus.AutoSize = true;
			this.lblStatus.Location = new System.Drawing.Point ( 63, 60 );
			this.lblStatus.Name = "lblStatus";
			this.lblStatus.Size = new System.Drawing.Size ( 145, 13 );
			this.lblStatus.TabIndex = 4;
			this.lblStatus.Text = "Reading Portal, please wait...";
			this.lblStatus.Visible = false;
			// 
			// AddServiceFromHydroPortalForm
			// 
			this.AcceptButton = this.btnGetServices;
			this.AutoScaleDimensions = new System.Drawing.SizeF ( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size ( 339, 90 );
			this.Controls.Add ( this.lblStatus );
			this.Controls.Add ( this.txtUrl );
			this.Controls.Add ( this.label1 );
			this.Controls.Add ( this.btnGetServices );
			this.Icon = ((System.Drawing.Icon)(resources.GetObject ( "$this.Icon" )));
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size ( 4000, 124 );
			this.MinimumSize = new System.Drawing.Size ( 293, 124 );
			this.Name = "AddServiceFromHydroPortalForm";
			this.Text = "Add Services From HydroPortal";
			this.ResumeLayout ( false );
			this.PerformLayout ();

		}

		#endregion

		private System.Windows.Forms.Button btnGetServices;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox txtUrl;
		private System.Windows.Forms.Label lblStatus;
	}
}