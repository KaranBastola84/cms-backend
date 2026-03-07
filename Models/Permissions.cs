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

        // User Management
        public const string ViewUsers = "view-users";
        public const string ManageUsers = "manage-users";

        // Student Management
        public const string ViewStudents = "view-students";
        public const string ManageStudents = "manage-students";

        // Staff Management
        public const string ViewStaff = "view-staff";
        public const string ManageStaff = "manage-staff";

        // Trainer Management
        public const string ViewTrainers = "view-trainers";
        public const string ManageTrainers = "manage-trainers";

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

        // Audit Logs
        public const string AuditLogs = "audit-logs";

        // Inventory & Orders
        public const string Inventory = "inventory";
        public const string Orders = "orders";

        /// <summary>
        /// Full master list with labels and grouping — returned by GET /api/permissions
        /// </summary>
        public static List<PermissionDefinition> All => new()
        {
            new("dashboard", "Dashboard", "General"),
            new("view-users", "View Users", "User Management"),
            new("manage-users", "Manage Users", "User Management"),
            new("view-students", "View Students", "Student Management"),
            new("manage-students", "Manage Students", "Student Management"),
            new("view-staff", "View Staff", "Staff Management"),
            new("manage-staff", "Manage Staff", "Staff Management"),
            new("view-trainers", "View Trainers", "Trainer Management"),
            new("manage-trainers", "Manage Trainers", "Trainer Management"),
            new("courses-batches", "Courses & Batches", "Academic"),
            new("attendance", "Attendance", "Academic"),
            new("inquiries", "Inquiries", "CRM"),
            new("payment-finance", "Payments & Finance", "Finance"),
            new("student-documents", "Student Documents", "Student Management"),
            new("reports", "Reports", "Finance"),
            new("audit-logs", "Audit Logs", "Administration"),
            new("inventory", "Inventory", "Store"),
            new("orders", "Orders", "Store"),
        };

        /// <summary>
        /// Default permissions per role — used to seed the DB on first run.
        /// Admin always gets all permissions (handled in service, not seeded).
        /// </summary>
        public static Dictionary<string, List<string>> RoleDefaults => new()
        {
            [Roles.Staff] = new()
            {
                Dashboard, ViewStudents, ManageStudents, Attendance, Inquiries, StudentDocuments
            },
            [Roles.Trainer] = new()
            {
                Dashboard, ViewStudents, Attendance, CoursesBatches
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
