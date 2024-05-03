using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Validator
{
    public partial class LineNumber : UserControl
    {
        private readonly LineNumberStrip _strip;

        public LineNumber()
        {
            InitializeComponent();
            _strip = new LineNumberStrip(richTextBox);
            this.Controls.Add(_strip);
        }

        public RichTextBox RichTextBox
        {
            get { return richTextBox; }
        }

        public LineNumberStrip Strip
        {
            get { return _strip; }
        }
    }

    public class LineNumberStrip : RichTextBox
    {
        private BufferedGraphics _bufferedGraphics;
        private readonly BufferedGraphicsContext _bufferContext = BufferedGraphicsManager.Current;
        private readonly RichTextBox _richTextBox;
        private Brush _fontBrush;
        private float _fontHeight;
        private const float _FONT_MODIFIER = 0.09f;
        private bool _speedBump;
        private const int _DRAWING_OFFSET = 1;
        private int _lastYPos = -1, _dragDistance, _lastLineCount;
        private int _scrollingLineIncrement = 5, _numPadding = 10;

        public LineNumberStrip(RichTextBox plainTextBox)
        {
            _richTextBox = plainTextBox;
            plainTextBox.TextChanged += _richTextBox_TextChanged;
            plainTextBox.VScroll += _richTextBox_VScroll;

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            this.Size = new Size(10, 10);
            base.BackColor = ColorTranslator.FromHtml("#222E33");
            base.Dock = DockStyle.Left;
            base.ForeColor = Color.LightGray; 

            _fontBrush = new SolidBrush(base.ForeColor);

            SetFontHeight();
            UpdateBackBuffer();
            this.SendToBack();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button.Equals(MouseButtons.Left) && _scrollingLineIncrement != 0)
            {
                _lastYPos = Cursor.Position.Y;
                this.Cursor = Cursors.NoMoveVert;
            }
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            SetControlWidth();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            this.Cursor = Cursors.Default;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button.Equals(MouseButtons.Left) && _scrollingLineIncrement != 0)
            {
                _dragDistance += Cursor.Position.Y - _lastYPos;

                if (_dragDistance > _fontHeight)
                {
                    int selectionStart = _richTextBox.GetFirstCharIndexFromLine(NextLineDown);
                    _richTextBox.Select(selectionStart, 0);
                    _dragDistance = 0;
                }
                else if (_dragDistance < _fontHeight * -1)
                {
                    int selectionStart = _richTextBox.GetFirstCharIndexFromLine(NextLineUp);
                    _richTextBox.Select(selectionStart, 0);
                    _dragDistance = 0;
                }

                _lastYPos = Cursor.Position.Y;
            }
        }

        #region Functions
        private void UpdateBackBuffer()
        {
            if (this.Width > 0)
            {
                _bufferContext.MaximumBuffer = new Size(this.Width + 1, this.Height + 1);
                _bufferedGraphics = _bufferContext.Allocate(this.CreateGraphics(), this.ClientRectangle);
            }
        }

        /// <summary>
        /// This method keeps the painted text aligned with the text in the corisponding 
        /// textbox perfectly. GetFirstCharIndexFromLine will yeild -1 if line not
        /// present. GetPositionFromCharIndex will yeild an empty point to char index -1.
        /// To explicitly say that line is not present return -1.
        /// </summary>
        private int GetPositionOfRtbLine(int lineNumber)
        {
            int index = _richTextBox.GetFirstCharIndexFromLine(lineNumber);
            Point pos = _richTextBox.GetPositionFromCharIndex(index);
            return index.Equals(-1) ? -1 : pos.Y;
        }

        private void SetFontHeight()
        {
            // Shrink the font for minor compensation
            this.Font = new Font(_richTextBox.Font.FontFamily, _richTextBox.Font.Size -
                _FONT_MODIFIER, _richTextBox.Font.Style);

            _fontHeight = _bufferedGraphics.Graphics.MeasureString("123ABC", this.Font).Height;
        }

        private void SetControlWidth()
        {
            this.Width = WidthOfWidestLineNumber + _numPadding * 2;
            this.Invalidate(false);
        }
        #endregion

        #region Event Handlers
        private void _richTextBox_TextChanged(object sender, EventArgs e)
        {

            if (!_lastLineCount.Equals(_richTextBox.Lines.Length))
            {
                SetControlWidth();
            }
            if (_lastLineCount == 0 || _lastLineCount != _richTextBox.Lines.Length)
            {
                _bufferedGraphics.Graphics.Clear(this.BackColor);
                int firstIndex = _richTextBox.GetCharIndexFromPosition(Point.Empty);
                int firstLine = _richTextBox.GetLineFromCharIndex(firstIndex);
                Point bottomLeft = new Point(0, this.ClientRectangle.Height);
                int lastIndex = _richTextBox.GetCharIndexFromPosition(bottomLeft);
                int lastLine = _richTextBox.GetLineFromCharIndex(lastIndex);

                for (int i = firstLine; i <= lastLine + 1; i++)
                {
                    int charYPos = GetPositionOfRtbLine(i);
                    if (charYPos.Equals(-1)) continue;
                    float yPos = GetPositionOfRtbLine(i) + _DRAWING_OFFSET;

                    PointF stringPos = new PointF(_numPadding, yPos);
                    string line = (i + 1).ToString(CultureInfo.InvariantCulture);
                    _bufferedGraphics.Graphics.DrawString(line, this.Font, _fontBrush, stringPos);

                }
            }
            _lastLineCount = _richTextBox.Lines.Length;
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            _fontBrush = new SolidBrush(this.ForeColor);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateBackBuffer();
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            _bufferedGraphics.Render(pevent.Graphics);
        }

        private void _richTextBox_VScroll(object sender, EventArgs e)
        {
            this.ScrollBars = _richTextBox.ScrollBar;
            //----------------------------------------------------------------------------
            // Decrease the paint calls by one half when there is more than 3000 lines
            //if (_richTextBox.Lines.Length > 3000 && _speedBump)
            //{
            //    _speedBump = !_speedBump;
            //    return;
            //}
            //this.Invalidate(false);
        }
        #endregion

        #region Properties
        private int NextLineDown
        {
            get
            {
                int yPos = _richTextBox.ClientSize.Height + (int)(_fontHeight * ScrollSpeed + 0.5f);
                Point topPos = new Point(0, yPos);
                int index = _richTextBox.GetCharIndexFromPosition(topPos);
                return _richTextBox.GetLineFromCharIndex(index);
            }
        }

        private int NextLineUp
        {
            get
            {
                Point topPos = new Point(0, (int)(_fontHeight * (ScrollSpeed * -1) + -0.5f));
                int index = _richTextBox.GetCharIndexFromPosition(topPos);
                return _richTextBox.GetLineFromCharIndex(index);
            }
        }

        /// <summary>
        /// Gets the width of the widest number on the strip
        /// </summary>
        private int WidthOfWidestLineNumber
        {
            get
            {
                if (_bufferedGraphics.Graphics != null)
                {
                    string strNumber = (_richTextBox.Lines.Length).ToString(CultureInfo.InvariantCulture);
                    SizeF size = _bufferedGraphics.Graphics.MeasureString(strNumber, _richTextBox.Font);
                    return (int)(size.Width + 0.5);
                }

                return 1;
            }
        }
        [Category("Layout")]
        [Description("Gets or sets the spacing from the left and right of the numbers to the let and right of the control")]
        public int NumberPadding
        {
            get { return _numPadding; }
            set
            {
                _numPadding = value;

                if (_richTextBox != null)
                {
                    SetControlWidth();
                }
            }
        }

        /// <summary>
        /// Gets or sets the scrolling speed in the number of lines
        /// to increment or decrement
        /// </summary>
        [Category("Behavior")]
        public int ScrollSpeed
        {
            get { return _scrollingLineIncrement; }
            set { _scrollingLineIncrement = value; }
        }
        #endregion
    }
}