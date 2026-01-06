using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMA.Helpers
{
    public static class ServiceHelper
    {
        public static T GetService<T>() => Current.GetService<T>();

        private static IServiceProvider Current =>
            Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("MAUI service provider not available");
    }
    
}
