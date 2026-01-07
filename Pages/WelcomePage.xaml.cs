using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using PortableStorage;
using SMA.Helpers;
using SMA.Services;
using System;
using System.IO;
using System.Threading.Tasks;
#if ANDROID
using AndroidX.DocumentFile.Provider;
using Android.Net;
#endif

namespace SMA.Pages
{
    public partial class WelcomePage : ContentPage
    {
        private Manifest man;

        public WelcomePage()
        {
            InitializeComponent();
            man = Manifest.GetManifest();
        }

        private async void OnJustStartClicked(object sender, EventArgs e)
        {
            man.FirstRun = false;
            man.Save();
            await ShowMainPageAsync();
        }

        private async void OnImportClicked(object sender, EventArgs e)
        {
            var dialogService = ServiceHelper.GetService<IUserDialogService>();
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                var status = await Permissions.RequestAsync<Permissions.StorageRead>();
                if (status != PermissionStatus.Granted)
                {
                    await dialogService.ShowMessage("Error", "Permission to access storage is required.");
                    return;
                }
            }
#if ANDROID
            var result = await FilePicker.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Выберите файлы"
            });

            if (result == null)
                return;

            var folder = Path.Combine(FileSystem.AppDataDirectory, "maFiles");
            Directory.CreateDirectory(folder);

            foreach (var file in result)
            {
                using var input = await file.OpenReadAsync();

                var targetPath = Path.Combine(folder, file.FileName);

                using var output = File.Create(targetPath);

                await input.CopyToAsync(output);
            }
#else



            var folderPicker = FolderPicker.Default;

            var result = await folderPicker.PickAsync(CancellationToken.None);

            if (!result.IsSuccessful)
            {
                await dialogService.ShowMessage("Canceled", "Folder selection was canceled.");
                return;
            }

            string path = result.Folder.Path;
            string pathToCopy = null;

            if (Directory.Exists(Path.Combine(path, "maFiles")))
                pathToCopy = Path.Combine(path, "maFiles");
            else if (File.Exists(Path.Combine(path, "manifest.json")))
                pathToCopy = path;
            else
            {
                await dialogService.ShowMessage("Error",
                    "This folder does not contain either a manifest.json or an maFiles folder.\nPlease select the correct Steam Desktop Authenticator directory.");
                return;
            }

            string destDir = Path.Combine(FileSystem.AppDataDirectory, "maFiles");

            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);


            var files = Directory.GetFiles(pathToCopy, "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string destFile = file.Replace(pathToCopy, destDir);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                File.Copy(file, destFile, true);
            }
#endif
            try
            {
                man = Manifest.GetManifest(true);
                man.FirstRun = false;
                man.Save();
            }
            catch (ManifestParseException)
            {
                try
                {
                    await dialogService.ShowMessage("Steam Mobile Authenticator",
                        "Your settings were corrupted and have been reset to defaults.");
                    man = Manifest.GenerateNewManifest(true);
                }
                catch (MaFileEncryptedException)
                {
                    await dialogService.ShowMessage("Error",
                        "SDA was unable to recover your encrypted accounts. You'll need to recover manually.");
                    await Launcher.OpenAsync(new System.Uri("https://github.com/Jessecar96/SteamDesktopAuthenticator/wiki/Help!-I'm-locked-out-of-my-account"));
                    return;
                }
            }

            await dialogService.ShowMessage("Success",
                "All accounts and settings have been imported! Tap OK to continue.");
            await ShowMainPageAsync();
        }

        private async Task ShowMainPageAsync()
        {
            Application.Current.MainPage = new NavigationPage(new MainPage());
        }

        private async void OnIconClicked(object sender, EventArgs e)
        {
            await Launcher.OpenAsync(new System.Uri("https://github.com/F1enddd"));
        }
    }
}
