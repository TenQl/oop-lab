using System.Drawing;

namespace NetworkSim.Model
{
    /// <summary>Объект, умеющий изображать себя на схеме средствами GDI+.</summary>
    public interface IDrawable
    {
        /// <summary>Рисует объект на схеме.</summary>
        void Draw(Graphics g);
    }
}
