namespace NetworkSim
{
    partial class MainForm
    {
        /// <summary>Обязательная переменная конструктора.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Освобождает используемые ресурсы.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        private System.Windows.Forms.Panel pnlSettings;
        private System.Windows.Forms.Label lblDataError;
        private System.Windows.Forms.ComboBox cmbDataError;
        private System.Windows.Forms.Label lblOutage;
        private System.Windows.Forms.ComboBox cmbOutage;
        private System.Windows.Forms.Label lblProcTime;
        private System.Windows.Forms.ComboBox cmbProcTime;
        private System.Windows.Forms.Label lblSpeed;
        private System.Windows.Forms.TrackBar trbSpeed;
        private System.Windows.Forms.CheckBox chkAuto;
        private System.Windows.Forms.Button btnStartStop;
        private System.Windows.Forms.Button btnSendTask;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnLog;
        private View.SimulationCanvas canvas;
        private System.Windows.Forms.ListBox lstLog;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblCounters;
        private System.Windows.Forms.ToolStripStatusLabel lblHint;

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте
        /// содержимое этого метода при помощи редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlSettings = new System.Windows.Forms.Panel();
            this.lblDataError = new System.Windows.Forms.Label();
            this.cmbDataError = new System.Windows.Forms.ComboBox();
            this.lblOutage = new System.Windows.Forms.Label();
            this.cmbOutage = new System.Windows.Forms.ComboBox();
            this.lblProcTime = new System.Windows.Forms.Label();
            this.cmbProcTime = new System.Windows.Forms.ComboBox();
            this.lblSpeed = new System.Windows.Forms.Label();
            this.trbSpeed = new System.Windows.Forms.TrackBar();
            this.chkAuto = new System.Windows.Forms.CheckBox();
            this.btnStartStop = new System.Windows.Forms.Button();
            this.btnSendTask = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnLog = new System.Windows.Forms.Button();
            this.canvas = new View.SimulationCanvas();
            this.lstLog = new System.Windows.Forms.ListBox();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblCounters = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblHint = new System.Windows.Forms.ToolStripStatusLabel();
            this.pnlSettings.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            this.lblDataError.AutoSize = true;
            this.lblDataError.Location = new System.Drawing.Point(10, 12);
            this.lblDataError.Name = "lblDataError";
            this.lblDataError.Text = "Вероятность ошибки данных:";
            this.cmbDataError.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDataError.FormattingEnabled = true;
            this.cmbDataError.Location = new System.Drawing.Point(203, 8);
            this.cmbDataError.Name = "cmbDataError";
            this.cmbDataError.Size = new System.Drawing.Size(58, 23);
            this.cmbDataError.TabIndex = 0;
            this.cmbDataError.SelectedIndexChanged +=
                new System.EventHandler(this.cmbDataError_SelectedIndexChanged);
            this.lblOutage.AutoSize = true;
            this.lblOutage.Location = new System.Drawing.Point(278, 12);
            this.lblOutage.Name = "lblOutage";
            this.lblOutage.Text = "Вероятность отказа БД:";
            this.cmbOutage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbOutage.FormattingEnabled = true;
            this.cmbOutage.Location = new System.Drawing.Point(431, 8);
            this.cmbOutage.Name = "cmbOutage";
            this.cmbOutage.Size = new System.Drawing.Size(58, 23);
            this.cmbOutage.TabIndex = 1;
            this.cmbOutage.SelectedIndexChanged +=
                new System.EventHandler(this.cmbOutage_SelectedIndexChanged);
            this.lblProcTime.AutoSize = true;
            this.lblProcTime.Location = new System.Drawing.Point(506, 12);
            this.lblProcTime.Name = "lblProcTime";
            this.lblProcTime.Text = "Время обработки, с:";
            this.cmbProcTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProcTime.FormattingEnabled = true;
            this.cmbProcTime.Location = new System.Drawing.Point(641, 8);
            this.cmbProcTime.Name = "cmbProcTime";
            this.cmbProcTime.Size = new System.Drawing.Size(50, 23);
            this.cmbProcTime.TabIndex = 2;
            this.cmbProcTime.SelectedIndexChanged +=
                new System.EventHandler(this.cmbProcTime_SelectedIndexChanged);
            this.lblSpeed.AutoSize = true;
            this.lblSpeed.Location = new System.Drawing.Point(10, 47);
            this.lblSpeed.Name = "lblSpeed";
            this.lblSpeed.Text = "Скорость:";
            this.trbSpeed.LargeChange = 1;
            this.trbSpeed.Location = new System.Drawing.Point(80, 40);
            this.trbSpeed.Maximum = 8;
            this.trbSpeed.Minimum = 1;
            this.trbSpeed.Name = "trbSpeed";
            this.trbSpeed.Size = new System.Drawing.Size(160, 30);
            this.trbSpeed.SmallChange = 1;
            this.trbSpeed.TabIndex = 3;
            this.trbSpeed.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trbSpeed.Value = 2;
            this.trbSpeed.ValueChanged +=
                new System.EventHandler(this.trbSpeed_ValueChanged);
            this.chkAuto.AutoSize = true;
            this.chkAuto.Location = new System.Drawing.Point(262, 46);
            this.chkAuto.Name = "chkAuto";
            this.chkAuto.Text = "Автоотправка задач";
            this.chkAuto.UseVisualStyleBackColor = true;
            this.chkAuto.CheckedChanged +=
                new System.EventHandler(this.chkAuto_CheckedChanged);
            this.btnStartStop.Location = new System.Drawing.Point(428, 40);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(90, 27);
            this.btnStartStop.TabIndex = 4;
            this.btnStartStop.Text = "Старт";
            this.btnStartStop.UseVisualStyleBackColor = true;
            this.btnStartStop.Click +=
                new System.EventHandler(this.btnStartStop_Click);
            this.btnSendTask.Location = new System.Drawing.Point(532, 40);
            this.btnSendTask.Name = "btnSendTask";
            this.btnSendTask.Size = new System.Drawing.Size(90, 27);
            this.btnSendTask.TabIndex = 5;
            this.btnSendTask.Text = "Задача";
            this.btnSendTask.UseVisualStyleBackColor = true;
            this.btnSendTask.Click +=
                new System.EventHandler(this.btnSendTask_Click);
            this.btnReset.Location = new System.Drawing.Point(636, 40);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(90, 27);
            this.btnReset.TabIndex = 6;
            this.btnReset.Text = "Сброс";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click +=
                new System.EventHandler(this.btnReset_Click);
            this.btnLog.Location = new System.Drawing.Point(748, 40);
            this.btnLog.Name = "btnLog";
            this.btnLog.Size = new System.Drawing.Size(130, 27);
            this.btnLog.TabIndex = 7;
            this.btnLog.Text = "Скрыть журнал";
            this.btnLog.UseVisualStyleBackColor = true;
            this.btnLog.Click +=
                new System.EventHandler(this.btnLog_Click);
            this.pnlSettings.Controls.Add(this.lblDataError);
            this.pnlSettings.Controls.Add(this.cmbDataError);
            this.pnlSettings.Controls.Add(this.lblOutage);
            this.pnlSettings.Controls.Add(this.cmbOutage);
            this.pnlSettings.Controls.Add(this.lblProcTime);
            this.pnlSettings.Controls.Add(this.cmbProcTime);
            this.pnlSettings.Controls.Add(this.lblSpeed);
            this.pnlSettings.Controls.Add(this.trbSpeed);
            this.pnlSettings.Controls.Add(this.chkAuto);
            this.pnlSettings.Controls.Add(this.btnStartStop);
            this.pnlSettings.Controls.Add(this.btnSendTask);
            this.pnlSettings.Controls.Add(this.btnReset);
            this.pnlSettings.Controls.Add(this.btnLog);
            this.pnlSettings.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlSettings.Location = new System.Drawing.Point(0, 0);
            this.pnlSettings.Name = "pnlSettings";
            this.pnlSettings.Size = new System.Drawing.Size(1090, 76);
            this.pnlSettings.TabIndex = 0;
            this.canvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.canvas.Location = new System.Drawing.Point(0, 76);
            this.canvas.Name = "canvas";
            this.canvas.Size = new System.Drawing.Size(1090, 540);
            this.canvas.TabIndex = 1;
            this.lstLog.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lstLog.IntegralHeight = false;
            this.lstLog.ItemHeight = 15;
            this.lstLog.Location = new System.Drawing.Point(0, 616);
            this.lstLog.Name = "lstLog";
            this.lstLog.Size = new System.Drawing.Size(1090, 130);
            this.lstLog.TabIndex = 2;
            this.lstLog.HorizontalScrollbar = true;
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.lblCounters,
                this.lblHint});
            this.statusStrip.Location = new System.Drawing.Point(0, 746);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1090, 22);
            this.lblCounters.Name = "lblCounters";
            this.lblCounters.Text = "";
            this.lblHint.Name = "lblHint";
            this.lblHint.Spring = true;
            this.lblHint.Text = "";
            this.lblHint.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1090, 768);
            this.Controls.Add(this.canvas);
            this.Controls.Add(this.lstLog);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.pnlSettings);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Модель работы вычислительной сети";
            this.FormClosing +=
                new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.pnlSettings.ResumeLayout(false);
            this.pnlSettings.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
