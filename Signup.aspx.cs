using System;
using System.Configuration;
using System.Data.SqlClient;
using BCrypt.Net;
using System.Net;
using System.Net.Mail;
using System.Linq;
using System.Web.UI.WebControls;
using System.Text.RegularExpressions;

namespace SeniorProjectHub3
{
    public partial class Signup : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        private void SendConfirmationEmail(string toEmail, string fullName)
        {
            Response.Write("📩 Starting email send...<br/>");

            try
            {
                string gmailEmail = "seniorprojecthub@gmail.com";
                string gmailPassword = "fjbs suqx ygkt uoaf";

                Response.Write("✅ Credentials loaded.<br/>");

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(gmailEmail, "Senior Project Hub");
                mail.To.Add(toEmail);
                mail.Subject = "Welcome to Senior Project Hub!";
                mail.Body = $"Dear {fullName},\n\nThank you for signing up! You're now ready to use the Senior Project Hub.\n\nBest regards,\nFCIT KAU Team";
                mail.IsBodyHtml = false;

                Response.Write("📦 Mail message created.<br/>");

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential(gmailEmail, gmailPassword);
                smtp.EnableSsl = true;

                Response.Write("🔐 SMTP configured. Attempting to send...<br/>");

                smtp.Send(mail);

                Response.Write("✅ Email sent successfully.<br/>");
            }
            catch (SmtpException smtpEx)
            {
                Response.Write("❌ SMTP Error: " + smtpEx.ToString() + "<br/>");
            }
            catch (Exception ex)
            {
                Response.Write("❌ General Error: " + ex.ToString() + "<br/>");
            }
        }
        protected void btnSignup_Click(object sender, EventArgs e)
        {
            string fullName = txtFullName.Text.Trim();
            string email = txtEmail.Text.Trim().ToLower();
            string phoneInput = txtPhone.Text.Trim();    // just the 9 digits, e.g. "5xxxxxxxx"
            string major = ddlMajor.SelectedValue;
            string interests = ddlInterests.SelectedValue;
            string password = txtPassword.Text;
            string confirmPass = txtConfirmPassword.Text;

            // 1) Passwords match?
            if (password != confirmPass)
            {
                lblMessage.CssClass = "error";
                lblMessage.Text = "Passwords do not match.";
                return;
            }

            // 2) Phone format: exactly 9 digits, starting with 5
            if (!Regex.IsMatch(phoneInput, @"^5\d{8}$"))
            {
                lblMessage.CssClass = "error";
                lblMessage.Text = "Please enter your phone as 9 digits starting with 5";
                return;
            }
            // build the full stored phone number
            string phoneNumber = "+966" + phoneInput;

            // 3) Email format: KAU only hlabani@kau.edu.sa
            bool isCoord = email.Equals("fsallah@kau.edu.sa", StringComparison.OrdinalIgnoreCase)
                        || email.Equals("hhlabani@kau.edu.sa", StringComparison.OrdinalIgnoreCase);
            if (!(isCoord
               || email.EndsWith("@stu.kau.edu.sa")
               || email.EndsWith("@kau.edu.sa")))
            {
                lblMessage.CssClass = "error";
                lblMessage.Text = "Invalid email format. Use a KAU email.";
                return;
            }

            string connString = ConfigurationManager.ConnectionStrings["MyDB"].ConnectionString;

            // 4) Email uniqueness
            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM TP WHERE Email = @Email", conn))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                conn.Open();
                if ((int)cmd.ExecuteScalar() > 0)
                {
                    lblMessage.CssClass = "error";
                    lblMessage.Text = "There is already an account with this email.";
                    return;
                }
            }

            // 5) Determine role, and if Student, verify pre-assignment
            string role = "";
            bool isStudent = false;
            if (email.Equals("hod@kau.edu.sa", StringComparison.OrdinalIgnoreCase))
            {
                role = "HOD";
            }
            else if (isCoord)
            {
                role = "Coordinator";
            }
            else if (email.EndsWith("@stu.kau.edu.sa"))
            {
                role = "Student";
                isStudent = true;
            }
            else // Default for other kau.edu.sa emails
            {
                role = "Supervisor";
            }


            if (isStudent)
            {
                using (var conn = new SqlConnection(connString))
                using (var chk = new SqlCommand(
                    "SELECT COUNT(*) FROM AssignedStudents WHERE StudentEmail = @Email", conn))
                {
                    chk.Parameters.AddWithValue("@Email", email);
                    conn.Open();
                    if ((int)chk.ExecuteScalar() == 0)
                    {
                        lblMessage.CssClass = "error";
                        lblMessage.Text = "Email not recognized. Please contact the coordinator.";
                        return;
                    }
                }
            }

            // 6) All good: hash password & insert
            string hash = BCrypt.Net.BCrypt.HashPassword(password);
            using (var conn = new SqlConnection(connString))
            using (var ins = new SqlCommand(@"
        INSERT INTO TP
         (FullName, Email, PhoneNumber, Major, Interests, PasswordHash, Role, CreatedAt)
        VALUES
         (@FullName, @Email, @Phone, @Major, @Interests, @Hash, @Role, GETDATE())", conn))
            {
                ins.Parameters.AddWithValue("@FullName", fullName);
                ins.Parameters.AddWithValue("@Email", email);
                ins.Parameters.AddWithValue("@Phone", phoneNumber);
                ins.Parameters.AddWithValue("@Major", string.IsNullOrEmpty(major) ? DBNull.Value : (object)major);
                ins.Parameters.AddWithValue("@Interests", string.IsNullOrEmpty(interests) ? DBNull.Value : (object)interests);
                ins.Parameters.AddWithValue("@Hash", hash);
                ins.Parameters.AddWithValue("@Role", role);

                conn.Open();
                if (ins.ExecuteNonQuery() > 0)
                {
                    SendConfirmationEmail(email, fullName);
                    lblMessage.CssClass = "success";
                    lblMessage.Text = "Sign-up successful! Redirecting to login…";
                    Response.Redirect("Login.aspx");
                }
                else
                {
                    lblMessage.CssClass = "error";
                    lblMessage.Text = "Sign-up failed. Please try again.";
                }
            }
        }


    }
}