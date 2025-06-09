using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;

namespace SeniorProjectHub3
{
    public partial class CoordinatorPortal : System.Web.UI.Page
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
                    int dbWeek = 0;
                    using (SqlConnection conn = new SqlConnection(connString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX([Week]), 0) FROM WeeklyReports", conn))
                        {
                            dbWeek = (int)cmd.ExecuteScalar();
                        }
                    }

                    int currentWeek = GetCurrentSemesterWeek();
                    int effectiveWeek = Math.Max(currentWeek, dbWeek + 1);

                    bool reportsExistForWeek = false;
                    using (var conn = new SqlConnection(connString))
                    {
                        conn.Open();
                        string checkQuery = "SELECT COUNT(*) FROM WeeklyReports WHERE [Week] = @Week";
                        using (var cmd = new SqlCommand(checkQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@Week", effectiveWeek);
                            reportsExistForWeek = (int)cmd.ExecuteScalar() > 0;
                        }
                    }

                    if (!reportsExistForWeek)
                    {
                        SendWeeklyReportsJob();  // automatic!
                    }

                    lblCurrentWeek.Text = "Send Weekly Report Number " + effectiveWeek;

                    // other loads
                    LoadGroups();
                    LoadStudentStatus();
                    LoadAssignedStudents();
                    LoadAllGroupsAndIdeas();
                    SyncGroupIDsToTP();
                    LoadSupervisorIdeasPendingApproval();
                    LoadThisWeekReports();
                    LoadPreviousWeekReports();
                    LoadApprovedSupervisorProjects();
                    LoadSampleGroupFunctionality();
                }
            }

            LoadAllGroupGrades();
        }



        protected void gvSupervisorIdeas_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            // grab the ProjectID from the CommandArgument
            int projectId = Convert.ToInt32(e.CommandArgument);

            using (var conn = new SqlConnection(connString))
            {
                conn.Open();

                if (e.CommandName == "Approve")
                {
                    // 1) mark it approved in the DB
                    using (var cmd = new SqlCommand(
                        "UPDATE SupervisorProjects SET ApprovedByCoordinator = 1 WHERE ProjectID = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", projectId);
                        cmd.ExecuteNonQuery();
                    }

                    // 2) look up the supervisor’s email & title
                    string supEmail = null, projectTitle = null;
                    using (var cmd = new SqlCommand(
                        "SELECT SupervisorEmail, ProjectTitle FROM SupervisorProjects WHERE ProjectID = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", projectId);
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                supEmail = rdr.GetString(0);
                                projectTitle = rdr.GetString(1);
                            }
                        }
                    }

                    // 3) send them a notification
                    if (!string.IsNullOrEmpty(supEmail))
                    {
                        SendEmail(
                          toEmail: supEmail,
                          subject: "Your Supervisor Project Idea Was Approved",
                          body: $@"Dear Supervisor,

Your project idea ""{projectTitle}"" has just been approved by the coordinator.

Please log in to your portal to view or manage it.

— Senior Project Hub");
                    }
                }
                else if (e.CommandName == "Reject")
                {
                    // delete or mark rejected
                    using (var cmd = new SqlCommand(
                        "DELETE FROM SupervisorProjects WHERE ProjectID = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", projectId);
                        cmd.ExecuteNonQuery();
                    }

                    // (Optionally email the supervisor here, if you want to notify of rejection.)
                }
            }

            // 4) refresh your pending-approval grid
            LoadSupervisorIdeasPendingApproval();
        }


        private void LoadAllGroupGrades()
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
            SELECT DISTINCT
                sg.GroupID,              -- ← include GroupID
                tp.FullName,
                tp.Email,
                sg.MaxGrade,
                sg.SOS,
                sg.CLO,
                sg.SubmissionType,
                sg.WeekNumber,
                sg.SupervisorEvaluation,
                sg.ExaminationCommittee,
                sg.CoordinationCommittee,
                sg.FinalDecision,
                sg.SOAttainment
            FROM StudentGrades sg
            INNER JOIN TP tp
              ON sg.StudentID = tp.UserID
            ORDER BY sg.GroupID, tp.FullName";

                DataTable dt = new DataTable();
                new SqlDataAdapter(query, conn).Fill(dt);

                gvAllGroupGrades.DataSource = dt;
                gvAllGroupGrades.DataBind();
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


        private void NotifyStudentIdeaRejected(string toEmail, string ideaTitle)
        {
            string subject = "❌ Project Idea Rejected";
            string body = $"Dear Student,\n\nYour idea titled \"{ideaTitle}\" has been rejected by the coordinator. You may revise and submit a new one.\n\n– Senior Project Hub";
            SendEmail(toEmail, subject, body);
        }

        private void InsertAssignedStudent(string fullName, string email)
        {
            string connStr = ConfigurationManager.ConnectionStrings["MyDB"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                string query = "INSERT INTO AssignedStudents (FullName, StudentEmail) VALUES (@Name, @Email)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", fullName);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.ExecuteNonQuery();
            }
        }

        protected void btnSendWeeklyReport_Click(object sender, EventArgs e)
        {
            int currentWeek = GetCurrentSemesterWeek();

            // 1) Insert one blank WeeklyReports row per student
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                string getGroupsQuery = @"
       SELECT sg.GroupID, sg.SupervisorEmail
       FROM StudentGroups sg
       WHERE sg.SupervisorEmail IS NOT NULL";

                using (SqlCommand cmd = new SqlCommand(getGroupsQuery, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int groupId = Convert.ToInt32(reader["GroupID"]);
                        string supEmail = reader["SupervisorEmail"].ToString();
                        InsertReportForGroup(groupId, supEmail, null, currentWeek);
                    }
                }
            }

            // 2) Send out all the student emails
            SendWeeklyReportNotificationToStudents(currentWeek);

            // 3) Update your labels
            lblCurrentWeek.Text = "Send Weekly Report Number " + (currentWeek + 1);
            lblSendStatus.Text = $"Report request for Week {currentWeek} has been sent!";

            // ─────────────────────────────────────────────────────────────────
            // 4) **NEW**: Refresh the two repeaters right away
            LoadThisWeekReports();
            LoadPreviousWeekReports();
        }

        protected void btnUploadCSV_Click(object sender, EventArgs e)
        {
            if (csvFileUpload.HasFile)
            {
                string fileExt = System.IO.Path.GetExtension(csvFileUpload.FileName).ToLower();
                if (fileExt != ".csv")
                {
                    lblUploadStatus.Text = "Only CSV files are allowed.";
                    lblUploadStatus.ForeColor = System.Drawing.Color.Red;
                    return;
                }

                try
                {
                    StreamReader sr = new StreamReader(csvFileUpload.FileContent);

                    bool skipHeader = true;
                    int insertedCount = 0;
                    int failedCount = 0;

                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (skipHeader) { skipHeader = false; continue; }

                        string[] parts = line.Split(',');

                        if (parts.Length >= 2)
                        {
                            string fullName = parts[0].Trim();
                            string email = parts[1].Trim();

                            try
                            {
                                InsertAssignedStudent(fullName, email);
                                insertedCount++;

                                // ✉️ Send email notification to student
                                SendEmail(
                                    toEmail: email,
                                    subject: "You have been added to the Senior Project Hub",
                                    body: $"Dear {fullName},\n\nYou have been successfully added to the Senior Project Hub system. Please log in to complete your profile and participate in your group project.\n\nThank you,\nSenior Project Hub Team"
                                );
                            }
                            catch (Exception exRow)
                            {
                                failedCount++;
                                System.Diagnostics.Debug.WriteLine("Failed: " + email + " → " + exRow.Message);
                            }
                        }
                    }

                    lblUploadStatus.Text = $"Upload complete: {insertedCount} inserted, {failedCount} failed.";
                    lblUploadStatus.ForeColor = System.Drawing.Color.Green;

                    LoadAssignedStudents(); // Refresh grid
                }
                catch (Exception ex)
                {
                    lblUploadStatus.Text = "Error: " + ex.Message;
                    lblUploadStatus.ForeColor = System.Drawing.Color.Red;
                }
            }
        }



        private void LoadGroups()
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
            SELECT DISTINCT 
                GroupID,
                GroupCode
            FROM StudentGroups
            ORDER BY GroupCode";
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                ddlGroups.DataSource = dt;
                ddlGroups.DataTextField = "GroupCode";
                ddlGroups.DataValueField = "GroupID";
                ddlGroups.DataBind();

                ddlGroups.Items.Insert(0, new ListItem("-- Select a Group --", ""));
            }
        }


        private void LoadSampleGroupFunctionality()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("GroupCode");
            dt.Columns.Add("IdeaSubmitted");
            dt.Columns.Add("IdeaApproved");
            dt.Columns.Add("SupervisorAssigned");
            dt.Columns.Add("CPIS498Status");
            dt.Columns.Add("CPIS499Status");

            // Sample realistic data
            dt.Rows.Add("2E584A", "Yes", "Yes", "Yes", "Completed", "Completed");
            dt.Rows.Add("3B129C", "Yes", "Yes", "Yes", "Incomplete", "");
            dt.Rows.Add("4C7F21", "Yes", "No", "Yes", "", "");
            dt.Rows.Add("7A9B8E", "No", "No", "No", "", "");
            dt.Rows.Add("6D42FE", "Yes", "Yes", "Yes", "Completed", "Completed");

            gvGroupFunctionality.DataSource = dt;
            gvGroupFunctionality.DataBind();
        }




        private void LoadAllGroupsAndIdeas()
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
        SELECT 
            pi.IdeaID,
            ISNULL(sg.GroupCode, 'Individual Project') AS GroupCode,
            pi.StudentEmail AS Student1Email,
            ISNULL(sg.Student1Email, pi.StudentEmail) AS GroupMember1,
            ISNULL(sg.Student2Email, 'N/A') AS GroupMember2,
            pi.IdeaTitle,
            pi.IdeaDescription,
            pi.Status
        FROM ProjectIdeas pi
        LEFT JOIN StudentGroups sg ON pi.GroupID = sg.GroupID
        WHERE pi.Status = 'Pending'";

                using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvAllGroupsIdeas.DataSource = dt;
                    Label1.Text = "Rows loaded: " + dt.Rows.Count.ToString();

                    gvAllGroupsIdeas.DataBind();
                    gvAllGroupsIdeas.Visible = true;
                }
            }
        }
        private int GetProjectIdForGroup(int groupId)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                string query = "SELECT ProjectID FROM StudentGroups WHERE GroupID = @GroupID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupID", groupId);
                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
                    else
                    {
                        return 0; // No ProjectID found
                    }
                }
            }
        }


        private void InsertReportForGroup(int groupId, string supervisorEmail, string supervisorName, int week)
        {
            const string sqlFetch = @"
        SELECT Email 
        FROM TP 
        WHERE Role='Student' AND GroupID=@G";
            const string sqlInsert = @"
        IF NOT EXISTS (
          SELECT 1 FROM WeeklyReports 
           WHERE StudentEmail=@StudentEmail AND [Week]=@Week
        )
        BEGIN
          INSERT INTO WeeklyReports
            (StudentEmail, SupervisorEmail, GroupID, [Week], ReportSummary)
          VALUES 
            (@StudentEmail, @SupervisorEmail, @GroupID, @Week, '');
        END";

            using (var conn = new SqlConnection(connString))
            using (var fetchCmd = new SqlCommand(sqlFetch, conn))
            {
                conn.Open();
                fetchCmd.Parameters.AddWithValue("@G", groupId);
                var emails = new List<string>();
                using (var rdr = fetchCmd.ExecuteReader())
                    while (rdr.Read())
                        emails.Add(rdr.GetString(0));

                foreach (var studentEmail in emails)
                {
                    using (var insertCmd = new SqlCommand(sqlInsert, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@StudentEmail", studentEmail);
                        insertCmd.Parameters.AddWithValue("@SupervisorEmail", supervisorEmail);
                        insertCmd.Parameters.AddWithValue("@GroupID", groupId);
                        insertCmd.Parameters.AddWithValue("@Week", week);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }



        private void SendWeeklyReportNotificationToStudents(int weekNumber)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                string query = "SELECT FullName, Email FROM TP WHERE Role = 'Student'";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader["FullName"].ToString();
                        string email = reader["Email"].ToString();

                        try
                        {
                            MailMessage mail = new MailMessage();
                            mail.From = new MailAddress("seniorprojecthub@gmail.com", "Senior Project Hub");
                            mail.To.Add(email);
                            mail.Subject = $"Weekly Report #{weekNumber} Submission Request";
                            mail.Body = $"Dear {name},\n\nThe coordinator has initiated the Weekly Report #{weekNumber}. Please submit your report as soon as possible.\n\nThank you,\nSenior Project Hub";
                            mail.IsBodyHtml = false;

                            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                            smtp.Credentials = new NetworkCredential("seniorprojecthub@gmail.com", "fjbs suqx ygkt uoaf");
                            smtp.EnableSsl = true;
                            smtp.Send(mail);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Email Error: " + ex.Message);
                        }
                    }
                }
            }
        }


        private void IncrementWeekInDB()
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                string query = "UPDATE ReportWeekTracker SET CurrentWeek = CurrentWeek + 1";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        protected void btnApproveCoordinator_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            string reportId = btn.CommandArgument;

            RepeaterItem item = (RepeaterItem)btn.NamingContainer;
            TextBox txtGrade = (TextBox)item.FindControl("txtCoordinatorGrade");
            if (!decimal.TryParse(txtGrade.Text, out decimal finalGrade))
            {
                lblCoordinatorGradeError.Text = "Only allowed in decimal";
                return;
            }
            lblCoordinatorGradeError.Text = "";

            using (var conn = new SqlConnection(connString))
            {
                conn.Open();
                // only update if it wasn’t already graded
                string check = "SELECT FinalDecision FROM WeeklyReports WHERE ReportID=@ReportID AND FinalDecision IS NULL";
                using (var cmd = new SqlCommand(check, conn))
                {
                    cmd.Parameters.AddWithValue("@ReportID", reportId);
                    if (cmd.ExecuteScalar() == null) return;
                }
                string upd = "UPDATE WeeklyReports SET FinalDecision=@Grade WHERE ReportID=@ReportID";
                using (var cmd = new SqlCommand(upd, conn))
                {
                    cmd.Parameters.AddWithValue("@Grade", finalGrade);
                    cmd.Parameters.AddWithValue("@ReportID", reportId);
                    cmd.ExecuteNonQuery();
                }

            }


            // **here** re-bind your repeaters so the new grade shows and the input panel hides
            LoadThisWeekReports();
            LoadPreviousWeekReports();

            // optionally give a little feedback
            lblSendStatus.CssClass = "success";
            lblSendStatus.Text = $"Coordinator grade {finalGrade} saved.";
        }





        private void LoadSupervisorIdeasPendingApproval()
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
            SELECT ProjectID, SupervisorEmail, ProjectTitle, ProjectDescription, CreatedAt 
            FROM SupervisorProjects
            WHERE ApprovedByCoordinator = 0";

                using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvSupervisorIdeas.DataSource = dt;
                    gvSupervisorIdeas.DataBind();
                }
            }
        }





        protected void ddlGroups_SelectedIndexChanged(object sender, EventArgs e)
        {
            string groupId = ddlGroups.SelectedValue;
            if (string.IsNullOrEmpty(groupId)) return;

            EnsureGradeRowsExist(groupId);

            using (var conn = new SqlConnection(connString))
            {
                conn.Open();

                // 1) Build a CTE that ranks each student's grades by WeekNumber (desc)
                string sql = @"
WITH LatestGrade AS (
  SELECT 
    sg.GradeID,
    sg.StudentID,
    sg.SupervisorEvaluation,
    sg.ExaminationCommittee,
    sg.CoordinationCommittee,
    sg.FinalDecision,
    sg.SOAttainment,
    sg.MaxGrade,
    sg.SOS,
    sg.CLO,
    sg.SubmissionType,
    sg.WeekNumber,
    ROW_NUMBER() OVER (
      PARTITION BY sg.StudentID 
      ORDER BY sg.WeekNumber DESC
    ) AS rn
  FROM StudentGrades sg
  WHERE sg.GroupID = @GroupID
)

-- 2) Select only the top-ranked (rn = 1) row per student
SELECT
  lg.GradeID,
  tp.FullName,
  tp.Email,
  lg.SupervisorEvaluation,
  lg.ExaminationCommittee,
  lg.CoordinationCommittee,
  lg.FinalDecision,
  lg.SOAttainment,
  lg.MaxGrade,
  lg.SOS,
  lg.CLO,
  lg.SubmissionType,
  lg.WeekNumber
FROM LatestGrade lg
INNER JOIN TP tp ON lg.StudentID = tp.UserID
WHERE lg.rn = 1
ORDER BY tp.FullName;
";

                using (var da = new SqlDataAdapter(sql, conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@GroupID", groupId);
                    var dt = new DataTable();
                    da.Fill(dt);

                    gvStudentGrades.DataSource = dt;
                    gvStudentGrades.DataBind();
                    gvStudentGrades.Visible = true;
                }
            }
        }




        private void SyncGroupIDsToTP()
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                string query = "SELECT GroupID, Student1Email, Student2Email FROM StudentGroups";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    var groupUpdates = new List<(int GroupID, string Email)>();

                    while (reader.Read())
                    {
                        int groupId = Convert.ToInt32(reader["GroupID"]);
                        string student1 = reader["Student1Email"].ToString();
                        string student2 = reader["Student2Email"].ToString();

                        groupUpdates.Add((groupId, student1));
                        groupUpdates.Add((groupId, student2));
                    }

                    reader.Close();

                    foreach (var update in groupUpdates)
                    {
                        using (SqlCommand updateCmd = new SqlCommand("UPDATE TP SET GroupID = @GroupID WHERE Email = @Email", conn))
                        {
                            updateCmd.Parameters.AddWithValue("@GroupID", update.GroupID);
                            updateCmd.Parameters.AddWithValue("@Email", update.Email);
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private void UpdateTPGroupID(SqlConnection conn, string email, int groupId)
        {
            if (string.IsNullOrEmpty(email)) return;

            string updateQuery = "UPDATE TP SET GroupID = @GroupID WHERE Email = @Email";

            using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
            {
                cmd.Parameters.AddWithValue("@GroupID", groupId);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.ExecuteNonQuery();
            }
        }


        protected void btnJoinGroup_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            string targetStudentEmail = btn.CommandArgument;
            string currentStudentEmail = Session["UserEmail"].ToString();

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // Get GroupID of target student
                string getGroupIdQuery = "SELECT GroupID FROM TP WHERE Email = @TargetEmail";
                SqlCommand getGroupCmd = new SqlCommand(getGroupIdQuery, conn);
                getGroupCmd.Parameters.AddWithValue("@TargetEmail", targetStudentEmail);
                object result = getGroupCmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    int targetGroupId = Convert.ToInt32(result);

                    // Assign current student to the same group
                    string assignQuery = "UPDATE TP SET GroupID = @GroupID WHERE Email = @CurrentEmail";
                    SqlCommand assignCmd = new SqlCommand(assignQuery, conn);
                    assignCmd.Parameters.AddWithValue("@GroupID", targetGroupId);
                    assignCmd.Parameters.AddWithValue("@CurrentEmail", currentStudentEmail);
                    assignCmd.ExecuteNonQuery();

                    UpdateGroupStatus(currentStudentEmail);
                }
            }

            Response.Redirect(Request.RawUrl); // Reload the page
        }



        private void InsertInitialGradesForGroup(int groupId)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // Get student IDs from TP table where GroupID matches
                string getStudentsQuery = "SELECT UserID FROM TP WHERE GroupID = @GroupID AND Role = 'Student'";

                using (SqlCommand cmd = new SqlCommand(getStudentsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupID", groupId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int studentId = Convert.ToInt32(reader["UserID"]);

                            // Check if grade record already exists
                            string checkQuery = "SELECT COUNT(*) FROM StudentGrades WHERE StudentID = @StudentID AND GroupID = @GroupID";
                            using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                            {
                                checkCmd.Parameters.AddWithValue("@StudentID", studentId);
                                checkCmd.Parameters.AddWithValue("@GroupID", groupId);

                                int count = (int)checkCmd.ExecuteScalar();

                                if (count == 0)
                                {
                                    // Insert empty grade row
                                    string insertQuery = @"
                                INSERT INTO StudentGrades 
                                (GroupID, StudentID, SupervisorEvaluation, ExaminationCommittee, CoordinationCommittee, FinalDecision, SOAttainment)
                                VALUES 
                                (@GroupID, @StudentID, '', '', '', '', '')";

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
            }
        }


        private void UpdateGroupStatus(string studentEmail)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                string query = "UPDATE AssignedStudents SET GroupStatus = 'Grouped' WHERE StudentEmail = @Email";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", studentEmail);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void UpdateProjectStatus(string studentEmail)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                string query = "UPDATE AssignedStudents SET ProjectStatus = 'Submitted' WHERE StudentEmail = @Email";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", studentEmail);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void SendIdeaDecisionEmailToGroup(int ideaID, string decision)
        {
            string connStr = ConfigurationManager.ConnectionStrings["MyDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                string query = @"
            SELECT pi.IdeaTitle, pi.GroupID, tp.FullName, tp.Email
            FROM ProjectIdeas pi
            JOIN TP tp ON tp.GroupID = pi.GroupID
            WHERE pi.IdeaID = @IdeaID AND tp.Role = 'Student'";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdeaID", ideaID);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            string ideaTitle = reader["IdeaTitle"].ToString();
                            string fullName = reader["FullName"].ToString();
                            string email = reader["Email"].ToString();
                            UpdateProjectStatus(email);

                            try
                            {
                                MailMessage mail = new MailMessage();
                                mail.From = new MailAddress("seniorprojecthub@gmail.com", "Senior Project Hub");
                                mail.To.Add(email);
                                mail.Subject = $"Your Group Project Idea Was {decision}";
                                mail.Body = $"Dear {fullName},\n\nYour group idea titled \"{ideaTitle}\" has been {decision.ToLower()} by the coordinator.\n\nBest regards,\nSenior Project Hub Team";
                                mail.IsBodyHtml = false;

                                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                                smtp.Credentials = new NetworkCredential("seniorprojecthub@gmail.com", "fjbs suqx ygkt uoaf");
                                smtp.EnableSsl = true;

                                smtp.Send(mail);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("Email Error: " + ex.Message);
                            }
                        }
                    }
                }
            }
        }



        private void EnsureGradeRowsExist(string groupId)
        {
            int parsedGroupId = Convert.ToInt32(groupId);
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // Step 1: Get SupervisorEmail from StudentGroups
                string supervisorEmailQuery = "SELECT SupervisorEmail FROM StudentGroups WHERE GroupID = @GroupID";
                string supervisorEmail = "";

                using (SqlCommand supCmd = new SqlCommand(supervisorEmailQuery, conn))
                {
                    supCmd.Parameters.AddWithValue("@GroupID", parsedGroupId);
                    object result = supCmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        supervisorEmail = result.ToString();
                    }
                }

                // Step 2: Get SupervisorID (int) from TP table using SupervisorEmail
                int supervisorId = 0;
                if (!string.IsNullOrEmpty(supervisorEmail))
                {
                    string supervisorIdQuery = "SELECT UserID FROM TP WHERE Email = @Email AND Role = 'Supervisor'";
                    using (SqlCommand idCmd = new SqlCommand(supervisorIdQuery, conn))
                    {
                        idCmd.Parameters.AddWithValue("@Email", supervisorEmail);
                        object result = idCmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            supervisorId = Convert.ToInt32(result);
                        }
                    }
                }

                if (supervisorId == 0)
                {
                    Console.WriteLine("No SupervisorID found for GroupID: " + parsedGroupId);
                    return; // stop if no supervisor
                }

                // Step 3: Get Student IDs in group
                string getStudentsQuery = "SELECT UserID FROM TP WHERE GroupID = @GroupID AND Role = 'Student'";
                List<int> studentIds = new List<int>();

                using (SqlCommand cmd = new SqlCommand(getStudentsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@GroupID", parsedGroupId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            studentIds.Add(Convert.ToInt32(reader["UserID"]));
                        }
                    }
                }

                // Step 4: Insert grade rows if missing
                foreach (int studentId in studentIds)
                {
                    string checkQuery = "SELECT COUNT(*) FROM StudentGrades WHERE StudentID = @StudentID AND GroupID = @GroupID";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@StudentID", studentId);
                        checkCmd.Parameters.AddWithValue("@GroupID", parsedGroupId);

                        int count = (int)checkCmd.ExecuteScalar();

                        if (count == 0)
                        {
                            string insertQuery = @"
                    INSERT INTO StudentGrades 
                    (GroupID, StudentID, SupervisorEvaluation, ExaminationCommittee, CoordinationCommittee, FinalDecision, SOAttainment, MaxGrade, SOS, CLO, SubmissionType, WeekNumber, SupervisorID)
                    VALUES 
                    (@GroupID, @StudentID, 0, 0, 0, 0, 0, 0, 0, '', '', 0, @SupervisorID)";

                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@GroupID", parsedGroupId);
                                insertCmd.Parameters.AddWithValue("@StudentID", studentId);
                                insertCmd.Parameters.AddWithValue("@SupervisorID", supervisorId);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }





        private void LoadApprovedSupervisorProjects()
        {
            const string sql = @"
        SELECT
            sp.ProjectID,
            sp.SupervisorEmail,
            sp.ProjectTitle,
            sp.ProjectDescription,
            COALESCE(sg.GroupCode, 'No Group Yet') AS GroupCode
        FROM SupervisorProjects sp
        LEFT JOIN StudentGroups sg
            ON sg.Student1Email = sp.AssignedStudent
            OR sg.Student2Email = sp.AssignedStudent
        WHERE sp.ApprovedByCoordinator = 1
        ORDER BY sp.ProjectID DESC
    ";

            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                gvApprovedSupervisorProjects.DataSource = dt;
                gvApprovedSupervisorProjects.DataBind();
            }
        }


        private void LoadThisWeekReports()
        {
            int currentWeek = GetCurrentSemesterWeek();
            const string sql = @"
      SELECT 
        ReportID,
        StudentEmail,
        SupervisorEmail,
        [Week],
        ReportSummary,
        SubmittedAt,
        SupervisorComment,
        SupervisorGrade,
        FinalDecision,
        GroupID
      FROM WeeklyReports
      WHERE [Week] = @Week
        AND ReportSummary <> ''         -- student filled
        AND SupervisorComment IS NOT NULL -- supervisor filled
        AND SupervisorGrade IS NOT NULL  -- supervisor filled
      ORDER BY SubmittedAt DESC";

            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Week", currentWeek);
                conn.Open();
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                rptThisWeekReports.DataSource = dt;
                rptThisWeekReports.DataBind();
            }
        }



        private void LoadPreviousWeekReports()
        {
            int currentWeek = GetCurrentSemesterWeek();
            const string sql = @"
      SELECT 
        ReportID,
        StudentEmail,
        SupervisorEmail,
        [Week],
        ReportSummary,
        SubmittedAt,          -- real student timestamp
        SupervisorComment,
        SupervisorGrade,
        FinalDecision,
        GroupID
      FROM WeeklyReports
      WHERE [Week] < @Week
        AND ReportSummary <> '' 
      ORDER BY [Week] DESC, SubmittedAt DESC";

            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Week", currentWeek);
                conn.Open();
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                rptPreviousWeekReports.DataSource = dt;
                rptPreviousWeekReports.DataBind();
            }
        }



        public void SendWeeklyReportsJob()
        {
            int currentWeek = GetCurrentSemesterWeek();

            // 1) Gather all groups + their supervisors
            var groups = new List<(int GroupID, string SupervisorEmail)>();
            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(@"
        SELECT GroupID, SupervisorEmail
        FROM StudentGroups
        WHERE SupervisorEmail IS NOT NULL", conn))
            {
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        groups.Add(((int)rdr["GroupID"], rdr["SupervisorEmail"].ToString()));
            }

            // 2) For each group, insert a WeeklyReports row per student with non-null ReportSummary
            using (var conn = new SqlConnection(connString))
            {
                conn.Open();
                const string sqlFetchStudents = @"
            SELECT Email 
            FROM TP 
            WHERE Role='Student' AND GroupID=@G";
                const string sqlInsertReport = @"
            IF NOT EXISTS (
                SELECT 1 FROM WeeklyReports 
                WHERE StudentEmail=@StudentEmail AND [Week]=@Week
            )
            BEGIN
                INSERT INTO WeeklyReports
                  (StudentEmail, SupervisorEmail, GroupID, [Week], ReportSummary)
                VALUES
                  (@StudentEmail, @SupervisorEmail, @GroupID, @Week, '');
            END";

                foreach (var (groupId, supEmail) in groups)
                {
                    // fetch members
                    var emails = new List<string>();
                    using (var cmd = new SqlCommand(sqlFetchStudents, conn))
                    {
                        cmd.Parameters.AddWithValue("@G", groupId);
                        using (var rdr = cmd.ExecuteReader())
                            while (rdr.Read())
                                emails.Add(rdr.GetString(0));
                    }

                    // insert blank report rows
                    foreach (var studentEmail in emails)
                    {
                        using (var cmd = new SqlCommand(sqlInsertReport, conn))
                        {
                            cmd.Parameters.AddWithValue("@StudentEmail", studentEmail);
                            cmd.Parameters.AddWithValue("@SupervisorEmail", supEmail);
                            cmd.Parameters.AddWithValue("@GroupID", groupId);
                            cmd.Parameters.AddWithValue("@Week", currentWeek);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            // 3) Email every student to notify them
            SendWeeklyReportNotificationToStudents(currentWeek);
        }



        protected void btnSubmitGrades_Click(object sender, EventArgs e)
        {
            Label3.Text = "";              // clear previous error
            Label1.Text = "";              // clear success
            bool hasError = false;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                foreach (GridViewRow row in gvStudentGrades.Rows)
                {
                    // skip if no key
                    if (gvStudentGrades.DataKeys[row.RowIndex].Value == DBNull.Value)
                        continue;

                    int gradeId = Convert.ToInt32(gvStudentGrades.DataKeys[row.RowIndex].Value);

                    // grab the textboxes
                    var txtSup = (TextBox)row.FindControl("txtSupervisorEval");
                    var txtExam = (TextBox)row.FindControl("txtExamCommittee");
                    var txtCoord = (TextBox)row.FindControl("txtCoordCommittee");
                    var txtFinal = (TextBox)row.FindControl("txtFinalDecision");
                    var txtSO = (TextBox)row.FindControl("txtSOAttainment");

                    // try-parse each into decimal
                    if (!decimal.TryParse(txtSup.Text, out decimal supEval) ||
                        !decimal.TryParse(txtExam.Text, out decimal examComm) ||
                        !decimal.TryParse(txtCoord.Text, out decimal coordComm) ||
                        !decimal.TryParse(txtFinal.Text, out decimal finalDec) ||
                        !decimal.TryParse(txtSO.Text, out decimal soAtt))
                    {
                        Label3.Text = "Only allowed in decimal";
                        hasError = true;
                        break;
                    }

                    // the other integer fields you already guard with int.TryParse
                    var txtMax = (TextBox)row.FindControl("txtMaxGrade");
                    var txtSOS = (TextBox)row.FindControl("txtSOS");
                    var txtCLO = (TextBox)row.FindControl("txtCLO");
                    var txtWN = (TextBox)row.FindControl("txtWeekNumber");

                    int maxGrade = int.TryParse(txtMax.Text, out var mg) ? mg : 0;
                    int sos = int.TryParse(txtSOS.Text, out var s) ? s : 0;
                    int clo = int.TryParse(txtCLO.Text, out var c) ? c : 0;
                    int weekNumber = int.TryParse(txtWN.Text, out var w) ? w : 0;

                    // now safe to run your UPDATE
                    const string updateQuery = @"
                UPDATE StudentGrades
                   SET SupervisorEvaluation = @SupervisorEval,
                       ExaminationCommittee   = @ExamCommittee,
                       CoordinationCommittee  = @CoordCommittee,
                       FinalDecision          = @FinalDecision,
                       SOAttainment           = @SOAttainment,
                       MaxGrade               = @MaxGrade,
                       SOS                    = @SOS,
                       CLO                    = @CLO,
                       WeekNumber             = @WeekNumber
                 WHERE GradeID = @GradeID";

                    using (var cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@SupervisorEval", supEval);
                        cmd.Parameters.AddWithValue("@ExamCommittee", examComm);
                        cmd.Parameters.AddWithValue("@CoordCommittee", coordComm);
                        cmd.Parameters.AddWithValue("@FinalDecision", finalDec);
                        cmd.Parameters.AddWithValue("@SOAttainment", soAtt);

                        cmd.Parameters.AddWithValue("@MaxGrade", maxGrade);
                        cmd.Parameters.AddWithValue("@SOS", sos);
                        cmd.Parameters.AddWithValue("@CLO", clo);
                        cmd.Parameters.AddWithValue("@WeekNumber", weekNumber);

                        cmd.Parameters.AddWithValue("@GradeID", gradeId);

                        cmd.ExecuteNonQuery();
                    }
                }
            }

            if (hasError)
            {
                // jump out without showing success
                return;
            }

            // all rows saved
            Label1.Text = "Grades saved successfully!";
            Label1.CssClass = "success";
            LoadAllGroupGrades();
        }





        protected void btnApproveGroupIdea_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            int ideaID = Convert.ToInt32(btn.CommandArgument);

            // 1) mark the idea approved
            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(
                "UPDATE ProjectIdeas SET Status='Approved' WHERE IdeaID=@ID", conn))
            {
                cmd.Parameters.AddWithValue("@ID", ideaID);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            // 2) send the group notification
            SendIdeaDecisionEmailToGroup(ideaID, "Approved");

            // 3) notify all matching supervisors
            NotifySupervisorsOfApprovedIdea(ideaID);

            // 4) refresh your grid
            LoadAllGroupsAndIdeas();
        }




        private void NotifySupervisorsOfApprovedIdea(int ideaID)
        {
            // 1) pull title, description, type
            string title, description, ideaType;
            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(@"
        SELECT IdeaTitle, IdeaDescription, IdeaType
          FROM ProjectIdeas
         WHERE IdeaID = @ID", conn))
            {
                cmd.Parameters.AddWithValue("@ID", ideaID);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (!rdr.Read()) return;
                    title = rdr.GetString(0);
                    description = rdr.GetString(1);
                    ideaType = rdr.GetString(2);
                }
            }

            // 2) find all supervisors whose Interests LIKE this type
            var emails = new List<string>();
            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(@"
        SELECT Email
          FROM TP
         WHERE Role = 'Supervisor'
           AND Interests LIKE @Type", conn))
            {
                cmd.Parameters.AddWithValue("@Type", "%" + ideaType + "%");
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                        emails.Add(rdr.GetString(0));
            }

            // 3) send each one a notification
            foreach (var supEmail in emails)
            {
                try
                {
                    var mail = new MailMessage(
                        from: "seniorprojecthub@gmail.com",
                        to: supEmail,
                        subject: $"📢 New Approved Idea in {ideaType}",
                        body: $@"Dear Supervisor,

A new student idea in your interest area ({ideaType}) has just been approved:

    Title: {title}
    Description: {description}

Please log in to your portal to view or apply.

– Senior Project Hub"
                    );
                    mail.IsBodyHtml = false;

                    using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential("seniorprojecthub@gmail.com", "fjbs suqx ygkt uoaf");
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
                    }
                }
                catch
                {
                    // swallow/log
                }
            }
        }


        protected void btnRejectGroupIdea_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            int ideaID = Convert.ToInt32(btn.CommandArgument);

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                string query = "UPDATE ProjectIdeas SET Status = 'Rejected' WHERE IdeaID = @IdeaID";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdeaID", ideaID);
                    cmd.ExecuteNonQuery();
                }
            }

            SendIdeaDecisionEmailToGroup(ideaID, "Rejected");

            LoadAllGroupsAndIdeas(); // refresh grid
        }



        private void LoadStudentStatus()
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
SELECT 
    a.FullName,
    a.StudentEmail,
    ISNULL(pi.IdeaTitle, 'No Idea Yet') AS ProjectTitle,
    ISNULL(NULLIF(pi.AssignedSupervisorEmail, ''), 'Not Assigned') AS SupervisorEmail,
    sg.GroupID,
    sg.GroupCode,
    CASE 
        WHEN sg.Student1Email = a.StudentEmail THEN sg.Student2Email
        WHEN sg.Student2Email = a.StudentEmail THEN sg.Student1Email
        ELSE NULL
    END AS Teammate
