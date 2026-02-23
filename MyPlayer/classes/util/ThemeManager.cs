using Microsoft.Win32;

namespace MyPlayer.classes.util
{
    public static class ThemeManager
    {
        // Cores modernas para Modo Escuro
        private static readonly Color DarkBackColor = Color.FromArgb(30, 30, 30);
        private static readonly Color DarkForeColor = Color.FromArgb(220, 220, 220);
        private static readonly Color DarkControlColor = Color.FromArgb(40, 40, 40);
        private static readonly Color DarkInputColor = Color.FromArgb(50, 50, 50);
        private static readonly Color DarkBorderColor = Color.FromArgb(60, 60, 60);
        private static readonly Color DarkAccentColor = Color.FromArgb(0, 120, 215);
        private static readonly Color DarkSelectionColor = Color.FromArgb(0, 90, 170);
        private static readonly Color DarkAlternateRow = Color.FromArgb(45, 45, 45);
        
        // Cores para Modo Claro
        private static readonly Color LightBackColor = SystemColors.Control;
        private static readonly Color LightForeColor = SystemColors.ControlText;
        private static readonly Color LightControlColor = SystemColors.Window;

        public static bool IsSystemDarkMode()
        {
            try
            {
                const string registryKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                using var key = Registry.CurrentUser.OpenSubKey(registryKey);
                if (key != null)
                {
                    object? registryValueObject = key.GetValue("AppsUseLightTheme");
                    if (registryValueObject != null)
                    {
                        int registryValue = (int)registryValueObject;
                        return registryValue == 0;
                    }
                }
            }
            catch { }
            return false;
        }

        public static void ApplyTheme(Form form, bool isDark)
        {
            Color backColor = isDark ? DarkBackColor : LightBackColor;
            Color foreColor = isDark ? DarkForeColor : LightForeColor;
            
            form.BackColor = backColor;
            form.ForeColor = foreColor;

            ApplyRecursive(form, isDark);
        }

        private static void ApplyRecursive(Control parent, bool isDark)
        {
            Color controlBack = isDark ? DarkControlColor : LightControlColor;
            Color controlFore = isDark ? DarkForeColor : LightForeColor;

            foreach (Control c in parent.Controls)
            {
                UpdateControlTheme(c, isDark, controlBack, controlFore);

                if (c.HasChildren)
                {
                    ApplyRecursive(c, isDark);
                }
            }
        }

        private static void UpdateControlTheme(Control c, bool isDark, Color controlBack, Color controlFore)
        {
            if (c is TextBox textBox)
            {
                textBox.BackColor = isDark ? DarkInputColor : SystemColors.Window;
                textBox.ForeColor = controlFore;
                textBox.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (c is ListView listView)
            {
                listView.BackColor = isDark ? DarkControlColor : SystemColors.Window;
                listView.ForeColor = controlFore;
                listView.GridLines = !isDark;
                listView.UseCompatibleStateImageBehavior = false;
            }
            else if (c is TreeView treeView)
            {
                treeView.BackColor = isDark ? DarkControlColor : SystemColors.Window;
                treeView.ForeColor = controlFore;
                treeView.FullRowSelect = true;
                treeView.ShowLines = !isDark;
            }
            else if (c is ListBox listBox)
            {
                listBox.BackColor = isDark ? DarkControlColor : SystemColors.Window;
                listBox.ForeColor = controlFore;
            }
            else if (c is ComboBox comboBox)
            {
                comboBox.BackColor = isDark ? DarkInputColor : SystemColors.Window;
                comboBox.ForeColor = controlFore;
            }
            else if (c is Button btn)
            {
                if (isDark)
                {
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 1;
                    btn.FlatAppearance.BorderColor = DarkBorderColor;
                    btn.BackColor = Color.FromArgb(55, 55, 55);
                    btn.ForeColor = Color.White;
                    btn.UseVisualStyleBackColor = false;
                }
                else
                {
                    btn.FlatStyle = FlatStyle.Standard;
                    btn.BackColor = SystemColors.Control;
                    btn.ForeColor = SystemColors.ControlText;
                    btn.UseVisualStyleBackColor = true;
                }
            }
            else if (c is Label label)
            {
                label.ForeColor = controlFore;
            }
            else if (c is CheckBox checkBox)
            {
                checkBox.ForeColor = controlFore;
            }
            else if (c is RadioButton radioButton)
            {
                radioButton.ForeColor = controlFore;
            }
            else if (c is GroupBox groupBox)
            {
                groupBox.ForeColor = controlFore;
            }
            else if (c is ProgressBar progressBar)
            {
                if (isDark)
                {
                    progressBar.BackColor = DarkControlColor;
                }
            }
            else if (c is TrackBar trackBar)
            {
                trackBar.BackColor = isDark ? DarkBackColor : SystemColors.Control;
            }
            else if (c is Panel panel)
            {
                if (isDark && panel.Name != "panelVisualization")
                {
                    panel.BackColor = isDark ? DarkControlColor : SystemColors.Control;
                }
            }
            else if (c is PictureBox)
            {
                c.BackColor = Color.Transparent;
            }
            else if (c is ContextMenuStrip contextMenuStrip)
            {
                if (isDark)
                {
                    contextMenuStrip.BackColor = DarkControlColor;
                    contextMenuStrip.ForeColor = DarkForeColor;
                }
            }
            else if (c is ToolStrip toolStrip)
            {
                if (isDark)
                {
                    toolStrip.BackColor = DarkControlColor;
                    toolStrip.ForeColor = DarkForeColor;
                }
            }
        }
    }
}
