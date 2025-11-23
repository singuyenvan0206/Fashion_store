namespace WpfApp1
{

    public enum UserRole
    {

        Admin = 1,

        Manager = 2,

        Cashier = 3
    }

    public static class UserRoleExtensions
    {

        public static bool CanManageTierSettings(this UserRole role)
        {
            return role == UserRole.Admin || role == UserRole.Manager;
        }

    }
}
