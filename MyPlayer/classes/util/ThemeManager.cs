using Microsoft.Win32;

namespace MyPlayer.classes.util
{
    public static class ThemeManager
    {
        // Cores para o Modo Escuro
        private static readonly Color DarkBackColor = Color.FromArgb(32, 32, 32);
        private static readonly Color DarkForeColor = Color.White;
        private static readonly Color DarkControlColor = Color.FromArgb(45, 45, 48); // Para TextBox, ListView, etc.
        
        // Cores para o Modo Claro (Padrão do Sistema)
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
                        return registryValue == 0; // 0 = Dark, 1 = Light
                    }
                }
            }
            catch
            {
                // Falha silenciosa, assume Light por padrão se não conseguir ler
            }
            return false;
        }

        public static void ApplyTheme(Form form, bool isDark)
        {
            Color backColor = isDark ? DarkBackColor : LightBackColor;
            Color foreColor = isDark ? DarkForeColor : LightForeColor;
            
            form.BackColor = backColor;
            form.ForeColor = foreColor;

            ApplyRecursive(form, isDark);
            
            // Força o redesenho da barra de título se possível (limitação do WinForms nativo)
            // Em .NET modernos, o DarkMode nativo na barra de título requer P/Invoke dwm api, 
            // mas vamos focar no conteúdo interno primeiro.
        }

        private static void ApplyRecursive(Control parent, bool isDark)
        {
            Color controlBack = isDark ? DarkControlColor : LightControlColor;
            Color controlFore = isDark ? DarkForeColor : LightForeColor;
            Color btnBack = isDark ? Color.FromArgb(60, 60, 60) : SystemColors.Control;

            foreach (Control c in parent.Controls)
            {
                UpdateControlTheme(c, isDark, controlBack, controlFore, btnBack);

                if (c.HasChildren)
                {
                    ApplyRecursive(c, isDark);
                }
            }
        }

        private static void UpdateControlTheme(Control c, bool isDark, Color controlBack, Color controlFore, Color btnBack)
        {
            // Ignora controles que queremos manter com cor específica ou transparente, se houver
            
            if (c is TextBox || c is ListView || c is TreeView || c is ListBox)
            {
                c.BackColor = controlBack;
                c.ForeColor = controlFore;
            }
            else if (c is Button btn)
            {
                if (isDark)
                {
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 1; // Borda fina
                    btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80); // Borda cinza escura sutil
                    
                    btn.BackColor = btnBack;
                    btn.ForeColor = Color.WhiteSmoke; // Garante contraste
                    btn.UseVisualStyleBackColor = false; 
                }
                else
                {
                    // Restaura padrão do Windows
                    btn.FlatStyle = FlatStyle.Standard; 
                    btn.BackColor = SystemColors.Control;
                    btn.ForeColor = SystemColors.ControlText;
                    btn.UseVisualStyleBackColor = true;
                }
            }
            else if (c is Label || c is CheckBox || c is RadioButton || c is GroupBox)
            {
                c.ForeColor = controlFore;
                // Labels geralmente herdam o BackColor transparente ou do pai, não mudamos BackColor forçadamente
            }
            else if (c is PictureBox)
            {
                c.BackColor = Color.Transparent; // Garante transparência
            }
            else if (c is ProgressBar)
            {
                // ProgressBar é difícil de estilizar nativamente no WinForms sem OwnerDraw
            }
        }
    }
}
