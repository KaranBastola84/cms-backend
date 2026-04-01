namespace JWTAuthAPI.Models
{
    /// <summary>
    /// Master list of all permission keys used in the system.
    /// These keys must match what the frontend DashboardLayout uses.
    /// </summary>
    public static class Permissions
    {
        // Dashboard
        public const string Dashboard = "dashboard";

        // Student Management
        public const string ViewStudents = "view-students";
        public const string ManageStudents = "manage-students";

        // Courses & Batches
        public const string CoursesBatches = "courses-batches";

        // Attendance
        public const string Attendance = "attendance";

        // Inquiries
        public const string Inquiries = "inquiries";

        // Finance & Payments
        public const string PaymentFinance = "payment-finance";

        // Student Documents
        public const string StudentDocuments = "student-documents";

        // Reports
        public const string Reports = "reports";

        /// <summary>
        /// Full master list with labels and grouping — returned by GET /api/permissions
        /// </summary>
        public static List<PermissionDefinition> All => new()
        {
            new("dashboard", "Dashboard", "General"),
            new("view-students", "View Students", "Student Management"),
            new("manage-students", "Manage Students", "Student Management"),
            new("courses-batches", "Courses & Batches", "Academic"),
            new("attendance", "Attendance", "Academic"),
            new("inquiries", "Inquiries", "CRM"),
            new("payment-finance", "Payments & Finance", "Finance"),
            new("student-documents", "Student Documents", "Student Management"),
            new("reports", "Reports", "Finance"),
        };

        /// <summary>
        /// Default permissions per role — used to seed the DB on first run.
        /// Admin always gets all permissions (handled in service, not seeded).
        /// </summary>
        public static Dictionary<string, List<string>> RoleDefaults => new()
        {
            [Roles.Staff] = new()
            {
                Dashboard,
                ViewStudents,
                ManageStudents,
                CoursesBatches,
                Attendance,
                Inquiries,
                PaymentFinance,
                Reports,
                StudentDocuments
            },
            [Roles.Trainer] = new()
            {
                Dashboard,
                ViewStudents,
                ManageStudents,
                CoursesBatches,
                Attendance,
                Inquiries,
                PaymentFinance,
                Reports
            },
            [Roles.Student] = new()
            {
                Dashboard
            }
        };
    }

    public class PermissionDefinition
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Group { get; set; }

        public PermissionDefinition(string key, string label, string group)
        {
            Key = key;
            Label = label;
            Group = group;
        }
    }

    public class RolePermission
    {
        public int Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string PermissionKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// Per-user permission override. IsGranted=true adds a permission on top of role defaults.
    /// IsGranted=false removes a permission from role defaults.
    /// </summary>
    public class UserPermission
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string PermissionKey { get; set; } = string.Empty;
        public bool IsGranted { get; set; } = true;

        public virtual ApplicationUser? User { get; set; }
    }

    // DTOs
    public class RolePermissionsDto
    {
        public string Role { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
    }

    public class UpdateRolePermissionsDto
    {
        public List<string> Permissions { get; set; } = new();
    }

    public class UserPermissionsDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<string> EffectivePermissions { get; set; } = new();
        public List<string> RolePermissions { get; set; } = new();
        public List<string> GrantedOverrides { get; set; } = new();
        public List<string> RevokedOverrides { get; set; } = new();
    }

    public class UpdateUserPermissionsDto
    {
        public List<string> Grant { get; set; } = new();
        public List<string> Revoke { get; set; } = new();
    }
}
