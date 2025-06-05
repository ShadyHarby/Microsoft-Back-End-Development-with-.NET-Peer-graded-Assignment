using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieve all users
        /// </summary>
        /// <returns>A list of all users</returns>
        /// <response code="200">Returns the list of users</response>
        /// <response code="401">If authentication is required</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            try
            {
                _logger.LogInformation("Getting all users");
                var users = await _userService.GetAllUsersAsync();
                
                _logger.LogInformation("Successfully retrieved {Count} users", users.Count());
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all users");
                throw; // Let middleware handle the exception
            }
        }

        /// <summary>
        /// Retrieve a specific user by ID
        /// </summary>
        /// <param name="id">The user ID</param>
        /// <returns>The user with the specified ID</returns>
        /// <response code="200">Returns the user</response>
        /// <response code="400">If the ID is invalid</response>
        /// <response code="401">If authentication is required</response>
        /// <response code="404">If the user is not found</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<User>> GetUserById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid user ID provided: {UserId}", id);
                    return BadRequest(new { error = "Invalid user ID", message = "User ID must be a positive integer" });
                }

                _logger.LogInformation("Getting user with ID: {UserId}", id);
                var user = await _userService.GetUserByIdAsync(id);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", id);
                    return NotFound(new { error = "User not found", message = $"User with ID {id} does not exist" });
                }

                _logger.LogInformation("Successfully retrieved user with ID: {UserId}", id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving user with ID: {UserId}", id);
                throw; // Let middleware handle the exception
            }
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        /// <param name="createUserDto">The user data to create</param>
        /// <returns>The created user</returns>
        /// <response code="201">Returns the newly created user</response>
        /// <response code="400">If the user data is invalid</response>
        /// <response code="401">If authentication is required</response>
        /// <response code="409">If a user with the same email already exists</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpPost]
        [ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for user creation: {@ModelState}", ModelState);
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Creating new user with email: {Email}", createUserDto.Email);
                var user = await _userService.CreateUserAsync(createUserDto);

                _logger.LogInformation("Successfully created user with ID: {UserId}", user.Id);
                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User creation failed due to business rule violation: {Message}", ex.Message);
                return Conflict(new { error = "User creation failed", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating user with email: {Email}", createUserDto.Email);
                throw; // Let middleware handle the exception
            }
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        /// <param name="id">The user ID to update</param>
        /// <param name="updateUserDto">The updated user data</param>
        /// <returns>The updated user</returns>
        /// <response code="200">Returns the updated user</response>
        /// <response code="400">If the user data is invalid</response>
        /// <response code="401">If authentication is required</response>
        /// <response code="404">If the user is not found</response>
        /// <response code="409">If a user with the same email already exists</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<User>> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid user ID provided for update: {UserId}", id);
                    return BadRequest(new { error = "Invalid user ID", message = "User ID must be a positive integer" });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for user update: {@ModelState}", ModelState);
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Updating user with ID: {UserId}", id);
                var user = await _userService.UpdateUserAsync(id, updateUserDto);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for update", id);
                    return NotFound(new { error = "User not found", message = $"User with ID {id} does not exist" });
                }

                _logger.LogInformation("Successfully updated user with ID: {UserId}", id);
                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User update failed due to business rule violation: {Message}", ex.Message);
                return Conflict(new { error = "User update failed", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user with ID: {UserId}", id);
                throw; // Let middleware handle the exception
            }
        }

        /// <summary>
        /// Delete a user
        /// </summary>
        /// <param name="id">The user ID to delete</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">If the user was successfully deleted</response>
        /// <response code="400">If the ID is invalid</response>
        /// <response code="401">If authentication is required</response>
        /// <response code="404">If the user is not found</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid user ID provided for deletion: {UserId}", id);
                    return BadRequest(new { error = "Invalid user ID", message = "User ID must be a positive integer" });
                }

                _logger.LogInformation("Deleting user with ID: {UserId}", id);
                var deleted = await _userService.DeleteUserAsync(id);

                if (!deleted)
                {
                    _logger.LogWarning("User with ID {UserId} not found for deletion", id);
                    return NotFound(new { error = "User not found", message = $"User with ID {id} does not exist" });
                }

                _logger.LogInformation("Successfully deleted user with ID: {UserId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting user with ID: {UserId}", id);
                throw; // Let middleware handle the exception
            }
        }

        /// <summary>
        /// Check if a user exists
        /// </summary>
        /// <param name="id">The user ID to check</param>
        /// <returns>Boolean indicating if user exists</returns>
        /// <response code="200">Returns true if user exists, false otherwise</response>
        /// <response code="400">If the ID is invalid</response>
        /// <response code="401">If authentication is required</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpHead("{id:int}")]
        [HttpGet("{id:int}/exists")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<bool>> UserExists(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid user ID provided for existence check: {UserId}", id);
                    return BadRequest(new { error = "Invalid user ID", message = "User ID must be a positive integer" });
                }

                _logger.LogDebug("Checking if user exists with ID: {UserId}", id);
                var exists = await _userService.UserExistsAsync(id);

                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if user exists with ID: {UserId}", id);
                throw; // Let middleware handle the exception
            }
        }
    }
} 