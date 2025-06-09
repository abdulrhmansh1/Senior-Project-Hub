using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net.Mail;
using System.Net;


namespace SeniorProjectHub3
{
    public partial class SearchGroups : Page
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
                    LoadAvailableStudents();
                    LoadListedIdeas();
                    LoadMyGroupApplications();
                    LoadGroupRequests();
                }
            }

        }

        protected void btnAcceptGroupRequest_Click(object sender, EventArgs e)
        {
            Button btnAccept = (Button)sender;
            GridViewRow row = (GridViewRow)btnAccept.NamingContainer;
            int requestID = Convert.ToInt32(gvGroupRequests.DataKeys[row.RowIndex].Value);

            string query = "UPDATE GroupApplications SET Status = 'Accepted' WHERE RequestID = @RequestID";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RequestID", requestID);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            lblStatus.Text = "Group request accepted!";
            LoadGroupRequests(); // Refreshes the list
            LoadMyGroupApplications();
        }


        protected void btnRejectGroupRequest_Click(object sender, EventArgs e)
        {
            Button btnReject = (Button)sender;
            GridViewRow row = (GridViewRow)btnReject.NamingContainer;
            int requestID = Convert.ToInt32(gvGroupRequests.DataKeys[row.RowIndex].Value);

            string query = "UPDATE GroupApplications SET Status = 'Rejected' WHERE RequestID = @RequestID";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RequestID", requestID);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            lblStatus.Text = "Group request rejected!";
            LoadGroupRequests(); // Refreshes the list
            LoadMyGroupApplications();
        }
        protected void gvListedIdeas_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                string studentEmail = Session["UserEmail"]?.ToString();
                string ideaOwnerEmail = DataBinder.Eval(e.Row.DataItem, "StudentEmail")?.ToString();


                string status = DataBinder.Eval(e.Row.DataItem, "Status")?.ToString();

                Button btnRequest = (Button)e.Row.FindControl("btnRequest");

                if (btnRequest != null)
                {
                    if (status == "Pending")
                    {
                        btnRequest.Text = "Pending...";
                        btnRequest.Enabled = false;
                        btnRequest.Attributes["class"] = "button pending-button";  // Ensures class is applied
                    }
                    else if (ideaOwnerEmail == studentEmail)
                    {
                        btnRequest.Visible = false;  // Hide the button if the logged-in user owns the idea
                    }
                }
            }
        }


        private void LoadGroupRequests()
        {
            string studentEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(studentEmail)) return;

            string query = @"
        SELECT RequestID, ApplicantEmail, Status
        FROM GroupApplications 
        WHERE RequestedStudentEmail = @StudentEmail";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@StudentEmail", studentEmail);
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvGroupRequests.DataSource = dt;
                    gvGroupRequests.DataBind();
                }
            }
        }

        private void LoadAvailableStudents()
        {
            string currentEmail = Session["UserEmail"]?.ToString();

            string studentEmail = Session["UserEmail"]?.ToString(); // to exclude self

            string query = @"
SELECT 
    a.FullName AS StudentFullName,
    a.StudentEmail,
    COALESCE(tp.Interests, 'Not Specified') AS Interests
FROM AssignedStudents a
LEFT JOIN TP tp ON a.StudentEmail = tp.Email
WHERE a.StudentEmail <> @CurrentStudentEmail
  AND a.StudentEmail NOT IN (
      SELECT Student1Email FROM StudentGroups
      UNION
      SELECT Student2Email FROM StudentGroups
  )";



            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CurrentStudentEmail", studentEmail);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvAvailableStudents.DataSource = dt;
                    gvAvailableStudents.DataBind();
                }
            }

        }

        private int CreateGroupAndReturnId(string studentEmail1, string studentEmail2)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["MyDB"].ConnectionString))
            {
                conn.Open();

                string groupCode = "GRP-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                string query = @"
            INSERT INTO StudentGroups (GroupCode, Student1Email, Student2Email)
            VALUES (@GroupCode, @Student1Email, @Student2Email);
            SELECT SCOPE_IDENTITY();";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupCode", groupCode);
                    cmd.Parameters.AddWithValue("@Student1Email", studentEmail1);
                    cmd.Parameters.AddWithValue("@Student2Email", studentEmail2);

                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : -1;
                }
            }
        }


        protected void btnApproveRequest_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            GridViewRow row = (GridViewRow)btn.NamingContainer;
            string toStudentEmail = ((Label)row.FindControl("lblToEmail")).Text;
            string fromStudentEmail = Session["UserEmail"].ToString();

            int groupId = CreateGroupAndReturnId(fromStudentEmail, toStudentEmail);

            if (groupId > 0)
            {
                AssignGroupToTPStudents(groupId, fromStudentEmail, toStudentEmail);
                InitializeGrades(groupId);
                lblStatus.Text = "Group created and assigned successfully.";
            }
            else
            {
                lblStatus.Text = "Group creation failed.";
            }
        }

        private void InitializeGrades(int groupId)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                string getStudentIdsQuery = "SELECT UserID FROM TP WHERE GroupID = @GroupID";

                using (SqlCommand cmd = new SqlCommand(getStudentIdsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupID", groupId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        List<int> studentIds = new List<int>();

                        while (reader.Read())
                        {
                            studentIds.Add(Convert.ToInt32(reader["UserID"]));
                        }

                        reader.Close(); // Close reader before reuse of connection
                        foreach (int studentId in studentIds)
                        {
                            string insertQuery = @"
                        IF NOT EXISTS (
                            SELECT 1 FROM StudentGrades WHERE StudentID = @StudentID AND GroupID = @GroupID
                        )
                        BEGIN
                            INSERT INTO StudentGrades 
                            (GroupID, StudentID, SupervisorEvaluation, ExaminationCommittee, CoordinationCommittee, FinalDecision, SOAttainment)
                            VALUES 
                            (@GroupID, @StudentID, '', '', '', '', '')
                        END";

                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@GroupID", groupId);
                                insertCmd.Parameters.AddWithValue("@StudentID", studentId);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }



        protected void btnSearch_Click(object sender, EventArgs e)
        {
            string searchQuery = txtSearch.Text.Trim();
            string selectedInterest = ddlInterestFilter.SelectedValue;
            string studentEmail = Session["UserEmail"]?.ToString();

            string query = @"
    SELECT 
        a.FullName AS StudentFullName,
        a.StudentEmail,
        COALESCE(tp.Interests, 'Not Specified') AS Interests
    FROM AssignedStudents a
    LEFT JOIN TP tp ON a.StudentEmail = tp.Email
    WHERE a.StudentEmail <> @CurrentEmail
      AND a.StudentEmail NOT IN (
          SELECT Student1Email FROM StudentGroups
          UNION
          SELECT Student2Email FROM StudentGroups
      )
      AND a.FullName LIKE @Search";

            if (!string.IsNullOrEmpty(selectedInterest))
            {
                query += " AND tp.Interests LIKE @Interest";
            }

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Search", "%" + searchQuery + "%");
                    cmd.Parameters.AddWithValue("@CurrentEmail", studentEmail);

                    if (!string.IsNullOrEmpty(selectedInterest))
                    {
                        cmd.Parameters.AddWithValue("@Interest", "%" + selectedInterest + "%");
                    }

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvAvailableStudents.DataSource = dt;
                    gvAvailableStudents.DataBind();
                }
            }
        }







        private void LoadListedIdeas()
        {
            string query = @"
    SELECT gr.RequestID, 
           a.FullName AS StudentFullName, 
           gr.StudentEmail, 
           COALESCE(pi.IdeaTitle, 'No Idea Yet') AS IdeaTitle, 
           gr.Status 
    FROM GroupRequests gr
    JOIN AssignedStudents a ON gr.StudentEmail = a.StudentEmail
    LEFT JOIN ProjectIdeas pi ON gr.IdeaID = pi.IdeaID";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvListedIdeas.DataSource = dt;
                    gvListedIdeas.DataBind();
                }
            }
        }



        private void LoadApprovedIdeas()
        {
            string studentEmail = Session["UserEmail"]?.ToString();
            string query = @"
                SELECT IdeaID, IdeaTitle, IdeaDescription 
                FROM ProjectIdeas 
                WHERE StudentEmail = @StudentEmail 
                AND Status = 'Approved'
                AND IdeaID NOT IN (SELECT IdeaID FROM GroupRequests WHERE StudentEmail = @StudentEmail)";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@StudentEmail", studentEmail);
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvApprovedIdeas.DataSource = dt;
                    gvApprovedIdeas.DataBind();
                }
            }
        }

        protected void btnRequest_Click(object sender, EventArgs e)
        {
            Button btnRequest = (Button)sender;
            GridViewRow row = (GridViewRow)btnRequest.NamingContainer;
            int requestID = Convert.ToInt32(gvListedIdeas.DataKeys[row.RowIndex].Value);

            string studentEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(studentEmail)) return;

            //  Update status to "Pending"
            string query = "UPDATE GroupRequests SET Status = 'Pending' WHERE RequestID = @RequestID";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RequestID", requestID);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            //  Refresh GridView to reflect the changes
            LoadListedIdeas();
        }

        protected void btnBackToPortal_Click(object sender, EventArgs e)
        {
            Response.Redirect("StudentPortal.aspx");
        }


        protected void btnApply_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            string requestedEmail = btn.CommandArgument;
            string applicantEmail = Session["UserEmail"]?.ToString();

            if (requestedEmail == applicantEmail)
            {
                lblStatus.Text = "You cannot apply to yourself!";
                return;
            }

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // Check if already applied
                string checkQuery = @"SELECT COUNT(*) FROM GroupApplications 
                              WHERE ApplicantEmail = @ApplicantEmail AND RequestedStudentEmail = @RequestedStudentEmail";

                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@ApplicantEmail", applicantEmail);
                    checkCmd.Parameters.AddWithValue("@RequestedStudentEmail", requestedEmail);
                    int exists = (int)checkCmd.ExecuteScalar();

                    if (exists > 0)
                    {
                        lblStatus.Text = "You have already applied to this student.";
                        return;
                    }
                }

                // Insert request
                string insertQuery = @"INSERT INTO GroupApplications (ApplicantEmail, RequestedStudentEmail, Status)
                               VALUES (@ApplicantEmail, @RequestedStudentEmail, 'Pending')";

                using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@ApplicantEmail", applicantEmail);
                    insertCmd.Parameters.AddWithValue("@RequestedStudentEmail", requestedEmail);
                    insertCmd.ExecuteNonQuery();
                    SendGroupApplicationEmail(requestedEmail, applicantEmail);
                }
            }

            lblStatus.CssClass = "success";
            lblStatus.Text = "Request sent successfully!";
            LoadMyGroupApplications(); // Refresh list
        }
        private void SendGroupApplicationEmail(string toEmail, string fromEmail)
        {
            try
            {
                string fromAddress = "seniorprojecthub@gmail.com";
                string appPassword = "fjbs suqx ygkt uoaf"; // Gmail App Password

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromAddress, "Senior Project Hub");
                mail.To.Add(toEmail);
                mail.Subject = "New Group Join Request";
                mail.Body = $"Dear Student,\n\n{fromEmail} has applied to join your group.\nPlease log in to your portal to accept or reject the request.\n\nBest regards,\nSenior Project Hub Team";
                mail.IsBodyHtml = false;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential(fromAddress, appPassword);
                smtp.EnableSsl = true;

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Email error: " + ex.Message);
            }
        }



        protected void btnRequestGroup_Click(object sender, EventArgs e)
        {
            if (Session["UserEmail"] == null)
            {
                Response.Redirect("Login.aspx");
                return;
            }

            string currentStudentEmail = Session["UserEmail"].ToString();
            Button btnRequestGroup = (Button)sender;
            GridViewRow row = (GridViewRow)btnRequestGroup.NamingContainer;

            // Prevent error if no rows exist in the GridView
            if (gvAvailableStudents.DataKeys.Count == 0 || row.RowIndex >= gvAvailableStudents.DataKeys.Count)
            {
                lblStatus.Text = "Error: No students available to request.";
                return;
            }

            string requestedStudentEmail = gvAvailableStudents.DataKeys[row.RowIndex]?.Value.ToString();

            if (string.IsNullOrEmpty(requestedStudentEmail))
            {
                lblStatus.Text = "Error: No student selected.";
                return;
            }

            string query = @"
    INSERT INTO GroupRequests (StudentEmail, IdeaID, Status) 
    VALUES (@ApplicantEmail, NULL, 'Pending')";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ApplicantEmail", currentStudentEmail);
                    cmd.Parameters.AddWithValue("@RequestedStudentEmail", requestedStudentEmail);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            lblStatus.Text = "Group request submitted successfully!";
        }

        private void AssignGroupToTPStudents(int groupId, string email1, string email2)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // First update
                using (SqlCommand cmd1 = new SqlCommand("UPDATE TP SET GroupID = @GroupID WHERE Email = @Email", conn))
                {
                    cmd1.Parameters.AddWithValue("@GroupID", groupId);
                    cmd1.Parameters.AddWithValue("@Email", email1);
                    cmd1.ExecuteNonQuery();
                }

                // Second update
                using (SqlCommand cmd2 = new SqlCommand("UPDATE TP SET GroupID = @GroupID WHERE Email = @Email", conn))
                {
                    cmd2.Parameters.AddWithValue("@GroupID", groupId);
                    cmd2.Parameters.AddWithValue("@Email", email2);
                    cmd2.ExecuteNonQuery();
                }
            }
        }





        private void LoadMyGroupApplications()
        {
            string email = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(email)) return;

            string query = @"SELECT RequestedStudentEmail, Status FROM GroupApplications WHERE ApplicantEmail = @Email";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvMyApplications.DataSource = dt;
                    gvMyApplications.DataBind();
                }
            }
        }




    }
}