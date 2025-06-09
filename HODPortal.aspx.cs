using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SeniorProjectHub3
{
    public partial class HODPortal : System.Web.UI.Page
    {
        private readonly string connString = ConfigurationManager.ConnectionStrings["MyDB"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadSampleRoadmap(); // <-- use this instead
                LoadGroups();
                LoadGrades();
            }
        }

        private void LoadRoadmap()
        {
            DataTable dt = new DataTable();

            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(@"
        SELECT 
            g.GroupCode,
            g.Student1Email + ISNULL(' & ' + g.Student2Email, '') AS Members,
            CASE WHEN pi.IdeaID IS NOT NULL THEN 'Yes' ELSE 'No' END AS IdeaSubmitted,
            CASE WHEN pi.Status = 'Approved' THEN 'Yes' ELSE 'No' END AS IdeaApproved,
            CASE WHEN g.SupervisorEmail IS NOT NULL THEN 'Yes' ELSE 'No' END AS SupervisorAssigned,
            -- CPIS 498: based on FinalDecision existing
            CASE 
                WHEN EXISTS (
                    SELECT 1 FROM StudentGrades sg 
                    WHERE sg.GroupID = g.GroupID AND sg.FinalDecision IS NOT NULL
                ) THEN 'Completed' ELSE 'Incomplete'
            END AS CPIS498Status,
            -- CPIS 499: only show if 498 is Completed
            CASE 
                WHEN EXISTS (
                    SELECT 1 FROM StudentGrades sg 
                    WHERE sg.GroupID = g.GroupID AND sg.FinalDecision IS NOT NULL
                ) THEN 
                    CASE 
                        WHEN EXISTS (
                            SELECT 1 FROM StudentGrades sg2 
                            WHERE sg2.GroupID = g.GroupID AND sg2.SOAttainment IS NOT NULL
                        ) THEN 'Completed'
                        ELSE 'Incomplete'
                    END
                ELSE ''
            END AS CPIS499Status,
            ISNULL(pi.IdeaTitle, '-') AS IdeaTitle,
            ISNULL(pi.IdeaDescription, '-') AS IdeaDescription,
            ISNULL(g.SupervisorEmail, '-') AS Supervisor
        FROM StudentGroups g
        LEFT JOIN ProjectIdeas pi ON g.GroupID = pi.GroupID
    ", conn))
            {
                conn.Open();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }

            gvRoadmap.DataSource = dt;
            gvRoadmap.DataBind();

            // Generate expandable detail rows
            string html = "";
            foreach (DataRow row in dt.Rows)
            {
                string groupCode = row["GroupCode"].ToString();
                string ideaTitle = row["IdeaTitle"].ToString();
                string ideaDesc = row["IdeaDescription"].ToString();
                string supervisor = row["Supervisor"].ToString();

                html += $"<tr id='details-{groupCode}' class='dropdown-row' style='display:none;'>";
                html += $"<td colspan='7' class='dropdown-cell'>";
                html += $"<strong>Idea Title:</strong> {ideaTitle}<br/>";
                html += $"<strong>Description:</strong> {ideaDesc}<br/>";
                html += $"<strong>Supervisor:</strong> {supervisor}";
                html += $"</td></tr>";
            }

            ltExtraRows.Text = html;
        }

        private void LoadSampleRoadmap()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("GroupCode");
            dt.Columns.Add("Members");
            dt.Columns.Add("IdeaSubmitted");
            dt.Columns.Add("IdeaApproved");
            dt.Columns.Add("SupervisorAssigned");
            dt.Columns.Add("CPIS498Status");
            dt.Columns.Add("CPIS499Status");

            // ✅ Realistic hex-style GroupCodes with male student names
            dt.Rows.Add("2E584A", "Ali & Fahad", "Yes", "Yes", "Yes", "Completed", "Completed"); // ✅✅✅
            dt.Rows.Add("3B129C", "Faisal & Omar", "Yes", "Yes", "Yes", "Incomplete", "");       // ✅❌ blank
            dt.Rows.Add("4C7F21", "Saud & Salem", "Yes", "No", "Yes", "", "");                   // ❌❌ blank
            dt.Rows.Add("7A9B8E", "Abdullah & Nawaf", "No", "No", "No", "", "");                 // ❌❌ blank
            dt.Rows.Add("6D42FE", "Yousef & Bader", "Yes", "Yes", "Yes", "Completed", "Completed");

            gvRoadmap.DataSource = dt;
            gvRoadmap.DataBind();

            // Optional: expandable rows with dummy idea info
            string html = "";
            foreach (DataRow row in dt.Rows)
            {
                string groupCode = row["GroupCode"].ToString();
                html += $"<tr id='details-{groupCode}' class='dropdown-row' style='display:none;'>";
                html += $"<td colspan='7' class='dropdown-cell'>";
                html += $"<strong>Idea Title:</strong> Smart Campus Platform<br/>";
                html += $"<strong>Description:</strong> A centralized system for booking labs and tracking attendance.<br/>";
                html += $"<strong>Supervisor:</strong> Dr. Saleh Alzahrani";
                html += $"</td></tr>";
            }

            ltExtraRows.Text = html;
        }


        protected void gvRoadmap_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                // Columns: 2 = IdeaSubmitted, 3 = IdeaApproved, 4 = SupervisorAssigned
                for (int i = 2; i <= 4; i++)
                {
                    TableCell cell = e.Row.Cells[i];
                    string value = cell.Text.Trim().ToLower();

                    if (value == "yes")
                    {
                        cell.CssClass = "status-yes";
                        cell.Text = "✅";
                    }
                    else if (value == "no")
                    {
                        cell.CssClass = "status-no";
                        cell.Text = "❌";
                    }
                }

                // Check if IdeaApproved is approved ✅ before showing CPIS 498/499
                string ideaApproved = e.Row.Cells[3].Text.Trim(); // Already set above as ✅ or ❌

                if (ideaApproved != "✅")
                {
                    // Blank CPIS 498 and 499 if idea not approved
                    e.Row.Cells[5].Text = "";
                    e.Row.Cells[6].Text = "";
                    return;
                }

                // Column 5 = CPIS 498
                string cpis498 = e.Row.Cells[5].Text.Trim().ToLower();
                if (cpis498 == "completed")
                {
                    e.Row.Cells[5].CssClass = "status-yes";
                    e.Row.Cells[5].Text = "✅";
                }
                else if (cpis498 == "incomplete")
                {
                    e.Row.Cells[5].CssClass = "status-no";
                    e.Row.Cells[5].Text = "❌";
                }

                // Column 6 = CPIS 499
                string cpis499 = e.Row.Cells[6].Text.Trim().ToLower();
                if (cpis499 == "completed")
                {
                    e.Row.Cells[6].CssClass = "status-yes";
                    e.Row.Cells[6].Text = "✅";
                }
                else if (cpis499 == "incomplete")
                {
                    e.Row.Cells[6].CssClass = "status-no";
                    e.Row.Cells[6].Text = "❌";
                }
            }
        }



        private void LoadGroups()
        {
            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand("SELECT GroupCode, Student1Email, Student2Email, SupervisorEmail FROM StudentGroups", conn))
            {
                conn.Open();
                var reader = cmd.ExecuteReader();
                gvGroups.DataSource = reader;
                gvGroups.DataBind();
            }
        }

        private void LoadGrades()
        {
            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand("SELECT GroupID, MaxGrade, SupervisorEvaluation, FinalDecision FROM StudentGrades", conn))
            {
                conn.Open();
                var reader = cmd.ExecuteReader();
                gvGrades.DataSource = reader;
                gvGrades.DataBind();
            }
        }

      
    }
}
