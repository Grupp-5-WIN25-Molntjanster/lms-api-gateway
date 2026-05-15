namespace Lms.ApiGateway.Authorization;

/// <summary>
/// Centralized policy names for role-based authorization.
/// Using constants prevents typos and makes refactoring safe.
/// 
/// These policies are used by:
/// - The gateway itself (for route-level protection)
/// - Downstream services (for endpoint-level protection)
/// 
/// Role Hierarchy:
///   Admin > Instructor > Student
///   Admin can do everything
///   Instructor can manage their own courses
///   Student can view and enroll
/// </summary>
public static class Policies
{
    /// <summary>
    /// Requires the user to be authenticated (any role).
    /// Used for: viewing own profile, enrolling in courses.
    /// </summary>
    public const string Authenticated = "Authenticated";

    /// <summary>
    /// Requires Instructor or Admin role.
    /// Used for: creating/editing course content, viewing enrollment lists.
    /// </summary>
    public const string InstructorOrAdmin = "InstructorOrAdmin";

    /// <summary>
    /// Requires Admin role only.
    /// Used for: user management, system configuration, deleting content.
    /// </summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>
    /// All valid roles in the system.
    /// </summary>
    public static class Roles
    {
        public const string Student = "Student";
        public const string Instructor = "Instructor";
        public const string Admin = "Admin";
    }
}