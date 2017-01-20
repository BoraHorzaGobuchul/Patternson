using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Patternson
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private void ErrorHandling(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("A error occured: " + e.Exception.Message);
            e.Handled = true;
        }
    }
}
