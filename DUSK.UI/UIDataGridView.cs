namespace DUSK.UI;

using DUSK.Core;

/// <summary>
/// Data grid control for displaying tabular data.
/// Supports sorting, selection, and custom cell rendering.
/// MUI-style with beveled headers and Amiga color scheme.
/// </summary>
public class UIDataGridView : UIElementBase
{
    private readonly List<DataGridColumn> _columns = new();
    private readonly List<DataGridRow> _rows = new();
    private int _selectedRowIndex = -1;
    private readonly List<int> _selectedRows = new();
    private int _scrollOffset;
    private int _hoveredRowIndex = -1;
    private int _hoveredColumnIndex = -1;
    private int _sortColumnIndex = -1;
    private bool _sortAscending = true;

    public IReadOnlyList<DataGridColumn> Columns => _columns.AsReadOnly();
    public IReadOnlyList<DataGridRow> Rows => _rows.AsReadOnly();

    /// <summary>
    /// Height of each row in pixels.
    /// </summary>
    public int RowHeight { get; set; } = 24;

    /// <summary>
    /// Height of the header row.
    /// </summary>
    public int HeaderHeight { get; set; } = 28;

    /// <summary>
    /// Allow multiple row selection.
    /// </summary>
    public bool MultiSelect { get; set; } = false;

    /// <summary>
    /// Show grid lines between cells.
    /// </summary>
    public bool ShowGridLines { get; set; } = true;

    /// <summary>
    /// Allow sorting by clicking column headers.
    /// </summary>
    public bool AllowSorting { get; set; } = true;

    /// <summary>
    /// Alternate row colors for better readability.
    /// </summary>
    public bool AlternateRowColors { get; set; } = true;

    /// <summary>
    /// Currently selected row index (-1 if none).
    /// </summary>
    public int SelectedRowIndex
    {
        get => _selectedRowIndex;
        set
        {
            if (value >= -1 && value < _rows.Count && _selectedRowIndex != value)
            {
                _selectedRowIndex = value;
                SelectionChanged?.Invoke(this, new DataGridSelectionEventArgs(_selectedRowIndex));
            }
        }
    }

    /// <summary>
    /// Get selected row data.
    /// </summary>
    public DataGridRow? SelectedRow => _selectedRowIndex >= 0 && _selectedRowIndex < _rows.Count
        ? _rows[_selectedRowIndex]
        : null;

    public event EventHandler<DataGridSelectionEventArgs>? SelectionChanged;
    public event EventHandler<DataGridCellEventArgs>? CellClick;
    public event EventHandler<DataGridCellEventArgs>? CellDoubleClick;
    public event EventHandler<DataGridSortEventArgs>? ColumnSorted;

    public UIDataGridView(string? id = null) : base(id)
    {
        Bounds = new DuskRect(0, 0, 400, 300);
    }

    /// <summary>
    /// Add a column definition.
    /// </summary>
    public void AddColumn(string name, string header, int width = 100, DataGridColumnType type = DataGridColumnType.Text)
    {
        _columns.Add(new DataGridColumn
        {
            Name = name,
            Header = header,
            Width = width,
            ColumnType = type
        });
    }

    /// <summary>
    /// Add a row of data.
    /// </summary>
    public void AddRow(params object?[] values)
    {
        var row = new DataGridRow();
        for (int i = 0; i < Math.Min(values.Length, _columns.Count); i++)
        {
            row.Cells.Add(new DataGridCell
            {
                Value = values[i],
                Column = _columns[i]
            });
        }
        // Fill remaining columns with null
        for (int i = values.Length; i < _columns.Count; i++)
        {
            row.Cells.Add(new DataGridCell { Value = null, Column = _columns[i] });
        }
        _rows.Add(row);
    }

    /// <summary>
    /// Clear all rows.
    /// </summary>
    public void ClearRows()
    {
        _rows.Clear();
        _selectedRowIndex = -1;
        _selectedRows.Clear();
        _scrollOffset = 0;
    }

    /// <summary>
    /// Clear everything.
    /// </summary>
    public void Clear()
    {
        _columns.Clear();
        ClearRows();
    }

