using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Ana
{
    internal class NoteBoard
    {
        public Matrix Transform { get; set; }
        public List<string> Texts { get; set; }
        public List<Point> Positions { get; set; }

        public NoteBoard()
        {
            Texts = new List<string>();
            Positions = new List<Point>();
        }
    }
}
