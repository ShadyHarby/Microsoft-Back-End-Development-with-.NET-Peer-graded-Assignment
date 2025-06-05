using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models
{
    public class UpdateUserDto
    {
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string? FirstName { get; set; }
        
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string? LastName { get; set; }
        
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string? Email { get; set; }
        
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string? PhoneNumber { get; set; }
        
        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
        public string? Department { get; set; }
        
        [StringLength(100, ErrorMessage = "Position cannot exceed 100 characters")]
        public string? Position { get; set; }
        
        public bool? IsActive { get; set; }
    }
} 