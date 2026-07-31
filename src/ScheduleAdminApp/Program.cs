using System;
using System.Drawing;
using System.Windows.Forms;
using DepartmentMainForm = ScheduleDepartmentApp.MainForm;

namespace ScheduleAdminApp
{
    internal static class Program
    {
        private const string AdminPassword = "Admin_MISIS_NF";

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (LoginForm loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new DepartmentMainForm());
                }
            }
        }

        private sealed class LoginForm : Form
        {
            private readonly TextBox _passwordTextBox;
            private readonly Label _errorLabel;

            public LoginForm()
            {
                Text = "Администратор - Schedule Parser NUST MISIS";
                StartPosition = FormStartPosition.CenterScreen;
                MinimumSize = new Size(420, 240);
                MaximizeBox = false;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                BackColor = Color.FromArgb(243, 247, 249);
                Font = new Font("Segoe UI", 9.5f);

                TableLayoutPanel root = new TableLayoutPanel();
                root.Dock = DockStyle.Fill;
                root.ColumnCount = 1;
                root.RowCount = 5;
                root.Padding = new Padding(18);
                root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
                Controls.Add(root);

                Label title = new Label();
                title.Text = "Доступ администратора";
                title.ForeColor = Color.FromArgb(21, 35, 45);
                title.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
                title.AutoSize = true;
                root.Controls.Add(title, 0, 0);

                _passwordTextBox = new TextBox();
                _passwordTextBox.Dock = DockStyle.Fill;
                _passwordTextBox.PasswordChar = '*';
                _passwordTextBox.BorderStyle = BorderStyle.FixedSingle;
                _passwordTextBox.KeyDown += PasswordTextBoxKeyDown;
                root.Controls.Add(_passwordTextBox, 0, 2);

                _errorLabel = new Label();
                _errorLabel.Dock = DockStyle.Fill;
                _errorLabel.ForeColor = Color.FromArgb(240, 111, 88);
                _errorLabel.TextAlign = ContentAlignment.MiddleLeft;
                root.Controls.Add(_errorLabel, 0, 3);

                FlowLayoutPanel actions = new FlowLayoutPanel();
                actions.Dock = DockStyle.Fill;
                actions.FlowDirection = FlowDirection.RightToLeft;
                actions.WrapContents = false;
                root.Controls.Add(actions, 0, 4);

                Button enterButton = CreateButton("Войти", true);
                enterButton.Click += EnterButtonClick;
                actions.Controls.Add(enterButton);

                Button cancelButton = CreateButton("Отмена", false);
                cancelButton.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
                actions.Controls.Add(cancelButton);
            }

            private static Button CreateButton(string text, bool primary)
            {
                Button button = new Button();
                button.Text = text;
                button.Width = 112;
                button.Height = 36;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = primary ? Color.FromArgb(11, 107, 111) : Color.FromArgb(216, 225, 232);
                button.BackColor = primary ? Color.FromArgb(11, 107, 111) : Color.White;
                button.ForeColor = primary ? Color.White : Color.FromArgb(21, 35, 45);
                button.UseVisualStyleBackColor = false;
                return button;
            }

            private void PasswordTextBoxKeyDown(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    TryEnter();
                }
            }

            private void EnterButtonClick(object sender, EventArgs e)
            {
                TryEnter();
            }

            private void TryEnter()
            {
                if (string.Equals(_passwordTextBox.Text, AdminPassword, StringComparison.Ordinal))
                {
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }

                _passwordTextBox.Clear();
                _errorLabel.Text = "Доступ не открыт.";
                _passwordTextBox.Focus();
            }
        }
    }
}
