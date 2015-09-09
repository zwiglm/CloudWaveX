using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PlasticWonderland.Resources;

namespace PlasticWonderland.Class
{
    public partial class PivotHelpControl : UserControl
    {
        public PivotHelpControl()
        {
            InitializeComponent();
        }

        public string textblock_debug_text
        {
            get
            {
                return textblock_debug.Text;
            }
            set
            {
                textblock_debug.Text = value;
            }
        }

        public bool toogle_debug_state
        {
            get
            {
                return toggle_debug.IsChecked.Value;
            }
            set
            {
                toggle_debug.IsChecked = value;
            }
        }

        private void toggle_debug_Unchecked(object sender, RoutedEventArgs e)
        {
            toggle_debug.Content = AppResources.PivotHelpControl_Off;
            GlobalVariables.IsDebugMode = false;
        }

        private void toggle_debug_Checked(object sender, RoutedEventArgs e)
        {
            toggle_debug.Content = AppResources.PivotHelpControl_On;
            GlobalVariables.IsDebugMode = true;
        }

        private void debug_send_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalVariables.IsDebugMode == true)
            {
                if (App.logger.hasCriticalLogged())
                {
                    App.logger.emailReport(textblock_debug_text);
                    App.logger.clearEventsFromLog();
                }
            }
        }

        private void toggle_debug_Click(object sender, RoutedEventArgs e)
        {
            App.logger.log(LogLevel.debug, "click toogle");
        }
    }
}
