using System.Drawing;
using System.Windows.Forms;

namespace ScheduleViewerApp
{
    internal static class UiTheme
    {
        public static readonly Color Background = Color.FromArgb(243, 247, 249);
        public static readonly Color Header = Color.FromArgb(13, 44, 54);
        public static readonly Color HeaderMuted = Color.FromArgb(201, 219, 225);
        public static readonly Color Accent = Color.FromArgb(11, 107, 111);
        public static readonly Color AccentWarm = Color.FromArgb(240, 111, 88);
        public static readonly Color Line = Color.FromArgb(216, 225, 232);
        public static readonly Color Text = Color.FromArgb(21, 35, 45);
        public static readonly Color Muted = Color.FromArgb(99, 114, 127);
        public static readonly Color Surface = Color.White;

        public static void StyleForm(Form form)
        {
            form.BackColor = Background;
            form.Font = new Font("Segoe UI", 9.5f);
        }

        public static void StyleButton(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = primary ? Accent : Line;
            button.BackColor = primary ? Accent : Surface;
            button.ForeColor = primary ? Color.White : Text;
            button.Height = 36;
            button.Padding = new Padding(10, 0, 10, 0);
            button.UseVisualStyleBackColor = false;
        }

        public static void StyleTextBox(TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.BackColor = Surface;
            textBox.ForeColor = Text;
            textBox.Height = 26;
        }

        public static void StyleCombo(ComboBox comboBox)
        {
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.BackColor = Surface;
            comboBox.ForeColor = Text;
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Header;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grid.DefaultCellStyle.BackColor = Surface;
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(215, 240, 233);
            grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 251, 252);
            grid.GridColor = Line;
            grid.RowTemplate.MinimumHeight = 34;
        }

        public static Label CreateTitle(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.ForeColor = Color.White;
            label.Font = new Font("Segoe UI", 19f, FontStyle.Bold);
            label.AutoSize = true;
            return label;
        }

        public static Label CreateSubtitle(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.ForeColor = HeaderMuted;
            label.Font = new Font("Segoe UI", 10f);
            label.AutoSize = true;
            return label;
        }
    }
}
