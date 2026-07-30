using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
        private TextBox _prefixTextBox;
        private Label _statusLabel;
        private Label _serverLabel;
        private DataGridView _grid;
        private Button _saveButton;
        private Button _startServerButton;
        private Button _stopServerButton;

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
            Text = "Учебный отдел - парсер расписания НФ НИТУ МИСИС";
            MinimumSize = new Size(1080, 680);
            StartPosition = FormStartPosition.CenterScreen;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.Padding = new Padding(12);
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            FlowLayoutPanel importPanel = new FlowLayoutPanel();
            importPanel.Dock = DockStyle.Fill;
            importPanel.AutoSize = true;
            importPanel.WrapContents = false;
            root.Controls.Add(importPanel, 0, 0);

            Button openButton = new Button();
            openButton.Text = "Открыть Excel";
            openButton.Width = 130;
            openButton.Height = 32;
            openButton.Click += OpenButtonClick;
            importPanel.Controls.Add(openButton);

            _saveButton = new Button();
            _saveButton.Text = "Сохранить JSON";
            _saveButton.Width = 140;
            _saveButton.Height = 32;
            _saveButton.Enabled = false;
            _saveButton.Click += SaveButtonClick;
            importPanel.Controls.Add(_saveButton);

            Label jsonLabel = new Label();
            jsonLabel.Text = "Файл публикации:";
            jsonLabel.TextAlign = ContentAlignment.MiddleLeft;
            jsonLabel.Width = 110;
            jsonLabel.Height = 32;
            importPanel.Controls.Add(jsonLabel);

            _jsonPathTextBox = new TextBox();
            _jsonPathTextBox.Width = 420;
            _jsonPathTextBox.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "schedule.json");
            importPanel.Controls.Add(_jsonPathTextBox);

            _filesTextBox = new TextBox();
            _filesTextBox.Dock = DockStyle.Fill;
            _filesTextBox.ReadOnly = true;
            _filesTextBox.Margin = new Padding(0, 8, 0, 8);
            root.Controls.Add(_filesTextBox, 0, 1);

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
            root.Controls.Add(_grid, 0, 2);

            TableLayoutPanel bottom = new TableLayoutPanel();
            bottom.Dock = DockStyle.Fill;
            bottom.ColumnCount = 1;
            bottom.RowCount = 3;
            bottom.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bottom.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bottom.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(bottom, 0, 3);

            FlowLayoutPanel serverPanel = new FlowLayoutPanel();
            serverPanel.Dock = DockStyle.Fill;
            serverPanel.AutoSize = true;
            serverPanel.WrapContents = false;
            bottom.Controls.Add(serverPanel, 0, 0);

            Label prefixLabel = new Label();
            prefixLabel.Text = "HTTP-адрес:";
            prefixLabel.TextAlign = ContentAlignment.MiddleLeft;
            prefixLabel.Width = 85;
            prefixLabel.Height = 32;
            serverPanel.Controls.Add(prefixLabel);

            _prefixTextBox = new TextBox();
            _prefixTextBox.Width = 260;
            _prefixTextBox.Text = "http://localhost:5088/";
            serverPanel.Controls.Add(_prefixTextBox);

            _startServerButton = new Button();
            _startServerButton.Text = "Запустить сервер";
            _startServerButton.Width = 150;
            _startServerButton.Height = 32;
            _startServerButton.Enabled = false;
            _startServerButton.Click += StartServerButtonClick;
            serverPanel.Controls.Add(_startServerButton);

            _stopServerButton = new Button();
            _stopServerButton.Text = "Остановить";
            _stopServerButton.Width = 105;
            _stopServerButton.Height = 32;
            _stopServerButton.Enabled = false;
            _stopServerButton.Click += StopServerButtonClick;
            serverPanel.Controls.Add(_stopServerButton);

            _serverLabel = new Label();
            _serverLabel.AutoSize = true;
            _serverLabel.Padding = new Padding(0, 8, 0, 0);
            bottom.Controls.Add(_serverLabel, 0, 1);

            _statusLabel = new Label();
            _statusLabel.AutoSize = true;
            _statusLabel.Padding = new Padding(0, 8, 0, 0);
            _statusLabel.Text = "Выберите Excel-файл расписания.";
            bottom.Controls.Add(_statusLabel, 0, 2);
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
                    SetStatus(string.Format("Готово: {0} занятий, {1} групп, {2} преподавателей. JSON сохранен.", _document.Lessons.Count, _document.Groups.Count, _document.Teachers.Count));
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
                _server.Start(_prefixTextBox.Text, _jsonPathTextBox.Text);
                _startServerButton.Enabled = false;
                _stopServerButton.Enabled = true;
                _serverLabel.Text = "Сервер запущен: " + _server.Prefix + "schedule.json" + GetLanHint();
                SetStatus("Расписание опубликовано по сети.");
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
            _serverLabel.Text = string.Empty;
            SetStatus("Сервер остановлен.");
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
                if (lesson == null)
                {
                    continue;
                }

                ApplyLessonColor(row, lesson);
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

        private static string GetLanHint()
        {
            string ip = GetLocalIpAddress();
            if (string.IsNullOrWhiteSpace(ip))
            {
                return string.Empty;
            }

            return " | Для другой машины в сети: http://" + ip + ":5088/schedule.json";
        }

        private static string GetLocalIpAddress()
        {
            try
            {
                IPAddress[] addresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                foreach (IPAddress address in addresses)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                    {
                        return address.ToString();
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }
    }
}
