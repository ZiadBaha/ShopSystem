using ShopSystem.Core.Dtos.Account;
using ShopSystem.Core.Enums;
using ShopSystem.Core.Errors;
using ShopSystem.Core.Models.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopSystem.Core.Services
{
    public interface IAccountService
    {
        Task<ApiResponse> RegisterAsync(Register dto, string roleName, Func<string, string, string> generateCallBackUrl);
        //Task<ApiResponse> RegisterAsync(Register user, Func<string, string, string> generateCallBackUrl);
        Task<ApiResponse> LoginAsync(Login dto);
        Task<ApiResponse> ForgetPassword(string email);
        ApiResponse VerfiyOtp(VerifyOtp dto);
        Task SendEmailAsync(string To, string Subject, string Body, CancellationToken Cancellation = default);
        Task<bool> ConfirmUserEmailAsync(string userId, string token);
        Task<ApiResponse> ResetPasswordAsync(ResetPassword dto);

        // add later 
        Task<UserDto> GetUserInfoByIdAsync(string userId);
        Task<ApiResponse> UpdateUserInfoAsync(UpdateUserDto dto);


        Task<List<UserDto>> GetUsersAsync(); // To get all users
        Task<int> GetUsersCountAsync(); // Method to get users count
        Task<ApiResponse> DeleteUserAsync(string userId); // Method to delete user

    }
}
