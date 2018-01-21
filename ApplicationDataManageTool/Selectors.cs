using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AppDataManageTool
{
    class AppListEmptyOrFullSelector : DataTemplateSelector
    {
        public DataTemplate Full { get; set; }
        public DataTemplate Empty { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var groupItem = item as DataGroup;

            if (groupItem == null)
                return Full;

            var isEmpty = groupItem.Items == null || groupItem.Items.Count == 0;

            // Disable empty items
            var selectorItem = container as Control;
            if (selectorItem != null)
            {
                selectorItem.IsEnabled = !isEmpty;
            }

            return (!isEmpty) ? Full : Empty;
        }
    }
}
