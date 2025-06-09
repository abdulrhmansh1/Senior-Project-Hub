<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CoordinatorPortal.aspx.cs" Inherits="SeniorProjectHub3.CoordinatorPortal" %>


<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Coordinator Portal - FCIT KAU</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background: url("https://smapse.com/storage/2018/09/converted/825_585_4671-king-abdulaziz-university-kau-health-sciences-center-and-university-campus-project-4674.jpg") no-repeat center center fixed;
            background-size: cover;
            margin: 0;
            min-height: 100vh;
        }
        .nav-menu {
            background-color: #006400;
            padding: 20px 0;
            display: flex;
            justify-content: center;
            gap: 30px;
            box-shadow: 0 2px 5px rgba(0,0,0,0.1);
        }
        .desc-preview,
.desc-full {
    white-space: pre-wrap; 
    word-wrap: break-word;
    max-width: 400px;
}
        .nav-link {
            color: white;
            text-decoration: none;
            padding: 12px 20px;
            border-radius: 6px;
            transition: all 0.3s ease;
            font-weight: 500;
            cursor: pointer;
        }
        /* container for rows of cards */
.report-container {
  display: flex;
  flex-wrap: wrap;
  margin: 0 -5px;
}

/* each report is a “card” occupying ~1/3 of the width */
.report-card {
  box-sizing: border-box;
  flex: 0 0 calc(33.333% - 10px);
  margin: 5px;
  background: #fff;
  padding: 15px;
  border-radius: 8px;
  box-shadow: 0 0 5px rgba(0,0,0,0.1);
}

        .nav-link:hover {
            background-color: #00cc33;
            transform: translateY(-2px);
        }
        .container {
            background-color: #fff;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 0 15px rgba(0,0,0,0.2);
            width: 85%;
            margin: 25px auto;
            display: none;
        }
        .active-section {
            display: block;
            animation: fadeIn 0.5s ease;
        }
        .table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
        }
        .table th, .table td {
            border: 1px solid #ddd;
            padding: 10px;
            text-align: left;
        }
        .table th {
            background-color: #004d00; 
            color: white;
        }
        
#grade-students .input {
    width: 80px;
    font-size: 13px;
    padding: 4px;
}
/* ✅ Green background */
#gvGroupFunctionality td:contains('✅') {
    background-color: #dff5e1 !important;
    color: #27ae60;
    font-weight: bold;
}

/* ❌ Red background */
#gvGroupFunctionality td:contains('❌') {
    background-color: #fcebea !important;
    color: #e74c3c;
    font-weight: bold;
}


#grade-students .table {
    display: block;
    overflow-x: auto;
    white-space: nowrap;
}


