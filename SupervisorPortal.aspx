<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SupervisorPortal.aspx.cs" Inherits="SeniorProjectHub3.SupervisorPortal" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Supervisor Portal - FCIT KAU</title>
  <style>
    body {
      font-family: Arial, sans-serif;
      background: url("https://smapse.com/storage/2018/09/converted/825_585_4671-king-abdulaziz-university-kau-health-sciences-center-and-university-campus-project-4674.jpg") no-repeat center center fixed;
      background-size: cover;
      margin: 0;
      min-height: 100vh;
    }
     .nav-menu {
     background-color: #006400; /* Dark Green */

     padding: 20px 0;
     display: flex;
     justify-content: center;
     gap: 30px;
     box-shadow: 0 2px 5px rgba(0,0,0,0.1);
 }
     
/* your existing styles... */
.error {
    color: red;
    font-weight: bold;
    margin-top: 10px;
    display: block;
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
      background-color: #004d00; /* Darker Green */
      color: white;
    }
    .input {
      width: 100%;
      padding: 10px;
      margin: 8px 0;
      border: 1px solid #ccc;
      border-radius: 4px;
    }
    .summary-clip {
  max-height: 60px;
  overflow: hidden;
  word-wrap: break-word;
  white-space: pre-wrap;
  transition: max-height 0.3s ease;
}
    button, .button {
      padding: 10px 15px;
      background-color: #004d00; /* Darker Green */
      border: none;
      border-radius: 4px;
      color: #fff;
      cursor: pointer;
    }
    button:hover, .button:hover {
      background-color: #00cc33;
    }
  </style>
</head>
<body>
  <form id="form1" runat="server">
      <center>
   <img src="https://fcitweb.kau.edu.sa/fcitwebsite/images/white-logo.png" alt="FCIT Logo" class="logo" width="1000" 
height="300"/>
       </center>
    <!-- Navigation Menu -->
    <nav class="nav-menu">
      <a href="#" class="nav-link" data-section="profile">Profile</a>
      <a href="#" class="nav-link" data-section="view-student-ideas">View Student Ideas</a>
      <a href="#" class="nav-link" data-section="post-idea">Post Idea</a>
      <a href="#" class="nav-link" data-section="weekly-reports">Weekly Reports</a>
      <a href="#" class="nav-link" data-section="student-applications">Student Applications</a>
      <a href="#" class="nav-link" data-section="my-applications">My Applications</a>
     <a href="#" class="nav-link" data-section="my-ideas">My Ideas</a>
<a href="#" class="nav-link" data-section="my-groups">My Groups</a>


      <a href="#" class="nav-link" data-section="help">Help</a>
    </nav>

    <!-- Supervisor Profile Section (with Logout button) -->
    <div class="container content-section active-section" id="profile">
      <h2>Supervisor Profile</h2>
      <div><strong>Name:</strong> <asp:Label ID="lblName" runat="server" /></div>
      <div><strong>Email:</strong> <asp:Label ID="lblEmail" runat="server" /></div>
      <div><strong>Office:</strong> <asp:Label ID="lblOffice" runat="server" /></div>
      <div><strong>Specialization:</strong> <asp:Label ID="lblSpecialization" runat="server" /></div>
      <br />
      <asp:Button ID="btnLogout" runat="server" Text="Logout" CssClass="button" OnClick="btnLogout_Click" />
    </div>

    <!-- View Student Ideas Section -->
    <!-- View Student Ideas Section -->
<div class="container content-section" id="view-student-ideas">
    <h2>Approved Student Ideas</h2>

    <!-- Filter Dropdown -->
    <asp:DropDownList ID="ddlIdeaType" runat="server" CssClass="input">
        <asp:ListItem Text="All Types" Value="" />
        <asp:ListItem Text="Database" Value="Database" />
        <asp:ListItem Text="Machine Learning" Value="Machine Learning" />
        <asp:ListItem Text="Web Development" Value="Web Development" />
        <asp:ListItem Text="AI" Value="AI" />
        <asp:ListItem Text="Networking" Value="Networking" />
         <asp:ListItem Text="Other" Value="Other" />
    </asp:DropDownList>
    <asp:Button ID="btnFilterIdeas" runat="server" Text="Filter" CssClass="button" OnClick="btnFilterIdeas_Click" />

    <!-- GridView -->
    <asp:GridView ID="gvStudentIdeas" runat="server" AutoGenerateColumns="False" CssClass="table" DataKeyNames="IdeaID">
        <Columns>
            <asp:BoundField DataField="IdeaTitle" HeaderText="Idea Title" />
            <asp:BoundField DataField="StudentEmail" HeaderText="Student Email" />
            <asp:BoundField DataField="IdeaType" HeaderText="Idea Type" />
            <asp:TemplateField HeaderText="Description">
    <ItemTemplate>
        <div class="desc-preview" style="max-height: 60px; overflow: hidden;">
            <asp:Label ID="lblDesc" runat="server"
                Text='<%# FormatWithBreaks(Eval("IdeaDescription").ToString(), 50) %>' />
        </div>
        <asp:LinkButton ID="btnToggleDesc" runat="server" Text="More"
            OnClientClick="toggleDescription(this); return false;" />
    </ItemTemplate>
