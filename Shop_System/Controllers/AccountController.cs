using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopSystem.Core.Dtos.Account;
using ShopSystem.Core.Enums;
using ShopSystem.Core.Models;
using ShopSystem.Core.Models.Account;
using ShopSystem.Core.Services;
using System.ComponentModel.DataAnnotations;

namespace Shop_System.Controllers
{
    public class AccountController : ApiBaseController
    {
        private readonly IAccountService _accountService;
        private readonly IFileService _fileService;

        public AccountController(IAccountService accountService, IFileService fileService)
        {
            _accountService = accountService;
            _fileService = fileService;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] Register model)
        {
            // Validate the model state
            if (!ModelState.IsValid)
            {
                return BadRequest(new ContentContainer<string>(null, "Please provide valid data"));
            }

            // Handle the role assignment
            string roleName;
            switch (model.Role)
            {
                case UserRole.User:
                    roleName = "User";
                    break;
                case UserRole.Admin:
                    roleName = "Admin";
                    break;
                default:
                    return BadRequest(new ContentContainer<string>(null, "Invalid role selected"));
            }

            // Handle image file upload
            if (model.ImageFile != null)
            {
                var fileResult = _fileService.SaveImage(model.ImageFile);
                if (fileResult.Item1 == 1)
                {
                    model.Image = fileResult.Item2; // getting name of image
                }
                else
                {
                    return BadRequest(new ContentContainer<string>(null, "Error uploading image"));
                }
            }

            try
            {
                // Pass the role name to the account service for registration
                var result = await _accountService.RegisterAsync(model, roleName, GenerateCallBackUrl);

                if (result.StatusCode == 200)
                {
                    return Ok(new ContentContainer<string>(result.Message, "Registration successful"));
                }
                else
                {
                    return StatusCode(result.StatusCode, new ContentContainer<string>(null, result.Message));
                }
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ContentContainer<string>(null, "An unexpected error occurred"));
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(Login dto)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _accountService.LoginAsync(dto);
            if (result.StatusCode == 400)
            {
                return BadRequest(result.Message);
            }
            return Ok(result);
        }

        [HttpPost("forgetPassword")]
        public async Task<IActionResult> ForgetPassword([FromHeader][EmailAddress] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email address is required.");
            }
            try
            {
                var result = await _accountService.ForgetPassword(email);

                if (result.StatusCode == 200)
                {
                    return Ok("Password reset email sent successfully.");
                }
                else
                {
                    return StatusCode(result.StatusCode, result.Message);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpPost("verfiyOtp")]
        public IActionResult VerfiyOtp(VerifyOtp dto)
        {
            var result = _accountService.VerfiyOtp(dto);

            if (result.StatusCode == 200)
            {
                return Ok(result.Message);
            }
            else
            {
                return BadRequest(result.Message); // Return the error message directly
            }
        }

        [HttpPut("resetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPassword dto)
        {
            var result = await _accountService.ResetPasswordAsync(dto);

            // Handle different response statuses
            switch (result.StatusCode)
            {
                case 200:
                    return Ok(result.Message);
                case 400:
                    return BadRequest(result.Message);
                case 500:
                    return StatusCode(500, result.Message);
                default:
                    return StatusCode(500, "An unexpected error occurred.");
            }
        }
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmUserEmail(string userId, string confirmationToken)
        {
            var result = await _accountService.ConfirmUserEmailAsync(userId!, confirmationToken!);

            if (result)
            {
                return RedirectPermanent(@"https://www.google.com/webhp?authuser=0");
            }
            else
            {
                return BadRequest("Failed to confirm user email.");
            }
        }

        private string GenerateCallBackUrl(string token, string userId)
        {
            var encodedToken = Uri.EscapeDataString(token);
            var encodedUserId = Uri.EscapeDataString(userId);
            var callBackUrl = $"{Request.Scheme}://{Request.Host}/api/Account/confirm-email?userId={encodedUserId}&confirmationToken={encodedToken}";
            return callBackUrl;
        }


        // Existing methods ...
        // this endpoint Get User Info By Id

        [HttpGet("getUserInfo/{userId}")]
        public async Task<IActionResult> GetUserInfoById(string userId)
        {
            var result = await _accountService.GetUserInfoByIdAsync(userId);
            if (result == null)
            {
                return NotFound(new ContentContainer<string>(null, "User not found."));
            }

            return Ok(new ContentContainer<UserDto>(result, "User information retrieved successfully."));
        }

        [HttpGet("getUsers")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _accountService.GetUsersAsync();
                return Ok(new ContentContainer<List<UserDto>>(users, "Users retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ContentContainer<string>(null, "An error occurred while retrieving users."));
            }
        }


        [HttpGet("usersCount")]
        public async Task<IActionResult> GetUsersCount()
        {
            try
            {
                // Get the count of users from the service
                var count = await _accountService.GetUsersCountAsync();

                return Ok(new ContentContainer<int>(count, "User count retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ContentContainer<string>(null, $"An error occurred: {ex.Message}"));
            }
        }


        [HttpDelete("deleteUser/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                // Call the service method to delete the user
                var result = await _accountService.DeleteUserAsync(userId);

                // Return appropriate response based on the result
                if (result.StatusCode == 200)
                {
                    return Ok(new ContentContainer<string>(result.Message, "User deleted successfully."));
                }
                else
                {
                    return StatusCode(result.StatusCode, new ContentContainer<string>(null, result.Message));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ContentContainer<string>(null, $"An error occurred: {ex.Message}"));
            }
        }


        [HttpPut("updateUserInfo")]
        public async Task<IActionResult> UpdateUserInfo([FromBody] UpdateUserDto updateUserDto)
        {
            // Validate the model state
            if (!ModelState.IsValid)
            {
                return BadRequest(new ContentContainer<string>(null, "Please provide valid data"));
            }

            try
            {
                // Call the service method to update user information
                var result = await _accountService.UpdateUserInfoAsync(updateUserDto);

                // Return appropriate response based on the result
                if (result.StatusCode == 200)
                {
                    return Ok(new ContentContainer<string>(result.Message, "User information updated successfully."));
                }
                else
                {
                    return StatusCode(result.StatusCode, new ContentContainer<string>(null, result.Message));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ContentContainer<string>(null, $"An error occurred: {ex.Message}"));
            }
        }

       
    }

}
