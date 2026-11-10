using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using NetworkSim.Model;

namespace NetworkSim.View
{
    /// <summary>
    /// Панель, на которой рисуется сцена моделирования. Обладает двойной
    /// буферизацией (без мерцания) и центрирует логическую схему в окне.
    /// </summary>
    public class SimulationCanvas : Panel
    {
        private SimulationWorld _world;     // моделируемый мир

        /// <summary>Создаёт холст с двойной буферизацией.</summary>
        public SimulationCanvas()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.FromArgb(250, 250, 251);
        }

        /// <summary>Задаёт моделируемый мир для отображения.</summary>
        public void SetWorld(SimulationWorld world)
        {
            _world = world;
            Invalidate();
        }

        /// <summary>Отрисовка сцены: качество, масштаб, центрирование.</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_world == null)
            {
                return;
            }
            float scale = Math.Min(Width / (float)Model.Layout.CanvasWidth,
                                   Height / (float)Model.Layout.CanvasHeight);
            if (scale <= 0f)
            {
                return;
            }
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.TranslateTransform((Width - Model.Layout.CanvasWidth * scale) / 2f,
                                 (Height - Model.Layout.CanvasHeight * scale) / 2f);
            g.ScaleTransform(scale, scale);
            _world.Draw(g);
        }
    }
}
