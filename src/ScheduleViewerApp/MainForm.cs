using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ScheduleParser.Core;

namespace ScheduleViewerApp
{
    public sealed class MainForm : Form
    {
        private ScheduleDocument _document;
        private TextBox _urlTextBox;
        private ComboBox _modeComboBox;
        private ComboBox _filterComboBox;
        private ComboBox _dateComboBox;
        private Label _currentLabel;
        private Label _statusLabel;
        private DataGridView _grid;
        private TextBox _notesTextBox;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Расписание НФ НИТУ МИСИС";
            MinimumSize = new Size(1080, 720);
            StartPosition = FormStartPosition.CenterScreen;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 6;
            root.Padding = new Padding(12);
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            FlowLayoutPanel loadPanel = new FlowLayoutPanel();
            loadPanel.Dock = DockStyle.Fill;
            loadPanel.AutoSize = true;
            loadPanel.WrapContents = false;
            root.Controls.Add(loadPanel, 0, 0);

            Label urlLabel = new Label();
            urlLabel.Text = "URL:";
            urlLabel.TextAlign = ContentAlignment.MiddleLeft;
            urlLabel.Width = 38;
            urlLabel.Height = 32;
            loadPanel.Controls.Add(urlLabel);

            _urlTextBox = new TextBox();
            _urlTextBox.Width = 430;
            _urlTextBox.Text = "http://localhost:5088/schedule.json";
            loadPanel.Controls.Add(_urlTextBox);

            Button loadButton = new Button();
            loadButton.Text = "Загрузить";
            loadButton.Width = 105;
            loadButton.Height = 32;
            loadButton.Click += LoadButtonClick;
            loadPanel.Controls.Add(loadButton);

            Button openButton = new Button();
            openButton.Text = "Открыть JSON";
            openButton.Width = 120;
            openButton.Height = 32;
            openButton.Click += OpenButtonClick;
            loadPanel.Controls.Add(openButton);

            FlowLayoutPanel filterPanel = new FlowLayoutPanel();
            filterPanel.Dock = DockStyle.Fill;
            filterPanel.AutoSize = true;
            filterPanel.WrapContents = false;
            filterPanel.Margin = new Padding(0, 8, 0, 8);
            root.Controls.Add(filterPanel, 0, 1);

            _modeComboBox = new ComboBox();
            _modeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _modeComboBox.Width = 150;
            _modeComboBox.Items.Add("Группа");
            _modeComboBox.Items.Add("Преподаватель");
            _modeComboBox.SelectedIndex = 0;
            _modeComboBox.SelectedIndexChanged += FilterChanged;
            filterPanel.Controls.Add(_modeComboBox);

            _filterComboBox = new ComboBox();
            _filterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _filterComboBox.Width = 220;
            _filterComboBox.SelectedIndexChanged += FilterChanged;
            filterPanel.Controls.Add(_filterComboBox);

            _dateComboBox = new ComboBox();
            _dateComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _dateComboBox.Width = 220;
            _dateComboBox.SelectedIndexChanged += FilterChanged;
            filterPanel.Controls.Add(_dateComboBox);

            _currentLabel = new Label();
            _currentLabel.Dock = DockStyle.Fill;
            _currentLabel.AutoSize = true;
            _currentLabel.Font = new Font(Font.FontFamily, 10, FontStyle.Bold);
            _currentLabel.Padding = new Padding(0, 0, 0, 8);
            _currentLabel.Text = "Загрузите расписание.";
            root.Controls.Add(_currentLabel, 0, 2);

            _grid = new DataGridView();
            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.RowHeadersVisible = false;
            _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.BackgroundColor = Color.White;
            _grid.AutoGenerateColumns = false;
            ConfigureGrid(_grid);
            root.Controls.Add(_grid, 0, 3);

