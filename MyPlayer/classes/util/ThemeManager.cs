using Microsoft.Win32;

namespace MyPlayer.classes.util
{
    public enum ThemeType
    {
        Light,
        Dark
    }

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

        // Cores do Arco-íris
        private static readonly Color[] RainbowColors = new[]
        {
            Color.FromArgb(255, 0, 0),    // Vermelho
            Color.FromArgb(255, 127, 0),  // Laranja
            Color.FromArgb(255, 255, 0),  // Amarelo
            Color.FromArgb(0, 255, 0),    // Verde
            Color.FromArgb(0, 0, 255),    // Azul
            Color.FromArgb(75, 0, 130),   // Indigo
            Color.FromArgb(238, 130, 238) // Violeta
        };

        public static Color GetRainbowColor(int index)
        {
            return RainbowColors[index % RainbowColors.Length];
        }

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

        public static void ApplyTheme(Form form, ThemeType themeType)
        {
            bool isDark = themeType == ThemeType.Dark;
            bool isRainbow = themeType == ThemeType.Dark;
            
            Color backColor = isRainbow ? Color.FromArgb(20, 20, 30) : (isDark ? DarkBackColor : LightBackColor);
            Color foreColor = isRainbow ? Color.White : (isDark ? DarkForeColor : LightForeColor);
            
            form.BackColor = backColor;
            form.ForeColor = foreColor;

            ApplyRecursive(form, themeType);
        }

        public static void ApplyTheme(ContextMenuStrip contextMenuStrip, ThemeType themeType)
        {
            bool isDark = themeType == ThemeType.Dark;
            bool isRainbow = themeType == ThemeType.Dark;

            Color controlBack = isRainbow ? Color.FromArgb(35, 35, 50) : (isDark ? DarkControlColor : LightControlColor);
            Color controlFore = isRainbow ? Color.White : (isDark ? DarkForeColor : LightForeColor);

            if (isDark || isRainbow)
            {
                contextMenuStrip.BackColor = controlBack;
                contextMenuStrip.ForeColor = controlFore;
            }
            else
            {
                contextMenuStrip.BackColor = SystemColors.Control;
                contextMenuStrip.ForeColor = SystemColors.ControlText;
            }
        }

        public static void ApplyTheme(Form form, bool isDark)
        {
            ApplyTheme(form, isDark ? ThemeType.Dark : ThemeType.Light);
        }

        private static void ApplyRecursive(Control parent, ThemeType themeType)
        {
            bool isDark = themeType == ThemeType.Dark;
            bool isRainbow = themeType == ThemeType.Dark;
            
            Color controlBack = isRainbow ? Color.FromArgb(35, 35, 50) : (isDark ? DarkControlColor : LightControlColor);
            Color controlFore = isRainbow ? Color.White : (isDark ? DarkForeColor : LightForeColor);

            foreach (Control c in parent.Controls)
            {
                UpdateControlTheme(c, themeType, controlBack, controlFore);

                if (c.HasChildren)
                {
                    ApplyRecursive(c, themeType);
                }
            }
        }

        private static void UpdateControlTheme(Control c, ThemeType themeType, Color controlBack, Color controlFore)
        {
            bool isDark = themeType == ThemeType.Dark;
            bool isRainbow = themeType == ThemeType.Dark;
            Color inputColor = isRainbow ? Color.FromArgb(50, 50, 70) : DarkInputColor;
            
            if (c is TextBox textBox)
            {
                textBox.BackColor = isDark || isRainbow ? inputColor : SystemColors.Window;
                textBox.ForeColor = controlFore;
                textBox.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (c is ListView listView)
            {
                listView.BackColor = controlBack;
                listView.ForeColor = controlFore;
                listView.GridLines = !isDark && !isRainbow;
                listView.UseCompatibleStateImageBehavior = false;
            }
            else if (c is TreeView treeView)
            {
                treeView.BackColor = controlBack;
                treeView.ForeColor = controlFore;
                treeView.FullRowSelect = true;
                treeView.ShowLines = !isDark && !isRainbow;
            }
            else if (c is ListBox listBox)
            {
                listBox.BackColor = controlBack;
                listBox.ForeColor = controlFore;
            }
            else if (c is ComboBox comboBox)
            {
                comboBox.BackColor = isDark || isRainbow ? inputColor : SystemColors.Window;
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
                else if (isRainbow)
                {
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 1;
                    btn.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 150);
                    btn.BackColor = Color.FromArgb(60, 60, 90);
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
                if (isDark || isRainbow)
                {
                    progressBar.BackColor = controlBack;
                }
            }
            else if (c is TrackBar trackBar)
            {
                trackBar.BackColor = isDark || isRainbow ? controlBack : SystemColors.Control;
            }
            else if (c is Panel panel)
            {
                if ((isDark || isRainbow) && panel.Name != "panelVisualization")
                {
                    panel.BackColor = controlBack;
                }
            }
            else if (c is PictureBox)
            {
                c.BackColor = Color.Transparent;
            }
            else if (c is ContextMenuStrip contextMenuStrip)
            {
                if (isDark || isRainbow)
                {
                    contextMenuStrip.BackColor = controlBack;
                    contextMenuStrip.ForeColor = controlFore;
                }
            }
            else if (c is ToolStrip toolStrip)
            {
                if (isDark || isRainbow)
                {
                    toolStrip.BackColor = controlBack;
                    toolStrip.ForeColor = controlFore;
                }
            }
        }
    }
}