    /// <summary>
    /// Bind to a data source (list of objects).
    /// </summary>
    public void DataBind<T>(IEnumerable<T> dataSource, params (string Property, string Header, int Width)[] columnDefs)
    {
        Clear();

        // Create columns
        foreach (var (property, header, width) in columnDefs)
        {
            AddColumn(property, header, width);
        }

        // Create rows
        var type = typeof(T);
        foreach (var item in dataSource)
        {
            var values = new object?[columnDefs.Length];
            for (int i = 0; i < columnDefs.Length; i++)
            {
                var prop = type.GetProperty(columnDefs[i].Property);
                values[i] = prop?.GetValue(item);
            }
            AddRow(values);
        }
    }

    /// <summary>
    /// Sort by column.
    /// </summary>
    public void SortByColumn(int columnIndex, bool ascending = true)
    {
        if (columnIndex < 0 || columnIndex >= _columns.Count) return;

        _sortColumnIndex = columnIndex;
        _sortAscending = ascending;

        _rows.Sort((a, b) =>
        {
            var valA = a.Cells[columnIndex].Value;
            var valB = b.Cells[columnIndex].Value;

            int cmp;
            if (valA == null && valB == null) cmp = 0;
            else if (valA == null) cmp = -1;
            else if (valB == null) cmp = 1;
            else if (valA is IComparable compA) cmp = compA.CompareTo(valB);
            else cmp = string.Compare(valA.ToString(), valB.ToString(), StringComparison.Ordinal);

            return ascending ? cmp : -cmp;
        });

        ColumnSorted?.Invoke(this, new DataGridSortEventArgs(columnIndex, ascending));
    }

    public override void HandleMouseDown(MouseEventArgs args)
    {
        base.HandleMouseDown(args);
        if (!Enabled) return;

        var localY = args.Position.Y - Bounds.Y;
        var localX = args.Position.X - Bounds.X;

        // Header click - sort
        if (localY < HeaderHeight && AllowSorting)
        {
            int colIndex = GetColumnAtX(localX);
            if (colIndex >= 0)
            {
                bool newAscending = _sortColumnIndex == colIndex ? !_sortAscending : true;
                SortByColumn(colIndex, newAscending);
            }
            return;
        }

        // Row click - select
        int rowIndex = GetRowAtY(localY);
        if (rowIndex >= 0 && rowIndex < _rows.Count)
        {
            if (MultiSelect && (args.Modifiers & KeyModifiers.Control) != 0)
            {
                if (_selectedRows.Contains(rowIndex))
                    _selectedRows.Remove(rowIndex);
                else
                    _selectedRows.Add(rowIndex);
            }
            else
            {
                _selectedRows.Clear();
                _selectedRows.Add(rowIndex);
            }
            SelectedRowIndex = rowIndex;

            int colIndex = GetColumnAtX(localX);
            CellClick?.Invoke(this, new DataGridCellEventArgs(rowIndex, colIndex, _rows[rowIndex].Cells[colIndex]));
        }
    }

    private int GetColumnAtX(int x)
    {
        int currentX = 0;
        for (int i = 0; i < _columns.Count; i++)
        {
            if (x >= currentX && x < currentX + _columns[i].Width)
                return i;
            currentX += _columns[i].Width;
        }
        return -1;
    }

    private int GetRowAtY(int y)
    {
        if (y < HeaderHeight) return -1;
        return (y - HeaderHeight + _scrollOffset) / RowHeight;
    }

    protected override void OnUpdate(float deltaTime)
    {
        // Could handle smooth scrolling here
    }

    protected override void OnRender(IRenderer renderer)
    {
        // Background
        renderer.FillRect(Bounds, new DuskColor(255, 255, 255));

        // Render header
        RenderHeader(renderer);

        // Render visible rows
        RenderRows(renderer);

        // Border
        renderer.DrawRect(Bounds, new DuskColor(80, 80, 80));
    }