            _notesTextBox = new TextBox();
            _notesTextBox.Dock = DockStyle.Fill;
            _notesTextBox.Multiline = true;
            _notesTextBox.ReadOnly = true;
            _notesTextBox.ScrollBars = ScrollBars.Vertical;
            _notesTextBox.BackColor = Color.White;
            root.Controls.Add(_notesTextBox, 0, 4);

            _statusLabel = new Label();
            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.AutoSize = true;
            _statusLabel.Padding = new Padding(0, 8, 0, 0);
            _statusLabel.Text = "Сетевой просмотр требует доступного интернета и сервера расписания.";
            root.Controls.Add(_statusLabel, 0, 5);
        }

        private static void ConfigureGrid(DataGridView grid)
        {
            AddColumn(grid, "Date", "Дата", 90);
            AddColumn(grid, "DayName", "День", 105);
            AddColumn(grid, "PairNumber", "№", 45);
            AddColumn(grid, "TimeRange", "Время", 100);
            AddColumn(grid, "Group", "Группа", 90);
            AddColumn(grid, "Subgroup", "Подгруппа", 95);
            AddColumn(grid, "Subject", "Предмет", 270);
            AddColumn(grid, "LessonType", "Тип", 95);
            AddColumn(grid, "Teacher", "Преподаватель", 140);
            AddColumn(grid, "Room", "Где", 90);
        }