#grade-students .table th,
#grade-students .table td {
    padding: 6px;
    font-size: 13px;
}

        button, .button {
            padding: 10px 15px;
            background-color: #004d00; 
            border: none;
            border-radius: 4px;
            color: #fff;
            cursor: pointer;
        }
        button:hover, .button:hover {
            background-color: #00cc33;
        }
        .input {
            padding: 8px;
            border: 1px solid #ddd;
            border-radius: 4px;
            width: 200px;
        }
        .status-message {
            display: block;
            margin-top: 10px;
            font-weight: bold;
        }
        .error {
            color: red;
        }
        .success {
            color: green;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <center>
            <img src="https://fcitweb.kau.edu.sa/fcitwebsite/images/white-logo.png" alt="FCIT Logo" class="logo" width="1000" height="300"/>
        </center>
        <asp:Button ID="btnLogout" runat="server" Text="Logout" CssClass="button" OnClick="btnLogout_Click" />

        <nav class="nav-menu">
            
            <a href="#" class="nav-link" data-section="review-supervisor-ideas">Supervisor Ideas</a> 
            <a href="#" class="nav-link" data-section="all-groups">Group Ideas</a>
            <a href="#" class="nav-link" data-section="weekly-reports">Weekly Reports</a>
            <a href="#" class="nav-link" data-section="create-groups">Students</a>
            <a href="#" class="nav-link" data-section="grade-students">Grading</a>
            <a href="#" class="nav-link" data-section="help">Help</a>
        </nav>

       




<!-- Groups Ideas Section -->
<div id="all-groups" class="container">
    <h2>Group Project Ideas</h2>
    <asp:GridView ID="gvAllGroupsIdeas" runat="server" AutoGenerateColumns="False" CssClass="table" DataKeyNames="IdeaID">
        <Columns>
            <asp:BoundField DataField="GroupCode" HeaderText="Group Code" />
            <asp:BoundField DataField="GroupMember1" HeaderText="Student 1 Email" />
            <asp:BoundField DataField="GroupMember2" HeaderText="Student 2 Email" />
            <asp:BoundField DataField="IdeaTitle" HeaderText="Idea Title" />
            <asp:TemplateField HeaderText="Description">
    <ItemTemplate>
        <div class="desc-preview">
            <asp:Label ID="lblShortDesc" runat="server"
                Text='<%# FormatDescription(Eval("IdeaDescription").ToString(), 50) %>' />
        </div>
        <asp:Panel ID="pnlFullDesc" runat="server" CssClass="desc-full" Style="display:none;">
            <asp:Label ID="lblFullDesc" runat="server" Text='<%# Eval("IdeaDescription") %>' />
        </asp:Panel>
        <asp:LinkButton ID="btnToggleDesc" runat="server" Text="More"
            OnClientClick="toggleFullDescription(this); return false;" />
    </ItemTemplate>
</asp:TemplateField>
            <asp:BoundField DataField="Status" HeaderText="Status" />
            <asp:TemplateField HeaderText="Actions">
                <ItemTemplate>
                    <asp:Button ID="btnApproveGroupIdea" runat="server" Text="Approve" CssClass="button"
                        CommandArgument='<%# Eval("IdeaID") %>' OnClick="btnApproveGroupIdea_Click" />
                    <asp:Button ID="btnRejectGroupIdea" runat="server" Text="Reject" CssClass="button"
                        CommandArgument='<%# Eval("IdeaID") %>' OnClick="btnRejectGroupIdea_Click" />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</div>

<!-- Review Supervisor Ideas Section -->
<div id="review-supervisor-ideas" class="container">
    <h2>Review Supervisor Submitted Ideas</h2>

    <asp:GridView ID="gvSupervisorIdeas" runat="server" AutoGenerateColumns="False" CssClass="table"
        DataKeyNames="ProjectID" OnRowCommand="gvSupervisorIdeas_RowCommand">
        <Columns>
            <asp:BoundField DataField="ProjectID" HeaderText="Project ID" />
            <asp:BoundField DataField="SupervisorEmail" HeaderText="Supervisor Email" />
            <asp:BoundField DataField="ProjectTitle" HeaderText="Title" />
            
            <asp:TemplateField HeaderText="Description">
    <ItemTemplate>
        <div class="desc-preview">
            <asp:Label ID="lblShortDesc" runat="server"
                Text='<%# FormatDescription(Eval("ProjectDescription").ToString(), 50) %>' />
        </div>
        <asp:Panel ID="pnlFullDesc" runat="server" CssClass="desc-full" Style="display:none;">
            <asp:Label ID="lblFullDesc" runat="server" Text='<%# Eval("ProjectDescription") %>' />
        </asp:Panel>
        <asp:LinkButton ID="btnToggleDesc" runat="server" Text="More"
            OnClientClick="toggleFullDescription(this); return false;" />
    </ItemTemplate>
</asp:TemplateField>

            <asp:BoundField DataField="CreatedAt" HeaderText="Created At" />

            <asp:TemplateField HeaderText="Actions">
                <ItemTemplate>
                    <asp:Button ID="btnApprove" runat="server" Text="Approve" CssClass="button"
                        CommandName="Approve" CommandArgument='<%# Eval("ProjectID") %>' />
                    <asp:Button ID="btnReject" runat="server" Text="Reject" CssClass="button"
                        CommandName="Reject" CommandArgument='<%# Eval("ProjectID") %>' />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <asp:Label ID="lblSupervisorIdeaStatus" runat="server" CssClass="status-message"></asp:Label>

<h3>Approved Supervisor Ideas</h3>
  <asp:GridView 
      ID="gvApprovedSupervisorProjects" 
      runat="server"
      AutoGenerateColumns="False"
      CssClass="table"
      EmptyDataText="No approved supervisor ideas."
      ShowHeaderWhenEmpty="True">
    <Columns>
      <asp:BoundField 
          DataField="ProjectID" 
          HeaderText="ID" 
          ReadOnly="True" />
      <asp:BoundField 
          DataField="SupervisorEmail" 
          HeaderText="Supervisor Email" 
          ReadOnly="True" />
      <asp:BoundField 
          DataField="ProjectTitle" 
          HeaderText="Title" 
          ReadOnly="True" />
      <asp:BoundField 
          DataField="ProjectDescription" 
          HeaderText="Description" 
          ReadOnly="True" />
      <asp:BoundField 
          DataField="GroupCode" 
          HeaderText="Group Code" 
          ReadOnly="True" />
    </Columns>
  </asp:GridView>



</div>

<!-- Weekly Reports Section -->
<div id="weekly-reports" class="container">
    <h2>Weekly Reports</h2>

    <!-- First container: Send new report -->
    <div style="margin-bottom: 20px;">
        <asp:Label ID="lblCurrentWeek" runat="server" CssClass="status-message" Text="Send Weekly Report Number X"></asp:Label><br />
       <asp:Button ID="btnSendWeeklyReport" runat="server" Text="Send Report to Students" CssClass="button" OnClick="btnSendWeeklyReport_Click" />
        <asp:Label ID="lblSendStatus" runat="server" CssClass="status-message"></asp:Label>
    </div>

 <h3>This Week’s Reports</h3>
    <asp:Label 
    ID="lblCoordinatorGradeError" 
    runat="server" 
    CssClass="error" />

<asp:Repeater ID="rptThisWeekReports" runat="server">
  <HeaderTemplate>
    <div class="report-container">
  </HeaderTemplate>
  <ItemTemplate>
    <div class="report-card">
      <h4>Group <%# Eval("GroupID") %> – Week <%# Eval("Week") %></h4>
      <p><strong>Student:</strong> <%# Eval("StudentEmail") %></p>
      <p><strong>Summary:</strong> <%# Eval("ReportSummary") %></p>
      <p><strong>Student Submitted at::</strong> <%# Eval("SubmittedAt","{0:g}") %></p>

      <!-- Supervisor feedback -->
      <p><strong>Supervisor:</strong> <%# Eval("SupervisorEmail") %></p>
      <p><strong>Grade:</strong> <%# Eval("SupervisorGrade") %></p>
      <p><strong>Comment:</strong> <%# Eval("SupervisorComment") %></p>

      <!-- Coordinator input (only if no FinalDecision yet) -->
      <asp:Panel runat="server" Visible='<%# Eval("FinalDecision") == DBNull.Value %>'>
        <asp:TextBox ID="txtCoordinatorGrade" runat="server" CssClass="input"
                     Placeholder="Your Grade" />
        <asp:Button ID="btnApproveCoordinator" runat="server" 
                    Text="Approve & Save"
                    CommandArgument='<%# Eval("ReportID") %>'
                    OnClick="btnApproveCoordinator_Click"
                    CssClass="button" />
      </asp:Panel>
      <!-- Show final decision when present -->
      <asp:Panel runat="server" Visible='<%# Eval("FinalDecision") != DBNull.Value %>'>
        <p><strong>Final Decision:</strong> <%# Eval("FinalDecision") %></p>
      </asp:Panel>
    </div>
  </ItemTemplate>
  <FooterTemplate>
    </div>
  </FooterTemplate>
</asp:Repeater>

<!-- All Previous Weeks -->
<h3>All Previous Week Reports</h3>
<asp:Repeater ID="rptPreviousWeekReports" runat="server">
  <HeaderTemplate>
    <div class="report-container">
  </HeaderTemplate>
  <ItemTemplate>
    <div class="report-card">
      <h4>Week <%# Eval("Week") %> – Group <%# Eval("GroupID") %></h4>
      <p><strong>Student:</strong> <%# Eval("StudentEmail") %></p>
      <p><strong>Summary:</strong> <%# Eval("ReportSummary") %></p>
      <p><strong>Student Submitted at:</strong> <%# Eval("SubmittedAt","{0:g}") %></p>

      <p><strong>Supervisor Grade:</strong> <%# Eval("SupervisorGrade") %></p>
      <p><strong>Supervisor Comment:</strong> <%# Eval("SupervisorComment") %></p>
      <p><strong>Coordinator Decision:</strong> <%# Eval("FinalDecision") %></p>
    </div>
  </ItemTemplate>
  <FooterTemplate>
    </div>
  </FooterTemplate>
</asp:Repeater>



</div>

        <!-- Create Groups Section -->
        <div id="create-groups" class="container">
            <h2>Create and Manage Student Groups</h2>
            <div>
               <asp:FileUpload ID="csvFileUpload" runat="server" />
               <asp:Button ID="btnUploadCSV" runat="server" Text="Upload CSV" OnClick="btnUploadCSV_Click" />
               <asp:Label ID="lblUploadStatus" runat="server" ForeColor="Green" />
            </div>

            <h3>📊 Group Functionality Overview</h3>
<asp:GridView ID="gvGroupFunctionality" runat="server" CssClass="table" AutoGenerateColumns="False">

    <Columns>
        <asp:BoundField DataField="GroupCode" HeaderText="Group Code" />
        <asp:BoundField DataField="IdeaSubmitted" HeaderText="Idea Submitted" />
        <asp:BoundField DataField="IdeaApproved" HeaderText="Idea Approved" />
        <asp:BoundField DataField="SupervisorAssigned" HeaderText="Supervisor Assigned" />
        <asp:BoundField DataField="CPIS498Status" HeaderText="CPIS 498" />
        <asp:BoundField DataField="CPIS499Status" HeaderText="CPIS 499" />
    </Columns>
</asp:GridView>

            

            <h3>Assigned Students</h3>
            <asp:DropDownList 
    ID="ddlGroupFilter" 
    runat="server" 
    AutoPostBack="true" 
    OnSelectedIndexChanged="ddlGroupFilter_SelectedIndexChanged"
    CssClass="input" 
    Style="width:150px; margin-bottom:10px;">
  <asp:ListItem Text="All Students" Value="All" />
  <asp:ListItem Text="In a Group" Value="InGroup" />
  <asp:ListItem Text="Not in a Group" Value="NoGroup" />
</asp:DropDownList>

           <asp:GridView ID="gvAssignedStudents" runat="server" AutoGenerateColumns="False" CssClass="table">
    <Columns>
      <asp:BoundField DataField="FullName" HeaderText="Student Name" />
<asp:BoundField DataField="StudentEmail" HeaderText="Student Email" />
<asp:BoundField DataField="ProjectTitle" HeaderText="Project Title" />
<asp:BoundField DataField="SupervisorEmail" HeaderText="Supervisor" />
<asp:BoundField DataField="GroupID" HeaderText="Group ID" />
<asp:BoundField DataField="GroupCode" HeaderText="Group Code" />
<asp:BoundField DataField="Teammate" HeaderText="Teammate Email" />

    </Columns>
</asp:GridView>


          
        </div>

        <!-- Grade Students Section -->
        <div id="grade-students" class="container">
            <h2>Grade Student Groups</h2>
            <label for="ddlGroups">Select Group:</label>
           <asp:DropDownList ID="ddlGroups" runat="server" AutoPostBack="true"
    CssClass="input" OnSelectedIndexChanged="ddlGroups_SelectedIndexChanged">
</asp:DropDownList>
<asp:Label ID="Label2" runat="server" CssClass="status-message"></asp:Label>

         <asp:Label ID="Label1" runat="server" CssClass="status-message"></asp:Label>

            
     

      
   
                  <asp:Label 
ID="Label3" 
runat="server" 
CssClass="error" />
            <asp:GridView 
ID="gvStudentGrades" 
runat="server" 
AutoGenerateColumns="False"
CssClass="table" 
DataKeyNames="GradeID" 
Visible="true">

    <Columns>
        <asp:BoundField DataField="FullName" HeaderText="Student Name" />
        <asp:BoundField DataField="Email" HeaderText="Email" />

        <asp:TemplateField HeaderText="Supervisor Evaluation">
            <ItemTemplate>
                <asp:TextBox ID="txtSupervisorEval" runat="server" CssClass="input"
                    Text='<%# Bind("SupervisorEvaluation") %>' />
            </ItemTemplate>
        </asp:TemplateField>

        <asp:TemplateField HeaderText="Examination Committee">
            <ItemTemplate>
                <asp:TextBox ID="txtExamCommittee" runat="server" CssClass="input"
                    Text='<%# Bind("ExaminationCommittee") %>' />
            </ItemTemplate>
        </asp:TemplateField>

        <asp:TemplateField HeaderText="Coordination Committee">
            <ItemTemplate>
                <asp:TextBox ID="txtCoordCommittee" runat="server" CssClass="input"
                    Text='<%# Bind("CoordinationCommittee") %>' />
            </ItemTemplate>
        </asp:TemplateField>

        <asp:TemplateField HeaderText="Final Decision">
            <ItemTemplate>
                <asp:TextBox ID="txtFinalDecision" runat="server" CssClass="input"
                    Text='<%# Bind("FinalDecision") %>' />
            </ItemTemplate>
        </asp:TemplateField>

        <asp:TemplateField HeaderText="SO Attainment">
            <ItemTemplate>
                <asp:TextBox ID="txtSOAttainment" runat="server" CssClass="input"
                    Text='<%# Bind("SOAttainment") %>' />
            </ItemTemplate>
        </asp:TemplateField>

        
        <asp:TemplateField HeaderText="Max Grade">
            <ItemTemplate>
                <asp:TextBox ID="txtMaxGrade" runat="server" CssClass="input"
                    Text='<%# Bind("MaxGrade") %>' />
            </ItemTemplate>
        </asp:TemplateField>

        <asp:TemplateField HeaderText="SOS">
            <ItemTemplate>
                <asp:TextBox ID="txtSOS" runat="server" CssClass="input"
                    Text='<%# Bind("SOS") %>' />
            </ItemTemplate>
        </asp:TemplateField>



        <asp:TemplateField HeaderText="CLO">
            <ItemTemplate>
                <asp:TextBox ID="txtCLO" runat="server" CssClass="input"
                    Text='<%# Bind("CLO") %>' />
            </ItemTemplate>
        </asp:TemplateField>

        <asp:TemplateField HeaderText="Submission Type">
            <ItemTemplate>
                <asp:TextBox ID="txtSubmissionType" runat="server" CssClass="input"
                    Text='<%# Bind("SubmissionType") %>' />
            </ItemTemplate>
        </asp:TemplateField>

        <asp:TemplateField HeaderText="Week Number">
            <ItemTemplate>
                <asp:TextBox ID="txtWeekNumber" runat="server" CssClass="input"
                    Text='<%# Bind("WeekNumber") %>' />
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
</asp:GridView>

            

<br />
<asp:Button ID="btnExportCSV" runat="server" Text="Export Grades to CSV" CssClass="button" OnClick="btnExportCSV_Click" />

             <asp:Literal ID="litRowCount" runat="server" />


<asp:Button ID="btnSubmitGrades" runat="server" Text="Submit Grades"
    CssClass="button" OnClick="btnSubmitGrades_Click" />
            <asp:Label ID="Label4" runat="server" CssClass="error" />
                        <h3>All Group Grades Overview</h3>
<asp:GridView ID="gvAllGroupGrades" runat="server" AutoGenerateColumns="False" CssClass="gridview" GridLines="Horizontal">
    <Columns>
        <asp:BoundField DataField="GroupID" HeaderText="Group ID" />
        <asp:BoundField DataField="FullName" HeaderText="Student Name" />
        <asp:BoundField DataField="Email" HeaderText="Email" />
        <asp:BoundField DataField="MaxGrade" HeaderText="Max Grade" />
        <asp:BoundField DataField="SOS" HeaderText="SO" />
        <asp:BoundField DataField="CLO" HeaderText="CLO" />
        <asp:BoundField DataField="SubmissionType" HeaderText="Submission Type" />
        <asp:BoundField DataField="WeekNumber" HeaderText="Week" />
        <asp:BoundField DataField="SupervisorEvaluation" HeaderText="Supervisor Eval" />
        <asp:BoundField DataField="ExaminationCommittee" HeaderText="Exam Committee" />
        <asp:BoundField DataField="CoordinationCommittee" HeaderText="Coordination Committee" />
        <asp:BoundField DataField="FinalDecision" HeaderText="Final Decision" />
        <asp:BoundField DataField="SOAttainment" HeaderText="SO Attainment" />
    </Columns>
</asp:GridView>
        </div>

        <!-- Help Section -->
        <div id="help" class="container">
            <h2>Help & Support</h2>
            <p>If you need assistance, please contact <a href="mailto:support@kau.edu.sa">support@kau.edu.sa</a></p>
            <p>Phone: <a href="tel:+966123456789">+966123456789</a></p>
        </div>
    </form>

    <script>
        document.querySelectorAll('.nav-link').forEach(function (link) {
            link.addEventListener('click', function (e) {
                e.preventDefault();
                var sectionId = this.getAttribute('data-section');
                document.querySelectorAll('.container').forEach(function (section) {
                    section.classList.remove('active-section');
                    section.style.display = "none";
                });
                var activeSection = document.getElementById(sectionId);
                activeSection.classList.add('active-section');
                activeSection.style.display = "block";
            });
        });
       

        // Ensure the first section is visible on page load.
        document.getElementById('review-supervisor-ideas').style.display = "block";
    </script>
    <script>
        function toggleFullDescription(link) {
            const shortDesc = link.parentNode.querySelector('.desc-preview');
            const fullDesc = link.parentNode.querySelector('.desc-full');

            if (fullDesc.style.display === "none") {
                fullDesc.style.display = "block";
                shortDesc.style.display = "none";
                link.innerText = "Less";
            } else {
                fullDesc.style.display = "none";
                shortDesc.style.display = "block";
                link.innerText = "More";
            }
        }
    </script>

</body>
</html>