</asp:TemplateField>
            <asp:TemplateField HeaderText="Action">
                <ItemTemplate>
                    <asp:Button ID="btnApply" runat="server" Text="Apply" CssClass="button"
                        OnClick="btnApply_Click" CommandArgument='<%# Eval("IdeaID") %>' />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</div>


    <!-- Post Idea Section -->
<div class="container content-section" id="post-idea">
  <h2>Post New Project Idea</h2>

  <asp:TextBox ID="txtProjectTitle" runat="server" CssClass="input" Placeholder="Project Title"></asp:TextBox>
  <asp:TextBox ID="txtProjectDescription" runat="server" CssClass="input" Placeholder="Project Description" TextMode="MultiLine"></asp:TextBox>

  <!-- Dropdown for selecting the idea type -->
  <asp:DropDownList ID="ddlSupervisorIdeaType" runat="server" CssClass="input">
    <asp:ListItem Text="Select Idea Type" Value="" />
    <asp:ListItem Text="Database" Value="Database" />
    <asp:ListItem Text="Machine Learning" Value="Machine Learning" />
    <asp:ListItem Text="Web Development" Value="Web Development" />
    <asp:ListItem Text="AI" Value="AI" />
    <asp:ListItem Text="Networking" Value="Networking" />
          <asp:ListItem Text="Other" Value="Other" />
  </asp:DropDownList>

  <asp:Button ID="btnPostIdea" runat="server" CssClass="button" Text="Post Idea" OnClick="btnPostIdea_Click" />
</div>


  <!-- Weekly Reports Section -->
<div class="container content-section" id="weekly-reports">
    <h2>Weekly Reports from Students</h2>
    <asp:Label 
    ID="lblSupervisorGradeError" 
    runat="server" 
    CssClass="error" />


    <asp:GridView ID="gvWeeklyReports" runat="server" AutoGenerateColumns="False" CssClass="table">
        <Columns>
            <asp:BoundField DataField="StudentEmail" HeaderText="Student Email" />
            <asp:BoundField DataField="GroupID" HeaderText="Group ID" />
            <asp:BoundField DataField="Week" HeaderText="Week" />
         
<asp:TemplateField HeaderText="Summary">
  <ItemTemplate>
    <div class="summary-clip">
      <%# FormatWithBreaks(Eval("ReportSummary").ToString(), 50) %>
    </div>
    <asp:LinkButton
      ID="lnkToggleSummary"
      runat="server"
      Text="More"
      OnClientClick="toggleSummary(this); return false;"
      Visible='<%# (Eval("ReportSummary") ?? "").ToString().Length > 60 %>'
      Style="margin-left:6px;" />
  </ItemTemplate>
</asp:TemplateField>



            <asp:BoundField DataField="SubmittedAt" HeaderText="Submitted On" DataFormatString="{0:MM/dd/yyyy hh:mm tt}" />

            <asp:TemplateField HeaderText="Supervisor Comment">
                <ItemTemplate>
                    <asp:TextBox ID="txtComment" runat="server" CssClass="input" Text='<%# Eval("SupervisorComment") %>' />
                </ItemTemplate>
            </asp:TemplateField>

            <asp:TemplateField HeaderText="Grade">
                <ItemTemplate>
                    <asp:TextBox ID="txtGrade" runat="server" CssClass="input" Text='<%# Eval("SupervisorGrade") %>' />
                </ItemTemplate>
            </asp:TemplateField>

            <asp:TemplateField HeaderText="Action">
                <ItemTemplate>
                    <asp:Button ID="btnSubmitFeedback" runat="server" Text="Submit" CssClass="button"
                        CommandArgument='<%# Eval("ReportID") %>' OnClick="btnSubmitFeedback_Click" />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
    <br />
<asp:Label ID="lblSubmittedReportsHeader" runat="server" Text="Submitted Reports:" Font-Bold="true" Font-Size="Large" />
<asp:GridView ID="gvSubmittedReports" runat="server" AutoGenerateColumns="False" CssClass="table">
    <Columns>
        <asp:BoundField DataField="StudentEmail" HeaderText="Student Email" />
        <asp:BoundField DataField="GroupID" HeaderText="Group ID" />
        <asp:BoundField DataField="Week" HeaderText="Week Number" />
<asp:TemplateField HeaderText="Summary">
  <ItemTemplate>
    <div class="summary-clip">
      <%# FormatWithBreaks(Eval("ReportSummary").ToString(), 50) %>
    </div>
    <asp:LinkButton
      ID="lnkToggleSummary"
      runat="server"
      Text="More"
      OnClientClick="toggleSummary(this); return false;"
      Visible='<%# (Eval("ReportSummary") ?? "").ToString().Length > 60 %>'
      Style="margin-left:6px;" />
  </ItemTemplate>
</asp:TemplateField>


        <asp:BoundField DataField="SupervisorComment" HeaderText="Supervisor Comment" />
        <asp:BoundField DataField="SupervisorGrade" HeaderText="Grade" />
        <asp:BoundField DataField="SupervisorSubmittedAt" HeaderText="Graded At" DataFormatString="{0:MM/dd/yyyy hh:mm tt}" />
    </Columns>
