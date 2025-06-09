using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI;
using BCrypt.Net;

namespace SeniorProjectHub3
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Clears existing sessions 
            Session.Clear();
            lblSignInError.Text = "";
        }

        protected void btnSignIn_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim().ToLower();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Please enter both email and password.";
                return;
            }

            string connString = ConfigurationManager.ConnectionStrings["MyDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                // Retrieve password hash and role from TP.
                string query = "SELECT PasswordHash, Role FROM TP WHERE LOWER(Email) = @Email";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        string storedHash = reader["PasswordHash"].ToString();
                        string role = reader["Role"].ToString();

                        if (BCrypt.Net.BCrypt.Verify(password, storedHash))
                        {

                            Session["UserEmail"] = email;



                            if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
                                Response.Redirect("StudentPortal.aspx");
                            else if (role.Equals("Supervisor", StringComparison.OrdinalIgnoreCase))
                                Response.Redirect("SupervisorPortal.aspx");
                            else if (role.Equals("Coordinator", StringComparison.OrdinalIgnoreCase))
                                Response.Redirect("CoordinatorPortal.aspx");
                            else if (role.Equals("HOD", StringComparison.OrdinalIgnoreCase))
                                Response.Redirect("HODPortal.aspx");
                            else
                                lblError.Text = "Unknown role.";

                        }
                        else
                        {
                            lblSignInError.Text = "Invalid email or password.";
                        }
                    }
                    else
                    {
                        lblSignInError.Text = "User not found.";
                    }
                }
            }
        }
    }
}