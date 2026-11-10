using System;
using System.Windows.Forms;
using NetworkSim.Model;

namespace NetworkSim
{
    /// <summary>
    /// Главное окно приложения: панель настроек модели, сцена моделирования,
    /// журнал событий и строка состояния со счётчиками. Создаёт мир модели
    /// (SimulationWorld) и обрабатывает команды пользователя.
    /// </summary>
    public partial class MainForm : Form
    {
        private SimulationWorld _world;     // моделируемый мир

        /// <summary>Создаёт главное окно и запускает модель в собранном виде.</summary>
        public MainForm()
        {
            InitializeComponent();
            InitSettingsControls();
            CreateWorld();
        }

        // Элемент выпадающего списка: число + готовый текст (без зависимости
        // от языковых настроек, где разделитель может быть точкой).
        private class ValueItem
        {
            private readonly string _text;

            public double Value { get; }

            public ValueItem(double value, string text)
            {
                Value = value;
                _text = text;
            }

            public override string ToString()
            {
                return _text;
            }
        }

        // Заполняет выпадающие списки допустимыми значениями параметров.
        private void InitSettingsControls()
        {
            cmbDataError.Items.Add(new ValueItem(0.0, "0,0"));
            cmbDataError.Items.Add(new ValueItem(0.1, "0,1"));
            cmbDataError.Items.Add(new ValueItem(0.2, "0,2"));
            cmbDataError.Items.Add(new ValueItem(0.3, "0,3"));
            cmbDataError.SelectedIndex = 1;

            cmbOutage.Items.Add(new ValueItem(0.0, "0,0"));
            cmbOutage.Items.Add(new ValueItem(0.05, "0,05"));
            cmbOutage.Items.Add(new ValueItem(0.1, "0,1"));
            cmbOutage.Items.Add(new ValueItem(0.2, "0,2"));
            cmbOutage.SelectedIndex = 1;

            cmbProcTime.Items.Add(new ValueItem(1.0, "1"));
            cmbProcTime.Items.Add(new ValueItem(2.0, "2"));
            cmbProcTime.Items.Add(new ValueItem(3.0, "3"));
            cmbProcTime.SelectedIndex = 1;
        }

        // Создаёт новый мир и подключает его к окну (запуск и кнопка «Сброс»).
        private void CreateWorld()
        {
            if (_world != null)
            {
                _world.Clock.Tick -= OnClockTick;
                _world.Logger.MessageLogged -= OnLogMessage;
                _world.Clock.Stop();
            }
            _world = new SimulationWorld();
            _world.Client.AutoSend = chkAuto.Checked;
            _world.Clock.SpeedFactor = trbSpeed.Value / 2.0;
            canvas.SetWorld(_world);
            _world.Clock.Tick += OnClockTick;
            _world.Logger.MessageLogged += OnLogMessage;
            ApplySettingsFromUi();
            btnStartStop.Text = "Старт";
            lstLog.Items.Clear();
            _world.Logger.Log("Модель", "модель готова к работе, нажмите «Старт»");
            UpdateStatusLabel();
            canvas.Invalidate();
        }

        // Применяет выбранные настройки к модели. Недопустимое значение
        // приводит к исключению, которое перехватывается и показывается.
        private void ApplySettingsFromUi()
        {
            if (_world == null)
            {
                return;
            }
            try
            {
                double dataError = ((ValueItem)cmbDataError.SelectedItem).Value;
                double outage = ((ValueItem)cmbOutage.SelectedItem).Value;
                double processingTime = ((ValueItem)cmbProcTime.SelectedItem).Value;
                _world.ApplySettings(dataError, outage, processingTime);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                MessageBox.Show(ex.Message, "Недопустимое значение параметра",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Шаг модельного времени: обновить счётчики и перерисовать сцену.
        private void OnClockTick(object sender, TickEventArgs e)
        {
            UpdateStatusLabel();
            canvas.Invalidate();
        }

        // Новая запись журнала: добавить строку, ограничить длину и прокрутить.
        private void OnLogMessage(object sender, LogEventArgs e)
        {
            lstLog.Items.Add(e.Line);
            while (lstLog.Items.Count > 300)
            {
                lstLog.Items.RemoveAt(0);
            }
            lstLog.TopIndex = lstLog.Items.Count - 1;
        }

        // Обновляет строку состояния со счётчиками модели.
        private void UpdateStatusLabel()
        {
            lblCounters.Text = string.Format(
                "Время: {0:0.0} с      Отправлено: {1}      Доставлено: {2}      Возвращено ошибок: {3}" +
                "      Ремонтов: {4}      Задач в полёте: {5}",
                _world.Clock.Elapsed, _world.SentCount, _world.DeliveredCount,
                _world.ErrorsReturned, _world.RepairsDone, _world.TasksActive);
        }

        // «Старт/Стоп»: запуск и остановка модельного времени.
        private void btnStartStop_Click(object sender, EventArgs e)
        {
            if (_world.Clock.IsRunning)
            {
                _world.Clock.Stop();
                btnStartStop.Text = "Старт";
            }
            else
            {
                _world.Clock.Start();
                btnStartStop.Text = "Стоп";
            }
            canvas.Invalidate();
        }

        // «Задача»: отправка новой задачи по команде пользователя.
        private void btnSendTask_Click(object sender, EventArgs e)
        {
            string reason;
            if (_world.TrySendTask(out reason))
            {
                lblHint.Text = "";
            }
            else
            {
                lblHint.Text = reason;
            }
            canvas.Invalidate();
        }

        // «Сброс»: полная пересборка модели в исходное состояние.
        private void btnReset_Click(object sender, EventArgs e)
        {
            CreateWorld();
            lblHint.Text = "";
        }

        // «Скрыть журнал»: освобождает место для сцены моделирования.
        private void btnLog_Click(object sender, EventArgs e)
        {
            lstLog.Visible = !lstLog.Visible;
            btnLog.Text = lstLog.Visible ? "Скрыть журнал" : "Показать журнал";
            canvas.Invalidate();
        }

        private void cmbDataError_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplySettingsFromUi();
        }

        private void cmbOutage_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplySettingsFromUi();
        }

        private void cmbProcTime_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplySettingsFromUi();
        }

        // Скорость: положение движка (1–8) делится на 2 — от 0,5 до 4.
        private void trbSpeed_ValueChanged(object sender, EventArgs e)
        {
            if (_world != null)
            {
                _world.Clock.SpeedFactor = trbSpeed.Value / 2.0;
            }
        }

        private void chkAuto_CheckedChanged(object sender, EventArgs e)
        {
            if (_world != null)
            {
                _world.Client.AutoSend = chkAuto.Checked;
            }
        }

        // При закрытии окна обязательно останавливаем модельное время.
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _world.Clock.Stop();
        }
    }
}