    private void RenderHeader(IRenderer renderer)
    {
        var headerBounds = new DuskRect(Bounds.X, Bounds.Y, Bounds.Width, HeaderHeight);

        // Header background - MUI style bevel
        renderer.FillRect(headerBounds, new DuskColor(190, 190, 190));

        // Top highlight
        renderer.DrawLine(
            new DuskPoint(headerBounds.X, headerBounds.Y),
            new DuskPoint(headerBounds.X + headerBounds.Width, headerBounds.Y),
            DuskColor.White
        );

        // Bottom shadow
        renderer.DrawLine(
            new DuskPoint(headerBounds.X, headerBounds.Y + headerBounds.Height - 1),
            new DuskPoint(headerBounds.X + headerBounds.Width, headerBounds.Y + headerBounds.Height - 1),
            new DuskColor(100, 100, 100)
        );

        int currentX = Bounds.X;
        var font = new DuskFont("Default", 11, DuskFontStyle.Bold);

        for (int i = 0; i < _columns.Count; i++)
        {
            var col = _columns[i];
            var cellBounds = new DuskRect(currentX, Bounds.Y, col.Width, HeaderHeight);

            // Sort indicator
            string headerText = col.Header;
            if (_sortColumnIndex == i)
            {
                headerText += _sortAscending ? " ▲" : " ▼";
            }

            // Header text
            renderer.DrawText(headerText, new DuskPoint(currentX + 6, Bounds.Y + 6), font, DuskColor.Black);

            // Column separator
            renderer.DrawLine(
                new DuskPoint(currentX + col.Width - 1, Bounds.Y),
                new DuskPoint(currentX + col.Width - 1, Bounds.Y + HeaderHeight),
                new DuskColor(150, 150, 150)
            );

            currentX += col.Width;
        }
    }

    private void RenderRows(IRenderer renderer)
    {
        int visibleRows = (Bounds.Height - HeaderHeight) / RowHeight;
        int startRow = _scrollOffset / RowHeight;
        int endRow = Math.Min(startRow + visibleRows + 1, _rows.Count);

        var font = new DuskFont("Default", 11);
        var clipBounds = new DuskRect(Bounds.X, Bounds.Y + HeaderHeight, Bounds.Width, Bounds.Height - HeaderHeight);

        for (int rowIdx = startRow; rowIdx < endRow; rowIdx++)
        {
            int y = Bounds.Y + HeaderHeight + (rowIdx * RowHeight) - _scrollOffset;

            // Row background
            DuskColor rowBg;
            if (_selectedRows.Contains(rowIdx) || rowIdx == _selectedRowIndex)
            {
                rowBg = new DuskColor(100, 130, 180); // Selection blue
            }
            else if (rowIdx == _hoveredRowIndex)
            {
                rowBg = new DuskColor(220, 230, 240); // Hover highlight
            }
            else if (AlternateRowColors && rowIdx % 2 == 1)
            {
                rowBg = new DuskColor(245, 245, 250); // Alternate
            }
            else
            {
                rowBg = new DuskColor(255, 255, 255); // Normal
            }

            var rowBounds = new DuskRect(Bounds.X, y, Bounds.Width, RowHeight);
            renderer.FillRect(rowBounds, rowBg);

            // Cells
            int currentX = Bounds.X;
            var row = _rows[rowIdx];
            var textColor = (_selectedRows.Contains(rowIdx) || rowIdx == _selectedRowIndex)
                ? DuskColor.White
                : DuskColor.Black;

            for (int colIdx = 0; colIdx < _columns.Count && colIdx < row.Cells.Count; colIdx++)
            {
                var col = _columns[colIdx];
                var cell = row.Cells[colIdx];
                var cellBounds = new DuskRect(currentX, y, col.Width, RowHeight);

                // Render cell content
                RenderCell(renderer, cell, cellBounds, font, textColor);

                // Grid line
                if (ShowGridLines)
                {
                    renderer.DrawLine(
                        new DuskPoint(currentX + col.Width - 1, y),
                        new DuskPoint(currentX + col.Width - 1, y + RowHeight),
                        new DuskColor(220, 220, 220)
                    );
                }

                currentX += col.Width;
            }

            // Horizontal grid line
            if (ShowGridLines)
            {
                renderer.DrawLine(
                    new DuskPoint(Bounds.X, y + RowHeight - 1),
                    new DuskPoint(Bounds.X + Bounds.Width, y + RowHeight - 1),
                    new DuskColor(220, 220, 220)
                );
            }
        }
    }

