using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using SteamAuth;
using SMA.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SMA.Pages
{
    public partial class ImportAccountPage : ContentPage
    {
        private Manifest mManifest;
        private static TaskCompletionSource<bool> _tcs;
        public ImportAccountPage()
        {
            InitializeComponent();
            _tcs = new TaskCompletionSource<bool>();
            mManifest = Manifest.GetManifest();
        }

        private async void OnImportClicked(object sender, EventArgs e)
        {
            // Определяем путь к manifest.json
            string manifestPath = Path.Combine(FileSystem.AppDataDirectory, "maFiles", "manifest.json");

            if (!File.Exists(manifestPath))
            {
                await DisplayAlert("Error", "Manifest file missing! Restart the app.", "OK");
                return;
            }

            // Читаем манифест
            var manifestJson = await File.ReadAllTextAsync(manifestPath);
            var appManifest = JsonConvert.DeserializeObject<AppManifest>(manifestJson);

            if (appManifest.Encrypted)
            {
                await DisplayAlert("Error", "Existing account is encrypted. Decrypt it first.", "OK");
                return;
            }

            // Получаем ключ шифрования из Entry
            string encryptionKey = PasskeyEntry.Text;

            try
            {
                // File Picker для .maFile
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.WinUI, new[] { ".mafile", "*" } },
        { DevicePlatform.Android, new[] { "*/*" } },
        { DevicePlatform.iOS, new[] { "public.data" } },
        { DevicePlatform.MacCatalyst, new[] { "public.data" } }
    }),
                    PickerTitle = "Select .maFile"
                });

                if (result == null) return; // Пользователь отменил

                string fileContents;
                using (var stream = await result.OpenReadAsync())
                using (var reader = new StreamReader(stream))
                    fileContents = await reader.ReadToEndAsync();

                // Определяем путь к manifest.json той же папки, где находится файл
                string fileDirectory = Path.GetDirectoryName(result.FullPath);
                string localManifestPath = Path.Combine(fileDirectory, "manifest.json");

                // Если ключ пустой — импорт обычного maFile
                if (string.IsNullOrEmpty(encryptionKey))
                {
                    await ImportPlainMaFile(fileContents);
                }
                else
                {
                    await ImportEncryptedMaFile(fileContents, encryptionKey, localManifestPath, result.FileName);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Import failed: {ex.Message}", "OK");
                _tcs.TrySetResult(false);
            }
        }

        private async Task ImportPlainMaFile(string fileContents)
        {
            var maFile = JsonConvert.DeserializeObject<SteamGuardAccount>(fileContents);

            if (maFile.Session == null || maFile.Session.SteamID == 0 || maFile.Session.IsAccessTokenExpired())
            {
                var loginPage = new LoginPage(LoginPage.LoginType.Import, maFile);
                await Navigation.PushModalAsync(loginPage);
                bool loginResult = await loginPage.WaitForLoginAsync();

                if (!loginResult || loginPage.Session == null || loginPage.Session.SteamID == 0)
                {
                    await DisplayAlert("Error", "Login failed. Try to import this account again.", "OK");
                    _tcs.TrySetResult(false);
                    return;
                }

                maFile.Session = loginPage.Session;
            }

            mManifest.SaveAccount(maFile, false);
            await DisplayAlert("Success", "Account Imported!", "OK");
            _tcs.TrySetResult(true);
            await Navigation.PopModalAsync();
        }

        private async Task ImportEncryptedMaFile(string fileContents, string key, string manifestPath, string maFileName)
        {
            if (!File.Exists(manifestPath))
            {
                await DisplayAlert("Error", "manifest.json missing in selected folder. Import failed.", "OK");
                _tcs.TrySetResult(false);
                return;
            }

            var manifestJson = await File.ReadAllTextAsync(manifestPath);
            var importManifest = JsonConvert.DeserializeObject<ImportManifest>(manifestJson);

            ImportManifestEntry entry = importManifest.Entries.Find(e => e.Filename == maFileName);
            if (entry == null || string.IsNullOrEmpty(entry.IV) || string.IsNullOrEmpty(entry.Salt))
            {
                await DisplayAlert("Error", "Encrypted info not found in manifest.json. Import failed.", "OK");
                _tcs.TrySetResult(false);
                return;
            }

            string decryptedText = FileEncryptor.DecryptData(key, entry.Salt, entry.IV, fileContents);
            if (decryptedText == null)
            {
                await DisplayAlert("Error", "Decryption failed. Import failed.", "OK");
                _tcs.TrySetResult(false);
                return;
            }

            var maFile = JsonConvert.DeserializeObject<SteamGuardAccount>(decryptedText);

            if (maFile.Session == null || maFile.Session.SteamID == 0 || maFile.Session.IsAccessTokenExpired())
            {
                var loginPage = new LoginPage(LoginPage.LoginType.Import, maFile);
                await Navigation.PushModalAsync(loginPage);
                bool loginResult = await loginPage.WaitForLoginAsync();

                if (!loginResult || loginPage.Session == null || loginPage.Session.SteamID == 0)
                {
                    await DisplayAlert("Error", "Login failed. Try to import this account again.", "OK");
                    return;
                }

                maFile.Session = loginPage.Session;
            }

            mManifest.SaveAccount(maFile, false);
            await DisplayAlert("Success", "Encrypted account imported and decrypted!", "OK");
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
        public static Task<bool> WaitForImportAsync() => _tcs.Task;
    }

    public class AppManifest
    {
        public bool Encrypted { get; set; }
    }

    public class ImportManifest
    {
        public bool Encrypted { get; set; }
        public List<ImportManifestEntry> Entries { get; set; }
    }

    public class ImportManifestEntry
    {
        public string IV { get; set; }
        public string Salt { get; set; }
        public string Filename { get; set; }
        public ulong SteamID { get; set; }
    }
}
