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
        public static TResult GetValue<TControl, TResult>(TControl ctrl, Func<TControl, TResult> getter)
            where TControl : Control
        {
            if (ctrl == null)
                throw new ArgumentNullException(nameof(ctrl));

            if (ctrl.InvokeRequired)
                return (TResult)ctrl.Invoke(new Func<TResult>(() => getter(ctrl)));
            else
                return getter(ctrl);
        }

        /// <summary>
        /// Define um valor de forma segura em um controle WinForms.
        /// </summary>
        public static void Access<TControl>(TControl ctrl, Action<TControl> setter)
            where TControl : Control
        {
            if (ctrl == null)
                throw new ArgumentNullException(nameof(ctrl));

            if (ctrl.InvokeRequired)
                ctrl.BeginInvoke(new Action(() => setter(ctrl)));
            else
                setter(ctrl);
        }

    }
}


/*

Exemplos:

InvokeAux.Access(listView1, lv =>
{
    lv.SelectedItems.Clear();
    itemAtual.Selected = true;
    itemAtual.EnsureVisible();
});

InvokeAux.Access(lblStatus, lbl => lbl.Text = "Parado");
InvokeAux.Access(progressBar1, pb => pb.Value = 0);
InvokeAux.Access(trackBar1, tb => tb.Value = 0);

_playerControl.SetPercent(
    InvokeAux.GetValue(trackBar1, tb => (double)tb.Value)
);


 
*/
