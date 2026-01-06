using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMA.Services
{
    public interface IUserDialogService
    {
        Task ShowMessage(string title, string message);
        Task<bool> Ask(string title, string message, string okText = "OK", string cancelText = "Отмена");
        Task<string> Prompt(string title, string placeholder = "", bool isPassword = false);
    }
}
