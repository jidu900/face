namespace demo_csharp
{
    partial class FaceTEST
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FaceTEST));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.wornglab = new System.Windows.Forms.Label();
            this.mess1 = new System.Windows.Forms.Label();
            this.mess = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pictureBox1.Location = new System.Drawing.Point(-5, -3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(840, 504);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // wornglab
            // 
            this.wornglab.AutoSize = true;
            this.wornglab.Font = new System.Drawing.Font("微软雅黑", 42F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.wornglab.ForeColor = System.Drawing.Color.Black;
            this.wornglab.Location = new System.Drawing.Point(12, 511);
            this.wornglab.Name = "wornglab";
            this.wornglab.Size = new System.Drawing.Size(312, 75);
            this.wornglab.TabIndex = 1;
            this.wornglab.Text = "系统运行中";
            // 
            // mess1
            // 
            this.mess1.AutoSize = true;
            this.mess1.Font = new System.Drawing.Font("微软雅黑", 72F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.mess1.Location = new System.Drawing.Point(17, 692);
            this.mess1.Name = "mess1";
            this.mess1.Size = new System.Drawing.Size(0, 124);
            this.mess1.TabIndex = 1;
            // 
            // mess
            // 
            this.mess.AutoSize = true;
            this.mess.Font = new System.Drawing.Font("微软雅黑", 72F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.mess.Location = new System.Drawing.Point(17, 523);
            this.mess.Name = "mess";
            this.mess.Size = new System.Drawing.Size(0, 128);
            this.mess.TabIndex = 1;
            // 
            // FaceTEST
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(834, 961);
            this.Controls.Add(this.mess);
            this.Controls.Add(this.mess1);
            this.Controls.Add(this.wornglab);
            this.Controls.Add(this.pictureBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FaceTEST";
            this.Text = "人脸识别系统V20190429";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FaceTEST_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label wornglab;
        private System.Windows.Forms.Label mess1;
        private System.Windows.Forms.Label mess;
    }
}