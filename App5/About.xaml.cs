using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace AppDataManageTool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class About : Page
    {
        public About()
        {
            this.InitializeComponent();
        }

        private void Secret1_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            App.secretCodeCounter++;
            if (App.secretCodeCounter > 3)
                App.secretCodeCounter = 0;
            System.Diagnostics.Debug.WriteLine("SECRET1");
        }

        private void Secret2_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if ((App.secretCodeCounter == 3) || (App.secretCodeCounter == 6))
                App.secretCodeCounter *= 2;
            else
                App.secretCodeCounter = 0;

            System.Diagnostics.Debug.WriteLine("SECRET2");
        }
    }
}
