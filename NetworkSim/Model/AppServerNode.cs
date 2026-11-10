using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NetworkSim.Model
{
    /// <summary>
    /// Сервер приложений — узел, производный от ProcessingNode. Вероятность
    /// ошибки данных растёт с нагрузкой (длиной очереди пакетов).
    /// Переопределяет Evaluate, OnProcessingCompleted и Draw базового класса.
    /// </summary>
    public class AppServerNode : ProcessingNode
    {
        private int _handledCount;      // число успешно обработанных задач

        private static readonly Brush GearBrush = new SolidBrush(Color.FromArgb(120, 96, 156));
        private static readonly Brush HubBrush = new SolidBrush(Color.White);

        /// <summary>Число успешно обработанных задач (только чтение).</summary>
        public int HandledCount
        {
            get { return _handledCount; }
        }

        /// <summary>Создаёт сервер приложений с заданными параметрами.</summary>
        public AppServerNode(string name, string waypointId, PointF location,
                             SimulationClock clock, double processingTime = 2.0,
                             double failureProbability = 0.1)
            : base(name, waypointId, location, clock, processingTime, failureProbability)
        {
        }

        /// <summary>
        /// Определяет исход обработки: как в базовом классе, но вероятность
        /// ошибки растёт пропорционально длине очереди.
        /// </summary>
        protected override FailureKind Evaluate(Packet packet)
        {
            double p = EffectiveProbability(packet) * (1.0 + 0.25 * QueueLength);
            if (p > 1.0)
            {
                p = 1.0;
            }
            return Random.Shared.NextDouble() < p
                ? FailureKind.InvalidData
                : FailureKind.None;
        }

        /// <summary>Учитывает успешно обработанную задачу.</summary>
        protected override void OnProcessingCompleted(NodeEventArgs e)
        {
            _handledCount++;
            base.OnProcessingCompleted(e);
        }

        /// <summary>Рисует узел с пиктограммой шестерёнки.</summary>
        public override void Draw(Graphics g)
        {
            DrawBody(g);
            if (Status != NodeStatus.Failed)
            {
                DrawGearIcon(g);
            }
        }

        // Пиктограмма шестерёнки: диск со спицами и белая ступица.
        private void DrawGearIcon(Graphics g)
        {
            float cx = Location.X;
            float cy = Location.Y + 4f;
            const float r = 11f;
            using (Pen p = new Pen(GearBrush, 3.5f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                for (int i = 0; i < 8; i++)
                {
                    double angle = i * Math.PI / 4.0;
                    float x1 = cx + (float)Math.Cos(angle) * (r - 2f);
                    float y1 = cy + (float)Math.Sin(angle) * (r - 2f);
                    float x2 = cx + (float)Math.Cos(angle) * (r + 3.5f);
                    float y2 = cy + (float)Math.Sin(angle) * (r + 3.5f);
                    g.DrawLine(p, x1, y1, x2, y2);
                }
            }
            g.FillEllipse(GearBrush, cx - r, cy - r, 2f * r, 2f * r);
            g.FillEllipse(HubBrush, cx - 4f, cy - 4f, 8f, 8f);
        }
    }
}
