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
            this.m_touchPanel = new GenericDemo.TouchControls.TouchPanel();
            this.m_touchButtonClose = new GenericDemo.TouchControls.TouchButton();
            this.m_touchRadioButton3 = new GenericDemo.TouchControls.TouchRadioButton();
            this.m_touchRadioButton2 = new GenericDemo.TouchControls.TouchRadioButton();
            this.m_touchRadioButton1 = new GenericDemo.TouchControls.TouchRadioButton();
            this.m_touchButtonAdd = new GenericDemo.TouchControls.TouchButton();
            this.m_listBox = new System.Windows.Forms.ListBox();
            this.m_touchButtonClear = new GenericDemo.TouchControls.TouchButton();
            this.m_touchPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_touchPanel
            // 
            this.m_touchPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.m_touchPanel.Controls.Add(this.m_touchButtonClose);
            this.m_touchPanel.Controls.Add(this.m_touchRadioButton3);
            this.m_touchPanel.Controls.Add(this.m_touchRadioButton2);
            this.m_touchPanel.Controls.Add(this.m_touchRadioButton1);
            this.m_touchPanel.Controls.Add(this.m_touchButtonAdd);
            this.m_touchPanel.Controls.Add(this.m_listBox);
            this.m_touchPanel.Controls.Add(this.m_touchButtonClear);
            this.m_touchPanel.Location = new System.Drawing.Point(121, 76);
            this.m_touchPanel.Name = "m_touchPanel";
            this.m_touchPanel.Size = new System.Drawing.Size(204, 149);
            this.m_touchPanel.TabIndex = 2;
            // 
            // m_touchButtonClose
            // 
            this.m_touchButtonClose.Location = new System.Drawing.Point(122, 118);
            this.m_touchButtonClose.Name = "m_touchButtonClose";
            this.m_touchButtonClose.Size = new System.Drawing.Size(75, 23);
            this.m_touchButtonClose.TabIndex = 7;
            this.m_touchButtonClose.Text = "Close";
            this.m_touchButtonClose.UseVisualStyleBackColor = true;
            this.m_touchButtonClose.Click += new System.EventHandler(this.touchButtonClose_Click);
            this.m_touchButtonClose.FingerTap += new Grafiti.GestureRecognizers.BasicMultiFingerEventHandler(this.OnTouchButtonClose_FingerTap);
            // 
            // m_touchRadioButton3
            // 
            this.m_touchRadioButton3.AutoSize = true;
            this.m_touchRadioButton3.Location = new System.Drawing.Point(4, 124);
            this.m_touchRadioButton3.Name = "m_touchRadioButton3";
            this.m_touchRadioButton3.Size = new System.Drawing.Size(32, 17);
            this.m_touchRadioButton3.TabIndex = 6;
            this.m_touchRadioButton3.Text = "C";
            this.m_touchRadioButton3.UseVisualStyleBackColor = true;
            // 
            // m_touchRadioButton2
            // 
            this.m_touchRadioButton2.AutoSize = true;
            this.m_touchRadioButton2.Location = new System.Drawing.Point(4, 102);
            this.m_touchRadioButton2.Name = "m_touchRadioButton2";
            this.m_touchRadioButton2.Size = new System.Drawing.Size(32, 17);
            this.m_touchRadioButton2.TabIndex = 6;
            this.m_touchRadioButton2.Text = "B";
            this.m_touchRadioButton2.UseVisualStyleBackColor = true;
            // 
            // m_touchRadioButton1
            // 
            this.m_touchRadioButton1.AutoSize = true;
            this.m_touchRadioButton1.Checked = true;
            this.m_touchRadioButton1.Location = new System.Drawing.Point(4, 79);
            this.m_touchRadioButton1.Name = "m_touchRadioButton1";
            this.m_touchRadioButton1.Size = new System.Drawing.Size(32, 17);
            this.m_touchRadioButton1.TabIndex = 6;
            this.m_touchRadioButton1.TabStop = true;
            this.m_touchRadioButton1.Text = "A";
            this.m_touchRadioButton1.UseVisualStyleBackColor = true;
            // 
            // m_touchButtonAdd
            // 
            this.m_touchButtonAdd.Location = new System.Drawing.Point(46, 76);
            this.m_touchButtonAdd.Name = "m_touchButtonAdd";
            this.m_touchButtonAdd.Size = new System.Drawing.Size(75, 23);
            this.m_touchButtonAdd.TabIndex = 5;
            this.m_touchButtonAdd.Text = "Add";
            this.m_touchButtonAdd.UseVisualStyleBackColor = true;
            this.m_touchButtonAdd.Click += new System.EventHandler(this.touchButtonAdd_Click);
            this.m_touchButtonAdd.FingerTap += new Grafiti.GestureRecognizers.BasicMultiFingerEventHandler(this.OnTouchButtonAdd_FingerTap);
            // 
            // m_listBox
            // 
            this.m_listBox.FormattingEnabled = true;
            this.m_listBox.Location = new System.Drawing.Point(3, 3);
            this.m_listBox.Name = "m_listBox";
            this.m_listBox.Size = new System.Drawing.Size(194, 69);
            this.m_listBox.TabIndex = 1;
            // 
            // m_touchButtonClear
            // 
            this.m_touchButtonClear.Location = new System.Drawing.Point(122, 76);
            this.m_touchButtonClear.Name = "m_touchButtonClear";
            this.m_touchButtonClear.Size = new System.Drawing.Size(75, 23);
            this.m_touchButtonClear.TabIndex = 0;
            this.m_touchButtonClear.Text = "Clear";
            this.m_touchButtonClear.UseVisualStyleBackColor = true;
            this.m_touchButtonClear.Click += new System.EventHandler(this.touchButtonClear_Click);
            this.m_touchButtonClear.FingerTap += new Grafiti.GestureRecognizers.BasicMultiFingerEventHandler(this.OnTouchButtonClear_FingerTap);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(471, 323);
            this.Controls.Add(this.m_touchPanel);
            this.KeyPreview = true;
            this.Name = "MainForm";
            this.ShowIcon = false;
            this.Text = "Grafiti Demo";
            this.SizeChanged += new System.EventHandler(this.OnResize);
            this.Resize += new System.EventHandler(this.OnResize);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnKeyDown);
            this.m_touchPanel.ResumeLayout(false);
            this.m_touchPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private GenericDemo.TouchControls.TouchPanel m_touchPanel;
        private GenericDemo.TouchControls.TouchButton m_touchButtonClose;
        private GenericDemo.TouchControls.TouchRadioButton m_touchRadioButton3;
        private GenericDemo.TouchControls.TouchRadioButton m_touchRadioButton2;
        private GenericDemo.TouchControls.TouchRadioButton m_touchRadioButton1;
        private GenericDemo.TouchControls.TouchButton m_touchButtonAdd;
        private System.Windows.Forms.ListBox m_listBox;
        private GenericDemo.TouchControls.TouchButton m_touchButtonClear;
    }
}