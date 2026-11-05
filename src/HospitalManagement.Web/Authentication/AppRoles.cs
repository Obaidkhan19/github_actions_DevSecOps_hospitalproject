namespace HospitalManagement.Web.Authentication;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string AdminOrStaff = "Admin,Staff";

    public static readonly string[] All = { Admin, Staff };
}
