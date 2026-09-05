namespace Frontend.Services;

public static class Translations
{
    public static readonly Dictionary<string, Dictionary<string, string>> Data = new()
    {
        ["en"] = new()
        {
            ["nav_home"] = "Home",
            ["nav_jobs"] = "Browse Jobs",
            ["nav_employer_dashboard"] = "Employer Dashboard",
            ["nav_worker_dashboard"] = "Worker Dashboard",
            ["nav_admin"] = "Admin Panel",
            ["nav_login"] = "Login",
            ["nav_register"] = "Register",
            ["nav_logout"] = "Logout",
            ["btn_apply"] = "Apply Now",
            ["job_salary"] = "Salary",
            ["job_location"] = "Location",
            ["job_category"] = "Category",
        },
        ["bn"] = new()
        {
            ["nav_home"] = "হোম",
            ["nav_jobs"] = "জব খুঁজুন",
            ["nav_employer_dashboard"] = "এমপ্লয়ার ড্যাশবোর্ড",
            ["nav_worker_dashboard"] = "ওয়ার্কার ড্যাশবোর্ড",
            ["nav_admin"] = "অ্যাডমিন প্যানেল",
            ["nav_login"] = "লগইন",
            ["nav_register"] = "রেজিস্টার",
            ["nav_logout"] = "লগআউট",
            ["btn_apply"] = "আবেদন করুন",
            ["job_salary"] = "বেতন",
            ["job_location"] = "অবস্থান",
            ["job_category"] = "ক্যাটাগরি",
        }
    };
}