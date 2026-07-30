using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ScheduleParser.Core;

namespace ScheduleDepartmentApp
{
    public sealed class MainForm : Form
    {
        private readonly XlsxScheduleParser _parser;
        private readonly SimpleScheduleServer _server;
        private ScheduleDocument _document;
        private TextBox _filesTextBox;
        private TextBox _jsonPathTextBox;
        private TextBox _portTextBox;
        private TextBox _serverUrlsTextBox;
        private Label _statusLabel;
        private Label _serverStateLabel;
        private DataGridView _grid;
        private Button _saveButton;
        private Button _startServerButton;
        private Button _stopServerButton;
        private Button _openWebButton;

        public MainForm()
        {
            _parser = new XlsxScheduleParser();
            _server = new SimpleScheduleServer();
            InitializeComponent();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _server.Dispose();
            base.OnFormClosing(e);
        }

        private void InitializeComponent()
        {
            Text = "Учебный отдел - " + AppInfo.ProductName + " v" + AppInfo.Version;
            MinimumSize = new Size(1120, 740);
            StartPosition = FormStartPosition.CenterScreen;
            UiTheme.StyleForm(this);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));
            Controls.Add(root);

            root.Controls.Add(CreateHeader(), 0, 0);
            root.Controls.Add(CreateImportPanel(), 0, 1);

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
            root.Controls.Add(_grid, 0, 2);

            root.Controls.Add(CreateServerPanel(), 0, 3);
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
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            header.Controls.Add(layout);

