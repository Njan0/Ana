using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ana
{
    internal class MoveableCanvas : Canvas
    {
        public class MoveableCanvasChild
        {
            public UIElement Element;
            public Point Position;

            public MoveableCanvasChild(UIElement element, Point position)
            {
                Element = element;
                Position = position;
            }
        }

        private Matrix _transform;
        public Matrix Transform
        {
            get { return _transform; }
            set
            {
                _transform = value;
                UpdateChildren();
            }
        }
        private Matrix _lastTransform;
        private Matrix _invTransform;
        public Matrix InvTransform
        {
            get
            {
                if (_lastTransform != _transform)
                {
                    _lastTransform = _invTransform = _transform;
                    _invTransform.Invert();
                }
                return _invTransform;
            }
        }

        private readonly List<MoveableCanvasChild> moveableChildren;
        public ReadOnlyCollection<MoveableCanvasChild> MoveableChildren => moveableChildren.AsReadOnly();

        public MoveableCanvas() : base()
        {
            moveableChildren = new List<MoveableCanvasChild>();
        }

        /// <summary>
        /// Update all children's positions
        /// </summary>
        private void UpdateChildren()
        {
            foreach (MoveableCanvasChild child in moveableChildren)
            {
                UpdateChild(child);
            }
        }

        /// <summary>
        /// Recalculate a child's positions
        /// </summary>
        /// <param name="child"></param>
        private void UpdateChild(MoveableCanvasChild child)
        {
            Matrix matrix = new Matrix()
            {
                OffsetX = child.Position.X,
                OffsetY = child.Position.Y
            };
            matrix.Append(_transform);
            child.Element.RenderTransform = new MatrixTransform(matrix);
        }

        /// <summary>
        /// Add a element to the canvas
        /// </summary>
        /// <param name="element"></param>
        /// <param name="position"></param>
        /// <param name="transformedPosition">true iff position is given in transformed space</param>
        public void AddChild(UIElement element, Point position, bool transformedPosition = true)
        {
            Point pos = transformedPosition ? InvTransform.Transform(position) : position;
            var newChild = new MoveableCanvasChild(element, pos);

            moveableChildren.Add(newChild);
            UpdateChild(newChild);
            Children.Add(element);
        }

        /// <summary>
        /// Remove a element to the canvas
        /// </summary>
        /// <param name="element"></param>
        public void RemoveChild(UIElement element)
        {
            var index = moveableChildren.FindIndex((child) => child.Element == element);
            moveableChildren.RemoveAt(index);
            Children.RemoveAt(index);
        }

        /// <summary>
        /// Transalte all children's positions
        /// </summary>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        public void Translate(double offsetX, double offsetY)
        {
            _transform.Translate(offsetX, offsetY);
            UpdateChildren();
        }

        /// <summary>
        /// Translate a child's position
        /// </summary>
        /// <param name="element">Child to shift</param>
        /// <param name="offsetX">Offset in transformed space</param>
        /// <param name="offsetY">Offset in transformed space</param>
        public void TranslateChild(UIElement element, double offsetX, double offsetY)
        {
            MoveableCanvasChild mcc = moveableChildren.Find((child) => child.Element == element);
            Point baseOffset = InvertTransformScale(new Point(offsetX, offsetY));
            mcc.Position.Offset(baseOffset.X, baseOffset.Y);
            UpdateChild(mcc);
        }

        /// <summary>
        /// Apply inverse of transform ignoring offsets
        /// </summary>
        /// <param name="offset">Offset in transformed space</param>
        /// <returns>Rescaled offset in base space</returns>
        private Point InvertTransformScale(Point offset)
        {
            Point transformed = InvTransform.Transform(offset);
            transformed.X -= InvTransform.OffsetX;
            transformed.Y -= InvTransform.OffsetY;

            return transformed;
        }

        /// <summary>
        /// Zoom about a origin point
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="factor"></param>
        public void Zoom(Point origin, double factor)
        {
            _transform.ScaleAt(factor, factor, origin.X, origin.Y);
            UpdateChildren();
        }
    }
}
