using Microsoft.Win32;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            tcNotes.SelectedItem = NoteBoardTabItem.NewTab(this);
        }

        /// <summary>
        /// Add event listener for given canvas
        /// </summary>
        /// <param name="canvas"></param>
        internal void RegisterCanvas(MoveableCanvas canvas)
        {
            canvas.MouseLeftButtonDown += Canvas_OnMouseLeftButtonDown;
            canvas.PreviewMouseRightButtonDown += Canvas_OnPreviewMouseRightButtonDown;
            canvas.MouseWheel += Canvas_OnMouseWheel;
        }


        /// <summary>
        /// Menu item to close tab clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            var target = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget as NoteBoardTabItem;
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
        /// LoadBoard helper to catch exceptions
        /// </summary>
        /// <param name="openFile"></param>
        private void LoadBoard(string openFile)
        {
            try
            {
                tcNotes.SelectedItem = NoteBoardTabItem.LoadBoard(this, openFile);
            }
            catch (JsonException exception)
            {
                MessageBox.Show(exception.ToString(), "Failed to parse file", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("Could not find file: " + openFile, "File not found", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString(), "Failed to open file", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Save current note board
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveTab(tcNotes.SelectedItem as NoteBoardTabItem, false);
        }

        /// <summary>
        /// Save current note board as...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveTab(tcNotes.SelectedItem as NoteBoardTabItem, true);
        }

        /// <summary>
        /// Save given note board
        /// </summary>
        /// <param name="tab"></param>
        /// <param name="saveAs">Force SaveFileDialog</param>
        /// <returns>true iff saved successfully</returns>
        private bool SaveTab(NoteBoardTabItem tab, bool saveAs)
        {
            if (tab.SaveFile == null || saveAs)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    DefaultExt = "json",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    tab.SaveFile = saveFileDialog.FileName;
                }
                else
                {
                    return false;
                }
            }

            tab.SaveBoard();
            return true;
        }

        /// <summary>
        /// Save all open tabs command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveAll();
        }

        /// <summary>
        /// Save all open tabs
        /// </summary>
        /// <returns>true iff all tabs successfully saved</returns>
        private bool SaveAll()
        {
            foreach (NoteBoardTabItem tab in tcNotes.Items)
            {
                if (!SaveTab(tab, false))
                {
                    return false;
                }
            }
            return true;
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
            {
                StartDragging(note, e.GetPosition(this));
            }
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
                    var tab = tcNotes.SelectedItem as NoteBoardTabItem;
                    tab.Canvas.TranslateChild(draggedObject, offsetX, offsetY);
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
            var newNote = (tcNotes.SelectedItem as NoteBoardTabItem).AddNote(rightClickPos);
            newNote.Focus();
        }

        /// <summary>
        /// Add event listener for given note
        /// </summary>
        /// <param name="note"></param>
        internal void RegisterNote(TextBox note)
        {
            note.PreviewMouseLeftButtonDown += Note_OnMouseLeftButtonDown;
            note.KeyDown += Note_KeyDown;
            note.LostKeyboardFocus += Note_LostKeyboardFocus;
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
            foreach (NoteBoardTabItem tab in tcNotes.Items)
            {
                if (tab.SaveFile != null)
                {
                    Properties.Settings.Default.Tabs.Add(tab.SaveFile);
                }
            }
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Check for unsaved note boards
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (NoteBoardTabItem tab in tcNotes.Items)
            {
                if (!tab.IsBoardSaved())
                {
                    var result = MessageBox.Show("Some note boards were not saved. Save all?", "Unsaved note boards", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

                    switch (result){
                        case MessageBoxResult.Yes: // save all
                            e.Cancel = !SaveAll(); // stop closing if save canceled
                            return;
                        case MessageBoxResult.No: // don't save
                            return;
                        default: // stop closing
                            e.Cancel = true;
                            return;
                    }
                }
            }
        }
    }

    public static class Command
    {
        public static readonly RoutedUICommand Exit = new RoutedUICommand
        (
            "Exit",
            "Exit",
            typeof(Command),
            new InputGestureCollection()
            {
                new KeyGesture(Key.F4, ModifierKeys.Alt)
            }
        );

        public static readonly RoutedUICommand SaveAll = new RoutedUICommand
        (
            "Save all",
            "SaveAll",
            typeof(Command),
            new InputGestureCollection()
            {
                new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)
            }
        );

    }
}
