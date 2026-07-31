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
        private const int GlobalAutoPublishIntervalMilliseconds = 300000;

        private readonly XlsxScheduleParser _parser;
        private readonly SimpleScheduleServer _server;
        private readonly List<string> _loadedFilePaths;
        private readonly Timer _globalAutoPublishTimer;
        private ScheduleDocument _document;
        private TextBox _filesTextBox;
        private TextBox _jsonPathTextBox;
        private TextBox _portTextBox;
        private TextBox _serverUrlsTextBox;
        private Label _statusLabel;
        private Label _serverStateLabel;
        private DataGridView _grid;
        private CheckBox[] _courseCheckBoxes;
        private Button _manageFilesButton;
        private Button _clearButton;
        private Button _saveButton;
        private Button _startServerButton;
        private Button _stopServerButton;
        private Button _openWebButton;
        private Button _publishGlobalButton;
        private CheckBox _autoGlobalPublishCheckBox;
        private CheckBox _protectedGlobalCheckBox;
        private TextBox _globalPasswordTextBox;
        private TextBox _githubTokenTextBox;
        private TextBox _globalUrlTextBox;
        private volatile bool _globalPublishInProgress;

        public MainForm()
        {
            _parser = new XlsxScheduleParser();
            _server = new SimpleScheduleServer();
            _loadedFilePaths = new List<string>();
            _globalAutoPublishTimer = new Timer();
            _globalAutoPublishTimer.Interval = GlobalAutoPublishIntervalMilliseconds;
            _globalAutoPublishTimer.Tick += GlobalAutoPublishTimerTick;
            InitializeComponent();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopGlobalAutoPublishLoop();
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
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 285));
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
            panel.Height = 150;
            panel.Padding = new Padding(14);
            panel.BackColor = UiTheme.Background;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 4;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
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

            _manageFilesButton = new Button();
            _manageFilesButton.Text = "Файлы";
            _manageFilesButton.Width = 96;
            _manageFilesButton.Enabled = false;
            _manageFilesButton.Click += ManageFilesButtonClick;
            UiTheme.StyleButton(_manageFilesButton, false);
            actions.Controls.Add(_manageFilesButton);

            _saveButton = new Button();
            _saveButton.Text = "Сохранить JSON";
            _saveButton.Width = 140;
            _saveButton.Enabled = false;
            _saveButton.Click += SaveButtonClick;
            UiTheme.StyleButton(_saveButton, false);
            actions.Controls.Add(_saveButton);

            _clearButton = new Button();
            _clearButton.Text = "Очистить";
            _clearButton.Width = 110;
            _clearButton.Enabled = false;
            _clearButton.Click += ClearButtonClick;
            UiTheme.StyleButton(_clearButton, false);
            actions.Controls.Add(_clearButton);

            Label jsonLabel = CreateSmallLabel("Файл публикации");
            jsonLabel.Width = 120;
            actions.Controls.Add(jsonLabel);

            _jsonPathTextBox = new TextBox();
            _jsonPathTextBox.Width = 390;
            _jsonPathTextBox.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "schedule.json");
            UiTheme.StyleTextBox(_jsonPathTextBox);
            actions.Controls.Add(_jsonPathTextBox);

            FlowLayoutPanel courses = new FlowLayoutPanel();
            courses.Dock = DockStyle.Fill;
            courses.WrapContents = false;
            courses.BackColor = UiTheme.Background;
            layout.Controls.Add(courses, 0, 1);

            Label courseLabel = CreateSmallLabel("Курсы для загрузки");
            courseLabel.Width = 132;
            courses.Controls.Add(courseLabel);

            _courseCheckBoxes = new CheckBox[6];
            for (int i = 0; i < _courseCheckBoxes.Length; i++)
            {
                CheckBox checkBox = new CheckBox();
                checkBox.Text = (i + 1).ToString() + " курс";
                checkBox.Width = 78;
                checkBox.Height = 28;
                checkBox.Checked = true;
                checkBox.ForeColor = UiTheme.Text;
                _courseCheckBoxes[i] = checkBox;
                courses.Controls.Add(checkBox);
            }

            _filesTextBox = new TextBox();
            _filesTextBox.Dock = DockStyle.Fill;
            _filesTextBox.ReadOnly = true;
            _filesTextBox.Text = "Файлы Excel пока не выбраны";
            UiTheme.StyleTextBox(_filesTextBox);
            layout.Controls.Add(_filesTextBox, 0, 2);

            _statusLabel = new Label();
            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.Text = "Выберите Excel-файл расписания. После импорта можно запустить сетевой сервер.";
            _statusLabel.ForeColor = UiTheme.Muted;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(_statusLabel, 0, 3);

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
            layout.RowCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
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
            hint.Text = "Локальный сервер работает в одной Wi-Fi/LAN сети. Для доступа откуда угодно используйте глобальную публикацию ниже.";
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

            FlowLayoutPanel globalPanel = new FlowLayoutPanel();
            globalPanel.Dock = DockStyle.Fill;
            globalPanel.WrapContents = true;
            globalPanel.Padding = new Padding(0, 10, 0, 0);
            layout.Controls.Add(globalPanel, 0, 2);
            layout.SetColumnSpan(globalPanel, 2);

            Label globalLabel = CreateSmallLabel("Глобально");
            globalLabel.Width = 80;
            globalPanel.Controls.Add(globalLabel);

            _publishGlobalButton = new Button();
            _publishGlobalButton.Text = "Опубликовать в интернет";
            _publishGlobalButton.Width = 176;
            _publishGlobalButton.Enabled = false;
            _publishGlobalButton.Click += PublishGlobalButtonClick;
            UiTheme.StyleButton(_publishGlobalButton, true);
            globalPanel.Controls.Add(_publishGlobalButton);

            _autoGlobalPublishCheckBox = new CheckBox();
            _autoGlobalPublishCheckBox.Text = "Автообновлять сайт";
            _autoGlobalPublishCheckBox.Width = 158;
            _autoGlobalPublishCheckBox.Height = 36;
            _autoGlobalPublishCheckBox.Checked = true;
            _autoGlobalPublishCheckBox.ForeColor = UiTheme.Text;
            _autoGlobalPublishCheckBox.CheckedChanged += AutoGlobalPublishCheckBoxChanged;
            globalPanel.Controls.Add(_autoGlobalPublishCheckBox);

            _protectedGlobalCheckBox = new CheckBox();
            _protectedGlobalCheckBox.Text = "Защищенный канал";
            _protectedGlobalCheckBox.Width = 154;
            _protectedGlobalCheckBox.Height = 36;
            _protectedGlobalCheckBox.Checked = true;
            _protectedGlobalCheckBox.ForeColor = UiTheme.Text;
            _protectedGlobalCheckBox.CheckedChanged += ProtectedGlobalCheckBoxChanged;
            globalPanel.Controls.Add(_protectedGlobalCheckBox);

            Label passwordLabel = CreateSmallLabel("Пароль");
            passwordLabel.Width = 58;
            globalPanel.Controls.Add(passwordLabel);

            _globalPasswordTextBox = new TextBox();
            _globalPasswordTextBox.Width = 142;
            _globalPasswordTextBox.Text = BrowserScheduleProtector.DefaultBrowserPassword;
            UiTheme.StyleTextBox(_globalPasswordTextBox);
            globalPanel.Controls.Add(_globalPasswordTextBox);

            Label tokenLabel = CreateSmallLabel("GitHub token");
            tokenLabel.Width = 86;
            globalPanel.Controls.Add(tokenLabel);

            _githubTokenTextBox = new TextBox();
            _githubTokenTextBox.Width = 170;
            _githubTokenTextBox.PasswordChar = '*';
            UiTheme.StyleTextBox(_githubTokenTextBox);
            globalPanel.Controls.Add(_githubTokenTextBox);

            _globalUrlTextBox = new TextBox();
            _globalUrlTextBox.Width = 360;
            _globalUrlTextBox.ReadOnly = true;
            _globalUrlTextBox.Text = "Глобальная ссылка появится после публикации.";
            UiTheme.StyleTextBox(_globalUrlTextBox);
            globalPanel.Controls.Add(_globalUrlTextBox);

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
            AddColumn(grid, "Course", "Курс", 80);
            AddColumn(grid, "WeekType", "Неделя", 90);
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

                ImportFilePaths(dialog.FileNames);
            }
        }

        private void ManageFilesButtonClick(object sender, EventArgs e)
        {
            using (LoadedFilesForm dialog = new LoadedFilesForm(_loadedFilePaths))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                ImportFilePaths(dialog.FilePaths);
            }
        }

        private void ImportFilePaths(IEnumerable<string> filePaths)
        {
            List<string> files = new List<string>();
            if (filePaths != null)
            {
                foreach (string filePath in filePaths)
                {
                    if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath) && !files.Contains(filePath))
                    {
                        files.Add(filePath);
                    }
                }
            }

            if (files.Count == 0)
            {
                ClearCurrentSchedule(true);
                SetStatus("Список Excel-файлов пуст. Текущее расписание очищено.");
                return;
            }

            try
            {
                int[] selectedCourses = GetSelectedCourses();
                if (selectedCourses.Length == 0)
                {
                    MessageBox.Show(this, "Выберите хотя бы один курс для загрузки.", "Курсы не выбраны", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                    bool restartServer = _server.IsRunning;
                    if (restartServer)
                    {
                        StopGlobalAutoPublishLoop();
                        _server.Stop();
                    }

                Cursor = Cursors.WaitCursor;
                _document = _parser.ParseFiles(files.ToArray(), selectedCourses);
                if (_document.Lessons.Count == 0)
                {
                    throw new InvalidOperationException("В выбранных файлах не найдено занятий для отмеченных курсов.");
                }

                _loadedFilePaths.Clear();
                _loadedFilePaths.AddRange(files);
                SetLoadedFilesText();
                BindLessons();
                SaveToPublicationPath();
                SetStatus(string.Format("Готово: {0} занятий, файлов: {1}, курсы: {2}, недели: {3}, групп: {4}, преподавателей: {5}.", _document.Lessons.Count, _loadedFilePaths.Count, JoinList(_document.Courses), JoinList(_document.WeekTypes), _document.Groups.Count, _document.Teachers.Count));
                _manageFilesButton.Enabled = true;
                _saveButton.Enabled = true;
                _clearButton.Enabled = true;
                _startServerButton.Enabled = true;
                _publishGlobalButton.Enabled = true;
                if (restartServer)
                {
                    StartServer();
                    SetStatus("Расписание обновлено, сервер перезапущен с новыми файлами.");
                }
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

        private void ClearButtonClick(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(this, "Очистить текущее расписание, остановить сервер и удалить опубликованный JSON?", "Очистка расписания", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
            {
                return;
            }

            ClearCurrentSchedule(true);

            SetStatus("Текущее расписание очищено. Можно загрузить новые файлы.");
        }

        private void ClearCurrentSchedule(bool deleteJson)
        {
            StopGlobalAutoPublishLoop();
            _server.Stop();
            _document = null;
            _loadedFilePaths.Clear();
            _grid.DataSource = new List<Lesson>();
            SetLoadedFilesText();
            _serverStateLabel.ForeColor = UiTheme.Muted;
            _serverStateLabel.Text = "Сервер остановлен";
            _serverUrlsTextBox.Text = "Ссылки появятся после запуска сервера.";
            _manageFilesButton.Enabled = false;
            _saveButton.Enabled = false;
            _clearButton.Enabled = false;
            _startServerButton.Enabled = false;
            _stopServerButton.Enabled = false;
            _openWebButton.Enabled = false;
            _publishGlobalButton.Enabled = false;
            _globalUrlTextBox.Text = "Глобальная ссылка появится после публикации.";

            if (!deleteJson)
            {
                return;
            }

            try
            {
                if (File.Exists(_jsonPathTextBox.Text))
                {
                    File.Delete(_jsonPathTextBox.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Не удалось удалить JSON", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                StartServer();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка запуска сервера", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Сервер не запущен.");
            }
        }

        private void StartServer()
        {
            SaveToPublicationPath();
            _server.Start(_portTextBox.Text, _jsonPathTextBox.Text);
            _startServerButton.Enabled = false;
            _stopServerButton.Enabled = true;
            _openWebButton.Enabled = true;
            _serverStateLabel.ForeColor = UiTheme.Accent;
            _serverStateLabel.Text = "Сервер работает на порту " + _server.Port.ToString();
            _serverUrlsTextBox.Text = BuildServerUrlsText();
            if (NetworkHelper.IsNetworkAvailable())
            {
                SetStatus("Расписание опубликовано как веб-страница и JSON API.");
            }
            else
            {
                SetStatus("Сервер запущен локально. Для доступа с телефона подключите компьютер к сети.");
            }

            StartGlobalAutoPublishLoop();
        }

        private void StopServerButtonClick(object sender, EventArgs e)
        {
            StopGlobalAutoPublishLoop();
            _server.Stop();
            _startServerButton.Enabled = _document != null;
            _stopServerButton.Enabled = false;
            _openWebButton.Enabled = false;
            _serverStateLabel.ForeColor = UiTheme.Muted;
            _serverStateLabel.Text = "Сервер остановлен";
            _serverUrlsTextBox.Text = "Ссылки появятся после запуска сервера.";
            SetStatus("Сервер остановлен.");
        }

        private void PublishGlobalButtonClick(object sender, EventArgs e)
        {
            if (_document == null)
            {
                MessageBox.Show(this, "Сначала импортируйте Excel-расписание.", "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                GitHubPublishResult result = PublishGlobalNow();
                _globalUrlTextBox.Text = result.PageUrl;
                Clipboard.SetText(result.PageUrl);
                string accessMode = result.IsProtected ? "защищенный" : "открытый";
                SetStatus("Расписание опубликовано в интернет: " + accessMode + " глобальный доступ. Ссылка скопирована: " + result.PageUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка глобальной публикации", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Не удалось опубликовать расписание в интернет.");
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void ProtectedGlobalCheckBoxChanged(object sender, EventArgs e)
        {
            _globalPasswordTextBox.Enabled = _protectedGlobalCheckBox.Checked;
            if (_protectedGlobalCheckBox.Checked && string.IsNullOrWhiteSpace(_globalPasswordTextBox.Text))
            {
                _globalPasswordTextBox.Text = BrowserScheduleProtector.DefaultBrowserPassword;
            }
        }

        private void AutoGlobalPublishCheckBoxChanged(object sender, EventArgs e)
        {
            if (_autoGlobalPublishCheckBox.Checked && _server.IsRunning)
            {
                StartGlobalAutoPublishLoop();
                return;
            }

            StopGlobalAutoPublishLoop();
        }

        private GitHubPublishResult PublishGlobalNow()
        {
            SaveToPublicationPath();

            string webIndexPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web", "index.html");
            GitHubPagesPublisher publisher = new GitHubPagesPublisher("flvmming-coder", "Schedule-parser-NUST-MISIS", "gh-pages");
            bool protectBrowserAccess = _protectedGlobalCheckBox.Checked;
            string browserPassword = GetBrowserPassword();
            return publisher.Publish(_document, webIndexPath, _githubTokenTextBox.Text, protectBrowserAccess, browserPassword);
        }

        private void StartGlobalAutoPublishLoop()
        {
            _globalAutoPublishTimer.Stop();
            if (!_autoGlobalPublishCheckBox.Checked || _document == null)
            {
                return;
            }

            _globalAutoPublishTimer.Start();
            QueueGlobalAutoPublish("запуска сервера");
        }

        private void StopGlobalAutoPublishLoop()
        {
            _globalAutoPublishTimer.Stop();
        }

        private void GlobalAutoPublishTimerTick(object sender, EventArgs e)
        {
            QueueGlobalAutoPublish("планового обновления");
        }

        private void QueueGlobalAutoPublish(string reason)
        {
            if (!_autoGlobalPublishCheckBox.Checked || !_server.IsRunning || _document == null || _globalPublishInProgress)
            {
                return;
            }

            _globalPublishInProgress = true;
            SaveToPublicationPath();

            GlobalPublishRequest request = new GlobalPublishRequest();
            request.Document = _document;
            request.WebIndexPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web", "index.html");
            request.GitHubToken = _githubTokenTextBox.Text;
            request.ProtectBrowserAccess = _protectedGlobalCheckBox.Checked;
            request.BrowserPassword = GetBrowserPassword();
            request.Reason = reason;

            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    GitHubPagesPublisher publisher = new GitHubPagesPublisher("flvmming-coder", "Schedule-parser-NUST-MISIS", "gh-pages");
                    GitHubPublishResult result = publisher.Publish(request.Document, request.WebIndexPath, request.GitHubToken, request.ProtectBrowserAccess, request.BrowserPassword);
                    SafeBeginInvoke(delegate
                    {
                        _globalUrlTextBox.Text = result.PageUrl;
                        SetStatus("Глобальный сайт обновлен после " + request.Reason + ". Следующая проверка через 5 минут.");
                    });
                }
                catch (Exception ex)
                {
                    SafeBeginInvoke(delegate
                    {
                        SetStatus("Глобальный сайт не обновлен: " + ex.Message);
                    });
                }
                finally
                {
                    _globalPublishInProgress = false;
                }
            });
        }

        private void SafeBeginInvoke(MethodInvoker action)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            try
            {
                BeginInvoke(action);
            }
            catch
            {
            }
        }

        private string GetBrowserPassword()
        {
            if (string.IsNullOrWhiteSpace(_globalPasswordTextBox.Text))
            {
                return BrowserScheduleProtector.DefaultBrowserPassword;
            }

            return _globalPasswordTextBox.Text;
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

        private void SetLoadedFilesText()
        {
            if (_loadedFilePaths.Count == 0)
            {
                _filesTextBox.Text = "Файлы Excel пока не выбраны";
                return;
            }

            _filesTextBox.Text = string.Join("; ", _loadedFilePaths.ToArray());
        }

        private int[] GetSelectedCourses()
        {
            List<int> selected = new List<int>();
            if (_courseCheckBoxes == null)
            {
                return selected.ToArray();
            }

            for (int i = 0; i < _courseCheckBoxes.Length; i++)
            {
                if (_courseCheckBoxes[i].Checked)
                {
                    selected.Add(i + 1);
                }
            }

            return selected.ToArray();
        }

        private static string JoinList(List<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return "не указано";
            }

            return string.Join(", ", values.ToArray());
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

    internal sealed class GlobalPublishRequest
    {
        public ScheduleDocument Document { get; set; }
        public string WebIndexPath { get; set; }
        public string GitHubToken { get; set; }
        public bool ProtectBrowserAccess { get; set; }
        public string BrowserPassword { get; set; }
        public string Reason { get; set; }
    }

    internal sealed class LoadedFilesForm : Form
    {
        private readonly ListBox _filesListBox;
        private readonly List<string> _filePaths;

        public LoadedFilesForm(IEnumerable<string> filePaths)
        {
            _filePaths = new List<string>();
            if (filePaths != null)
            {
                _filePaths.AddRange(filePaths);
            }

            Text = "Загруженные файлы расписания";
            MinimumSize = new Size(760, 420);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.StyleForm(this);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.Padding = new Padding(14);
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            Controls.Add(root);

            Label title = new Label();
            title.Text = "Файлы Excel";
            title.ForeColor = UiTheme.Text;
            title.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            title.AutoSize = true;
            root.Controls.Add(title, 0, 0);

            _filesListBox = new ListBox();
            _filesListBox.Dock = DockStyle.Fill;
            _filesListBox.HorizontalScrollbar = true;
            _filesListBox.Font = new Font("Segoe UI", 9.5f);
            root.Controls.Add(_filesListBox, 0, 1);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            actions.WrapContents = false;
            root.Controls.Add(actions, 0, 2);

            Button applyButton = CreateButton("Применить", true);
            applyButton.Click += ApplyButtonClick;
            actions.Controls.Add(applyButton);

            Button cancelButton = CreateButton("Отмена", false);
            cancelButton.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            actions.Controls.Add(cancelButton);

            Button deleteButton = CreateButton("Удалить", false);
            deleteButton.Click += DeleteButtonClick;
            actions.Controls.Add(deleteButton);

            Button replaceButton = CreateButton("Заменить", false);
            replaceButton.Click += ReplaceButtonClick;
            actions.Controls.Add(replaceButton);

            Button addButton = CreateButton("Добавить", false);
            addButton.Click += AddButtonClick;
            actions.Controls.Add(addButton);

            RefreshList();
        }

        public string[] FilePaths
        {
            get { return _filePaths.ToArray(); }
        }

        private static Button CreateButton(string text, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = 112;
            UiTheme.StyleButton(button, primary);
            return button;
        }

        private void AddButtonClick(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = CreateExcelDialog(true))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                foreach (string fileName in dialog.FileNames)
                {
                    if (!_filePaths.Contains(fileName))
                    {
                        _filePaths.Add(fileName);
                    }
                }
            }

            RefreshList();
        }

        private void ReplaceButtonClick(object sender, EventArgs e)
        {
            int index = _filesListBox.SelectedIndex;
            if (index < 0 || index >= _filePaths.Count)
            {
                MessageBox.Show(this, "Выберите файл для замены.", "Файл не выбран", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (OpenFileDialog dialog = CreateExcelDialog(false))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                _filePaths[index] = dialog.FileName;
            }

            RefreshList();
            if (_filesListBox.Items.Count > 0)
            {
                _filesListBox.SelectedIndex = Math.Min(index, _filesListBox.Items.Count - 1);
            }
        }

        private void DeleteButtonClick(object sender, EventArgs e)
        {
            int index = _filesListBox.SelectedIndex;
            if (index < 0 || index >= _filePaths.Count)
            {
                MessageBox.Show(this, "Выберите файл для удаления.", "Файл не выбран", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _filePaths.RemoveAt(index);
            RefreshList();
            if (_filesListBox.Items.Count > 0)
            {
                _filesListBox.SelectedIndex = Math.Min(index, _filesListBox.Items.Count - 1);
            }
        }

        private void ApplyButtonClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private static OpenFileDialog CreateExcelDialog(bool multiselect)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            dialog.Multiselect = multiselect;
            dialog.Title = "Выберите расписание Excel";
            return dialog;
        }

        private void RefreshList()
        {
            _filesListBox.Items.Clear();
            foreach (string filePath in _filePaths)
            {
                _filesListBox.Items.Add(filePath);
            }
        }
    }
}
