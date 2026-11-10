using System;
using System.Drawing;

namespace NetworkSim.Model
{
    /// <summary>
    /// Получатель результата (конечный пользователь). Стоит рядом с клиентом
    /// и радуется, когда клиент передаёт ему готовый результат. Пакеты
    /// пользователю не адресуются: результат всегда возвращается клиенту.
    /// </summary>
    public class EndUser : IDrawable
    {
        protected readonly SimulationClock Clock;

        private const float HappyTime = 2.0f;   // длительность радости, с

        private double _happyRemaining;         // сколько ещё длится радость, с
        private UserMood _mood = UserMood.Waiting;

        private static readonly Brush PersonBrush = new SolidBrush(Color.FromArgb(126, 96, 60));
        private static readonly Brush LabelBrush = new SolidBrush(Color.FromArgb(40, 44, 52));
        private static readonly Brush HappyBrush = new SolidBrush(Color.FromArgb(39, 134, 82));
        private static readonly Pen FacePen = new Pen(Color.FromArgb(40, 44, 52), 1.4f);
        private static readonly Font LabelFont = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel);
        private static readonly Font HintFont = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel);

        public string Name { get; }

        public PointF Location { get; }

        /// <summary>Настроение: ожидание или довольный (снаружи только чтение).</summary>
        public UserMood Mood
        {
            get { return _mood; }
        }

        /// <summary>Число полученных результатов (только чтение).</summary>
        public int ResultsReceived { get; private set; }

        /// <summary>Событие: результат задачи получен.</summary>
        public event EventHandler ResultReceived;

        /// <summary>Создаёт получателя результата и подписывает его на шаги времени.</summary>
        public EndUser(string name, PointF location, SimulationClock clock)
        {
            Name = name;
            Location = location;
            Clock = clock;
            clock.Tick += OnTick;
        }

        /// <summary>Клиент передал пользователю готовый результат — тот радуется.</summary>
        public void ShowResult()
        {
            ResultsReceived++;
            _mood = UserMood.Happy;
            _happyRemaining = HappyTime;
            OnResultReceived(EventArgs.Empty);
        }

        /// <summary>Вызывает событие ResultReceived.</summary>
        protected virtual void OnResultReceived(EventArgs e)
        {
            ResultReceived?.Invoke(this, e);
        }

        // Шаг модельного времени: отсчёт длительности радости.
        private void OnTick(object sender, TickEventArgs e)
        {
            if (_mood != UserMood.Happy)
            {
                return;
            }
            _happyRemaining -= e.Dt;
            if (_happyRemaining <= 0)
            {
                _mood = UserMood.Waiting;
            }
        }

        /// <summary>Рисует пользователя: фигуру человека и выражение лица.</summary>
        public void Draw(Graphics g)
        {
            g.FillEllipse(PersonBrush, Location.X - 9f, Location.Y - 26f, 18f, 18f);
            g.FillEllipse(PersonBrush, Location.X - 19f, Location.Y - 4f, 38f, 27f);
            if (_mood == UserMood.Happy)
            {
                // Улыбка на «лице» (голова — круг выше центра Location).
                g.DrawArc(FacePen, Location.X - 7f, Location.Y - 19f, 14f, 10f, 20f, 140f);
            }
            else
            {
                g.DrawLine(FacePen, Location.X - 5f, Location.Y - 13f,
                    Location.X + 5f, Location.Y - 13f);
            }
            g.DrawString(Name, LabelFont, LabelBrush, Location.X - 46f, Location.Y + 28f);
            if (_mood == UserMood.Happy)
            {
                g.DrawString("результат получен!", HintFont, HappyBrush,
                    Location.X - 52f, Location.Y + 46f);
            }
        }
    }
}
