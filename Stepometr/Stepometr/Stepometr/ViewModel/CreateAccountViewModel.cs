﻿using Rg.Plugins.Popup.Services;
using Stepometer.Models.Login;
using Stepometer.Page;
using Stepometer.Service.LoggerService;
using Stepometer.Service.LoginServices;
using Stepometer.Views.Popup;
using System;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Stepometer.ViewModel
{
    public class CreateAccountViewModel
    {
        private readonly ILogService _logService;
        private readonly ILoginService _loginService;

        public ICommand CreateAccountCommand { get; }
        public RegisterModel LoginModel { get; set; }

        public CreateAccountViewModel(ILogService logService, ILoginService loginService)
        {
            _logService = logService;
            _loginService = loginService;

            LoginModel = new();

            CreateAccountCommand = new Command(async () => await CreateNewAccount());
        }

        private async Task CreateNewAccount()
        {
            try
            {
                if (IsAnyPropEmpty())
                {
                    await PopupNavigation.Instance.PushAsync(new ErrorPopup("All fields must be filled"));
                    return;
                }

                if (!IsValidEmail(LoginModel.Email) || !IsValidPassword(LoginModel.Password))
                {
                    await PopupNavigation.Instance.PushAsync(new ErrorPopup("Invalid mail or password"));
                    return;
                }

                if (LoginModel.Password != LoginModel.ConfirmPassword)
                {
                    await PopupNavigation.Instance.PushAsync(new ErrorPopup("Error confirm password"));
                    return;
                }


                var isCreatedAccount = await _loginService.CreateNewAccount(LoginModel);

                if (!isCreatedAccount)
                {
                    await PopupNavigation.Instance.PushAsync(new ErrorPopup("Error creating account. \n\r Perhaps an account has already been created"));
                    return;
                }

                await Shell.Current.GoToAsync($"//{nameof(StepometerPage)}");
                await PopupNavigation.Instance.PopAsync();
            }
            catch (Exception e)
            {
                _logService.TrackException(e, MethodBase.GetCurrentMethod()?.Name);
            }
        }

        private bool IsAnyPropEmpty()
        {
            bool isAnyPropEmpty = LoginModel.GetType().GetProperties()
                     .Where(p => p.GetValue(LoginModel) is string) // selecting only string props
                     .Any(p => string.IsNullOrWhiteSpace((p.GetValue(LoginModel) as string)));
            return isAnyPropEmpty;
        }

        private bool IsValidEmail(string email)
        {
            var trimmedEmail = email.Trim();

            if (trimmedEmail.EndsWith("."))
            {
                return false;
            }
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == trimmedEmail;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPassword(string password)
        {
            if (password.Length < 6)
                return false;
            return true;
        }
    }
}
