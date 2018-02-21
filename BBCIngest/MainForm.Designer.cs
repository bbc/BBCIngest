namespace BBCIngest
{
    partial class MainForm
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
            fetcher.Dispose();
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonSettings = new System.Windows.Forms.Button();
            this.buttonRfTS = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.buttonRemoveTasks = new System.Windows.Forms.Button();
            this.buttonExitOrStart = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::BBCIngest.Properties.Resources.ws_tile;
            this.pictureBox1.Location = new System.Drawing.Point(8, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(381, 170);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 190);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 13);
            this.label1.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 207);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(0, 13);
            this.label2.TabIndex = 2;
            // 
            // buttonSettings
            // 
            this.buttonSettings.Location = new System.Drawing.Point(8, 232);
            this.buttonSettings.Name = "buttonSettings";
            this.buttonSettings.Size = new System.Drawing.Size(67, 23);
            this.buttonSettings.TabIndex = 3;
            this.buttonSettings.Text = "Settings";
            this.buttonSettings.UseVisualStyleBackColor = true;
            this.buttonSettings.Click += new System.EventHandler(this.buttonSettings_Click);
            // 
            // buttonRfTS
            // 
            this.buttonRfTS.Location = new System.Drawing.Point(82, 232);
            this.buttonRfTS.Name = "buttonRfTS";
            this.buttonRfTS.Size = new System.Drawing.Size(131, 23);
            this.buttonRfTS.TabIndex = 4;
            this.buttonRfTS.Text = "Update Task Scheduler";
            this.buttonRfTS.UseVisualStyleBackColor = true;
            this.buttonRfTS.Click += new System.EventHandler(this.buttonRfTS_Click);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // buttonRemoveTasks
            // 
            this.buttonRemoveTasks.Location = new System.Drawing.Point(219, 232);
            this.buttonRemoveTasks.Name = "buttonRemoveTasks";
            this.buttonRemoveTasks.Size = new System.Drawing.Size(101, 23);
            this.buttonRemoveTasks.TabIndex = 7;
            this.buttonRemoveTasks.Text = "Remove Tasks";
            this.buttonRemoveTasks.UseVisualStyleBackColor = true;
            this.buttonRemoveTasks.Click += new System.EventHandler(this.buttonRemoveTasks_Click);
            // 
            // buttonExitOrStart
            // 
            this.buttonExitOrStart.Location = new System.Drawing.Point(326, 232);
            this.buttonExitOrStart.Name = "buttonExitOrStart";
            this.buttonExitOrStart.Size = new System.Drawing.Size(63, 23);
            this.buttonExitOrStart.TabIndex = 8;
            this.buttonExitOrStart.Text = "Exit";
            this.buttonExitOrStart.UseVisualStyleBackColor = true;
            this.buttonExitOrStart.Click += new System.EventHandler(this.buttonExitOrStart_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(397, 261);
            this.Controls.Add(this.buttonExitOrStart);
            this.Controls.Add(this.buttonRemoveTasks);
            this.Controls.Add(this.buttonRfTS);
            this.Controls.Add(this.buttonSettings);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "BBC Ingest";
            this.Load += new System.EventHandler(this.OnLoad);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonSettings;
        private System.Windows.Forms.Button buttonRfTS;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button buttonRemoveTasks;
        private System.Windows.Forms.Button buttonExitOrStart;
    }
}

