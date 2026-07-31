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
        private const string AllCoursesText = "Все курсы";
        private const string AllWeeksText = "Все недели";
        private const string AllDatesText = "Все даты";

        private ScheduleDocument _document;
        private TextBox _urlTextBox;
        private ComboBox _courseComboBox;
        private ComboBox _weekComboBox;
        private ComboBox _modeComboBox;
        private ComboBox _filterComboBox;
        private ComboBox _dateComboBox;
        private Label _currentLabel;
        private Label _statusLabel;
        private DataGridView _grid;
        private TextBox _notesTextBox;
        private bool _updatingFilters;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = AppInfo.ProductName + " Viewer v" + AppInfo.Version;
            MinimumSize = new Size(1120, 760);
            StartPosition = FormStartPosition.CenterScreen;
            UiTheme.StyleForm(this);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 7;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            Controls.Add(root);

            root.Controls.Add(CreateHeader(), 0, 0);
            root.Controls.Add(CreateLoadPanel(), 0, 1);
            root.Controls.Add(CreateFilterPanel(), 0, 2);

            Panel currentPanel = new Panel();
            currentPanel.Dock = DockStyle.Fill;
            currentPanel.Padding = new Padding(14, 4, 14, 8);
            currentPanel.BackColor = UiTheme.Background;
            _currentLabel = new Label();
            _currentLabel.Dock = DockStyle.Fill;
            _currentLabel.BackColor = UiTheme.Surface;
            _currentLabel.BorderStyle = BorderStyle.FixedSingle;
            _currentLabel.ForeColor = UiTheme.Text;
            _currentLabel.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            _currentLabel.Padding = new Padding(12, 0, 12, 0);
            _currentLabel.TextAlign = ContentAlignment.MiddleLeft;
            _currentLabel.Text = "Загрузите расписание.";
            currentPanel.Controls.Add(_currentLabel);
            root.Controls.Add(currentPanel, 0, 3);

            _grid = new DataGridView();
            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.RowHeadersVisible = false;
            _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.AutoGenerateColumns = false;
            ConfigureGrid(_grid);
            UiTheme.StyleGrid(_grid);
            root.Controls.Add(_grid, 0, 4);

            Panel notesPanel = new Panel();
            notesPanel.Dock = DockStyle.Fill;
            notesPanel.Padding = new Padding(14, 8, 14, 8);
            notesPanel.BackColor = UiTheme.Background;
            _notesTextBox = new TextBox();
            _notesTextBox.Dock = DockStyle.Fill;
            _notesTextBox.Multiline = true;
            _notesTextBox.ReadOnly = true;
            _notesTextBox.ScrollBars = ScrollBars.Vertical;
            _notesTextBox.BackColor = Color.FromArgb(255, 250, 229);
            _notesTextBox.BorderStyle = BorderStyle.FixedSingle;
            _notesTextBox.ForeColor = Color.FromArgb(93, 74, 10);
            _notesTextBox.Text = "Коды подключения появятся для дистанционных занятий.";
            notesPanel.Controls.Add(_notesTextBox);
            root.Controls.Add(notesPanel, 0, 5);

            _statusLabel = new Label();
            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.BackColor = UiTheme.Background;
            _statusLabel.ForeColor = UiTheme.Muted;
            _statusLabel.Padding = new Padding(14, 0, 14, 0);
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusLabel.Text = "Введите адрес сервера, например http://127.0.0.1:5088/, или откройте локальный JSON.";
            root.Controls.Add(_statusLabel, 0, 6);
        }

        private Control CreateHeader()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = UiTheme.Header;
            header.Padding = new Padding(18, 16, 18, 12);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            header.Controls.Add(layout);

            layout.Controls.Add(UiTheme.CreateTitle("Расписание НФ НИТУ МИСИС"), 0, 0);
            layout.Controls.Add(UiTheme.CreateSubtitle("Просмотр по группе или преподавателю • подключение к серверу учебного отдела • v" + AppInfo.Version), 0, 1);
            return header;
        }

        private Control CreateLoadPanel()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(14, 14, 14, 6);
            panel.BackColor = UiTheme.Background;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 4;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            panel.Controls.Add(layout);

            Label label = CreateSmallLabel("Сетевой URL");
            layout.Controls.Add(label, 0, 0);

            _urlTextBox = new TextBox();
            _urlTextBox.Dock = DockStyle.Fill;
            _urlTextBox.Text = "http://127.0.0.1:5088/";
            UiTheme.StyleTextBox(_urlTextBox);
            layout.Controls.Add(_urlTextBox, 1, 0);

            Button loadButton = new Button();
            loadButton.Text = "Загрузить";
            loadButton.Dock = DockStyle.Fill;
            loadButton.Click += LoadButtonClick;
            UiTheme.StyleButton(loadButton, true);
            layout.Controls.Add(loadButton, 2, 0);

            Button openButton = new Button();
            openButton.Text = "Открыть JSON";
            openButton.Dock = DockStyle.Fill;
            openButton.Click += OpenButtonClick;
            UiTheme.StyleButton(openButton, false);
            layout.Controls.Add(openButton, 3, 0);

            return panel;
        }

        private Control CreateFilterPanel()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(14, 0, 14, 8);
            panel.BackColor = UiTheme.Background;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 5;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            panel.Controls.Add(layout);

            _courseComboBox = new ComboBox();
            _courseComboBox.Dock = DockStyle.Fill;
            _courseComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _courseComboBox.SelectedIndexChanged += FilterChanged;
            UiTheme.StyleCombo(_courseComboBox);
            layout.Controls.Add(_courseComboBox, 0, 0);

            _weekComboBox = new ComboBox();
            _weekComboBox.Dock = DockStyle.Fill;
            _weekComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _weekComboBox.SelectedIndexChanged += FilterChanged;
            UiTheme.StyleCombo(_weekComboBox);
            layout.Controls.Add(_weekComboBox, 1, 0);

            _modeComboBox = new ComboBox();
            _modeComboBox.Dock = DockStyle.Fill;
            _modeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _modeComboBox.Items.Add("Группа");
            _modeComboBox.Items.Add("Преподаватель");
            _modeComboBox.SelectedIndex = 0;
            _modeComboBox.SelectedIndexChanged += FilterChanged;
            UiTheme.StyleCombo(_modeComboBox);
            layout.Controls.Add(_modeComboBox, 2, 0);

            _filterComboBox = new ComboBox();
            _filterComboBox.Dock = DockStyle.Fill;
            _filterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _filterComboBox.SelectedIndexChanged += FilterChanged;
            UiTheme.StyleCombo(_filterComboBox);
            layout.Controls.Add(_filterComboBox, 3, 0);

            _dateComboBox = new ComboBox();
            _dateComboBox.Dock = DockStyle.Fill;
            _dateComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _dateComboBox.SelectedIndexChanged += FilterChanged;
            UiTheme.StyleCombo(_dateComboBox);
            layout.Controls.Add(_dateComboBox, 4, 0);

            return panel;
        }

        private static Label CreateSmallLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.ForeColor = UiTheme.Muted;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Dock = DockStyle.Fill;
            return label;
        }

        private static void ConfigureGrid(DataGridView grid)
        {
            AddColumn(grid, "Date", "Дата", 90);
            AddColumn(grid, "Course", "Курс", 80);
            AddColumn(grid, "WeekType", "Неделя", 90);
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
                string url = NetworkHelper.NormalizeScheduleJsonUrl(_urlTextBox.Text);
                _urlTextBox.Text = url;
                string json = NetworkHelper.LoadScheduleText(url, 8000);
                string status = new Uri(url).Scheme == Uri.UriSchemeFile
                    ? "Расписание открыто из локального JSON."
                    : "Расписание загружено по сети.";
                LoadDocument(ScheduleJsonSerializer.FromJson(json), status);
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
            if (_document == null)
            {
                return;
            }

            _updatingFilters = true;
            string selectedCourse = SelectedValue(_courseComboBox);
            string selectedWeek = SelectedValue(_weekComboBox);
            string selectedPerson = SelectedValue(_filterComboBox);
            string selectedDate = SelectedValue(_dateComboBox);

            FillCombo(_courseComboBox, AllCoursesText, _document.Courses, selectedCourse);
            FillCombo(_weekComboBox, AllWeeksText, _document.WeekTypes, selectedWeek);

            _filterComboBox.SelectedIndexChanged -= FilterChanged;
            _dateComboBox.SelectedIndexChanged -= FilterChanged;

            _filterComboBox.Items.Clear();
            _dateComboBox.Items.Clear();

            List<Lesson> baseLessons = FilterByCourseAndWeek(_document.Lessons).ToList();
            IEnumerable<string> values = _modeComboBox.SelectedIndex == 1
                ? baseLessons.Select(x => x.Teacher)
                : baseLessons.Select(x => x.Group);
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value) && !_filterComboBox.Items.Contains(value))
                {
                    _filterComboBox.Items.Add(value);
                }
            }

            _dateComboBox.Items.Add(AllDatesText);
            foreach (Lesson lesson in baseLessons.GroupBy(x => x.Date).Select(x => x.First()).OrderBy(x => x.Date))
            {
                _dateComboBox.Items.Add(lesson.Date + " " + lesson.DayName);
            }

            SelectComboValue(_filterComboBox, selectedPerson, false);
            SelectComboValue(_dateComboBox, selectedDate, true);
            _filterComboBox.SelectedIndexChanged += FilterChanged;
            _dateComboBox.SelectedIndexChanged += FilterChanged;
            _updatingFilters = false;
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            if (_updatingFilters)
            {
                return;
            }

            if (_document != null && (sender == _modeComboBox || sender == _courseComboBox || sender == _weekComboBox))
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
                _notesTextBox.Text = "Коды подключения появятся для дистанционных занятий.";
                return;
            }

            string selected = _filterComboBox.SelectedItem.ToString();
            bool byTeacher = _modeComboBox.SelectedIndex == 1;
            IEnumerable<Lesson> lessons = FilterByCourseAndWeek(_document.Lessons);

            if (byTeacher)
            {
                lessons = lessons.Where(x => string.Equals(x.Teacher, selected, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                lessons = lessons.Where(x => string.Equals(x.Group, selected, StringComparison.OrdinalIgnoreCase));
            }

            if (_dateComboBox.SelectedIndex > 0 && _dateComboBox.SelectedItem != null)
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
                _currentLabel.BackColor = UiTheme.Surface;
                _currentLabel.ForeColor = UiTheme.Text;
                _currentLabel.Text = "Сейчас занятие не идет.";
                return;
            }

            _currentLabel.BackColor = Color.FromArgb(255, 247, 230);
            _currentLabel.ForeColor = UiTheme.Text;
            if (byTeacher)
            {
                _currentLabel.Text = string.Format("Сейчас: {0} пара, {1}, группа {2}, {3}, ауд./среда: {4}", current.PairNumber, current.TimeRange, current.Group, current.Subject, current.Room);
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
                : "Коды подключения:" + Environment.NewLine + string.Join(Environment.NewLine, lines.ToArray());
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

        private IEnumerable<Lesson> FilterByCourseAndWeek(IEnumerable<Lesson> lessons)
        {
            string course = SelectedValue(_courseComboBox);
            string week = SelectedValue(_weekComboBox);

            foreach (Lesson lesson in lessons)
            {
                if (!string.IsNullOrWhiteSpace(course) && course != AllCoursesText &&
                    !string.Equals(lesson.Course, course, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(week) && week != AllWeeksText &&
                    !string.Equals(lesson.WeekType, week, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return lesson;
            }
        }

        private static void FillCombo(ComboBox comboBox, string allText, IEnumerable<string> values, string selectedValue)
        {
            comboBox.Items.Clear();
            comboBox.Items.Add(allText);
            if (values != null)
            {
                foreach (string value in values)
                {
                    if (!string.IsNullOrWhiteSpace(value) && !comboBox.Items.Contains(value))
                    {
                        comboBox.Items.Add(value);
                    }
                }
            }

            SelectComboValue(comboBox, selectedValue, true);
        }

        private static void SelectComboValue(ComboBox comboBox, string value, bool selectFirstWhenMissing)
        {
            if (!string.IsNullOrWhiteSpace(value) && comboBox.Items.Contains(value))
            {
                comboBox.SelectedItem = value;
                return;
            }

            if (selectFirstWhenMissing && comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
                return;
            }

            if (!selectFirstWhenMissing && comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
            }
        }

        private static string SelectedValue(ComboBox comboBox)
        {
            return comboBox != null && comboBox.SelectedItem != null ? comboBox.SelectedItem.ToString() : string.Empty;
        }
    }
}
