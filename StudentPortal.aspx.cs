using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SeniorProjectHub3
{
    public partial class StudentPortal : Page
    {
        private readonly string connString = ConfigurationManager.ConnectionStrings["MyDB"].ConnectionString;
        /// <summary>
        /// Returns (groupId, supervisorEmail) for the current student.
        /// </summary>
        /// 
        /// <summary>
        /// Returns the current student’s GroupID and the assigned supervisor’s email,
        /// by checking in order:
        ///   1) StudentGroups.SupervisorEmail
        ///   2) ProjectIdeas.AssignedSupervisorEmail  (student-submitted flow)
        ///   3) SupervisorProjects.AssignedStudent    (supervisor-posted flow)
        /// </summary>
        private (int groupId, string supervisorEmail) GetMyGroupAndSupervisor()
        {
            var studentEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(studentEmail))
                return (-1, null);

            int groupId = -1;
            string supervisor = null;

            using (var conn = new SqlConnection(connString))
            {
                conn.Open();

                // 1) find the GroupID
                using (var cmd = new SqlCommand(@"
            SELECT TOP 1 GroupID, SupervisorEmail
              FROM StudentGroups
             WHERE Student1Email = @E OR Student2Email = @E", conn))
                {
                    cmd.Parameters.AddWithValue("@E", studentEmail);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            groupId = Convert.ToInt32(rdr["GroupID"]);
                            supervisor = rdr["SupervisorEmail"] as string;
                        }
                    }
                }

                if (groupId < 0)
                    return (-1, null);

                // 2) If StudentGroups.SupervisorEmail was null/empty, check student-ideas flow:
                if (string.IsNullOrEmpty(supervisor))
                {
                    using (var cmd = new SqlCommand(@"
                SELECT TOP 1 AssignedSupervisorEmail
                  FROM ProjectIdeas
                 WHERE GroupID = @G 
                   AND AssignedSupervisorEmail IS NOT NULL
                 ORDER BY IdeaID DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@G", groupId);
                        supervisor = cmd.ExecuteScalar() as string;
                    }
                }

                // 3) If still none, check supervisor-projects flow:
                if (string.IsNullOrEmpty(supervisor))
                {
                    using (var cmd = new SqlCommand(@"
                SELECT TOP 1 SupervisorEmail
                  FROM SupervisorProjects
                 WHERE AssignedStudent IN (
                     SELECT Student1Email FROM StudentGroups WHERE GroupID = @G
                     UNION
                     SELECT Student2Email FROM StudentGroups WHERE GroupID = @G
                 )
                 ORDER BY ProjectID DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@G", groupId);
                        supervisor = cmd.ExecuteScalar() as string;

                        // also update StudentGroups.SupervisorEmail for future convenience
                        if (!string.IsNullOrEmpty(supervisor))
                        {
                            using (var upd = new SqlCommand(@"
                        UPDATE StudentGroups
                           SET SupervisorEmail = @Sup
                         WHERE GroupID = @G", conn))
                            {
                                upd.Parameters.AddWithValue("@Sup", supervisor);
                                upd.Parameters.AddWithValue("@G", groupId);
                                upd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }

            return (groupId, supervisor);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            int currentWeek = GetCurrentSemesterWeek();
            txtWeekNumber.Text = currentWeek.ToString();
            Session["CurrentWeekForStudent"] = currentWeek;

            // 2) *always* update the “ready/not yet” message and grid
            CheckIfReportIsRequired();
            LoadSubmittedWeeklyReports();
            if (!IsPostBack)
            {


                if (Session["UserEmail"] == null)
                {
                    Response.Redirect("Login.aspx");
                }
                else
                {
                    LoadStudentProfile();
                    LoadAssignedIdea();
                    LoadSubmissionStatus();
                    LoadProjectIdeas();
                    LoadSupervisorApplications();
                    LoadStudentApplications();
                    LoadGroupInfo();
                    LoadAppliedToGroupRequests();
                    LoadGroupRequests();
                    LoadAssignedWeeklyReports();
                    CheckIfReportIsRequired();





                    if (Session["CurrentWeekForStudent"] != null)
                    {
                        txtWeekNumber.Visible = true;
                        txtWeekNumber.Text = Session["CurrentWeekForStudent"].ToString();
                    }
                    else
                    {
                        txtWeekNumber.Visible = false;
                    }


                }


            }
        }




        private void SendErrorEmailToAdmin(Exception ex, string methodName)
        {
            try
            {
                string adminEmail = "admin@kau.edu.sa"; // Replace with actual admin email
                string subject = $"Error in {methodName}";
                string body = $@"
            <h3>Error Details</h3>
            <p>Method: {methodName}</p>
            <p>Error: {ex.Message}</p>
            <p>Stack Trace: {ex.StackTrace}</p>
            <p>Time: {DateTime.Now}</p>";

                MailMessage message = new MailMessage();
                message.From = new MailAddress("system@kau.edu.sa");
                message.To.Add(adminEmail);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using (SmtpClient smtp = new SmtpClient("mail.kau.edu.sa", 25))
                {
                    smtp.Send(message);
                }
            }
            catch
            {
                // Silent fail - we don't want error handling to cause more errors
            }
        }

        protected void gvSupervisorApplications_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int applicationId = Convert.ToInt32(e.CommandArgument);
            string studentEmail = Session["UserEmail"].ToString();
            string supervisorEmail = "";
            int ideaId = 0;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // 1) Look up the supervisorEmail & ideaId
                using (var cmd = new SqlCommand(
                    "SELECT SupervisorEmail, IdeaID FROM SupervisorApplications WHERE ApplicationID = @AppID", conn))
                {
                    cmd.Parameters.AddWithValue("@AppID", applicationId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            supervisorEmail = rdr.GetString(0);
                            ideaId = rdr.GetInt32(1);
                        }
                    }
                }

                if (e.CommandName == "Accept")
                {
                    // 2a) Assign them
                    using (var cmd = new SqlCommand(@"
                UPDATE ProjectIdeas 
                   SET AssignedSupervisorEmail = @SupervisorEmail 
                 WHERE IdeaID = @IdeaID;
                UPDATE SupervisorApplications 
                   SET Status = 'Accepted' 
                 WHERE ApplicationID = @AppID;", conn))
                    {
                        cmd.Parameters.AddWithValue("@SupervisorEmail", supervisorEmail);
                        cmd.Parameters.AddWithValue("@IdeaID", ideaId);
                        cmd.Parameters.AddWithValue("@AppID", applicationId);
                        cmd.ExecuteNonQuery();
                    }

                    // 3a) Notify supervisor
                    SendEmail(
                        toEmail: supervisorEmail,
                        subject: "✔️ Your application was ACCEPTED",
                        body: $"Hello,\n\nStudent {studentEmail} has accepted your application to supervise their idea (ID {ideaId}).\n\n– Senior Project Hub"
                    );
                }
                else if (e.CommandName == "Reject")
                {
                    // 2b) Remove their application
                    using (var cmd = new SqlCommand(
                        "DELETE FROM SupervisorApplications WHERE ApplicationID = @AppID", conn))
                    {
                        cmd.Parameters.AddWithValue("@AppID", applicationId);
                        cmd.ExecuteNonQuery();
                    }

                    // 3b) Notify supervisor
                    SendEmail(
                        toEmail: supervisorEmail,
                        subject: "❌ Your application was REJECTED",
                        body: $"Hello,\n\nUnfortunately, student {studentEmail} has rejected your application for idea ID {ideaId}.\n\n– Senior Project Hub"
                    );
                }
            }

            // 4) Refresh
            LoadSupervisorApplications();
        }
        private void LoadStudentProfile()
        {
            string studentEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(studentEmail))
            {
                Response.Redirect("Login.aspx");
                return;
            }

            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = "SELECT FullName, Email, PhoneNumber, Major, Interests FROM TP WHERE Email = @Email AND Role = 'Student'";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", studentEmail);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        lblName.Text = reader["FullName"].ToString();
                        lblEmail.Text = reader["Email"].ToString();
                        lblPhone.Text = reader["PhoneNumber"] != DBNull.Value ? reader["PhoneNumber"].ToString() : "Not Available";
                        lblMajor.Text = reader["Major"] != DBNull.Value ? reader["Major"].ToString() : "Not Available";
                        lblInterests.Text = reader["Interests"] != DBNull.Value ? reader["Interests"].ToString() : "Not Specified";

                    }
                    else
                    {
                        lblName.Text = "Unknown";
                        lblEmail.Text = studentEmail;
                        lblPhone.Text = "Not Available";
                        lblMajor.Text = "Not Assigned Yet";
                    }
                }
            }

            LoadGroupInfo(); // still call to show group status
        }



        // 1) Only enable the button if nobody in your group has yet submitted a non-empty summary
        private void CheckIfReportIsRequired()
        {
            string studentEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(studentEmail)) return;

            int currentWeek = GetCurrentSemesterWeek();
            txtWeekNumber.Text = currentWeek.ToString();

            // Find your group
            int groupId = -1;
            using (var conn = new SqlConnection(connString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
SELECT TOP 1 sg.GroupID
  FROM StudentGroups sg
 WHERE sg.Student1Email = @Email
    OR sg.Student2Email = @Email";
                cmd.Parameters.AddWithValue("@Email", studentEmail);
                var val = cmd.ExecuteScalar();
                if (val != null) groupId = (int)val;
            }

            if (groupId < 0)
            {
                lblWeekInfo.Text = "You are not in a group yet.";
                btnSubmitReport.Enabled = false;
                return;
            }

            // Has **any** member in that group submitted non-blank?
            int count;
            using (var conn = new SqlConnection(connString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
SELECT COUNT(*) 
  FROM WeeklyReports
 WHERE GroupID = @Grp
   AND [Week]  = @Week
   AND ReportSummary IS NOT NULL
   AND ReportSummary <> ''";
                cmd.Parameters.AddWithValue("@Grp", groupId);
                cmd.Parameters.AddWithValue("@Week", currentWeek);
                count = (int)cmd.ExecuteScalar();
            }

            if (count > 0)
            {
                lblWeekInfo.Text = $"Weekly report for week {currentWeek} has already been submitted.";
                btnSubmitReport.Enabled = false;
            }
            else
            {
                lblWeekInfo.Text = $"Weekly report for week {currentWeek} is ready for submission.";
                btnSubmitReport.Enabled = true;
            }
        }

        // 2) Show only the non-empty, group-level submissions
        private void LoadSubmittedWeeklyReports()
        {
            string studentEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(studentEmail)) return;

            // Get groupId
            int groupId = -1;
            using (var conn = new SqlConnection(connString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
SELECT TOP 1 sg.GroupID
  FROM StudentGroups sg
 WHERE sg.Student1Email = @Email
    OR sg.Student2Email = @Email";
                cmd.Parameters.AddWithValue("@Email", studentEmail);
                var val = cmd.ExecuteScalar();
                if (val != null) groupId = (int)val;
            }

            if (groupId < 0)
            {
                gvSubmittedReports.Visible = false;
                lblReportStatus.Text = "No reports (no group found).";
                return;
            }

            string query = @"
SELECT 
    [Week],
    ReportSummary,
    CONVERT(varchar, SubmittedAt, 120) AS SubmittedAt,
    ISNULL(SupervisorComment, 'Not reviewed yet') AS SupervisorComment,
    ISNULL(CAST(SupervisorGrade AS varchar), 'Not graded') AS SupervisorGrade,
    CASE 
      WHEN SupervisorSubmittedAt IS NULL THEN 'Pending'
      ELSE CONVERT(varchar, SupervisorSubmittedAt, 120)
    END AS SupervisorSubmittedAt
  FROM WeeklyReports
 WHERE GroupID       = @Grp
   AND ReportSummary <> ''
 ORDER BY [Week] DESC";

            var dt = new DataTable();
            using (var conn = new SqlConnection(connString))
            using (var da = new SqlDataAdapter(query, conn))
            {
                da.SelectCommand.Parameters.AddWithValue("@Grp", groupId);
                da.Fill(dt);
            }

            if (dt.Rows.Count == 0)
            {
                lblReportStatus.Text = "No submitted reports yet.";
                gvSubmittedReports.Visible = false;
            }
            else
            {
                gvSubmittedReports.Visible = true;
                gvSubmittedReports.DataSource = dt;
                gvSubmittedReports.DataBind();
            }
        }

        // 3) Submit/update exactly one placeholder per group/week
        protected void btnSubmitReport_Click(object sender, EventArgs e)
        {
            string studentEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(studentEmail))
            {
                lblReportStatus.Text = "Session expired. Please log in again.";
                return;
            }

            if (!int.TryParse(txtWeekNumber.Text, out int week))
            {
                lblReportStatus.Text = "Invalid week number.";
                return;
            }

            // Get groupId + supervisor
            int groupId = -1;
            string supervisorEmail = null;
            using (var conn = new SqlConnection(connString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
SELECT TOP 1 
       sg.GroupID,
       COALESCE(pi.AssignedSupervisorEmail, sg.SupervisorEmail) AS SupervisorEmail
  FROM StudentGroups sg
  LEFT JOIN ProjectIdeas pi ON pi.GroupID = sg.GroupID
 WHERE sg.Student1Email = @E
    OR sg.Student2Email = @E";
                cmd.Parameters.AddWithValue("@E", studentEmail);
                using (var r = cmd.ExecuteReader())
                    if (r.Read())
                    {
                        groupId = r.GetInt32(0);
                        supervisorEmail = r.IsDBNull(1) ? null : r.GetString(1);
                    }
            }

            if (groupId < 0 || string.IsNullOrEmpty(supervisorEmail))
            {
                lblReportStatus.Text = "You are not in a group or no supervisor assigned.";
                return;
            }

            string summary = txtReportSummary.Text.Trim();
            if (string.IsNullOrEmpty(summary))
            {
                lblReportStatus.Text = "Please enter your weekly summary.";
                return;
            }

            // Ensure nobody in the group has already submitted
            int already = 0;
            using (var conn = new SqlConnection(connString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
SELECT COUNT(*) 
  FROM WeeklyReports
 WHERE GroupID = @G
   AND [Week]  = @W
   AND ReportSummary <> ''";
                cmd.Parameters.AddWithValue("@G", groupId);
                cmd.Parameters.AddWithValue("@W", week);
                already = (int)cmd.ExecuteScalar();
            }
            if (already > 0)
            {
                lblReportStatus.Text = $"Week {week} has already been submitted.";
                return;
            }

            // Look for an existing blank placeholder
            int placeholderId = 0;
            using (var conn = new SqlConnection(connString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
SELECT TOP 1 ReportID
  FROM WeeklyReports
 WHERE GroupID       = @G
   AND [Week]        = @W
   AND (ReportSummary IS NULL OR ReportSummary = '')";
                cmd.Parameters.AddWithValue("@G", groupId);
                cmd.Parameters.AddWithValue("@W", week);
                var v = cmd.ExecuteScalar();
                if (v != null) placeholderId = (int)v;
            }

            using (var conn = new SqlConnection(connString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                if (placeholderId > 0)
                {
                    // update that blank
                    cmd.CommandText = @"
UPDATE WeeklyReports
   SET ReportSummary = @S,
       SubmittedAt   = GETDATE()
 WHERE ReportID = @ID";
                    cmd.Parameters.AddWithValue("@ID", placeholderId);
                }
                else
                {
                    // insert brand‐new
                    cmd.CommandText = @"
INSERT INTO WeeklyReports
  (StudentEmail, SupervisorEmail, [Week], ReportSummary, SubmittedAt, GroupID)
VALUES
  (@E, @Sup, @W, @S, GETDATE(), @G)";
                    cmd.Parameters.AddWithValue("@E", studentEmail);
                    cmd.Parameters.AddWithValue("@Sup", supervisorEmail);
                    cmd.Parameters.AddWithValue("@W", week);
                    cmd.Parameters.AddWithValue("@G", groupId);
                }
                cmd.Parameters.AddWithValue("@S", summary);

                if (cmd.ExecuteNonQuery() > 0)
                {
                    lblReportStatus.CssClass = "success";
                    lblReportStatus.Text = "Report submitted!";
                    txtReportSummary.Text = "";
                    LoadSubmittedWeeklyReports();
                    SendEmailNotification(supervisorEmail, studentEmail, week);
                }
                else
                {
                    lblReportStatus.Text = "Submission failed.";
                }
            }
        }


        private int GetCurrentSemesterWeek()
        {
            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand("SELECT MAX([Week]) FROM WeeklyReports", conn))
            {
                conn.Open();
                var result = cmd.ExecuteScalar();
                if (result != DBNull.Value && result != null)
                    return Convert.ToInt32(result);
                else
                    return 1; // fallback if no reports yet
            }
        }


        private void SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                string fromEmail = "seniorprojecthub@gmail.com";
                string password = "fjbs suqx ygkt uoaf";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromEmail, "Senior Project Hub");
                mail.To.Add(toEmail);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = false;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential(fromEmail, password);
                smtp.EnableSsl = true;

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Email error: " + ex.Message);
            }
        }





        private void SendEmailNotification(string supervisorEmail, string studentEmail, int week)
        {
            try
            {
                string subject = $"New Weekly Report - Week {week}";
                string body = $@"
            <h3>New Weekly Report Submission</h3>
            <p>Student: {studentEmail}</p>
            <p>Week: {week}</p>
            <p>Please review this report in the supervisor portal.</p>
            <p>Senior Project Hub System</p>";

                MailMessage message = new MailMessage();
                message.From = new MailAddress("noreply@kau.edu.sa");
                message.To.Add(supervisorEmail);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using (SmtpClient smtp = new SmtpClient("mail.kau.edu.sa", 25))
                {
                    smtp.Send(message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Email error: {ex.Message}");
            }
        }



        protected void btnFindGroup_Click(object sender, EventArgs e)
        {
            Response.Redirect("SearchGroups.aspx");
        }

        // Loads the assigned idea for the student.
        // If an idea with status 'Assigned' exists for this student, display its title; otherwise, show "No project yet".
        private void LoadAssignedIdea()
        {
            string email = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(email))
            {
                lblIdea.Text = "No Idea Yet";
                return;
            }

            int groupId = -1;
            string ideaTitle = null;

            using (var conn = new SqlConnection(connString))
            {
                conn.Open();

                // 1) Find your group
                using (var cmd = new SqlCommand(@"
            SELECT TOP 1 GroupID
              FROM StudentGroups
             WHERE Student1Email = @Email
                OR Student2Email = @Email", conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    var g = cmd.ExecuteScalar();
                    if (g != null && g != DBNull.Value)
                        groupId = (int)g;
                }

                if (groupId > 0)
                {
                    // 2) First look for any **assigned** idea (supervisor accepted)
                    using (var cmd = new SqlCommand(@"
                SELECT TOP 1 IdeaTitle
                  FROM ProjectIdeas
                 WHERE GroupID = @G
                   AND AssignedSupervisorEmail IS NOT NULL
                 ORDER BY IdeaID DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@G", groupId);
                        ideaTitle = cmd.ExecuteScalar() as string;
                    }

                    // 3) If none, fallback to any **approved** idea (coordinator approved)
                    if (string.IsNullOrEmpty(ideaTitle))
                    {
                        using (var cmd = new SqlCommand(@"
                    SELECT TOP 1 IdeaTitle
                      FROM ProjectIdeas
                     WHERE GroupID = @G
                       AND Status = 'Approved'
                     ORDER BY IdeaID DESC", conn))
                        {
                            cmd.Parameters.AddWithValue("@G", groupId);
                            ideaTitle = cmd.ExecuteScalar() as string;
                        }
                    }

                    // 4) If still none, check if your group applied to a supervisor-project and got accepted
                    if (string.IsNullOrEmpty(ideaTitle))
                    {
                        using (var cmd = new SqlCommand(@"
                    SELECT TOP 1 sp.ProjectTitle
                      FROM StudentApplications sa
                      JOIN SupervisorProjects sp
                        ON sa.SupervisorProjectID = sp.ProjectID
                     WHERE sa.Status = 'Accepted'
                       AND sa.StudentEmail IN (
                           SELECT Student1Email FROM StudentGroups WHERE GroupID = @G
                           UNION
                           SELECT Student2Email FROM StudentGroups WHERE GroupID = @G
                       )
                     ORDER BY sa.ApplicationID DESC", conn))
                        {
                            cmd.Parameters.AddWithValue("@G", groupId);
                            ideaTitle = cmd.ExecuteScalar() as string;
                        }
                    }
                }
            }

            lblIdea.Text = !string.IsNullOrEmpty(ideaTitle)
                ? ideaTitle
                : "No Idea Yet";
        }





        // Handles submission of a new idea (student's idea).
        protected void btnSubmitIdea_Click(object sender, EventArgs e)
        {
            // 1) Get the logged-in student’s email
            string studentEmail = Session["UserEmail"] as string;
            if (string.IsNullOrEmpty(studentEmail))
            {
                lblIdeaStatus.CssClass = "error";
                lblIdeaStatus.Text = "Session expired. Please log in again.";
                return;
            }

            int groupId;

            // 2) Find their GroupID in StudentGroups
            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(@"
        SELECT TOP 1 GroupID
          FROM StudentGroups
         WHERE Student1Email = @Email
            OR Student2Email = @Email", conn))
            {
                cmd.Parameters.AddWithValue("@Email", studentEmail);
                conn.Open();
                var o = cmd.ExecuteScalar();
                if (o == null || o == DBNull.Value)
                {
                    lblIdeaStatus.CssClass = "error";
                    lblIdeaStatus.Text = "You must be in a group before submitting an idea.";
                    return;
                }
                groupId = Convert.ToInt32(o);
            }

            // 3) Prevent duplicate submissions
            using (var conn = new SqlConnection(connString))
            {
                conn.Open();

                // A) Any non-rejected idea already in ProjectIdeas?
                using (var chk = new SqlCommand(@"
            SELECT COUNT(*) 
              FROM ProjectIdeas 
             WHERE GroupID = @G 
               AND Status <> 'Rejected'", conn))
                {
                    chk.Parameters.AddWithValue("@G", groupId);
                    if ((int)chk.ExecuteScalar() > 0)
                    {
                        lblIdeaStatus.CssClass = "error";
                        lblIdeaStatus.Text = "Your group already has an active idea.";
                        return;
                    }
                }

                // B) Any accepted supervisor request?
                using (var chk2 = new SqlCommand(@"
            SELECT COUNT(*) 
              FROM SupervisorRequests 
             WHERE StudentEmail = @Email 
               AND Status = 'Accepted'", conn))
                {
                    chk2.Parameters.AddWithValue("@Email", studentEmail);
                    if ((int)chk2.ExecuteScalar() > 0)
                    {
                        lblIdeaStatus.CssClass = "error";
                        lblIdeaStatus.Text = "You already have an assigned supervisor idea.";
                        return;
                    }
                }

                // 4) Insert the new idea (no CreatedAt column)
                using (var ins = new SqlCommand(@"
            INSERT INTO ProjectIdeas
              (GroupID, StudentEmail, IdeaTitle, IdeaDescription, IdeaType, Status)
            VALUES
              (@G, @Email, @Title, @Desc, @Type, 'Pending')", conn))
                {
                    ins.Parameters.AddWithValue("@G", groupId);
                    ins.Parameters.AddWithValue("@Email", studentEmail);
                    ins.Parameters.AddWithValue("@Title", txtIdeaTitle.Text.Trim());
                    ins.Parameters.AddWithValue("@Desc", txtIdeaDescription.Text.Trim());
                    ins.Parameters.AddWithValue("@Type", ddlIdeaType.SelectedValue);

                    ins.ExecuteNonQuery();
                }
            }
            SendCoordinatorNotificationEmail(
         ddlIdeaType.SelectedValue,
         txtIdeaTitle.Text.Trim(),
         txtIdeaDescription.Text.Trim(),
         studentEmail
     );
            // 5) Provide feedback
            lblIdeaStatus.CssClass = "success";
            lblIdeaStatus.Text = "Your idea has been submitted and is pending approval.";
        }

        private void NotifyStudentsOfNewIdea(string ideaType, string ideaTitle, string ideaDescription, string senderEmail)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
            SELECT FullName, Email
            FROM TP
            WHERE Role = 'Student' 
              AND Email <> @SenderEmail
              AND Interests LIKE @IdeaType";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SenderEmail", senderEmail);
                    cmd.Parameters.AddWithValue("@IdeaType", "%" + ideaType + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string studentName = reader["FullName"].ToString();
                            string studentEmail = reader["Email"].ToString();

                            SendStudentIdeaInterestEmail(studentEmail, studentName, ideaType, ideaTitle, ideaDescription);
                        }
                    }
                }
            }
        }



        private void SendStudentIdeaInterestEmail(string toEmail, string studentName, string ideaType, string ideaTitle, string ideaDescription)
        {
            try
            {
                string fromEmail = "seniorprojecthub@gmail.com";
                string password = "fjbs suqx ygkt uoaf";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromEmail, "Senior Project Hub");
                mail.To.Add(toEmail);
                mail.Subject = $"📢 New {ideaType} Project Idea from a Student!";
                mail.Body = $"Hello {studentName},\n\nA student just submitted a project idea in your area of interest: {ideaType}.\n\n" +
                            $"📌 Title: {ideaTitle}\n📝 Description: {ideaDescription}\n\nLog in to the portal to view or apply.\n\n– Senior Project Hub Team";
                mail.IsBodyHtml = false;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential(fromEmail, password);
                smtp.EnableSsl = true;

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Email error: " + ex.Message);
            }
        }

        // Update the supervisor request method in StudentPortal




        protected bool IsSupervisorRequestPending(string supervisorEmail)
        {
            string studentEmail = Session["UserEmail"]?.ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = "SELECT COUNT(*) FROM SupervisorRequests WHERE StudentEmail = @StudentEmail AND SupervisorEmail = @SupervisorEmail";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@StudentEmail", studentEmail);
                    cmd.Parameters.AddWithValue("@SupervisorEmail", supervisorEmail);
                    conn.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        protected string GetSupervisorButtonClass(string supervisorEmail)
        {
            return IsSupervisorRequestPending(supervisorEmail) ? "button pending-button" : "button";
        }

        // Loads the student's submitted ideas and their statuses.
        private void LoadSubmissionStatus()
        {
            string studentEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(studentEmail))
            {
                Response.Redirect("Login.aspx");
                return;
            }

            int groupId = -1;

            // Get student's group ID
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string groupQuery = "SELECT GroupID FROM StudentGroups WHERE Student1Email = @Email OR Student2Email = @Email";
                using (SqlCommand cmd = new SqlCommand(groupQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", studentEmail);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                        groupId = Convert.ToInt32(result);
                }
            }

            if (groupId == -1)
            {
                gvSubmissionStatus.DataSource = null;
                gvSubmissionStatus.DataBind();
                return;
            }

            // Load group's submitted ideas and their status
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
            SELECT IdeaTitle, IdeaDescription, Status, 
                   ISNULL(AssignedSupervisorEmail, 'Pending') AS AssignedSupervisor,
                   StudentEmail AS SubmittedBy
            FROM ProjectIdeas
            WHERE GroupID = @GroupID";

                using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@GroupID", groupId);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvSubmissionStatus.DataSource = dt;
                    gvSubmissionStatus.DataBind();
                }
            }
        }
        private void LoadProjectIdeas()
        {
            string studentEmail = Session["UserEmail"].ToString();
            string selectedType = ddlSupervisorIdeaType.SelectedValue;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
            SELECT ProjectID, SupervisorEmail, ProjectTitle, ProjectDescription, IdeaType, CreatedAt 
            FROM SupervisorProjects 
            WHERE ApprovedByCoordinator = 1
              AND NOT EXISTS (
                  SELECT 1 FROM StudentApplications 
                  WHERE SupervisorProjectID = SupervisorProjects.ProjectID 
                  AND StudentEmail = @StudentEmail
              )";

                if (!string.IsNullOrEmpty(selectedType))
                {
                    query += " AND IdeaType = @IdeaType";
                }

                using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@StudentEmail", studentEmail);
                    if (!string.IsNullOrEmpty(selectedType))
                    {
                        da.SelectCommand.Parameters.AddWithValue("@IdeaType", selectedType);
                    }

                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvProjectIdeas.DataSource = dt;
                    gvProjectIdeas.DataBind();
                }
            }
        }
        protected void ddlSupervisorIdeaType_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadProjectIdeas();
        }


        // Loads supervisor applications for the student's ideas.
        private void LoadSupervisorApplications()
        {
            string me = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(me)) return;

            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(@"
        SELECT 
            sa.ApplicationID,
            pi.IdeaTitle,
            sa.SupervisorEmail
        FROM SupervisorApplications sa
        INNER JOIN ProjectIdeas pi    ON sa.IdeaID    = pi.IdeaID
        INNER JOIN StudentGroups sg   ON pi.GroupID    = sg.GroupID
        WHERE sa.Status    = 'Pending'
          AND (sg.Student1Email = @Me OR sg.Student2Email = @Me)
    ", conn))
            {
                cmd.Parameters.AddWithValue("@Me", me);
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);

                gvSupervisorApplications.DataSource = dt;
                gvSupervisorApplications.DataBind();
            }
        }


        protected void gvSupervisorApplications_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                string status = DataBinder.Eval(e.Row.DataItem, "Status").ToString();
                var btnAccept = (Button)e.Row.FindControl("btnAccept");
                var btnReject = (Button)e.Row.FindControl("btnReject");
                if (status != "Pending")
                {
                    btnAccept.Enabled = btnReject.Enabled = false;
                }
            }
        }


        // Loads the student's applications to supervisor projects.
        private void LoadStudentApplications()
        {
            string studentEmail = Session["UserEmail"].ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
                    SELECT sa.ApplicationID, sp.ProjectTitle, sp.SupervisorEmail, sa.Status
                    FROM StudentApplications sa
                    JOIN SupervisorProjects sp ON sa.SupervisorProjectID = sp.ProjectID
                    WHERE sa.StudentEmail = @StudentEmail";
                using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@StudentEmail", studentEmail);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvStudentApplications.DataSource = dt;
                    gvStudentApplications.DataBind();
                }
            }
        }

        // Event handler for applying to a supervisor project idea.

        protected void btnApplyToSupervisor_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            int projectId = Convert.ToInt32(btn.CommandArgument);
            string studentEmail = Session["UserEmail"].ToString();
            string studentName = lblName.Text; // assuming lblName holds the student's name

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // Check if already applied.
                string checkQuery = "SELECT COUNT(*) FROM StudentApplications WHERE SupervisorProjectID = @ProjectID AND StudentEmail = @StudentEmail";
                using (SqlCommand cmd = new SqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ProjectID", projectId);
                    cmd.Parameters.AddWithValue("@StudentEmail", studentEmail);
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        return;
                    }
                }

                // Get supervisor email and project title
                string supervisorEmail = "", projectTitle = "";
                string getInfoQuery = "SELECT SupervisorEmail, ProjectTitle FROM SupervisorProjects WHERE ProjectID = @ProjectID";
                using (SqlCommand cmd = new SqlCommand(getInfoQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ProjectID", projectId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            supervisorEmail = reader["SupervisorEmail"].ToString();
                            projectTitle = reader["ProjectTitle"].ToString();
                        }
                    }
                }

                // Insert the new application
                string insertQuery = "INSERT INTO StudentApplications (SupervisorProjectID, StudentEmail, Status) VALUES (@ProjectID, @StudentEmail, 'Pending')";
                using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ProjectID", projectId);
                    cmd.Parameters.AddWithValue("@StudentEmail", studentEmail);
                    cmd.ExecuteNonQuery();
                }

                //  Send email to the supervisor
                SendStudentApplicationEmail(supervisorEmail, studentName, studentEmail, projectTitle);
            }

            LoadProjectIdeas();
            LoadStudentApplications();
        }

        private void SendStudentApplicationEmail(string toEmail, string studentName, string studentEmail, string projectTitle)
        {
            try
            {
                string fromEmail = "seniorprojecthub@gmail.com";
                string password = "fjbs suqx ygkt uoaf";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromEmail, "Senior Project Hub");
                mail.To.Add(toEmail);
                mail.Subject = "New Student Application for Your Project";
                mail.Body = $"Dear Supervisor,\n\nStudent {studentName} ({studentEmail}) has applied to your project titled \"{projectTitle}\".\n\nPlease review the application in the Supervisor Portal.\n\nBest regards,\nSenior Project Hub Team";
                mail.IsBodyHtml = false;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential(fromEmail, password);
                smtp.EnableSsl = true;

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Email Error: " + ex.Message);
            }
        }


        // Event handler for accepting a supervisor application (for student's ideas).
        protected void btnAccept_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            int applicationID = Convert.ToInt32(btn.CommandArgument);
            string supervisorEmail = "";
            int ideaID = 0;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // 1) Fetch the sup-email & ideaID
                using (var getCmd = new SqlCommand(
                    "SELECT SupervisorEmail, IdeaID FROM SupervisorApplications WHERE ApplicationID = @AppID", conn))
                {
                    getCmd.Parameters.AddWithValue("@AppID", applicationID);
                    using (var rdr = getCmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            supervisorEmail = rdr.GetString(0);
                            ideaID = rdr.GetInt32(1);
                        }
                    }
                }

                // 2) Assign them & reject the others
                string sql = @"
            UPDATE ProjectIdeas
               SET AssignedSupervisorEmail = @SupervisorEmail,
                   Status = 'Assigned'
             WHERE IdeaID = @IdeaID;

            UPDATE SupervisorApplications
               SET Status = CASE WHEN ApplicationID = @AppID THEN 'Accepted' ELSE 'Rejected' END
             WHERE IdeaID = @IdeaID;
        ";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@SupervisorEmail", supervisorEmail);
                    cmd.Parameters.AddWithValue("@IdeaID", ideaID);
                    cmd.Parameters.AddWithValue("@AppID", applicationID);
                    cmd.ExecuteNonQuery();
                }
            }

            // 3) Notify the newly-accepted supervisor
            SendEmail(
                toEmail: supervisorEmail,
                subject: "🎉 Your application was ACCEPTED",
                body: $"Hello,\n\nCongratulations! Your application for student-submitted idea ID {ideaID} has been accepted.\n\n– Senior Project Hub"
            );

            // 4) Refresh
            LoadSupervisorApplications();
            LoadSubmissionStatus();
            LoadAssignedIdea();
        }



        // Event handler for rejecting a supervisor application.
        protected void btnReject_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            string applicationID = btn.CommandArgument;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = "DELETE FROM SupervisorApplications WHERE ApplicationID = @ApplicationID";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ApplicationID", applicationID);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            LoadSupervisorApplications();
        }


        protected void btnCreateGroup_Click(object sender, EventArgs e)
        {
            string studentEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(studentEmail))
            {
                Response.Redirect("Login.aspx");
                return;
            }

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // Check if student is already in a group
                string checkQuery = "SELECT COUNT(*) FROM StudentGroups WHERE Student1Email = @Email OR Student2Email = @Email";
                using (SqlCommand cmd = new SqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", studentEmail);
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        lblGroupCode.Text = "You are already in a group.";
                        return;
                    }
                }

                // Generate a unique group code 
                string groupCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

                // Insert new group
                string insertQuery = "INSERT INTO StudentGroups (GroupCode, Student1Email, CreatedAt) VALUES (@GroupCode, @StudentEmail, GETDATE())";
                using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupCode", groupCode);
                    cmd.Parameters.AddWithValue("@StudentEmail", studentEmail);
                    cmd.ExecuteNonQuery();
                }

                lblGroupCode.Text = "Group created! Your code: " + groupCode;
            }

            LoadGroupInfo();
        }





        // Load group information
        private void LoadGroupInfo()
        {
            string studentEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(studentEmail)) return;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // Get group information
                string groupQuery = @"
            SELECT GroupID, GroupCode, Student1Email, Student2Email, SupervisorEmail
            FROM StudentGroups
            WHERE Student1Email = @Email OR Student2Email = @Email";

                int groupId = -1;
                string groupSupervisorEmail = null;

                using (SqlCommand cmd = new SqlCommand(groupQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", studentEmail);
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        groupId = reader["GroupID"] != DBNull.Value ? Convert.ToInt32(reader["GroupID"]) : -1;
                        groupSupervisorEmail = reader["SupervisorEmail"] != DBNull.Value ? reader["SupervisorEmail"].ToString() : null;

                        lblGroupCode.Text = reader["GroupCode"].ToString();

                        string teammateEmail = (reader["Student1Email"].ToString() == studentEmail)
                            ? reader["Student2Email"].ToString()
                            : reader["Student1Email"].ToString();

                        lblTeammate.Text = string.IsNullOrEmpty(teammateEmail) ? "Waiting for teammate..." : teammateEmail;
                    }
                    else
                    {
                        lblGroupCode.Text = "Not in a group";
                        lblTeammate.Text = "N/A";
                        lblSupervisor.Text = "Not Assigned Yet";
                        return;
                    }
                    reader.Close();

                    // If we have a supervisor from the group table, use it
                    if (!string.IsNullOrEmpty(groupSupervisorEmail))
                    {
                        lblSupervisor.Text = groupSupervisorEmail;
                    }
                    // Otherwise check in ProjectIdeas
                    else if (groupId > 0)
                    {
                        string ideaQuery = @"
                    SELECT AssignedSupervisorEmail
                    FROM ProjectIdeas
                    WHERE GroupID = @GroupID AND AssignedSupervisorEmail IS NOT NULL";

                        using (SqlCommand ideaCmd = new SqlCommand(ideaQuery, conn))
                        {
                            ideaCmd.Parameters.AddWithValue("@GroupID", groupId);
                            object result = ideaCmd.ExecuteScalar();

                            if (result != null && result != DBNull.Value)
                            {
                                lblSupervisor.Text = result.ToString();

                                // If we found supervisor in ProjectIdeas but not in StudentGroups,
                                // let's update StudentGroups for consistency
                                if (string.IsNullOrEmpty(groupSupervisorEmail))
                                {
                                    string updateQuery = @"
                                UPDATE StudentGroups
                                SET SupervisorEmail = @SupervisorEmail
                                WHERE GroupID = @GroupID";

                                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                                    {
                                        updateCmd.Parameters.AddWithValue("@SupervisorEmail", result.ToString());
                                        updateCmd.Parameters.AddWithValue("@GroupID", groupId);
                                        updateCmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            else
                            {
                                lblSupervisor.Text = "Not Assigned Yet";
                            }
                        }
                    }
                    else
                    {
                        lblSupervisor.Text = "Not Assigned Yet";
                    }
                }
            }
        }






        private void SendCoordinatorNotificationEmail(string ideaType, string title, string description, string submittedBy)
        {
            try
            {
                string fromEmail = "seniorprojecthub@gmail.com";
                string password = "fjbs suqx ygkt uoaf";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromEmail, "Senior Project Hub");

                //  Add both coordinators here
                mail.To.Add("fsallah@kau.edu.sa");
                mail.To.Add("hlabani@kau.edu.sa");

                mail.Subject = $"📬 New Student Idea Submitted in {ideaType}";
                mail.Body = $"Dear Coordinator,\n\nA new project idea has been submitted by a student.\n\n" +
                            $"📧 Submitted by: {submittedBy}\n📌 Title: {title}\n📝 Description: {description}\n\n" +
                            $"You can review and approve it from the Coordinator Portal.\n\n– Senior Project Hub";
                mail.IsBodyHtml = false;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential(fromEmail, password);
                smtp.EnableSsl = true;

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Coordinator email error: " + ex.Message);
            }
        }

        private void LoadGroupRequests()
        {
            string studentEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(studentEmail)) return;

            string query = @"
        SELECT RequestID, ApplicantEmail, Status
        FROM GroupApplications 
        WHERE RequestedStudentEmail = @StudentEmail AND Status = 'Pending'";

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



        private void LoadAppliedToGroupRequests()
        {
            string studentEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(studentEmail)) return;

            string query = @"
