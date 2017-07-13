using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace WPFExplorerStyleTreeView
{
    public class FileSystemInfoRevealConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is DirectoryInfo)
            {
                return value as DirectoryInfo;
            }
            else if(value is FileInfo)
            {
                return value as FileInfo;
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
