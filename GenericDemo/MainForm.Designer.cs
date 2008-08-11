namespace GenericDemo
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
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.touchPanel1 = new Grafiti.TouchControls.TouchPanel();
            this.touchButtonClose = new Grafiti.TouchControls.TouchButton();
            this.touchRadioButton3 = new Grafiti.TouchControls.TouchRadioButton();
            this.touchRadioButton2 = new Grafiti.TouchControls.TouchRadioButton();
            this.touchRadioButton1 = new Grafiti.TouchControls.TouchRadioButton();
            this.touchButtonAdd = new Grafiti.TouchControls.TouchButton();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.touchButtonClear = new Grafiti.TouchControls.TouchButton();
            this.touchPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // touchPanel1
            // 
            this.touchPanel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.touchPanel1.Controls.Add(this.touchButtonClose);
            this.touchPanel1.Controls.Add(this.touchRadioButton3);
            this.touchPanel1.Controls.Add(this.touchRadioButton2);
            this.touchPanel1.Controls.Add(this.touchRadioButton1);
            this.touchPanel1.Controls.Add(this.touchButtonAdd);
            this.touchPanel1.Controls.Add(this.listBox1);
            this.touchPanel1.Controls.Add(this.touchButtonClear);
            this.touchPanel1.Location = new System.Drawing.Point(44, 59);
            this.touchPanel1.Name = "touchPanel1";
            this.touchPanel1.Size = new System.Drawing.Size(204, 149);
            this.touchPanel1.TabIndex = 2;
            // 
            // touchButtonClose
            // 
            this.touchButtonClose.Location = new System.Drawing.Point(122, 118);
            this.touchButtonClose.Name = "touchButtonClose";
            this.touchButtonClose.Size = new System.Drawing.Size(75, 23);
            this.touchButtonClose.TabIndex = 7;
            this.touchButtonClose.Text = "Close";
            this.touchButtonClose.UseVisualStyleBackColor = true;
            // 
            // touchRadioButton3
            // 
            this.touchRadioButton3.AutoSize = true;
            this.touchRadioButton3.Location = new System.Drawing.Point(4, 124);
            this.touchRadioButton3.Name = "touchRadioButton3";
            this.touchRadioButton3.Size = new System.Drawing.Size(32, 17);
            this.touchRadioButton3.TabIndex = 6;
            this.touchRadioButton3.TabStop = true;
            this.touchRadioButton3.Text = "C";
            this.touchRadioButton3.UseVisualStyleBackColor = true;
            // 
            // touchRadioButton2
            // 
            this.touchRadioButton2.AutoSize = true;
            this.touchRadioButton2.Location = new System.Drawing.Point(4, 102);
            this.touchRadioButton2.Name = "touchRadioButton2";
            this.touchRadioButton2.Size = new System.Drawing.Size(32, 17);
            this.touchRadioButton2.TabIndex = 6;
            this.touchRadioButton2.TabStop = true;
            this.touchRadioButton2.Text = "B";
            this.touchRadioButton2.UseVisualStyleBackColor = true;
            // 
            // touchRadioButton1
            // 
            this.touchRadioButton1.AutoSize = true;
            this.touchRadioButton1.Location = new System.Drawing.Point(4, 79);
            this.touchRadioButton1.Name = "touchRadioButton1";
            this.touchRadioButton1.Size = new System.Drawing.Size(32, 17);
            this.touchRadioButton1.TabIndex = 6;
            this.touchRadioButton1.TabStop = true;
            this.touchRadioButton1.Text = "A";
            this.touchRadioButton1.UseVisualStyleBackColor = true;
            // 
            // touchButtonAdd
            // 
            this.touchButtonAdd.Location = new System.Drawing.Point(46, 76);
            this.touchButtonAdd.Name = "touchButtonAdd";
            this.touchButtonAdd.Size = new System.Drawing.Size(75, 23);
            this.touchButtonAdd.TabIndex = 5;
            this.touchButtonAdd.Text = "Add";
            this.touchButtonAdd.UseVisualStyleBackColor = true;
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(3, 3);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(194, 69);
            this.listBox1.TabIndex = 1;
            // 
            // touchButtonClear
            // 
            this.touchButtonClear.Location = new System.Drawing.Point(122, 76);
            this.touchButtonClear.Name = "touchButtonClear";
            this.touchButtonClear.Size = new System.Drawing.Size(75, 23);
            this.touchButtonClear.TabIndex = 0;
            this.touchButtonClear.Text = "Clear";
            this.touchButtonClear.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Controls.Add(this.touchPanel1);
            this.KeyPreview = true;
            this.Name = "MainForm";
            this.ShowIcon = false;
            this.Text = "Grafiti Demo";
            this.touchPanel1.ResumeLayout(false);
            this.touchPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Grafiti.TouchControls.TouchPanel touchPanel1;
        private Grafiti.TouchControls.TouchButton touchButtonClose;
        private Grafiti.TouchControls.TouchRadioButton touchRadioButton3;
        private Grafiti.TouchControls.TouchRadioButton touchRadioButton2;
        private Grafiti.TouchControls.TouchRadioButton touchRadioButton1;
        private Grafiti.TouchControls.TouchButton touchButtonAdd;
        private System.Windows.Forms.ListBox listBox1;
        private Grafiti.TouchControls.TouchButton touchButtonClear;
    }
}