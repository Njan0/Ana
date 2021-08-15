using Microsoft.Win32;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Ana
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // dragging
        private Point lastMousePos;
        private UIElement draggedObject;
        private bool isDragging;

        // catch canvas right clicks
        private MoveableCanvas canvasRightClicked;
        private Point rightClickPos;

        private static readonly Thickness NOTE_PADDING = new Thickness(10);

        public MainWindow()
        {
            InitializeComponent();
            isDragging = false;
        }

        private void CommonCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = tcNotes != null && tcNotes.SelectedIndex != -1;
        }

        /// <summary>
        /// Creates a new tab with no notes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            NewTab();
        }

        /// <summary>
        /// Create a new note board
        /// </summary>
        /// <returns></returns>
        private TabItem NewTab()
        {
            MoveableCanvas canvas = new MoveableCanvas
            {
                Background = Brushes.Black,
                ContextMenu = this.FindResource("cmCanvasAdd") as ContextMenu,
                Focusable = true,
                ClipToBounds = true
            };
            canvas.MouseLeftButtonDown += Canvas_OnMouseLeftButtonDown;
            canvas.PreviewMouseRightButtonDown += Canvas_OnPreviewMouseRightButtonDown;
            canvas.MouseWheel += Canvas_OnMouseWheel;

            TabItem newTabItem = new TabItem
            {
                Header = "New",
                Tag = null,
                ContextMenu = this.FindResource("cmTab") as ContextMenu,
                Content = canvas
            };

            tcNotes.Items.Add(newTabItem);
            tcNotes.SelectedItem = newTabItem;

            return newTabItem;
        }


        /// <summary>
        /// Menu item to close tab clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            var target = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget as TabItem;
            tcNotes.Items.Remove(target);
        }

        /// <summary>
        /// Show open file dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                DefaultExt = "json",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                LoadBoard(openFileDialog.FileName);
            }
        }

        /// <summary>
        /// Load board from json file
        /// </summary>
        /// <param name="loadFile"></param>
        private void LoadBoard(string loadFile)
        {
            if (File.Exists(loadFile))
            {
                string jsonString = File.ReadAllText(loadFile);
                try
                {
                    var data = JsonSerializer.Deserialize<NoteBoard>(jsonString);

                    var tab = NewTab();
                    tab.Tag = loadFile;
                    tab.Header = Path.GetFileName(loadFile);

                    var canvas = tab.Content as MoveableCanvas;
                    canvas.Transform = data.Transform;

                    int noteCount = Math.Min(data.Texts.Count, data.Positions.Count);
                    for (int i = 0; i < noteCount; ++i)
                    {
                        var newNote = AddNote(canvas, data.Positions[i], false);
                        newNote.Text = data.Texts[i];
                        newNote.IsReadOnly = true;
                    }
                }
                catch (JsonException exception)
                {
                    MessageBox.Show(exception.ToString(), "Failed to parse file", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Save current note board
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveTab(tcNotes.SelectedItem as TabItem, false);
        }

        /// <summary>
        /// Save current note board as...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveTab(tcNotes.SelectedItem as TabItem, true);
        }

        /// <summary>
        /// Save given note board
        /// </summary>
        /// <param name="tab"></param>
        /// <param name="saveAs">Force SaveFileDialog</param>
        private void SaveTab(TabItem tab, bool saveAs)
        {
            var canvas = tab.Content as MoveableCanvas;

            if (!(tab.Tag is string) || saveAs)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    DefaultExt = "json",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    tab.Tag = saveFileDialog.FileName;
                    tab.Header = Path.GetFileName(saveFileDialog.FileName);
                }
                else
                {
                    return;
                }
            }

            SaveBoard(canvas, tab.Tag as string);
        }

        /// <summary>
        /// Save notes into a json file
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="saveFile"></param>
        private static void SaveBoard(MoveableCanvas canvas, string saveFile)
        {
            NoteBoard data = new NoteBoard
            {
                Transform = canvas.Transform
            };
            foreach (var child in canvas.MoveableChildren)
            {
                data.Texts.Add((child.Element as TextBox).Text);
                data.Positions.Add(child.Position);
            }

            string jsonString = JsonSerializer.Serialize(data);
            File.WriteAllText(saveFile, jsonString);
        }

        /// <summary>
        /// Zoom canvas on mouse scroll
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var canvas = sender as MoveableCanvas;
            canvas.Zoom(e.GetPosition(canvas), Math.Pow(1.001, e.Delta));
        }

        /// <summary>
        /// Start dragging canvas on left mouse down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var canvas = sender as MoveableCanvas;
            StartDragging(canvas, e.GetPosition(this));
        }

        /// <summary>
        /// Start dragging note on left mouse down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Note_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var note = sender as TextBox;
            if (note.IsReadOnly)
                StartDragging(note, e.GetPosition(this));
        }

        /// <summary>
        /// Initiate dragging
        /// </summary>
        /// <param name="element"></param>
        /// <param name="position"></param>
        private void StartDragging(UIElement element, Point position)
        {
            if (!isDragging)
            {
                CaptureMouse();
                lastMousePos = position;
                isDragging = true;
                draggedObject = element;
                element.Focus();
            }
        }

        /// <summary>
        /// Capture right click position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            canvasRightClicked = sender as MoveableCanvas;
            rightClickPos = Mouse.GetPosition(canvasRightClicked);
        }

        /// <summary>
        /// Stop dragging on left mouse up
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (isDragging)
            {
                ReleaseMouseCapture();
                isDragging = false;
            }
        }

        /// <summary>
        /// When mouse is moved while dragging translate objects accordingly
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (isDragging)
            {
                Point pos = e.GetPosition(this);
                var offsetX = pos.X - lastMousePos.X;
                var offsetY = pos.Y - lastMousePos.Y;

                if (draggedObject is MoveableCanvas)
                {
                    (draggedObject as MoveableCanvas).Translate(offsetX, offsetY);
                }
                else
                {
                    var tab = tcNotes.SelectedItem as TabItem;
                    var canvas = tab.Content as MoveableCanvas;
                    canvas.TranslateChild(draggedObject, offsetX, offsetY);
                }

                lastMousePos = pos;
            }
        }

        /// <summary>
        /// Add a note
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItemAdd_Click(object sender, RoutedEventArgs e)
        {
            var newNote = AddNote(canvasRightClicked, rightClickPos);
            newNote.Focus();
        }

        /// <summary>
        /// Add a empty note to a canvas
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="position"></param>
        /// <param name="transformedPosition">true iff position is given in transformed space</param>
        /// <returns></returns>
        private TextBox AddNote(MoveableCanvas canvas, Point position, bool transformedPosition = true)
        {
            var newNote = new TextBox
            {
                Background = Brushes.White,
                ContextMenu = this.FindResource("cmCanvasSelected") as ContextMenu,
                AcceptsReturn = true,
                Padding = NOTE_PADDING
            };

            newNote.PreviewMouseLeftButtonDown += Note_OnMouseLeftButtonDown;
            newNote.KeyDown += Note_KeyDown;
            newNote.LostKeyboardFocus += Note_LostKeyboardFocus;

            canvas.AddChild(newNote, position, transformedPosition);
            return newNote;
        }

        /// <summary>
        /// Lock text of TextBox after focus is lost
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Note_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            (sender as TextBox).IsReadOnly = true;
        }

        /// <summary>
        /// Clear focus when ESC is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Note_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Keyboard.ClearFocus();
            }
        }

        /// <summary>
        /// Delete clicked note
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            var target = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;
            canvasRightClicked.RemoveChild(target);
        }

        /// <summary>
        /// Edit clicked note
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItemEdit_Click(object sender, RoutedEventArgs e)
        {
            var target = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget as TextBox;
            target.IsReadOnly = false;
            target.SelectAll();
        }

        private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Drop files to open as new tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files) {
                    LoadBoard(file);
                }
            }
        }

        /// <summary>
        /// Load tabs from Settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (string tab in Properties.Settings.Default.Tabs)
            {
                LoadBoard(tab);
            }
        }

        /// <summary>
        /// Store open tabs to Settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Tabs.Clear();
            foreach (TabItem tab in tcNotes.Items)
            {
                Properties.Settings.Default.Tabs.Add(tab.Tag as string);
            }
            Properties.Settings.Default.Save();
        }
    }
}
