namespace MyPlayer.classes.util.threads
{
    /// <summary>
    /// Acesso a componentes do form Thread Safe
    /// </summary>
    public static class InvokeAux
    {
        /// <summary>
        /// Recupera um valor de forma segura de um controle WinForms.
        /// </summary>
        public static T GetValue<T>(Control ctrl, Func<Control, T> getter)
        {
            if (ctrl == null)
                throw new ArgumentNullException(nameof(ctrl));

            if (ctrl.InvokeRequired)
            {
                return (T)ctrl.Invoke(new Func<T>(() => getter(ctrl)));
            }
            else
            {
                return getter(ctrl);
            }
        }

        /// <summary>
        /// Define um valor de forma segura em um controle WinForms.
        /// </summary>
        //public static void SetValue(Control ctrl, Action<Control> setter)
        //{
        //    if (ctrl == null)
        //        throw new ArgumentNullException(nameof(ctrl));

        //    if (ctrl.InvokeRequired)
        //    {
        //        ctrl.Invoke(new Action(() => setter(ctrl)));
        //    }
        //    else
        //    {
        //        setter(ctrl);
        //    }
        //}

        public static void SetValue(Control ctrl, Action<Control> setter)
        {
            if (ctrl == null)
                throw new ArgumentNullException(nameof(ctrl));

            if (ctrl.InvokeRequired)
            {
                ctrl.BeginInvoke(new Action(() => setter(ctrl)));
            }
            else
            {
                setter(ctrl);
            }
        }


    }
}

/*

Exemplos:

string texto = InvokeAux.GetValue(meuTextBox, c => ((TextBox)c).Text);

InvokeAux.SetValue(meuLabel, c => c.Text = "Processando...");

InvokeAux.SetValue(minhaProgressBar, c => {
    c.Enabled = true;
    ((ProgressBar)c).Value = 50;
});

 
*/
