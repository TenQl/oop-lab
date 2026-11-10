using System;
using System.Drawing;

namespace NetworkSim.Model
{
    /// <summary>
    /// База данных — узел, производный от ProcessingNode. Может полностью
    /// отказать (требуется ремонт администратором), причём вероятность отказа
    /// не зависит от номера попытки. Переопределяет Evaluate и Draw.
    /// </summary>
    public class DatabaseNode : ProcessingNode
    {
        private int _outageCount;       // число отказов базы данных

        private static readonly Brush CylinderBrush = new SolidBrush(Color.FromArgb(72, 138, 120));

        /// <summary>Число отказов базы данных (только чтение).</summary>
        public int OutageCount
        {
            get { return _outageCount; }
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
                return FailureKind.NodeOutage;
            }
            return FailureKind.None;
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