FROM AssignedStudents a
LEFT JOIN TP tp ON a.StudentEmail = tp.Email
LEFT JOIN StudentGroups sg ON tp.GroupID = sg.GroupID
LEFT JOIN ProjectIdeas pi ON tp.GroupID = pi.GroupID";




                using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvAssignedStudents.DataSource = dt;
                    gvAssignedStudents.DataBind();
                }
            }
        }



        private void LoadAssignedStudents()
        {
            // build WHERE clause based on filter
            string filter = ddlGroupFilter.SelectedValue;  // "All", "InGroup", or "NoGroup"
            string whereClause = filter == "InGroup"
                ? "WHERE sg.GroupID IS NOT NULL"
                : filter == "NoGroup"
                    ? "WHERE sg.GroupID IS NULL"
                    : "";

            string sql = $@"
SELECT 
    a.FullName,
    a.StudentEmail,
    sg.GroupID,
    sg.GroupCode,
    CASE 
      WHEN sg.Student1Email = a.StudentEmail THEN sg.Student2Email
      WHEN sg.Student2Email = a.StudentEmail THEN sg.Student1Email
      ELSE NULL
    END AS Teammate,

    -- First try a coordinator-approved group idea
    COALESCE(grpIdea.ProjectTitle, grpApp.AppProjectTitle, 'No Idea Yet')    AS ProjectTitle,
    COALESCE(grpIdea.SupervisorEmail, grpApp.AppSupervisorEmail, 'Not Assigned') AS SupervisorEmail

FROM AssignedStudents a
LEFT JOIN TP tp 
  ON a.StudentEmail = tp.Email
LEFT JOIN StudentGroups sg 
  ON tp.GroupID = sg.GroupID

OUTER APPLY (
    SELECT TOP 1 
        pi.IdeaTitle        AS ProjectTitle,
        pi.AssignedSupervisorEmail AS SupervisorEmail
    FROM ProjectIdeas pi
    WHERE pi.GroupID = sg.GroupID
      AND pi.Status <> 'Rejected'
    ORDER BY pi.IdeaID DESC
) grpIdea

OUTER APPLY (
    SELECT TOP 1 
        sp.ProjectTitle      AS AppProjectTitle,
        sp.SupervisorEmail   AS AppSupervisorEmail
    FROM StudentApplications sa
    JOIN SupervisorProjects sp 
      ON sa.SupervisorProjectID = sp.ProjectID
    WHERE sa.StudentEmail IN (sg.Student1Email, sg.Student2Email)
      AND sp.ApprovedByCoordinator = 1
    ORDER BY sa.ApplicationID DESC
) grpApp

{whereClause}
ORDER BY a.FullName;
";

            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);

                gvAssignedStudents.DataSource = dt;
                gvAssignedStudents.DataBind();
            }
        }



        protected void ddlGroupFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadAssignedStudents();
        }

        protected void btnExportCSV_Click(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
      SELECT sg.GroupID, tp.FullName, tp.Email, sg.MaxGrade, sg.SOS, sg.CLO, sg.SubmissionType, 
             sg.WeekNumber, sg.SupervisorEvaluation, sg.ExaminationCommittee, 
             sg.CoordinationCommittee, sg.FinalDecision, sg.SOAttainment
      FROM StudentGrades sg
      INNER JOIN TP tp ON sg.StudentID = tp.UserID
      ORDER BY sg.GroupID, tp.FullName";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                StringBuilder sb = new StringBuilder();


                sb.AppendLine("\"Group ID\";\"Student Name\";\"Email\";\"Max Grade\";\"SO\";\"CLO\";\"Submission Type\";\"Week\";\"Supervisor Eval\";\"Exam Committee\";\"Coordination Committee\";\"Final Decision\";\"SO Attainment\"");


                while (reader.Read())
                {
                    var row = new List<string>();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string rawValue = reader[i]?.ToString() ?? "";
                        string cleanValue = rawValue.Replace("\"", "\"\""); // escape quotes
                        row.Add($"\"{cleanValue}\"");
                    }


                    sb.AppendLine(string.Join(";", row));
                }

                reader.Close();


                Response.Clear();
                Response.Buffer = true;
                Response.AddHeader("content-disposition", "attachment;filename=AllGroupGrades.csv");
                Response.Charset = "";
                Response.ContentType = "text/csv";
                Response.Output.Write(sb.ToString());
                Response.Flush();
                Response.End();
            }
        }



        public override void VerifyRenderingInServerForm(Control control)
        {
        }

        private void SendIdeaStatusEmail(string toEmail, string ideaTitle, string status)
        {
            try
            {
                string gmailEmail = "seniorprojecthub@gmail.com";
                string gmailPassword = "fjbs suqx ygkt uoaf"; // Gmail App Password

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(gmailEmail, "Senior Project Hub");
                mail.To.Add(toEmail);
                mail.Subject = $"Project Idea {status}";
                mail.Body = $"Dear student,\n\nYour project idea titled \"{ideaTitle}\" has been {status.ToLower()}.\n\nBest regards,\nSenior Project Hub Team";
                mail.IsBodyHtml = false;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential(gmailEmail, gmailPassword);
                smtp.EnableSsl = true;

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Email Error: " + ex.Message);
            }
        }
        public string FormatDescription(string input, int chunkSize)
        {
            if (string.IsNullOrEmpty(input)) return "";
            for (int i = chunkSize; i < input.Length; i += chunkSize + 1)
            {
                input = input.Insert(i, "\n");
            }
            return input;
        }


        private int GetCurrentSemesterWeek()
        {
            DateTime semesterStart = new DateTime(2025, 1, 11);
            DateTime today = DateTime.Today;

            int weekNumber = ((today - semesterStart).Days / 7) + 1;

            // Wrap around after 15 weeks (or set your own max)
            if (weekNumber > 15)
                weekNumber = ((weekNumber - 1) % 15) + 1;

            return weekNumber < 1 ? 1 : weekNumber;
        }







        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("Login.aspx");
        }
    }
}