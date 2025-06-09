using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SeniorProjectHub3
{
    public partial class AssignedStudents : Page
    {
        private readonly string connString = ConfigurationManager.ConnectionStrings["MyDB"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["UserEmail"] == null)
                {
                    Response.Redirect("Login.aspx");
                }
                else
                {
                    LoadAssignedStudents(); // ✅ Load assigned students on page load
                }
            }
        }

        protected void btnAssignStudent_Click(object sender, EventArgs e)
        {
            string fullName = txtStudentName.Text.Trim();
            string studentEmail = txtStudentEmail.Text.Trim();

            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(studentEmail))
            {
                lblAssignStatus.Text = "Please enter both name and email.";
                return;
            }

            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
                INSERT INTO AssignedStudents (FullName, StudentEmail, ProjectStatus)
                VALUES (@FullName, @StudentEmail, 'No Idea Yet')";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FullName", fullName);
                    cmd.Parameters.AddWithValue("@StudentEmail", studentEmail);

                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        lblAssignStatus.CssClass = "success";
                        lblAssignStatus.Text = "Student assigned successfully!";
                    }
                    catch (SqlException ex)
                    {
                        lblAssignStatus.CssClass = "error";
                        lblAssignStatus.Text = "Error: " + ex.Message;
                    }
                }
            }

            // Refresh the assigned students list
            LoadAssignedStudents();
        }

        private void LoadAssignedStudents()
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = "SELECT FullName, StudentEmail, ProjectStatus FROM AssignedStudents";

                using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvAssignedStudents.DataSource = dt;
                    gvAssignedStudents.DataBind();
                }
            }
        }
    }
}