SELECT ga.RequestID,
       a.FullName AS StudentFullName,
       ga.RequestedStudentEmail AS StudentEmail,
       COALESCE(pi.IdeaTitle, 'No Idea') AS IdeaTitle,
       ga.Status
FROM GroupApplications ga
JOIN AssignedStudents a ON ga.RequestedStudentEmail = a.StudentEmail
LEFT JOIN ProjectIdeas pi ON ga.RequestedStudentEmail = pi.StudentEmail
WHERE ga.ApplicantEmail = @StudentEmail";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@StudentEmail", studentEmail);
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvAppliedToGroupRequests.DataSource = dt;
                    gvAppliedToGroupRequests.DataBind();
                }
            }
        }



        protected void btnAcceptRequest_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            int requestID = Convert.ToInt32(btn.CommandArgument);
            string currentStudentEmail = Session["UserEmail"]?.ToString();

            if (string.IsNullOrEmpty(currentStudentEmail))
            {
                lblStatus.Text = "Session expired. Please log in again.";
                return;
            }

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // Get ApplicantEmail from GroupApplications
                string applicantEmailQuery = "SELECT ApplicantEmail FROM GroupApplications WHERE RequestID = @RequestID";
                string applicantEmail;

                using (SqlCommand cmd = new SqlCommand(applicantEmailQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@RequestID", requestID);
                    var result = cmd.ExecuteScalar();
                    if (result == null)
                    {
                        lblStatus.Text = "Applicant not found.";
                        return;
                    }
                    applicantEmail = result.ToString();
                }

                // Check if either student is already in a group
                string checkGroupQuery = @"
            SELECT COUNT(*) FROM StudentGroups
            WHERE (Student1Email IN (@ApplicantEmail, @RequestedEmail) OR Student2Email IN (@ApplicantEmail, @RequestedEmail))";

                using (SqlCommand cmdCheck = new SqlCommand(checkGroupQuery, conn))
                {
                    cmdCheck.Parameters.AddWithValue("@ApplicantEmail", applicantEmail);
                    cmdCheck.Parameters.AddWithValue("@RequestedEmail", currentStudentEmail);
                    int count = (int)cmdCheck.ExecuteScalar();
                    if (count > 0)
                    {
                        lblStatus.Text = "One of the students is already in a group.";
                        return;
                    }
                }

                // Create new group with unique GroupCode
                string groupCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

                string createGroupQuery = @"
            INSERT INTO StudentGroups (GroupCode, Student1Email, Student2Email, CreatedAt)
            VALUES (@GroupCode, @ApplicantEmail, @RequestedEmail, GETDATE())";

                using (SqlCommand cmdGroup = new SqlCommand(createGroupQuery, conn))
                {
                    cmdGroup.Parameters.AddWithValue("@GroupCode", groupCode);
                    cmdGroup.Parameters.AddWithValue("@ApplicantEmail", applicantEmail);
                    cmdGroup.Parameters.AddWithValue("@RequestedEmail", currentStudentEmail);
                    cmdGroup.ExecuteNonQuery();
                }

                // Update GroupApplications status to Accepted
                string updateApplicationQuery = @"
            UPDATE GroupApplications SET Status = 'Accepted' WHERE RequestID = @RequestID";

                using (SqlCommand cmdUpdate = new SqlCommand(updateApplicationQuery, conn))
                {
                    cmdUpdate.Parameters.AddWithValue("@RequestID", requestID);
                    cmdUpdate.ExecuteNonQuery();
                }

                lblStatus.Text = "Group created successfully!";
                SendEmail(applicantEmail, "👥 Group Request Accepted", $"Your request was accepted. You are now grouped with {currentStudentEmail}.\n\n– Senior Project Hub");
                SendEmail(currentStudentEmail, "👥 Group Formed Successfully", $"You are now grouped with {applicantEmail}.\n\n– Senior Project Hub");

            }

            LoadGroupRequests();           // Refresh pending requests
            LoadGroupInfo();               // Update group status on profile
            LoadAppliedToGroupRequests();  // Refresh student's applications
        }


        protected void btnRejectRequest_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            int requestID = Convert.ToInt32(btn.CommandArgument);

            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = "UPDATE GroupApplications SET Status = 'Rejected' WHERE RequestID = @RequestID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RequestID", requestID);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            lblStatus.Text = "Group request rejected.";

            LoadGroupRequests();           // Refresh pending requests
            LoadAppliedToGroupRequests();  // Refresh applications
        }



        protected void gvAppliedToGroupRequests_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                string status = DataBinder.Eval(e.Row.DataItem, "Status").ToString();
                if (status == "Accepted")
                {
                    e.Row.BackColor = System.Drawing.Color.LightGreen;
                }
                else if (status == "Rejected")
                {
                    e.Row.BackColor = System.Drawing.Color.LightCoral;
                }
            }
        }

        protected void btnAcceptGroupRequest_Click(object sender, EventArgs e)
        {
            Button btnAccept = (Button)sender;
            int requestID = Convert.ToInt32(btnAccept.CommandArgument);
            string currentStudentEmail = Session["UserEmail"]?.ToString();

            if (string.IsNullOrEmpty(currentStudentEmail)) return;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // 1. Get ApplicantEmail from the request
                string getApplicantQuery = @"
            SELECT ApplicantEmail
            FROM GroupApplications
            WHERE RequestID = @RequestID";
                string applicantEmail = "";

                using (SqlCommand cmd = new SqlCommand(getApplicantQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@RequestID", requestID);
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                        applicantEmail = result.ToString();
                    else
                        return; // Safety check
                }

                // 2. Check if either student already has a group
                string checkGroupQuery = @"
            SELECT COUNT(*)
            FROM StudentGroups
            WHERE Student1Email IN (@ApplicantEmail, @RequestedEmail) OR 
                  Student2Email IN (@ApplicantEmail, @RequestedEmail)";

                using (SqlCommand cmd = new SqlCommand(checkGroupQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ApplicantEmail", applicantEmail);
                    cmd.Parameters.AddWithValue("@RequestedEmail", currentStudentEmail);
                    int groupCount = (int)cmd.ExecuteScalar();

                    if (groupCount > 0)
                    {
                        lblStatus.Text = "One of the students is already in a group!";
                        return;
                    }
                }

                // 3. Create a unique GroupCode
                string groupCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();

                // 4. Insert the students into the StudentGroups table
                string createGroupQuery = @"
            INSERT INTO StudentGroups (GroupCode, Student1Email, Student2Email, CreatedAt)
            VALUES (@GroupCode, @Student1, @Student2, GETDATE())";

                using (SqlCommand cmd = new SqlCommand(createGroupQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupCode", groupCode);
                    cmd.Parameters.AddWithValue("@Student1", applicantEmail);
                    cmd.Parameters.AddWithValue("@Student2", currentStudentEmail);
                    cmd.ExecuteNonQuery();
                }

                // 5. Update request status to 'Accepted'
                string updateStatusQuery = @"
            UPDATE GroupApplications
            SET Status = 'Accepted'
            WHERE RequestID = @RequestID";

                using (SqlCommand cmd = new SqlCommand(updateStatusQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@RequestID", requestID);
                    cmd.ExecuteNonQuery();
                }

                lblStatus.Text = "Group formed successfully!";
            }

            // 6. Refresh your GridView and group info
            LoadGroupRequests();
            LoadAppliedToGroupRequests();
            LoadGroupInfo(); // Important to reload student's group status
        }

        protected void btnRejectGroupRequest_Click(object sender, EventArgs e)
        {
            Button btnReject = (Button)sender;
            int requestID = Convert.ToInt32(btnReject.CommandArgument);

            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = "UPDATE GroupApplications SET Status = 'Rejected' WHERE RequestID = @RequestID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RequestID", requestID);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            lblStatus.Text = "Group request rejected.";

            LoadGroupRequests();
            LoadAppliedToGroupRequests();
        }

        private void LoadAssignedWeeklyReports()
        {
            string studentEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(studentEmail)) return;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                string query = @"
            SELECT DISTINCT Week
            FROM WeeklyReports
            WHERE GroupID = (
                SELECT GroupID FROM TP WHERE Email = @Email
            )";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", studentEmail);
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        txtWeekNumber.Text = result.ToString();
                    }
                }
            }
        }


        protected void gvGroupRequests_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                string status = DataBinder.Eval(e.Row.DataItem, "Status").ToString();
                Button btnAccept = (Button)e.Row.FindControl("btnAcceptRequest");
                Button btnReject = (Button)e.Row.FindControl("btnRejectRequest");

                if (status == "Accepted" || status == "Rejected")
                {
                    btnAccept.Enabled = false;
                    btnReject.Enabled = false;
                    btnAccept.CssClass = "button disabled";  // Gray out the button
                    btnReject.CssClass = "button disabled";  // Gray out the button
                }
            }
        }
        private void SendIdeaSubmissionEmail(string toEmail, string fullName, string ideaTitle)
        {
            try
            {
                string gmailEmail = "seniorprojecthub@gmail.com";
                string gmailPassword = "fjbs suqx ygkt uoaf";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(gmailEmail, "Senior Project Hub");
                mail.To.Add(toEmail);
                mail.Subject = "New Project Idea Submitted";
                mail.Body = $"Dear {fullName},\n\nYour idea \"{ideaTitle}\" was submitted successfully.\n\nRegards,\nSenior Project Hub Team";
                mail.IsBodyHtml = false;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential(gmailEmail, gmailPassword);
                smtp.EnableSsl = true;

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                //  log the error or display message
                Response.Write("Email Error: " + ex.Message);
            }
        }






        public string FormatDescription(string input, int lineLength)
        {
            if (string.IsNullOrEmpty(input)) return "";
            for (int i = lineLength; i < input.Length; i += lineLength + 1)
            {
                input = input.Insert(i, "<br/>");
            }
            return input;
        }




        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("Login.aspx");
        }
    }
}