using System;
using System.Drawing;

namespace NetworkSim.Model
{
    /// <summary>
    /// Делегат события «база данных отказала». Объявлен в классе-наследнике:
    /// у наследника есть собственный делегат (пункт 14.7).
    /// </summary>
    public delegate void OutageOccurredHandler(object sender, NodeCounterEventArgs e);

    /// <summary>
    /// База данных — узел, производный от ProcessingNode. Может полностью
    /// отказать (требуется ремонт администратором), причём вероятность отказа
    /// не зависит от номера попытки. Переопределяет Evaluate и Draw.
    /// </summary>
    public class DatabaseNode : ProcessingNode
    {
        private int _outageCount;       // число отказов базы данных
        private OutageOccurredHandler _outageOccurred;  // подписчики события OutageOccurred

        private static readonly Brush CylinderBrush = new SolidBrush(Color.FromArgb(72, 138, 120));

        /// <summary>Число отказов базы данных (только чтение).</summary>
        public int OutageCount
        {
            get { return _outageCount; }
        }

        /// <summary>
        /// Событие: база данных отказала. Объявлено полностью, с явными
        /// методами add и remove.
        /// </summary>
        public event OutageOccurredHandler OutageOccurred
        {
            add { _outageOccurred += value; }
            remove { _outageOccurred -= value; }
        }

        /// <summary>Создаёт базу данных с заданными параметрами.</summary>
        public DatabaseNode(string name, string waypointId, PointF location,
                            SimulationClock clock, double processingTime = 2.0,
                            double failureProbability = 0.05)
            : base(name, waypointId, location, clock, processingTime, failureProbability)
        {
        }

        /// <summary>
        /// Определяет исход обработки: с вероятностью FailureProbability
        /// база данных отказывает, иначе пакет обработан успешно.
        /// </summary>
        protected override FailureKind Evaluate(Packet packet)
        {
            if (Random.Shared.NextDouble() < FailureProbability)
            {
                _outageCount++;
                // Логика и генерация события — в одном методе (пункт 14.7).
                OnOutageOccurred(new NodeCounterEventArgs(this, packet, _outageCount));
                return FailureKind.NodeOutage;
            }
            return FailureKind.None;
        }

        /// <summary>Вызывает событие OutageOccurred.</summary>
        protected void OnOutageOccurred(NodeCounterEventArgs e)
        {
            _outageOccurred?.Invoke(this, e);
        }

        /// <summary>Рисует узел с пиктограммой цилиндра.</summary>
        public override void Draw(Graphics g)
        {
            DrawBody(g);
            if (Status != NodeStatus.Failed)
            {
                DrawCylinderIcon(g);
            }
        }

        // Пиктограмма базы данных: цилиндр.
        private void DrawCylinderIcon(Graphics g)
        {
            float cx = Location.X;
            float cy = Location.Y + 4f;
            const float w = 24f;
            const float h = 20f;
            const float ry = 5f;
            g.FillEllipse(CylinderBrush, cx - w / 2f, cy - h / 2f - ry, w, 2f * ry);
            g.FillRectangle(CylinderBrush, cx - w / 2f, cy - h / 2f, w, h);
            g.FillEllipse(CylinderBrush, cx - w / 2f, cy + h / 2f - ry, w, 2f * ry);
            using (Pen p = new Pen(Color.White, 1.2f))
            {
                g.DrawEllipse(p, cx - w / 2f, cy - h / 2f - ry, w, 2f * ry);
                g.DrawLine(p, cx - w / 2f, cy - h / 2f + ry, cx - w / 2f, cy + h / 2f);
                g.DrawLine(p, cx + w / 2f, cy - h / 2f + ry, cx + w / 2f, cy + h / 2f);
                g.DrawArc(p, cx - w / 2f, cy + h / 2f - ry, w, 2f * ry, 0f, 180f);
            }
        }
    }
}
