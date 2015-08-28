using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using AdRotator;
using Microsoft.Advertising.Mobile.UI;
using GoogleAds;

namespace AppSeafileClient.Class
{
    public partial class adDisplayControl : UserControl
    {
        public adDisplayControl()
        {
            InitializeComponent();

            Loaded += adDisplayControl_Loaded;
        }

        void adDisplayControl_Loaded(object sender, RoutedEventArgs e)
        {
            AdRotatorControl.Log += (s) => AdRotatorControl_Log(s);
            AdRotatorControl_Log("Adrotator Loaded");
        }

        void AdRotatorControl_Log(string message)
        {
            App.logger.log(LogLevel.debug, message);
        }

    }
}
