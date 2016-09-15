using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

namespace AppDataManageTool
{
    class AppListCacheUpdater
    {

        private static async Task UpdateStatusBar(string message, double? val = null)
        {
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                if (statusBar != null)
                {
                    if (message != null)
                    {
                        statusBar.ProgressIndicator.ProgressValue = val;
                        statusBar.ProgressIndicator.Text = message;
                        await statusBar.ProgressIndicator.ShowAsync();
                    }
                    else
                    {
                        statusBar.ProgressIndicator.Text = "";
                        await statusBar.ProgressIndicator.HideAsync();
                    }
                }
            }
        }

        private static async void LoadAppData_LoadingProgress(object sender, LoadingEventArgs e)
        {
            int percent = (int)Math.Round((100.0 * e.Current) / e.Total);
            //await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            //{
            await UpdateStatusBar("Updating app list cache " + percent.ToString() + "%");
            //});
        }

        internal static async void LoadAppsInBackground()
        {
            await Task.Run(async () =>
             {
                 while (App.updateCacheInProgress)
                     await Task.Delay(1000);

                 //Task would cause some wierd issues. So I used Dispatcher instead.
                 //Task.Run(async () =>
                 await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, async () =>
                  {
                      App.updateCacheInProgress = true;

                      LoadAppData loadAppData = new LoadAppData();

                      loadAppData.LoadingProgress += LoadAppData_LoadingProgress;

                      await loadAppData.LoadApps();
                      await LoadAppData.SaveAppList();

                      loadAppData.LoadingProgress -= LoadAppData_LoadingProgress;

                      await UpdateStatusBar(null);

                      App.updateCacheInProgress = false;
                  });
             });
        }
    }
}