            layout.Controls.Add(UiTheme.CreateTitle("Учебный отдел"), 0, 0);
            layout.Controls.Add(UiTheme.CreateSubtitle("Импорт Excel, публикация расписания в локальной сети и веб-доступ для телефона • v" + AppInfo.Version), 0, 1);
            return header;
        }

        private Control CreateImportPanel()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Top;
            panel.Height = 118;
            panel.Padding = new Padding(14);
            panel.BackColor = UiTheme.Background;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            panel.Controls.Add(layout);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.WrapContents = false;
            actions.BackColor = UiTheme.Background;
            layout.Controls.Add(actions, 0, 0);

            Button openButton = new Button();
            openButton.Text = "Открыть Excel";
            openButton.Width = 132;
            openButton.Click += OpenButtonClick;
            UiTheme.StyleButton(openButton, true);
            actions.Controls.Add(openButton);

            _saveButton = new Button();
            _saveButton.Text = "Сохранить JSON";
            _saveButton.Width = 150;
            _saveButton.Enabled = false;
            _saveButton.Click += SaveButtonClick;
            UiTheme.StyleButton(_saveButton, false);
            actions.Controls.Add(_saveButton);

            Label jsonLabel = CreateSmallLabel("Файл публикации");
            jsonLabel.Width = 120;
            actions.Controls.Add(jsonLabel);

            _jsonPathTextBox = new TextBox();
            _jsonPathTextBox.Width = 480;
            _jsonPathTextBox.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "schedule.json");
            UiTheme.StyleTextBox(_jsonPathTextBox);
            actions.Controls.Add(_jsonPathTextBox);

            _filesTextBox = new TextBox();
            _filesTextBox.Dock = DockStyle.Fill;
            _filesTextBox.ReadOnly = true;
            _filesTextBox.Text = "Файлы Excel пока не выбраны";
            UiTheme.StyleTextBox(_filesTextBox);
            layout.Controls.Add(_filesTextBox, 0, 1);

            _statusLabel = new Label();
            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.Text = "Выберите Excel-файл расписания. После импорта можно запустить сетевой сервер.";
            _statusLabel.ForeColor = UiTheme.Muted;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(_statusLabel, 0, 2);

            return panel;
        }

        private Control CreateServerPanel()
        {
            Panel outer = new Panel();
            outer.Dock = DockStyle.Fill;
            outer.Padding = new Padding(14, 8, 14, 14);
            outer.BackColor = UiTheme.Background;

            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(14);
            panel.BackColor = UiTheme.Surface;
            panel.BorderStyle = BorderStyle.FixedSingle;
            outer.Controls.Add(panel);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(layout);

            FlowLayoutPanel controls = new FlowLayoutPanel();
            controls.Dock = DockStyle.Fill;
            controls.WrapContents = false;
            layout.Controls.Add(controls, 0, 0);

            Label portLabel = CreateSmallLabel("Порт");
            portLabel.Width = 46;
            controls.Controls.Add(portLabel);

            _portTextBox = new TextBox();
            _portTextBox.Width = 78;
            _portTextBox.Text = "5088";
            UiTheme.StyleTextBox(_portTextBox);
            controls.Controls.Add(_portTextBox);

            _startServerButton = new Button();
            _startServerButton.Text = "Запустить";
            _startServerButton.Width = 118;
            _startServerButton.Enabled = false;
            _startServerButton.Click += StartServerButtonClick;
            UiTheme.StyleButton(_startServerButton, true);
            controls.Controls.Add(_startServerButton);

            _stopServerButton = new Button();
            _stopServerButton.Text = "Остановить";
            _stopServerButton.Width = 118;
            _stopServerButton.Enabled = false;
            _stopServerButton.Click += StopServerButtonClick;
            UiTheme.StyleButton(_stopServerButton, false);
            controls.Controls.Add(_stopServerButton);

            _openWebButton = new Button();
            _openWebButton.Text = "Открыть";
            _openWebButton.Width = 104;
            _openWebButton.Enabled = false;
            _openWebButton.Click += OpenWebButtonClick;
            UiTheme.StyleButton(_openWebButton, false);
            controls.Controls.Add(_openWebButton);

            _serverStateLabel = new Label();
            _serverStateLabel.Dock = DockStyle.Fill;
            _serverStateLabel.Text = "Сервер остановлен";
            _serverStateLabel.ForeColor = UiTheme.Muted;
            _serverStateLabel.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            _serverStateLabel.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(_serverStateLabel, 1, 0);

            Label hint = new Label();
            hint.Dock = DockStyle.Fill;
            hint.ForeColor = UiTheme.Muted;
            hint.Text = "На телефоне или другом ПК откройте ссылку из правого блока. Устройства должны быть в одной Wi-Fi/LAN сети. Если Windows спросит доступ через брандмауэр, разрешите частные сети.";
            hint.TextAlign = ContentAlignment.TopLeft;
            layout.Controls.Add(hint, 0, 1);

            _serverUrlsTextBox = new TextBox();
            _serverUrlsTextBox.Dock = DockStyle.Fill;
            _serverUrlsTextBox.Multiline = true;
            _serverUrlsTextBox.ReadOnly = true;
            _serverUrlsTextBox.ScrollBars = ScrollBars.Vertical;
            _serverUrlsTextBox.Text = "Ссылки появятся после запуска сервера.";
            UiTheme.StyleTextBox(_serverUrlsTextBox);
            layout.Controls.Add(_serverUrlsTextBox, 1, 1);

            return outer;
        }

        private static Label CreateSmallLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.ForeColor = UiTheme.Muted;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Height = 36;
            return label;
        }

        private static void ConfigureGrid(DataGridView grid)
        {
            AddColumn(grid, "Date", "Дата", 90);
            AddColumn(grid, "DayName", "День", 105);
            AddColumn(grid, "PairNumber", "№", 45);
            AddColumn(grid, "TimeRange", "Время", 100);
            AddColumn(grid, "Group", "Группа", 90);
            AddColumn(grid, "Subgroup", "Подгруппа", 95);
            AddColumn(grid, "Subject", "Предмет", 260);
            AddColumn(grid, "LessonType", "Тип", 95);
            AddColumn(grid, "Teacher", "Преподаватель", 140);
            AddColumn(grid, "Room", "Ауд.", 90);
            AddColumn(grid, "ColorHex", "Цвет", 75);
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

        private void OpenButtonClick(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
                dialog.Multiselect = true;
                dialog.Title = "Выберите расписание Excel";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    Cursor = Cursors.WaitCursor;
                    _document = _parser.ParseFiles(dialog.FileNames);
                    _filesTextBox.Text = string.Join("; ", dialog.FileNames);
                    BindLessons();
                    SaveToPublicationPath();
                    SetStatus(string.Format("Готово: {0} занятий, {1} групп, {2} преподавателей. Данные подготовлены для сети.", _document.Lessons.Count, _document.Groups.Count, _document.Teachers.Count));
                    _saveButton.Enabled = true;
                    _startServerButton.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Ошибка импорта", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetStatus("Не удалось разобрать Excel-файл.");
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }

        private void SaveButtonClick(object sender, EventArgs e)
        {
            if (_document == null)
            {
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                dialog.FileName = "schedule.json";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                ScheduleJsonSerializer.Save(_document, dialog.FileName);
                _jsonPathTextBox.Text = dialog.FileName;
                SetStatus("JSON сохранен: " + dialog.FileName);
            }
        }

        private void StartServerButtonClick(object sender, EventArgs e)
        {
            if (_document == null)
            {
                MessageBox.Show(this, "Сначала импортируйте Excel-расписание.", "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                if (!NetworkHelper.IsInternetAvailable())
                {
                    throw new InvalidOperationException("Нет подключения к интернету. По ТЗ сетевой режим должен показывать ошибку при отсутствии подключения.");
                }

                SaveToPublicationPath();
                _server.Start(_portTextBox.Text, _jsonPathTextBox.Text);
                _startServerButton.Enabled = false;
                _stopServerButton.Enabled = true;
                _openWebButton.Enabled = true;
                _serverStateLabel.ForeColor = UiTheme.Accent;
                _serverStateLabel.Text = "Сервер работает на порту " + _server.Port.ToString();
                _serverUrlsTextBox.Text = BuildServerUrlsText();
                SetStatus("Расписание опубликовано как веб-страница и JSON API.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка запуска сервера", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Сервер не запущен.");
            }
        }

        private void StopServerButtonClick(object sender, EventArgs e)
        {
            _server.Stop();
            _startServerButton.Enabled = _document != null;
            _stopServerButton.Enabled = false;
            _openWebButton.Enabled = false;
            _serverStateLabel.ForeColor = UiTheme.Muted;
            _serverStateLabel.Text = "Сервер остановлен";
            _serverUrlsTextBox.Text = "Ссылки появятся после запуска сервера.";
            SetStatus("Сервер остановлен.");
        }

        private void OpenWebButtonClick(object sender, EventArgs e)
        {
            if (!_server.IsRunning)
            {
                return;
            }

            try
            {
                Process.Start(_server.LocalUrl);
            }
            catch
            {
                Clipboard.SetText(_server.LocalUrl);
                SetStatus("Локальная ссылка скопирована в буфер обмена: " + _server.LocalUrl);
            }
        }

        private string BuildServerUrlsText()
        {
            List<string> lines = new List<string>();
            lines.Add("Открыть на этом компьютере:");
            lines.Add(_server.LocalUrl);
            lines.Add("");
            lines.Add("Открыть на телефоне или другом Windows-устройстве:");
            if (_server.NetworkUrls != null && _server.NetworkUrls.Length > 0)
            {
                foreach (string url in _server.NetworkUrls)
                {
                    lines.Add(url);
                }
            }
            else
            {
                lines.Add("LAN-адрес не найден автоматически. Проверьте IP компьютера командой ipconfig.");
            }
            lines.Add("");
            lines.Add("Для Windows-просмотрщика используйте JSON URL:");
            if (_server.NetworkUrls != null && _server.NetworkUrls.Length > 0)
            {
                foreach (string url in _server.NetworkUrls)
                {
                    lines.Add(url + "schedule.json");
                }
            }
            else
            {
                lines.Add(_server.LocalUrl + "schedule.json");
            }
            return string.Join(Environment.NewLine, lines.ToArray());
        }

        private void SaveToPublicationPath()
        {
            if (_document == null)
            {
                return;
            }

            ScheduleJsonSerializer.Save(_document, _jsonPathTextBox.Text);
        }

        private void BindLessons()
        {
            List<Lesson> lessons = _document == null ? new List<Lesson>() : _document.Lessons;
            _grid.DataSource = lessons;

            foreach (DataGridViewRow row in _grid.Rows)
            {
                Lesson lesson = row.DataBoundItem as Lesson;
                if (lesson != null)
                {
                    ApplyLessonColor(row, lesson);
                }
            }
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
