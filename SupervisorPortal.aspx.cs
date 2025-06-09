using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Net;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SeniorProjectHub3
{
    public partial class SupervisorPortal : System.Web.UI.Page
    {
        private readonly string connString = ConfigurationManager.ConnectionStrings["MyDB"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Ensure a valid session exists; if not, redirect to Login.aspx.
            if (Session["UserEmail"] == null)
            {
                Response.Redirect("Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                LoadSupervisorProfile();
                LoadApprovedStudentIdeas();
                LoadWeeklyReports();
                LoadStudentApplications();
                LoadMyIdeas();
                LoadMySupervisorApplications();
                LoadGroupIdeasForSupervisor();

                LoadSupervisedGroups();


            }
        }


        // Loads the supervisor's profile from the TP table.
        private void LoadSupervisorProfile()
        {
            string email = Session["UserEmail"]?.ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = "SELECT FullName, Email, PhoneNumber, Major FROM TP WHERE Email = @Email";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        lblName.Text = reader["FullName"].ToString();
                        lblEmail.Text = reader["Email"].ToString();
                        lblOffice.Text = reader["PhoneNumber"] != DBNull.Value ? reader["PhoneNumber"].ToString() : "Not Available";
                        lblSpecialization.Text = reader["Major"] != DBNull.Value ? reader["Major"].ToString() : "Not Available";


                    }
                }
            }
        }

        private void LoadGroupIdeasForSupervisor()
        {
            string supervisorEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(supervisorEmail))
                return;

            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(@"
        SELECT 
            sg.GroupCode,
            sg.Student1Email,
            sg.Student2Email,
            pi.IdeaTitle,
            pi.IdeaType    AS IdeaType,   -- pull the type here
            pi.Status
        FROM StudentGroups sg
        INNER JOIN ProjectIdeas pi 
            ON sg.GroupID = pi.GroupID
        WHERE pi.AssignedSupervisorEmail = @SupervisorEmail
    ", conn))
            {
                cmd.Parameters.AddWithValue("@SupervisorEmail", supervisorEmail);
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                gvGroupIdeas.DataSource = dt;
                gvGroupIdeas.DataBind();
            }
        }


        // Loads approved student ideas, excluding any idea with an application by this supervisor.
        private void LoadApprovedStudentIdeas()
        {
            string supervisorEmail = Session["UserEmail"]?.ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
    SELECT IdeaID, IdeaTitle, StudentEmail, IdeaDescription, IdeaType,
           ISNULL(AssignedSupervisorEmail, '') AS AssignedSupervisor 
    FROM ProjectIdeas 
    WHERE Status = 'Approved'
      AND NOT EXISTS (
          SELECT 1 FROM SupervisorApplications 
          WHERE IdeaID = ProjectIdeas.IdeaID AND SupervisorEmail = @SupervisorEmail
      )";

                using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@SupervisorEmail", supervisorEmail);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvStudentIdeas.DataSource = dt;
                    gvStudentIdeas.DataBind();
                }
            }
        }

        // Loads weekly reports.
        private void LoadWeeklyReports()
        {
            string email = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(email)) return;

            using (var conn = new SqlConnection(connString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();

                // 1) Pending reviews – only where the student has submitted a non-empty summary
                cmd.CommandText = @"
SELECT 
    ReportID,
    StudentEmail,
    GroupID,
    [Week],
    ReportSummary,
    SubmittedAt,
    SupervisorComment,
    SupervisorGrade,
    SupervisorSubmittedAt
FROM WeeklyReports
WHERE SupervisorEmail = @Email
  AND SupervisorSubmittedAt IS NULL
  AND ReportSummary IS NOT NULL
  AND ReportSummary <> ''
ORDER BY SubmittedAt DESC";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@Email", email);

                var dtPending = new DataTable();
                using (var da = new SqlDataAdapter(cmd))
                    da.Fill(dtPending);

                gvWeeklyReports.DataSource = dtPending;
                gvWeeklyReports.DataBind();


                // 2) Already graded – likewise only real submissions
                cmd.CommandText = @"
SELECT 
    ReportID,
    StudentEmail,
    GroupID,
    [Week],
    ReportSummary,
    SubmittedAt,
    SupervisorComment,
    SupervisorGrade,
    SupervisorSubmittedAt
FROM WeeklyReports
WHERE SupervisorEmail = @Email
  AND SupervisorSubmittedAt IS NOT NULL
  AND ReportSummary IS NOT NULL
  AND ReportSummary <> ''
ORDER BY SupervisorSubmittedAt DESC";

                var dtGraded = new DataTable();
                using (var da2 = new SqlDataAdapter(cmd))
                    da2.Fill(dtGraded);

                gvSubmittedReports.DataSource = dtGraded;
                gvSubmittedReports.DataBind();
            }
        }





        // Loads pending student applications submitted to the supervisor's projects.
        private void LoadStudentApplications()
        {
            string supervisorEmail = Session["UserEmail"]?.ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
                    SELECT sa.ApplicationID, sp.ProjectTitle, sa.StudentEmail, sa.Status
                    FROM StudentApplications sa
                    JOIN SupervisorProjects sp ON sa.SupervisorProjectID = sp.ProjectID
                    WHERE sp.SupervisorEmail = @SupervisorEmail AND sa.Status = 'Pending'";
                using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@SupervisorEmail", supervisorEmail);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvStudentApplications.DataSource = dt;
                    gvStudentApplications.DataBind();
                }
            }
        }

        private void LoadMyIdeas()
        {
            string supervisorEmail = Session["UserEmail"]?.ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
            SELECT sp.ProjectID,
                   sp.ProjectTitle,
                   sp.ProjectDescription,
                   sp.CreatedAt,
                   COALESCE(sp.AssignedStudent, 'Not assigned yet') AS AssignedStudent
            FROM SupervisorProjects sp
            WHERE sp.SupervisorEmail = @Email";

                using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@Email", supervisorEmail);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvMyIdeas.DataSource = dt;
                    gvMyIdeas.DataBind();
                }
            }
        }



        // Loads "My Applications" – the supervisor's own applications to student ideas.
        private void LoadMySupervisorApplications()
        {
            string supervisorEmail = Session["UserEmail"]?.ToString();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
            SELECT sa.ApplicationID, pi.IdeaTitle, pi.StudentEmail, sa.Status
            FROM SupervisorApplications sa
            JOIN ProjectIdeas pi ON sa.IdeaID = pi.IdeaID
            WHERE sa.SupervisorEmail = @SupervisorEmail";

                using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@SupervisorEmail", supervisorEmail);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvMyApplications.DataSource = dt;
                    gvMyApplications.DataBind();
                }
            }
        }

        private void NotifyCoordinatorsOfSupervisorIdea(string supervisorEmail, string ideaType, string ideaTitle, string ideaDescription)
        {
            try
            {
                string fromEmail = "seniorprojecthub@gmail.com";
                string password = "fjbs suqx ygkt uoaf";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromEmail, "Senior Project Hub");

                // ✅ Send to both coordinators
                mail.To.Add("fsallah@kau.edu.sa");
                mail.To.Add("hlabani@kau.edu.sa");

                mail.Subject = $"📬 New Supervisor Project Idea Submitted: {ideaType}";
                mail.Body = $"Dear Coordinator,\n\nA supervisor has submitted a new project idea:\n\n" +
                            $"👨‍🏫 Supervisor: {supervisorEmail}\n📌 Title: {ideaTitle}\n📝 Description: {ideaDescription}\n" +
                            $"📂 Category: {ideaType}\n\nPlease review and approve it from the Coordinator Portal.\n\n– Senior Project Hub";
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

        // Event handler for posting a new project idea.
        protected void btnPostIdea_Click(object sender, EventArgs e)
        {
            if (Session["UserEmail"] == null)
            {
                Response.Redirect("Login.aspx");
                return;
            }

            string supervisorEmail = Session["UserEmail"].ToString();
            string title = txtProjectTitle.Text.Trim();
            string description = txtProjectDescription.Text.Trim();
            string ideaType = ddlSupervisorIdeaType.SelectedValue;

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(description) || string.IsNullOrEmpty(ideaType))
                return;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
            INSERT INTO SupervisorProjects (SupervisorEmail, ProjectTitle, ProjectDescription, IdeaType, ApprovedByCoordinator)
            VALUES (@Email, @Title, @Description, @IdeaType, 0)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", supervisorEmail);
                    cmd.Parameters.AddWithValue("@Title", title);
                    cmd.Parameters.AddWithValue("@Description", description);
                    cmd.Parameters.AddWithValue("@IdeaType", ideaType);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    NotifyStudentsOfSupervisorIdea(ideaType, title, description);
                    NotifyCoordinatorsOfSupervisorIdea(supervisorEmail, ideaType, title, description);

                }
            }

            txtProjectTitle.Text = "";
            txtProjectDescription.Text = "";
            ddlSupervisorIdeaType.ClearSelection();
            LoadMyIdeas();
        }



        // Event handler for accepting a student application.


        // Step 1: Retrieve the student email & project ID from the application

        // Add this method to handle supervision request acceptance


        // Event handler for rejecting a student application.
        protected void btnAcceptStudent_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            if (!int.TryParse(btn.CommandArgument, out int applicationID)) return;

            string supervisorEmail = Session["UserEmail"] as string;
            if (supervisorEmail == null) { Response.Redirect("Login.aspx"); return; }

            int projectID;
            string studentEmail;

            using (var conn = new SqlConnection(connString))
            {
                conn.Open();

                // 1) load this application…
                using (var cmd = new SqlCommand(@"
            SELECT SupervisorProjectID, StudentEmail
              FROM StudentApplications
             WHERE ApplicationID = @AppID", conn))
                {
                    cmd.Parameters.AddWithValue("@AppID", applicationID);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (!rdr.Read()) return;
                        projectID = rdr.GetInt32(0);
                        studentEmail = rdr.GetString(1);
                    }
                }

                // 2) mark THIS one accepted
                using (var cmd = new SqlCommand(@"
            UPDATE StudentApplications
               SET Status = 'Accepted'
             WHERE ApplicationID = @AppID", conn))
                {
                    cmd.Parameters.AddWithValue("@AppID", applicationID);
                    cmd.ExecuteNonQuery();
                }

                // 3) assign them on the supervisor’s project
                using (var cmd = new SqlCommand(@"
            UPDATE SupervisorProjects
               SET AssignedStudent = @Stu
             WHERE ProjectID = @ProjID", conn))
                {
                    cmd.Parameters.AddWithValue("@Stu", studentEmail);
                    cmd.Parameters.AddWithValue("@ProjID", projectID);
                    cmd.ExecuteNonQuery();
                }

                // 4) propagate into StudentGroups and ProjectIdeas
                using (var cmd = new SqlCommand(@"
            -- make sure this student is in your groups table
            IF NOT EXISTS(
              SELECT 1 FROM StudentGroups
               WHERE Student1Email = @Stu OR Student2Email = @Stu
            )
            BEGIN
               INSERT INTO StudentGroups (GroupCode, Student1Email, SupervisorEmail, CreatedAt)
               VALUES (NEWID(), @Stu, @Sup, GETDATE())
            END
            ELSE
            BEGIN
               UPDATE StudentGroups
                  SET SupervisorEmail = @Sup
                WHERE Student1Email = @Stu
                   OR Student2Email = @Stu
            END;

            -- also mark it on any ProjectIdeas rows for that group
            UPDATE ProjectIdeas
               SET AssignedSupervisorEmail = @Sup
             WHERE GroupID IN (
               SELECT GroupID
                 FROM StudentGroups
                WHERE Student1Email = @Stu
                   OR Student2Email = @Stu
             );", conn))
                {
                    cmd.Parameters.AddWithValue("@Sup", supervisorEmail);
                    cmd.Parameters.AddWithValue("@Stu", studentEmail);
                    cmd.ExecuteNonQuery();
                }

                // 5) reject everyone else on that same project
                using (var cmd = new SqlCommand(@"
            UPDATE StudentApplications
               SET Status = 'Rejected'
             WHERE SupervisorProjectID = @ProjID
               AND ApplicationID <> @AppID", conn))
                {
                    cmd.Parameters.AddWithValue("@ProjID", projectID);
                    cmd.Parameters.AddWithValue("@AppID", applicationID);
                    cmd.ExecuteNonQuery();
                }

                // 6) get the title & fire off your email
                string projectTitle;
                using (var cmd = new SqlCommand(
                    "SELECT ProjectTitle FROM SupervisorProjects WHERE ProjectID = @P", conn))
                {
                    cmd.Parameters.AddWithValue("@P", projectID);
                    projectTitle = (cmd.ExecuteScalar() as string) ?? "";
                }
                SendStudentAcceptanceEmail(studentEmail, supervisorEmail, projectTitle);
            }

            // finally, refresh your grids
            LoadStudentApplications();
            LoadMyIdeas();
            LoadMySupervisorApplications();
        }



        protected void btnRejectStudent_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            int applicationID;
            if (!int.TryParse(btn.CommandArgument, out applicationID))
            {
                lblEmail.Text = "Invalid ApplicationID!";
                return;
            }

            string supervisorEmail = Session["UserEmail"]?.ToString();
            string studentEmail = "";
            int projectID = 0;

            using (var conn = new SqlConnection(connString))
            {
                conn.Open();

                // 1) Get student & project
                using (var cmd = new SqlCommand(
                    "SELECT StudentEmail, SupervisorProjectID FROM StudentApplications WHERE ApplicationID = @ID", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", applicationID);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            studentEmail = rdr.GetString(0);
                            projectID = rdr.GetInt32(1);
                        }
                    }
                }

                // 2) Remove the application
                using (var cmd = new SqlCommand(
                    "DELETE FROM StudentApplications WHERE ApplicationID = @ID", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", applicationID);
                    cmd.ExecuteNonQuery();
                }

                // 3) Get the project title for email
                string title;
                using (var cmd = new SqlCommand(
                    "SELECT ProjectTitle FROM SupervisorProjects WHERE ProjectID = @P", conn))
                {
                    cmd.Parameters.AddWithValue("@P", projectID);
                    title = (cmd.ExecuteScalar() as string) ?? "";
                }

                // 4) Send rejection email
                SendStudentRejectionEmail(studentEmail, supervisorEmail, title);
            }

            LoadStudentApplications();
            LoadMyIdeas();
            LoadMySupervisorApplications();
        }

        private void SendStudentAcceptanceEmail(string toEmail, string supervisorEmail, string projectTitle)
        {
            try
            {
                using (var mail = new MailMessage("seniorprojecthub@gmail.com", toEmail))
                {
                    mail.Subject = "✅ Your Application Has Been Accepted";
                    mail.Body = $"Dear Student,\n\n{supervisorEmail} has accepted your application for \"{projectTitle}\".\n\nBest,\nSenior Project Hub";
                    using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential("seniorprojecthub@gmail.com", "fjbs suqx ygkt uoaf");
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
                    }
                }
            }
            catch
            {
                // log or swallow
            }
        }

        private void SendStudentRejectionEmail(string toEmail, string supervisorEmail, string projectTitle)
        {
            try
            {
                using (var mail = new MailMessage("seniorprojecthub@gmail.com", toEmail))
                {
                    mail.Subject = "❌ Your Application Has Been Rejected";
                    mail.Body = $"Dear Student,\n\nWe’re sorry—{supervisorEmail} rejected your application for \"{projectTitle}\".\n\nBest,\nSenior Project Hub";
                    using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential("seniorprojecthub@gmail.com", "fjbs suqx ygkt uoaf");
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
                    }
                }
            }
            catch
            {
                // log or swallow
            }
        }

        // Event handler for the Apply button in the "View Student Ideas" section.
        protected void btnApply_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            int ideaId = Convert.ToInt32(btn.CommandArgument);
            string supervisorEmail = Session["UserEmail"].ToString();

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // ✅ Get student email and idea title first
                string studentEmail = "", ideaTitle = "";
                string getStudentQuery = "SELECT StudentEmail, IdeaTitle FROM ProjectIdeas WHERE IdeaID = @IdeaID";
                using (SqlCommand cmd = new SqlCommand(getStudentQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@IdeaID", ideaId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            studentEmail = reader["StudentEmail"].ToString();
                            ideaTitle = reader["IdeaTitle"].ToString();
                        }
                    }
                }

                // ✅ Insert supervisor application
                string insertQuery = "INSERT INTO SupervisorApplications (IdeaID, SupervisorEmail, StudentEmail, Status) VALUES (@IdeaID, @SupervisorEmail, @StudentEmail, 'Pending')";
                using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@IdeaID", ideaId);
                    cmd.Parameters.AddWithValue("@SupervisorEmail", supervisorEmail);
                    cmd.Parameters.AddWithValue("@StudentEmail", studentEmail);
                    cmd.ExecuteNonQuery();
                }

                // ✅ Send email notification to student
                SendSupervisorApplicationEmail(studentEmail, ideaTitle, supervisorEmail);
            }

            LoadApprovedStudentIdeas();  // Refresh the grid
        }

        private void SendSupervisorApplicationEmail(string toEmail, string ideaTitle, string supervisorEmail)
        {
            try
            {
                string fromEmail = "seniorprojecthub@gmail.com";
                string password = "fjbs suqx ygkt uoaf";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromEmail, "Senior Project Hub");
                mail.To.Add(toEmail);
                mail.Subject = "Supervisor Application to Your Project Idea";
                mail.Body = $"Dear Student,\n\nSupervisor ({supervisorEmail}) has applied to supervise your idea titled \"{ideaTitle}\".\n\nPlease review the application in your portal.\n\nBest regards,\nSenior Project Hub Team";
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




        protected void gvWeeklyReports_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "SubmitReview")
            {
                int reportID = Convert.ToInt32(e.CommandArgument);
                GridViewRow row = ((Button)e.CommandSource).NamingContainer as GridViewRow;

                TextBox txtComment = row.FindControl("txtSupervisorComment") as TextBox;
                TextBox txtGrade = row.FindControl("txtSupervisorGrade") as TextBox;

                string supervisorComment = txtComment?.Text.Trim();
                decimal supervisorGrade;

                if (!decimal.TryParse(txtGrade?.Text, out supervisorGrade))
                {
                    // Optional: Show a message to the supervisor about invalid grade
                    return;
                }

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    string query = @"
                UPDATE WeeklyReports 
                SET SupervisorComment = @Comment, 
                    SupervisorGrade = @Grade,
                    SupervisorSubmittedAt = GETDATE()
                WHERE ReportID = @ReportID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Comment", supervisorComment);
                        cmd.Parameters.AddWithValue("@Grade", supervisorGrade);
                        cmd.Parameters.AddWithValue("@ReportID", reportID);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                LoadWeeklyReports(); // Refresh the GridView
            }
        }
        protected void btnFilterIdeas_Click(object sender, EventArgs e)
        {
            string selectedType = ddlIdeaType.SelectedValue;
            string supervisorEmail = Session["UserEmail"]?.ToString();

            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
            SELECT IdeaID, IdeaTitle, StudentEmail, IdeaDescription, IdeaType
            FROM ProjectIdeas
            WHERE Status = 'Approved'
              AND (AssignedSupervisorEmail IS NULL OR AssignedSupervisorEmail = '')
              AND NOT EXISTS (
                  SELECT 1 FROM SupervisorApplications 
                  WHERE IdeaID = ProjectIdeas.IdeaID AND SupervisorEmail = @SupervisorEmail
              )";

                if (!string.IsNullOrEmpty(selectedType))
                {
                    query += " AND IdeaType = @IdeaType";
                }

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SupervisorEmail", supervisorEmail);
                    if (!string.IsNullOrEmpty(selectedType))
                    {
                        cmd.Parameters.AddWithValue("@IdeaType", selectedType);
                    }

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvStudentIdeas.DataSource = dt;
                    gvStudentIdeas.DataBind();
                }
            }
        }
        protected void btnSubmitFeedback_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            int reportId = Convert.ToInt32(btn.CommandArgument);

            GridViewRow row = (GridViewRow)btn.NamingContainer;

            // ✅ Match your .aspx TextBox IDs
            TextBox txtComment = (TextBox)row.FindControl("txtComment");
            TextBox txtGrade = (TextBox)row.FindControl("txtGrade");


            if (txtComment == null || txtGrade == null)
            {
                // Optional: log error or show a message
                return;
            }

            string comment = txtComment.Text.Trim();
            decimal grade;

            if (!decimal.TryParse(txtGrade.Text, out grade))
            {
                lblSupervisorGradeError.Text = "Only allowed in decimal";
                return;
            }

            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
        UPDATE WeeklyReports
        SET SupervisorComment = @Comment,
            SupervisorGrade = @Grade,
            SupervisorSubmittedAt = GETDATE()
        WHERE ReportID = @ReportID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Comment", comment);
                    cmd.Parameters.AddWithValue("@Grade", grade);
                    cmd.Parameters.AddWithValue("@ReportID", reportId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            // ✅ Refresh the GridViews after update
            LoadWeeklyReports();
        }


        private void NotifyStudentsOfSupervisorIdea(string ideaType, string ideaTitle, string ideaDescription)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"
            SELECT FullName, Email
            FROM TP
            WHERE Role = 'Student' AND Interests LIKE @IdeaType";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdeaType", "%" + ideaType + "%");
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string studentName = reader["FullName"].ToString();
                            string studentEmail = reader["Email"].ToString();

                            SendInterestMatchEmail(studentEmail, studentName, ideaType, ideaTitle, ideaDescription);
                        }
                    }
                }
            }
        }
        private void SendInterestMatchEmail(string toEmail, string studentName, string ideaType, string ideaTitle, string ideaDescription)
        {
            try
            {
                string fromEmail = "seniorprojecthub@gmail.com";
                string password = "fjbs suqx ygkt uoaf";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromEmail, "Senior Project Hub");
                mail.To.Add(toEmail);
                mail.Subject = $"📢 New {ideaType} Project Posted by a Supervisor!";
                mail.Body = $"Hi {studentName},\n\nA new supervisor project idea has been posted in your interest area: {ideaType}.\n\n" +
                            $"📌 Title: {ideaTitle}\n📝 Description: {ideaDescription}\n\nVisit your Student Portal to view and apply.\n\n— Senior Project Hub Team";
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
        private void NotifyStudentWeeklyReportGraded(string studentEmail, int week, decimal grade)
        {
            string subject = $"📊 Weekly Report {week} Graded";
            string body = $"Hello,\n\nYour Weekly Report for Week {week} has been graded.\nSupervisor Grade: {grade}/100.\n\n– Senior Project Hub";
            SendEmail(studentEmail, subject, body);
        }

        public string FormatWithBreaks(string text, int interval)
        {
            if (string.IsNullOrEmpty(text)) return "";
            for (int i = interval; i < text.Length; i += interval + 1)
            {
                text = text.Insert(i, "<br/>");
            }
            return text;
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

        private void LoadSupervisedGroups()
        {
            string supervisorEmail = Session["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(supervisorEmail)) return;

            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(@"
SELECT
    sg.GroupCode,
    sg.Student1Email,
    sg.Student2Email,

    -- Title (student‐submitted or supervisor‐posted)
    COALESCE(pi.IdeaTitle, sp.ProjectTitle) AS IdeaTitle,

    -- **TYPE** (student or supervisor)
    COALESCE(pi.IdeaType, sp.IdeaType, 'N/A')   AS IdeaType,

    -- Status (or 'Assigned' if only supervisor flow)
    COALESCE(pi.Status, 'Assigned')      AS Status

FROM StudentGroups sg

OUTER APPLY (
    SELECT TOP 1 
        IdeaTitle,
        IdeaType,
        Status
    FROM ProjectIdeas
    WHERE GroupID = sg.GroupID
      AND AssignedSupervisorEmail = @Email
    ORDER BY IdeaID DESC
) pi

OUTER APPLY (
    SELECT TOP 1
        ProjectTitle,
        IdeaType        -- ← include this!
    FROM SupervisorProjects
    WHERE SupervisorEmail = @Email
      AND AssignedStudent IN (sg.Student1Email, sg.Student2Email)
    ORDER BY ProjectID DESC
) sp

WHERE
    sg.SupervisorEmail = @Email
 OR pi.IdeaTitle    IS NOT NULL
 OR sp.ProjectTitle IS NOT NULL

ORDER BY sg.GroupCode
", conn))
            {
                cmd.Parameters.AddWithValue("@Email", supervisorEmail);

                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                gvGroupIdeas.DataSource = dt;
                gvGroupIdeas.DataBind();
            }
        }






        // Event handler for the Logout button in the Profile section.
        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("Login.aspx");
        }
    }
}