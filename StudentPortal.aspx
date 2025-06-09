<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="StudentPortal.aspx.cs" Inherits="SeniorProjectHub3.StudentPortal" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>FCIT KAU Student Portal</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background: url("https://smapse.com/storage/2018/09/converted/825_585_4671-king-abdulaziz-university-kau-health-sciences-center-and-university-campus-project-4674.jpg") no-repeat center center fixed;
            background-size: cover;
            margin: 0;
            min-height: 100vh;
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
        .nav-menu {
            background-color: #006400; 

            padding: 20px 0;
            display: flex;
            justify-content: center;
            gap: 30px;
            box-shadow: 0 2px 5px rgba(0,0,0,0.1);
        }
        .pending-button {
    background-color: gray !important;
    cursor: not-allowed;
}
        .report-container {
  display: flex;
  flex-wrap: wrap;
  margin: 0 -8px;
}
.report-card {
  box-sizing: border-box;
  flex: 0 0 calc(33.333% - 16px);
  margin: 8px;
  background: #fff;
  padding: 12px;
  border-radius: 6px;
  box-shadow: 0 0 5px rgba(0,0,0,0.1);
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
        .info {
            text-align: left;
            margin-bottom: 10px;
        }
        .label {
    font-weight: bold;
    color: #006400; 
}

        .value {
    color: #222; 
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
        .input {
            width: 100%;
            padding: 10px;
            margin: 5px 0 15px;
            border: 1px solid #ccc;
            border-radius: 5px;
        }
        .button {
            background-color: #004d00; 

            border: none;
            color: #fff;
            padding: 10px 20px;
            margin: 5px 0;
            border-radius: 5px;
            cursor: pointer;
        }
        .button:hover {
            background-color: #00cc33;
        }
        .error {
            color: red;
            font-weight: bold;
        }
        .success {
            color: green;
            font-weight: bold;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <!-- Logo -->
        <center>
        <img src="https://fcitweb.kau.edu.sa/fcitwebsite/images/white-logo.png" alt="FCIT Logo" class="logo" width="1000" 
     height="300"/>
            </center>

        <!-- Navigation Menu -->
        <nav class="nav-menu">
            <a href="#" class="nav-link" data-section="profile">Profile</a>
            <a href="#" class="nav-link" data-section="submit-idea">Submit Idea</a>
            <a href="#" class="nav-link" data-section="project-ideas">Project Ideas</a>
            <a href="#" class="nav-link" data-section="weekly-report">Weekly Reports</a>
        

       
            <a href="#" class="nav-link" data-section="submission-status">Submission Status</a>
            <a href="#" class="nav-link" data-section="supervisor-applications">Supervisor Applications</a>
            <a href="#" class="nav-link" data-section="group-requests">Requests to Your Group</a>
            <a href="#" class="nav-link" data-section="my-applications">My Applications</a>
            <a href="#" class="nav-link" data-section="help">Help</a>
        </nav>

        <!-- Profile Section -->
        <div class="container content-section active-section" id="profile">
            <h2>Student Profile</h2>
            <div class="info">
                <span class="label">Name:</span>
                <asp:Label ID="lblName" runat="server" CssClass="value"></asp:Label>
            </div>
            <div class="info">
                <span class="label">Email:</span>
                <asp:Label ID="lblEmail" runat="server" CssClass="value"></asp:Label>
            </div>
            <div class="info">
                <span class="label">Phone:</span>
                <asp:Label ID="lblPhone" runat="server" CssClass="value"></asp:Label>
            </div>
            <div class="info">
                <span class="label">Major:</span>
                <asp:Label ID="lblMajor" runat="server" CssClass="value"></asp:Label>
            </div>
            <div class="info">
    <span class="label">Interests:</span>
    <asp:Label ID="lblInterests" runat="server" CssClass="value"></asp:Label>
</div>
            <div class="info">
                <span class="label">Idea:</span>
                <asp:Label ID="lblIdea" runat="server" CssClass="value"></asp:Label>

            </div>
            


     <div class="info">
    <span class="label">Group Code:</span>
    <asp:Label ID="lblGroupCode" runat="server" CssClass="value"></asp:Label>
</div>


            <div>

            <!-- Group Section -->

<div id="group-info-container">
    <h2>Group Information</h2>
    <div>
        <strong>Supervisor:</strong>
        <asp:Label ID="lblSupervisor" runat="server" />
    </div>
    <div>
        <strong>Teammate:</strong>
        <asp:Label ID="lblTeammate" runat="server" />
    </div>
</div>


    <br />
    <asp:Button ID="btnFindGroup" runat="server" Text="Find a Group" CssClass="button" OnClick="btnFindGroup_Click" />
        



            <br />
<asp:Button ID="btnLogout" runat="server" Text="Logout" CssClass="button" OnClick="btnLogout_Click" />

        </div>
<!-- Weekly Report Section -->
            </div>
<div class="container content-section" id="weekly-report">
 <h2>Weekly Report
    <asp:Label ID="txtWeekNumber" runat="server" CssClass="input" ReadOnly="true" Visible="true" />
 </h2>
<br />
<asp:TextBox ID="TextBox1" runat="server" CssClass="input" ReadOnly="true" Visible="false" />

<asp:Label ID="lblWeekInfo" runat="server" CssClass="label" Visible="false" />






  

    <asp:Label ID="lblReportPrompt" runat="server" Text="Enter your weekly summary below:" CssClass="label"></asp:Label><br />
    <asp:TextBox ID="txtReportSummary" runat="server" CssClass="input" TextMode="MultiLine" Rows="6"></asp:TextBox><br />

    <asp:Button ID="btnSubmitReport" runat="server" Text="Submit Report" CssClass="button" OnClick="btnSubmitReport_Click" />
    <asp:Label ID="lblReportStatus" runat="server" CssClass="status-message"></asp:Label>

    <hr />
    <h3>My Submitted Reports</h3>
  <asp:GridView ID="gvSubmittedReports" runat="server" AutoGenerateColumns="False" CssClass="table">
    <Columns>
        <asp:BoundField DataField="Week" HeaderText="Week" />
        <asp:BoundField DataField="ReportSummary" HeaderText="Summary" />
        <asp:BoundField DataField="SubmittedAt" HeaderText="Submitted At" />
        <asp:BoundField DataField="SupervisorComment" HeaderText="Supervisor Comment" />
        <asp:BoundField DataField="SupervisorSubmittedAt" HeaderText="Reviewed At" />
        <asp:BoundField DataField="SupervisorGrade" HeaderText="Grade" />
    </Columns>
</asp:GridView>
</div>



       <!-- Submit Idea Section -->
<div class="container content-section" id="submit-idea">
    <h2>Submit a New Idea</h2>

    <asp:TextBox ID="txtIdeaTitle" runat="server" CssClass="input" Placeholder="Project Title"></asp:TextBox>
    <asp:TextBox ID="txtIdeaDescription" runat="server" CssClass="input" Placeholder="Project Description" TextMode="MultiLine"></asp:TextBox>

    <!--  New Dropdown for Idea Type -->
    <asp:DropDownList ID="ddlIdeaType" runat="server" CssClass="input">
        <asp:ListItem Text="Select Idea Type" Value="" />
        <asp:ListItem Text="Database" Value="Database" />
        <asp:ListItem Text="Machine Learning" Value="Machine Learning" />
        <asp:ListItem Text="Web Development" Value="Web Development" />
        <asp:ListItem Text="AI" Value="AI" />
        <asp:ListItem Text="Networking" Value="Networking" />
        <asp:ListItem Text="Other" Value="Other" />
    </asp:DropDownList>

    <asp:Button ID="btnSubmitIdea" runat="server" Text="Submit Idea" CssClass="button" OnClick="btnSubmitIdea_Click" />
    <asp:Label ID="lblIdeaStatus" runat="server" CssClass="error"></asp:Label>

 
</div>


        <!-- Project Ideas Section -->
        <div class="container content-section" id="project-ideas">
            <h2>Available Project Ideas from Supervisors</h2>
            <asp:DropDownList ID="ddlSupervisorIdeaType" runat="server" AutoPostBack="true" CssClass="input" OnSelectedIndexChanged="ddlSupervisorIdeaType_SelectedIndexChanged">
    <asp:ListItem Text="All Types" Value="" />
    <asp:ListItem Text="Database" Value="Database" />
    <asp:ListItem Text="Machine Learning" Value="Machine Learning" />
    <asp:ListItem Text="Web Development" Value="Web Development" />
    <asp:ListItem Text="AI" Value="AI" />
    <asp:ListItem Text="Networking" Value="Networking" />
                <asp:ListItem Text="Other" Value="Other" />
</asp:DropDownList>
            <asp:GridView ID="gvProjectIdeas" runat="server" AutoGenerateColumns="False" CssClass="table" DataKeyNames="ProjectID">
                <Columns>
                    <asp:BoundField DataField="ProjectTitle" HeaderText="Project Title" />
                    <asp:BoundField DataField="SupervisorEmail" HeaderText="Supervisor Email" />
                    <asp:TemplateField HeaderText="Description">
    <ItemTemplate>
        <div style="max-height: 60px; overflow: hidden;" class="desc-preview">
            <asp:Label ID="lblShortDesc" runat="server"
                Text='<%# FormatDescription(Eval("ProjectDescription").ToString(), 50) %>'
                EnableViewState="false" />
        </div>
        <asp:LinkButton ID="btnToggleDesc" runat="server" Text="More" OnClientClick="toggleDescription(this); return false;" />
    </ItemTemplate>
</asp:TemplateField>
                    <asp:BoundField DataField="IdeaType" HeaderText="Idea Type" /> 
                    <asp:TemplateField HeaderText="Action">
                        <ItemTemplate>
                            <!-- Apply button -->
                            <asp:Button ID="btnApplyToSupervisor" runat="server" Text="Apply" CssClass="button"
                                OnClick="btnApplyToSupervisor_Click" CommandArgument='<%# Eval("ProjectID") %>' />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>

        <!-- Submission Status Section -->
        <div class="container content-section" id="submission-status">
            <h2>Your Idea Submissions</h2>
            <asp:GridView ID="gvSubmissionStatus" runat="server" AutoGenerateColumns="False" CssClass="table">
    <Columns>
        <asp:BoundField DataField="IdeaTitle" HeaderText="Idea Title" />
        <asp:BoundField DataField="IdeaDescription" HeaderText="Description" />
        <asp:BoundField DataField="Status" HeaderText="Approval Status" />
        <asp:BoundField DataField="AssignedSupervisor" HeaderText="Assigned Supervisor" />
        <asp:BoundField DataField="SubmittedBy" HeaderText="Submitted By" />
    </Columns>
</asp:GridView>

        </div>

        <!-- Supervisor Applications Section -->
        <div class="container content-section" id="supervisor-applications">
            <h2>Supervisor Applications for Your Ideas</h2>
            <asp:GridView ID="gvSupervisorApplications" runat="server" AutoGenerateColumns="False" CssClass="table">
                <Columns>
                    <asp:BoundField DataField="IdeaTitle" HeaderText="Idea Title" />
                    <asp:BoundField DataField="SupervisorEmail" HeaderText="Supervisor Email" />
                    <asp:TemplateField HeaderText="Action">
                        <ItemTemplate>

                            <asp:Button ID="btnAccept" runat="server" Text="Accept" CssClass="button" OnClick="btnAccept_Click" CommandArgument='<%# Eval("ApplicationID") %>' />
                            <asp:Button ID="btnReject" runat="server" Text="Reject" CssClass="button" OnClick="btnReject_Click" CommandArgument='<%# Eval("ApplicationID") %>' />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>

        <div class="container content-section" id="group-requests">
    
    <asp:GridView ID="gvGroupRequests" runat="server" AutoGenerateColumns="False" CssClass="table"
    OnRowDataBound="gvGroupRequests_RowDataBound">

        <Columns>
           <asp:BoundField DataField="ApplicantEmail" HeaderText="Applicant Email" />

            <asp:BoundField DataField="Status" HeaderText="Request Status" />
            <asp:TemplateField HeaderText="Action">
                <ItemTemplate>
                    <asp:Button ID="btnAcceptRequest" runat="server" Text="Accept" CssClass="button" 
                        OnClick="btnAcceptRequest_Click" CommandArgument='<%# Eval("RequestID") %>' />
                    <asp:Button ID="btnRejectRequest" runat="server" Text="Reject" CssClass="button" 
                        OnClick="btnRejectRequest_Click" CommandArgument='<%# Eval("RequestID") %>' />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
            <asp:Label ID="lblStatus" runat="server" CssClass="success"></asp:Label>

</div>


        <!-- My Applications Section (Student's Applications to Supervisor Projects) -->
        <div class="container content-section" id="my-applications">
            <h2>My Applications to Supervisor Projects</h2>
            <asp:GridView ID="gvStudentApplications" runat="server" AutoGenerateColumns="False" CssClass="table">
                <Columns>
                    <asp:BoundField DataField="ProjectTitle" HeaderText="Project Title" />
                    <asp:BoundField DataField="SupervisorEmail" HeaderText="Supervisor Email" />
                    <asp:BoundField DataField="Status" HeaderText="Application Status" />
                </Columns>
            </asp:GridView>
            <h3>Applied to Group Request Status</h3>
<asp:GridView ID="gvAppliedToGroupRequests" runat="server" AutoGenerateColumns="False" CssClass="table">
    <Columns>
        <asp:BoundField DataField="StudentFullName" HeaderText="Student Name" />
        <asp:BoundField DataField="StudentEmail" HeaderText="Student Email" />
        <asp:BoundField DataField="Status" HeaderText="Status" />
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

        document.addEventListener("DOMContentLoaded", function () {
            // Initially hide the group info section
            document.getElementById("group-info-container").style.display = "none";

            document.querySelectorAll('.nav-link').forEach(function (link) {
                link.addEventListener('click', function (e) {
                    e.preventDefault();
                    var sectionId = this.getAttribute('data-section');

                    // Hide all sections
                    document.querySelectorAll('.content-section').forEach(function (section) {
                        section.style.display = "none";
                    });

                    // Show the selected section
                    var activeSection = document.getElementById(sectionId);
                    if (activeSection) {
                        activeSection.style.display = "block";
                    }

                    // Show "Group Information" ONLY if Profile is selected
                    if (sectionId === "profile") {
                        document.getElementById("group-info-container").style.display = "block";
                    } else {
                        document.getElementById("group-info-container").style.display = "none";
                    }
                });
            });

            // Ensure Profile section is visible on page load
            document.getElementById("profile").style.display = "block";
            document.getElementById("group-info-container").style.display = "block"; // Show Group Info on initial load
        });

    </script>
    <script type="text/javascript">
        function toggleDescription(link) {
            var previewDiv = link.previousElementSibling;
            if (previewDiv.style.maxHeight === "60px") {
                previewDiv.style.maxHeight = "none";
                link.innerText = "Less";
            } else {
                previewDiv.style.maxHeight = "60px";
                link.innerText = "More";
            }
        }
    </script>
    <script type="text/javascript">
        function toggleDescription(link) {
            var previewDiv = link.previousElementSibling;
            if (previewDiv.style.maxHeight === "60px") {
                previewDiv.style.maxHeight = "none";
                link.innerText = "Less";
            } else {
                previewDiv.style.maxHeight = "60px";
                link.innerText = "More";
            }
        }
    </script>

</body>
</html>