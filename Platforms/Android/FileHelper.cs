using Android.Content;
using Android.Net;
using AndroidX.DocumentFile.Provider;

namespace SMA
{
    public static class AndroidSafHelper
    {
        public static List<SafFile> ListFilesFromUri(Android.Net.Uri treeUri, Context context)
        {
            var list = new List<SafFile>();
            var doc = DocumentFile.FromTreeUri(context, treeUri);

            if (doc == null || !doc.IsDirectory)
                return list;

            foreach (var file in doc.ListFiles())
            {
                if (file.IsDirectory)
                {
                    // рекурсивно можно пройти поддиректории
                    // list.AddRange(ListFilesFromUri(file.Uri, context));
                }
                else
                {
                    list.Add(new SafFile
                    {
                        Name = file.Name,
                        Uri = file.Uri
                    });
                }
            }

            return list;
        }
    }

    public class SafFile
    {
        public string Name { get; set; }
        public Android.Net.Uri Uri { get; set; }
    }
}


