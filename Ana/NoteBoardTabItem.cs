using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ana
{
    internal class NoteBoardTabItem : TabItem
    {
        private static readonly Thickness NOTE_PADDING = new Thickness(10);

        private readonly MainWindow parent;

        public MoveableCanvas Canvas
        {
            get { return Content as MoveableCanvas; }
            set { Content = value; }
        }
        private string _saveFile;
        public string SaveFile
        {
            get { return _saveFile; }
            set
            {
                _saveFile = value;
                if (value != null)
                {
                    Header = Path.GetFileName(value);
                }
            }
        }

        private NoteBoardTabItem(MainWindow parent) : base()
        {
            this.parent = parent;
        }

        /// <summary>
        /// Save board to a json file
        /// </summary>
        public void SaveBoard()
        {
            File.WriteAllText(SaveFile, GetJSON());
        }

        /// <summary>
        /// Check if current board equals board
        /// stored in SaveFile
        /// </summary>
        /// <returns></returns>
        public bool IsBoardSaved()
        {
            try
            {
                return File.ReadAllText(SaveFile) == GetJSON();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Represent the note board as a json string
        /// </summary>
        /// <returns></returns>
        private string GetJSON()
        {
            NoteBoard data = new NoteBoard
            {
                Transform = Canvas.Transform
            };
            foreach (var child in Canvas.MoveableChildren)
            {
                data.Texts.Add((child.Element as TextBox).Text);
                data.Positions.Add(child.Position);
            }

            return JsonSerializer.Serialize(data);
        }

        /// <summary>
        /// Load board from json file
        /// </summary>
        /// <param name="main"></param>
        /// <param name="loadFile"></param>
        public static NoteBoardTabItem LoadBoard(MainWindow main, string loadFile)
        {
            string jsonString = File.ReadAllText(loadFile);
            var data = JsonSerializer.Deserialize<NoteBoard>(jsonString);

            var tab = NewTab(main);
            tab.SaveFile = loadFile;

            var canvas = tab.Canvas;
            canvas.Transform = data.Transform;

            int noteCount = Math.Min(data.Texts.Count, data.Positions.Count);
            for (int i = 0; i < noteCount; ++i)
            {
                var newNote = tab.AddNote(data.Positions[i], false);
                newNote.Text = data.Texts[i];
                newNote.IsReadOnly = true;
            }

            return tab;
        }

        /// <summary>
        /// Add a empty tab and add it to a MainWindow
        /// </summary>
        /// <param name="main"></param>
        /// <returns></returns>
        public static NoteBoardTabItem NewTab(MainWindow main)
        {
            MoveableCanvas canvas = new MoveableCanvas
            {
                Background = Brushes.Black,
                ContextMenu = main.FindResource("cmCanvasAdd") as ContextMenu,
                Focusable = true,
                ClipToBounds = true
            };
            main.RegisterCanvas(canvas);

            NoteBoardTabItem newTabItem = new NoteBoardTabItem(main)
            {
                Header = "New",
                ContextMenu = main.FindResource("cmTab") as ContextMenu,
                Canvas = canvas
            };
            main.tcNotes.Items.Add(newTabItem);

            return newTabItem;
        }

        /// <summary>
        /// Add a empty note
        /// </summary>
        /// <param name="position"></param>
        /// <param name="transformedPosition">true iff position is given in transformed space</param>
        /// <returns></returns>
        public TextBox AddNote(Point position, bool transformedPosition = true)
        {
            var newNote = new TextBox
            {
                Background = Brushes.White,
                ContextMenu = parent.FindResource("cmCanvasSelected") as ContextMenu,
                AcceptsReturn = true,
                Padding = NOTE_PADDING
            };

            parent.RegisterNote(newNote);

            Canvas.AddChild(newNote, position, transformedPosition);
            return newNote;
        }
    }
}
