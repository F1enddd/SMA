using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMA.Services
{

        public class MauiDialogService : IUserDialogService
        {
            public async Task ShowMessage(string title, string message)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert(title, message, "OK");
                });
            }

            public async Task<bool> Ask(string title, string message, string okText = "OK", string cancelText = "Отмена")
            {
                return await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    return await Application.Current.MainPage.DisplayAlert(title, message, okText, cancelText);
                });
            }

            public async Task<string> Prompt(string title, string placeholder = "", bool isPassword = false)
            {
                return await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    return await Application.Current.MainPage.DisplayPromptAsync(
                        title,
                        "",
                        "OK",
                        "Отмена",
                        placeholder,
                        keyboard: isPassword ? Keyboard.Text : Keyboard.Default);
                });
            }
        }

 
}