</asp:GridView>

</div>



    <!-- Student Applications Section (only pending applications) -->
    <div class="container content-section" id="student-applications">
      <h2>Student Applications for Your Projects</h2>
      <asp:GridView ID="gvStudentApplications" runat="server" AutoGenerateColumns="False" CssClass="table" DataKeyNames="ApplicationID">
        <Columns>
          <asp:BoundField DataField="ProjectTitle" HeaderText="Project Title" />
          <asp:BoundField DataField="StudentEmail" HeaderText="Student Email" />
          <asp:BoundField DataField="Status" HeaderText="Application Status" />
          <asp:TemplateField HeaderText="Action">
            <ItemTemplate>
              <asp:Button ID="btnAcceptStudent" runat="server" Text="Accept" CssClass="button" OnClick="btnAcceptStudent_Click" CommandArgument='<%# Eval("ApplicationID") %>' />
              <asp:Button ID="btnRejectStudent" runat="server" Text="Reject" CssClass="button" OnClick="btnRejectStudent_Click" CommandArgument='<%# Eval("ApplicationID") %>' />
            </ItemTemplate>
          </asp:TemplateField>
        </Columns>
      </asp:GridView>



    </div>

    <!-- My Applications Section (Supervisor's own applications to student ideas) -->
    <div class="container content-section" id="my-applications">
      <h2>My Applications</h2>
      <asp:GridView ID="gvMyApplications" runat="server" AutoGenerateColumns="False" CssClass="table" DataKeyNames="ApplicationID">
        <Columns>
          <asp:BoundField DataField="IdeaTitle" HeaderText="Idea Title" />
          <asp:BoundField DataField="StudentEmail" HeaderText="Student Email" />
          <asp:BoundField DataField="Status" HeaderText="Application Status" />
        </Columns>
      </asp:GridView>
    </div>

    <!-- My Ideas Section -->
    <div class="container content-section" id="my-ideas">
      <h2>My Ideas</h2>
      <asp:GridView ID="gvMyIdeas" runat="server" AutoGenerateColumns="False" CssClass="table" DataKeyNames="ProjectID">
        <Columns>
          <asp:BoundField DataField="ProjectTitle" HeaderText="Title" />
          <asp:BoundField DataField="ProjectDescription" HeaderText="Description" />
          <asp:BoundField DataField="CreatedAt" HeaderText="Posted On" DataFormatString="{0:MM/dd/yyyy}" />
          <asp:BoundField DataField="AssignedStudent" HeaderText="Assigned Student" />
        </Columns>
      </asp:GridView>
    </div>

      <div class="container content-section" id="my-groups">
      <h2>My Supervised Groups and Ideas</h2>
<asp:GridView ID="gvGroupIdeas" runat="server" CssClass="table" AutoGenerateColumns="False">
        <Columns>
          <asp:BoundField DataField="GroupCode" HeaderText="Group Code" />
          <asp:BoundField DataField="Student1Email" HeaderText="Student 1" />
          <asp:BoundField DataField="Student2Email" HeaderText="Student 2" />
          <asp:BoundField DataField="IdeaTitle" HeaderText="Idea Title" />
          <asp:BoundField DataField="IdeaType"        HeaderText="Idea Type" />
          <asp:BoundField DataField="Status" HeaderText="Idea Status" />
        </Columns>
      </asp:GridView>
          </div>

    <!-- Help Section -->
    <div class="container content-section" id="help">
      <h2>Support & Help</h2>
      <p>If you need assistance, please contact <a href="mailto:support@kau.edu.sa">support@kau.edu.sa</a></p>
<p>Phone: <a href="tel:+966123456789">+966123456789</a></p>
    </div>
  </form>

  <script>
      // Navigation toggling logic
      document.querySelectorAll('.nav-link').forEach(function (link) {
          link.addEventListener('click', function (e) {
              e.preventDefault();
              var sectionId = this.getAttribute('data-section');
              document.querySelectorAll('.content-section').forEach(function (section) {
                  section.classList.remove('active-section');
                  section.style.display = "none";
              });
              var activeSection = document.getElementById(sectionId);
              activeSection.classList.add('active-section');
              activeSection.style.display = "block";
          });
      });
      // Show profile section on initial load.
      document.getElementById('profile').style.display = "block";
  </script>
    <script>
        function toggleDescription(link) {
            var container = link.previousElementSibling;
            if (container.style.maxHeight === "60px") {
                container.style.maxHeight = "none";
                link.innerText = "Less";
            } else {
                container.style.maxHeight = "60px";
                link.innerText = "More";
            }
        }
    </script>
<script>
    function toggleSummary(link) {
        var clip = link.parentNode.querySelector('.summary-clip');
        if (link.innerText === 'More') {
            // expand to full height
            clip.style.maxHeight = clip.scrollHeight + 'px';
            link.innerText = 'Less';
        } else {
            // collapse back
            clip.style.maxHeight = '60px';
            link.innerText = 'More';
        }
    }
</script>


</body>
</html>