    private void RenderCell(IRenderer renderer, DataGridCell cell, DuskRect bounds, DuskFont font, DuskColor textColor)
    {
        string text = cell.Value?.ToString() ?? "";

        switch (cell.Column.ColumnType)
        {
            case DataGridColumnType.Text:
                renderer.DrawText(text, new DuskPoint(bounds.X + 4, bounds.Y + 4), font, textColor);
                break;

            case DataGridColumnType.Number:
                // Right-align numbers
                var size = renderer.MeasureText(text, font);
                renderer.DrawText(text, new DuskPoint(bounds.X + bounds.Width - size.Width - 4, bounds.Y + 4), font, textColor);
                break;

            case DataGridColumnType.Boolean:
                // Render as checkbox
                var checkBounds = new DuskRect(bounds.X + (bounds.Width - 16) / 2, bounds.Y + (bounds.Height - 16) / 2, 16, 16);
                renderer.FillRect(checkBounds, DuskColor.White);
                renderer.DrawRect(checkBounds, new DuskColor(100, 100, 100));
                if (cell.Value is true)
                {
                    // Checkmark
                    renderer.DrawLine(new DuskPoint(checkBounds.X + 3, checkBounds.Y + 8), new DuskPoint(checkBounds.X + 6, checkBounds.Y + 11), new DuskColor(0, 150, 0));
                    renderer.DrawLine(new DuskPoint(checkBounds.X + 6, checkBounds.Y + 11), new DuskPoint(checkBounds.X + 12, checkBounds.Y + 4), new DuskColor(0, 150, 0));
                }
                break;

            case DataGridColumnType.Progress:
                // Render as progress bar
                var progBounds = new DuskRect(bounds.X + 4, bounds.Y + (bounds.Height - 12) / 2, bounds.Width - 8, 12);
                renderer.FillRect(progBounds, new DuskColor(200, 200, 200));
                if (cell.Value is float or double or int)
                {
                    float progress = Convert.ToSingle(cell.Value);
                    int fillWidth = (int)(progBounds.Width * Math.Clamp(progress, 0, 1));
                    renderer.FillRect(new DuskRect(progBounds.X, progBounds.Y, fillWidth, progBounds.Height), new DuskColor(100, 150, 200));
                }
                renderer.DrawRect(progBounds, new DuskColor(150, 150, 150));
                break;

            case DataGridColumnType.Image:
                // Placeholder for image rendering
                renderer.DrawText("[IMG]", new DuskPoint(bounds.X + 4, bounds.Y + 4), font, new DuskColor(100, 100, 100));
                break;
        }
    }

    /// <summary>
    /// Scroll to show a specific row.
    /// </summary>
    public void EnsureRowVisible(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _rows.Count) return;

        int rowTop = rowIndex * RowHeight;
        int rowBottom = rowTop + RowHeight;
        int visibleTop = _scrollOffset;
        int visibleBottom = _scrollOffset + (Bounds.Height - HeaderHeight);

        if (rowTop < visibleTop)
        {
            _scrollOffset = rowTop;
        }
        else if (rowBottom > visibleBottom)
        {
            _scrollOffset = rowBottom - (Bounds.Height - HeaderHeight);
        }
    }
}

/// <summary>
/// Column definition for the data grid.
/// </summary>
public class DataGridColumn
{
    public string Name { get; set; } = "";
    public string Header { get; set; } = "";
    public int Width { get; set; } = 100;
    public DataGridColumnType ColumnType { get; set; } = DataGridColumnType.Text;
    public bool Sortable { get; set; } = true;
    public bool Resizable { get; set; } = true;
}

/// <summary>
/// Column type for specialized rendering.
/// </summary>
public enum DataGridColumnType
{
    Text,
    Number,
    Boolean,
    Progress,
    Image,
    Custom
}

/// <summary>
/// A row in the data grid.
/// </summary>
public class DataGridRow
{
    public List<DataGridCell> Cells { get; } = new();
    public object? Tag { get; set; }
}

/// <summary>
/// A cell in the data grid.
/// </summary>
public class DataGridCell
{
    public object? Value { get; set; }
    public DataGridColumn Column { get; set; } = null!;
    public DuskColor? ForegroundColor { get; set; }
    public DuskColor? BackgroundColor { get; set; }
}

/// <summary>
/// Event args for selection changes.
/// </summary>
public class DataGridSelectionEventArgs : EventArgs
{
    public int RowIndex { get; }
    public DataGridSelectionEventArgs(int rowIndex) => RowIndex = rowIndex;
}

/// <summary>
/// Event args for cell events.
/// </summary>
public class DataGridCellEventArgs : EventArgs
{
    public int RowIndex { get; }
    public int ColumnIndex { get; }
    public DataGridCell Cell { get; }

    public DataGridCellEventArgs(int row, int col, DataGridCell cell)
    {
        RowIndex = row;
        ColumnIndex = col;
        Cell = cell;
    }
}

/// <summary>
/// Event args for sort events.
/// </summary>
public class DataGridSortEventArgs : EventArgs
{
    public int ColumnIndex { get; }
    public bool Ascending { get; }

    public DataGridSortEventArgs(int col, bool asc)
    {
        ColumnIndex = col;
        Ascending = asc;
    }
}