        private static void AddColumn(DataGridView grid, string propertyName, string header, int width)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.DataPropertyName = propertyName;
            column.HeaderText = header;
            column.Width = width;
            column.SortMode = DataGridViewColumnSortMode.Automatic;
            grid.Columns.Add(column);
        }

        private void LoadButtonClick(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                if (!NetworkHelper.IsInternetAvailable())
                {
                    throw new InvalidOperationException("Нет подключения к интернету. Расписание по сети загрузить нельзя.");
                }

                string json = NetworkHelper.DownloadStringWithTimeout(_urlTextBox.Text, 8000);
                LoadDocument(ScheduleJsonSerializer.FromJson(json), "Расписание загружено по сети.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Не удалось загрузить расписание по сети.");
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void OpenButtonClick(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    LoadDocument(ScheduleJsonSerializer.Load(dialog.FileName), "Расписание открыто из JSON.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Ошибка открытия", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadDocument(ScheduleDocument document, string status)
        {
            _document = document;
            FillFilters();
            ApplyFilter();
            SetStatus(status);
        }

        private void FillFilters()
        {
            _filterComboBox.SelectedIndexChanged -= FilterChanged;
            _dateComboBox.SelectedIndexChanged -= FilterChanged;

            _filterComboBox.Items.Clear();
            _dateComboBox.Items.Clear();

            IEnumerable<string> values = _modeComboBox.SelectedIndex == 1 ? _document.Teachers : _document.Groups;
            foreach (string value in values)
            {
                _filterComboBox.Items.Add(value);
            }

            _dateComboBox.Items.Add("Все даты");
            foreach (Lesson lesson in _document.Lessons.GroupBy(x => x.Date).Select(x => x.First()).OrderBy(x => x.Date))
            {
                _dateComboBox.Items.Add(lesson.Date + " " + lesson.DayName);
            }

            if (_filterComboBox.Items.Count > 0)
            {
                _filterComboBox.SelectedIndex = 0;
            }

            _dateComboBox.SelectedIndex = 0;
            _filterComboBox.SelectedIndexChanged += FilterChanged;
            _dateComboBox.SelectedIndexChanged += FilterChanged;
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            if (sender == _modeComboBox && _document != null)
            {
                FillFilters();
            }

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (_document == null || _filterComboBox.SelectedItem == null)
            {
                _grid.DataSource = new List<Lesson>();
                _currentLabel.Text = "Загрузите расписание.";
                _notesTextBox.Text = string.Empty;
                return;
            }

            string selected = _filterComboBox.SelectedItem.ToString();
            bool byTeacher = _modeComboBox.SelectedIndex == 1;
            IEnumerable<Lesson> lessons = _document.Lessons;

            if (byTeacher)
            {
                lessons = lessons.Where(x => string.Equals(x.Teacher, selected, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                lessons = lessons.Where(x => string.Equals(x.Group, selected, StringComparison.OrdinalIgnoreCase));
            }

            if (_dateComboBox.SelectedIndex > 0)
            {
                string date = _dateComboBox.SelectedItem.ToString().Substring(0, 10);
                lessons = lessons.Where(x => string.Equals(x.Date, date, StringComparison.OrdinalIgnoreCase));
            }

            List<Lesson> visible = lessons.ToList();
            _grid.DataSource = visible;

            foreach (DataGridViewRow row in _grid.Rows)
            {
                Lesson lesson = row.DataBoundItem as Lesson;
                if (lesson != null)
                {
                    ApplyLessonColor(row, lesson);
                }
            }

            ShowCurrentLesson(visible, byTeacher);
            ShowDigitalToolNotes(visible);
            SetStatus(string.Format("Показано занятий: {0}.", visible.Count));
        }

        private void ShowCurrentLesson(List<Lesson> visible, bool byTeacher)
        {
            DateTime now = DateTime.Now;
            string today = now.ToString("yyyy-MM-dd");
            TimeSpan time = now.TimeOfDay;
            Lesson current = null;

            foreach (Lesson lesson in visible)
            {
                TimeSpan start;
                TimeSpan end;
                if (lesson.Date == today &&
                    TimeSpan.TryParse(lesson.StartTime, out start) &&
                    TimeSpan.TryParse(lesson.EndTime, out end) &&
                    time >= start &&
                    time <= end)
                {
                    current = lesson;
                    break;
                }
            }

            if (current == null)
            {
                _currentLabel.Text = "Сейчас занятие не идет.";
                return;
            }

            if (byTeacher)
            {
                _currentLabel.Text = string.Format("Сейчас: {0}, {1}, {2}, {3}, ауд./среда: {4}", current.PairNumber, current.TimeRange, current.Group, current.Subject, current.Room);
            }
            else
            {
                _currentLabel.Text = string.Format("Сейчас: {0} пара, {1}, {2}, преподаватель: {3}, ауд./среда: {4}", current.PairNumber, current.TimeRange, current.Subject, current.Teacher, current.Room);
            }
        }

        private void ShowDigitalToolNotes(List<Lesson> visible)
        {
            List<string> lines = new List<string>();
            foreach (Lesson lesson in visible.Where(x => x.IsRemote && !string.IsNullOrWhiteSpace(x.Teacher)))
            {
                DigitalTool tool = FindDigitalTool(lesson.Teacher);
                string info = tool == null || string.IsNullOrWhiteSpace(tool.ConnectionInfo) ? "код не указан" : tool.ConnectionInfo;
                string line = lesson.Teacher + " - " + info;
                if (!lines.Contains(line))
                {
                    lines.Add(line);
                }
            }

            _notesTextBox.Text = lines.Count == 0
                ? "Удаленных занятий в выбранном расписании нет."
                : "Коды подключения:\r\n" + string.Join("\r\n", lines.ToArray());
        }

        private DigitalTool FindDigitalTool(string teacher)
        {
            if (_document == null || _document.DigitalTools == null)
            {
                return null;
            }

            foreach (DigitalTool tool in _document.DigitalTools)
            {
                if (string.Equals(tool.Teacher, teacher, StringComparison.OrdinalIgnoreCase))
                {
                    return tool;
                }
            }

            return null;
        }

        private static void ApplyLessonColor(DataGridViewRow row, Lesson lesson)
        {
            if (!string.IsNullOrWhiteSpace(lesson.ColorHex))
            {
                try
                {
                    row.DefaultCellStyle.BackColor = ColorTranslator.FromHtml(lesson.ColorHex);
                }
                catch
                {
                }
            }
            else if (lesson.IsRemote)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(222, 240, 255);
            }
        }

        private void SetStatus(string text)
        {
            _statusLabel.Text = text;
        }
    }
}
