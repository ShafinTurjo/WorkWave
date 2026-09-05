namespace Frontend.Services;

public static class Translations
{
    public static readonly Dictionary<string, Dictionary<string, string>> Data = new()
    {
        ["en"] = new()
        {
            // Navbar
            ["nav_home"] = "Home",
            ["nav_jobs"] = "Browse Jobs",
            ["nav_employer_dashboard"] = "Employer Dashboard",
            ["nav_worker_dashboard"] = "Worker Dashboard",
            ["nav_admin"] = "Admin Panel",
            ["nav_login"] = "Login",
            ["nav_register"] = "Register",
            ["nav_logout"] = "Logout",

            // Generic / shared
            ["btn_apply"] = "Apply Now",
            ["job_salary"] = "Salary",
            ["job_location"] = "Location",
            ["job_category"] = "Category",

            // Home page
            ["home_hero_title"] = "Find the Perfect Job or Hire Top Talent",
            ["home_hero_subtitle"] = "Connect with opportunities, showcase your skills, and streamline your workflow with WorkWave.",
            ["home_browse_jobs"] = "Browse Jobs",
            ["home_post_job"] = "Post a Job",
            ["home_feature1_title"] = "Smart Job Search",
            ["home_feature1_desc"] = "Easily filter and find projects tailored to your specific skillset and interests.",
            ["home_feature2_title"] = "Easy Applications",
            ["home_feature2_desc"] = "Apply to jobs in seconds with your customized profile and track status instantly.",
            ["home_feature3_title"] = "Verified Projects",
            ["home_feature3_desc"] = "Post and collaborate on authentic projects with complete confidence and security.",
            ["home_cta_title"] = "Ready to get started?",
            ["home_cta_subtitle"] = "Create an account today to start posting or applying for jobs.",
            ["home_create_account"] = "Create Account",

            // Login page
            ["login_title"] = "Welcome Back",
            ["login_subtitle"] = "Sign in to access your WorkWave account",
            ["lbl_email"] = "Email Address",
            ["lbl_password"] = "Password",
            ["remember_me"] = "Remember me",
            ["forgot_password"] = "Forgot password?",
            ["btn_signin"] = "Sign In",
            ["btn_signing_in"] = "Signing in...",
            ["msg_no_account"] = "Don't have an account?",
            ["link_create_account"] = "Create one here",
            ["err_invalid_credentials"] = "Invalid email or password.",
            ["err_server_unreachable"] = "Could not reach the server. Please try again.",

            // Register page
            ["register_title"] = "Create Account",
            ["register_subtitle"] = "Join WorkWave to find or post opportunities",
            ["lbl_i_want_to"] = "I want to:",
            ["role_worker"] = "Apply for Jobs (Worker)",
            ["role_employer"] = "Post Jobs (Employer)",
            ["lbl_full_name"] = "Full Name",
            ["lbl_confirm_password"] = "Confirm Password",
            ["btn_register"] = "Register",
            ["btn_creating_account"] = "Creating account...",
            ["msg_have_account"] = "Already have an account?",
            ["link_login_here"] = "Login here",
            ["err_passwords_mismatch"] = "Passwords do not match.",
            ["err_email_exists"] = "An account with this email already exists.",
            ["err_registration_failed"] = "Registration failed. Please check your details and try again.",
        },
        ["bn"] = new()
        {
            // Navbar
            ["nav_home"] = "হোম",
            ["nav_jobs"] = "জব খুঁজুন",
            ["nav_employer_dashboard"] = "এমপ্লয়ার ড্যাশবোর্ড",
            ["nav_worker_dashboard"] = "ওয়ার্কার ড্যাশবোর্ড",
            ["nav_admin"] = "অ্যাডমিন প্যানেল",
            ["nav_login"] = "লগইন",
            ["nav_register"] = "রেজিস্টার",
            ["nav_logout"] = "লগআউট",

            // Generic / shared
            ["btn_apply"] = "আবেদন করুন",
            ["job_salary"] = "বেতন",
            ["job_location"] = "অবস্থান",
            ["job_category"] = "ক্যাটাগরি",

            // Home page
            ["home_hero_title"] = "সেরা চাকরি খুঁজুন অথবা সেরা প্রতিভা নিয়োগ দিন",
            ["home_hero_subtitle"] = "WorkWave-এর মাধ্যমে সুযোগের সাথে সংযুক্ত হন, নিজের দক্ষতা প্রদর্শন করুন এবং আপনার কাজকে সহজ করুন।",
            ["home_browse_jobs"] = "জব খুঁজুন",
            ["home_post_job"] = "জব পোস্ট করুন",
            ["home_feature1_title"] = "স্মার্ট জব সার্চ",
            ["home_feature1_desc"] = "আপনার দক্ষতা ও আগ্রহ অনুযায়ী সহজেই প্রজেক্ট ফিল্টার করে খুঁজে নিন।",
            ["home_feature2_title"] = "সহজ আবেদন",
            ["home_feature2_desc"] = "আপনার প্রোফাইল দিয়ে সেকেন্ডেই আবেদন করুন এবং সাথে সাথে স্ট্যাটাস দেখুন।",
            ["home_feature3_title"] = "যাচাইকৃত প্রজেক্ট",
            ["home_feature3_desc"] = "সম্পূর্ণ নিরাপত্তা ও আস্থার সাথে প্রকৃত প্রজেক্টে পোস্ট ও কাজ করুন।",
            ["home_cta_title"] = "শুরু করতে প্রস্তুত?",
            ["home_cta_subtitle"] = "জব পোস্ট বা আবেদন শুরু করতে আজই একটা অ্যাকাউন্ট তৈরি করুন।",
            ["home_create_account"] = "অ্যাকাউন্ট তৈরি করুন",

            // Login page
            ["login_title"] = "স্বাগতম",
            ["login_subtitle"] = "আপনার WorkWave অ্যাকাউন্টে প্রবেশ করতে লগইন করুন",
            ["lbl_email"] = "ইমেইল ঠিকানা",
            ["lbl_password"] = "পাসওয়ার্ড",
            ["remember_me"] = "মনে রাখুন",
            ["forgot_password"] = "পাসওয়ার্ড ভুলে গেছেন?",
            ["btn_signin"] = "লগইন করুন",
            ["btn_signing_in"] = "লগইন হচ্ছে...",
            ["msg_no_account"] = "অ্যাকাউন্ট নেই?",
            ["link_create_account"] = "এখানে তৈরি করুন",
            ["err_invalid_credentials"] = "ইমেইল অথবা পাসওয়ার্ড ভুল।",
            ["err_server_unreachable"] = "সার্ভারের সাথে সংযোগ করা যাচ্ছে না। আবার চেষ্টা করুন।",

            // Register page
            ["register_title"] = "অ্যাকাউন্ট তৈরি করুন",
            ["register_subtitle"] = "সুযোগ খুঁজতে বা পোস্ট করতে WorkWave-এ যোগ দিন",
            ["lbl_i_want_to"] = "আমি চাই:",
            ["role_worker"] = "চাকরির জন্য আবেদন করতে (ওয়ার্কার)",
            ["role_employer"] = "চাকরি পোস্ট করতে (এমপ্লয়ার)",
            ["lbl_full_name"] = "পূর্ণ নাম",
            ["lbl_confirm_password"] = "পাসওয়ার্ড নিশ্চিত করুন",
            ["btn_register"] = "রেজিস্টার করুন",
            ["btn_creating_account"] = "অ্যাকাউন্ট তৈরি হচ্ছে...",
            ["msg_have_account"] = "আগে থেকেই অ্যাকাউন্ট আছে?",
            ["link_login_here"] = "এখানে লগইন করুন",
            ["err_passwords_mismatch"] = "পাসওয়ার্ড দুটো মিলছে না।",
            ["err_email_exists"] = "এই ইমেইল দিয়ে ইতিমধ্যে একটা অ্যাকাউন্ট আছে।",
            ["err_registration_failed"] = "রেজিস্ট্রেশন ব্যর্থ হয়েছে। আপনার তথ্য চেক করে আবার চেষ্টা করুন।",
        }
    };
}