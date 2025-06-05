using UserManagementAPI.Models;
using System.Collections.Concurrent;

namespace UserManagementAPI.Services
{
    public class UserService : IUserService
    {
        private readonly ConcurrentDictionary<int, User> _users;
        private readonly ILogger<UserService> _logger;
        private int _nextId = 1;

        public UserService(ILogger<UserService> logger)
        {
            _logger = logger;
            _users = new ConcurrentDictionary<int, User>();
            
            // Seed with some initial data
            SeedData();
        }

        private void SeedData()
        {
            try
            {
                var seedUsers = new List<User>
                {
                    new User
                    {
                        Id = _nextId++,
                        FirstName = "John",
                        LastName = "Doe",
                        Email = "john.doe@techhive.com",
                        PhoneNumber = "555-0123",
                        Department = "IT",
                        Position = "Software Engineer",
                        CreatedAt = DateTime.UtcNow.AddDays(-30)
                    },
                    new User
                    {
                        Id = _nextId++,
                        FirstName = "Jane",
                        LastName = "Smith",
                        Email = "jane.smith@techhive.com",
                        PhoneNumber = "555-0124",
                        Department = "HR",
                        Position = "HR Manager",
                        CreatedAt = DateTime.UtcNow.AddDays(-15)
                    }
                };

                foreach (var user in seedUsers)
                {
                    _users.TryAdd(user.Id, user);
                }

                _logger.LogInformation("Seeded {Count} users", seedUsers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding user data");
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all users");
                
                // Simulate async operation
                await Task.Delay(10);
                
                var users = _users.Values.OrderBy(u => u.Id).ToList();
                _logger.LogInformation("Retrieved {Count} users", users.Count);
                
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all users");
                throw;
            }
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving user with ID: {UserId}", id);
                
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid user ID provided: {UserId}", id);
                    return null;
                }

                // Simulate async operation
                await Task.Delay(10);
                
                var userExists = _users.TryGetValue(id, out var user);
                
                if (!userExists || user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", id);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved user with ID: {UserId}", id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving user with ID: {UserId}", id);
                throw;
            }
        }

        public async Task<User> CreateUserAsync(CreateUserDto createUserDto)
        {
            try
            {
                _logger.LogInformation("Creating new user with email: {Email}", createUserDto.Email);

                // Check if email already exists
                if (await EmailExistsAsync(createUserDto.Email))
                {
                    _logger.LogWarning("Email {Email} already exists", createUserDto.Email);
                    throw new InvalidOperationException($"A user with email '{createUserDto.Email}' already exists.");
                }

                var user = new User
                {
                    Id = _nextId++,
                    FirstName = createUserDto.FirstName.Trim(),
                    LastName = createUserDto.LastName.Trim(),
                    Email = createUserDto.Email.Trim().ToLowerInvariant(),
                    PhoneNumber = createUserDto.PhoneNumber?.Trim(),
                    Department = createUserDto.Department?.Trim(),
                    Position = createUserDto.Position?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                // Simulate async operation
                await Task.Delay(10);

                _users.TryAdd(user.Id, user);
                
                _logger.LogInformation("Successfully created user with ID: {UserId}", user.Id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating user with email: {Email}", createUserDto.Email);
                throw;
            }
        }

        public async Task<User?> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
        {
            try
            {
                _logger.LogInformation("Updating user with ID: {UserId}", id);

                if (id <= 0)
                {
                    _logger.LogWarning("Invalid user ID provided for update: {UserId}", id);
                    return null;
                }

                var userExists = _users.TryGetValue(id, out var existingUser);
                
                if (!userExists || existingUser == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for update", id);
                    return null;
                }

                // Check if email is being updated and already exists
                if (!string.IsNullOrWhiteSpace(updateUserDto.Email) && 
                    updateUserDto.Email.Trim().ToLowerInvariant() != existingUser.Email.ToLowerInvariant())
                {
                    if (await EmailExistsAsync(updateUserDto.Email, id))
                    {
                        _logger.LogWarning("Email {Email} already exists for another user", updateUserDto.Email);
                        throw new InvalidOperationException($"A user with email '{updateUserDto.Email}' already exists.");
                    }
                }

                // Update fields that are provided
                if (!string.IsNullOrWhiteSpace(updateUserDto.FirstName))
                    existingUser.FirstName = updateUserDto.FirstName.Trim();
                
                if (!string.IsNullOrWhiteSpace(updateUserDto.LastName))
                    existingUser.LastName = updateUserDto.LastName.Trim();
                
                if (!string.IsNullOrWhiteSpace(updateUserDto.Email))
                    existingUser.Email = updateUserDto.Email.Trim().ToLowerInvariant();
                
                if (updateUserDto.PhoneNumber != null)
                    existingUser.PhoneNumber = updateUserDto.PhoneNumber.Trim();
                
                if (updateUserDto.Department != null)
                    existingUser.Department = updateUserDto.Department.Trim();
                
                if (updateUserDto.Position != null)
                    existingUser.Position = updateUserDto.Position.Trim();
                
                if (updateUserDto.IsActive.HasValue)
                    existingUser.IsActive = updateUserDto.IsActive.Value;

                existingUser.UpdatedAt = DateTime.UtcNow;

                // Simulate async operation
                await Task.Delay(10);

                _logger.LogInformation("Successfully updated user with ID: {UserId}", id);
                return existingUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user with ID: {UserId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting user with ID: {UserId}", id);

                if (id <= 0)
                {
                    _logger.LogWarning("Invalid user ID provided for deletion: {UserId}", id);
                    return false;
                }

                // Simulate async operation
                await Task.Delay(10);

                var removed = _users.TryRemove(id, out var removedUser);
                
                if (removed && removedUser != null)
                {
                    _logger.LogInformation("Successfully deleted user with ID: {UserId}", id);
                    return true;
                }

                _logger.LogWarning("User with ID {UserId} not found for deletion", id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting user with ID: {UserId}", id);
                throw;
            }
        }

        public async Task<bool> UserExistsAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return false;

                // Simulate async operation
                await Task.Delay(5);
                
                return _users.ContainsKey(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if user exists with ID: {UserId}", id);
                throw;
            }
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                // Simulate async operation
                await Task.Delay(5);

                var normalizedEmail = email.Trim().ToLowerInvariant();
                
                return _users.Values.Any(u => 
                    u.Email.ToLowerInvariant() == normalizedEmail && 
                    (!excludeUserId.HasValue || u.Id != excludeUserId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if email exists: {Email}", email);
                throw;
            }
        }
    }
} 