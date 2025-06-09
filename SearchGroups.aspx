<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SearchGroups.aspx.cs" Inherits="SeniorProjectHub3.SearchGroups" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Find Students for Groups</title>
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
        }
        .search-bar {
            width: 100%;
            padding: 10px;
            margin: 10px 0;
            border: 1px solid #ccc;
            border-radius: 5px;
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
        .button {
            background-color: #004d00; 
            border: none;
            color: white;
            padding: 10px 15px;
            cursor: pointer;
            border-radius: 5px;
        }
        .button:hover {
            background-color: #00cc33;
        }
      .pending-button {
    background-color: gray !important;
    color: white !important;
    cursor: not-allowed !important;
    border: none !important;
}

    </style>
</head>
<body>
    <form id="form1" runat="server">

       
       <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" CssClass="table" DataKeyNames="IdeaID">
    <Columns>
        <asp:TemplateField HeaderText="Select">
            <ItemTemplate>
                <asp:CheckBox ID="chkSelectIdea" runat="server" />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField DataField="IdeaTitle" HeaderText="Idea Title" />
        <asp:BoundField DataField="IdeaDescription" HeaderText="Description" />
    </Columns>
</asp:GridView>


        
<asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="False" CssClass="table" DataKeyNames="RequestID">
    <Columns>
        <asp:BoundField DataField="RequestedStudentEmail" HeaderText="Requested Student" />
        <asp:BoundField DataField="Status" HeaderText="Status" />
    </Columns>
</asp:GridView>


<asp:GridView ID="GridView3" runat="server" AutoGenerateColumns="False" CssClass="table" DataKeyNames="RequestID">
    <Columns>
        <asp:BoundField DataField="ApplicantEmail" HeaderText="Applicant" />
        <asp:BoundField DataField="Status" HeaderText="Status" />
        <asp:TemplateField HeaderText="Action">
            <ItemTemplate>
                <asp:Button ID="btnAcceptGroupRequest" runat="server" Text="Accept" CssClass="button"
                    CommandArgument='<%# Eval("RequestID") %>' OnClick="btnAcceptGroupRequest_Click" />
                <asp:Button ID="btnRejectGroupRequest" runat="server" Text="Reject" CssClass="button"
                    CommandArgument='<%# Eval("RequestID") %>' OnClick="btnRejectGroupRequest_Click" />
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
</asp:GridView>

      
       <asp:GridView ID="gvApprovedIdeas" runat="server" AutoGenerateColumns="False" CssClass="table" DataKeyNames="IdeaID">
    <Columns>
        <asp:TemplateField HeaderText="Select">
            <ItemTemplate>
                <asp:CheckBox ID="chkSelectIdea" runat="server" />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField DataField="IdeaTitle" HeaderText="Idea Title" />
        <asp:BoundField DataField="IdeaDescription" HeaderText="Description" />
    </Columns>
</asp:GridView>


        
<asp:GridView ID="gvMyGroupApplications" runat="server" AutoGenerateColumns="False" CssClass="table" DataKeyNames="RequestID">
    <Columns>
        <asp:BoundField DataField="RequestedStudentEmail" HeaderText="Requested Student" />
        <asp:BoundField DataField="Status" HeaderText="Status" />
    </Columns>
</asp:GridView>


<asp:GridView ID="gvGroupRequests" runat="server" AutoGenerateColumns="False" CssClass="table" DataKeyNames="RequestID">
    <Columns>
        <asp:BoundField DataField="ApplicantEmail" HeaderText="Applicant" />
        <asp:BoundField DataField="Status" HeaderText="Status" />
        <asp:TemplateField HeaderText="Action">
            <ItemTemplate>
                <asp:Button ID="btnAcceptGroupRequest" runat="server" Text="Accept" CssClass="button"
                    CommandArgument='<%# Eval("RequestID") %>' OnClick="btnAcceptGroupRequest_Click" />
                <asp:Button ID="btnRejectGroupRequest" runat="server" Text="Reject" CssClass="button"
                    CommandArgument='<%# Eval("RequestID") %>' OnClick="btnRejectGroupRequest_Click" />
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
</asp:GridView>


        <!-- Button Post Idea to Group List -->
        
        <asp:Label ID="Label1" runat="server" CssClass="error"></asp:Label>

        <div class="container">
            <h2>Find Students for Groups</h2>
            
            <!-- Search Bar -->
            <asp:TextBox ID="txtSearch" runat="server" CssClass="search-bar" Placeholder="Search by Name..."></asp:TextBox>
            <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="button" OnClick="btnSearch_Click" />

            <!-- Filter by Interests -->
<asp:DropDownList ID="ddlInterestFilter" runat="server" CssClass="search-bar">
    <asp:ListItem Text="All Interests" Value="" />
    <asp:ListItem Text="AI" Value="AI" />
    <asp:ListItem Text="Database" Value="Database" />
    <asp:ListItem Text="Networking" Value="Networking" />
    <asp:ListItem Text="Web Development" Value="Web Development" />
    <asp:ListItem Text="Machine Learning" Value="Machine Learning" />
     <asp:ListItem Text="Other" Value="Other" />
</asp:DropDownList>


          <asp:GridView ID="gvAvailableStudents" runat="server" AutoGenerateColumns="False" CssClass="table" DataKeyNames="StudentEmail">
    <Columns>
       <asp:BoundField DataField="StudentFullName" HeaderText="Student Name" />
<asp:BoundField DataField="StudentEmail" HeaderText="Student Email" />
<asp:BoundField DataField="Interests" HeaderText="Interests" />



        <asp:TemplateField HeaderText="Action">
            <ItemTemplate>
                <asp:Button ID="btnApply" runat="server" Text="Apply" CssClass="button"
                    OnClick="btnApply_Click" CommandArgument='<%# Eval("StudentEmail") %>' />
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
</asp:GridView>


            <h3>My Applications</h3>
<asp:GridView ID="gvMyApplications" runat="server" AutoGenerateColumns="False" CssClass="table">
    <Columns>
        <asp:BoundField DataField="RequestedStudentEmail" HeaderText="Applied To" />
        <asp:BoundField DataField="Status" HeaderText="Status" />
    </Columns>
</asp:GridView>



            <asp:DropDownList ID="ddlAvailableStudents" runat="server" CssClass="input"></asp:DropDownList>

            <!-- GridView for Listed Ideas -->
<asp:GridView ID="gvListedIdeas" runat="server" AutoGenerateColumns="False" CssClass="table">
    <Columns>
        <asp:BoundField DataField="IdeaTitle" HeaderText="Idea Title" />
        <asp:BoundField DataField="StudentFullName" HeaderText="Student Name" />
        <asp:BoundField DataField="StudentEmail" HeaderText="Student Email" />
        <asp:BoundField DataField="Status" HeaderText="Status" />
        <asp:TemplateField HeaderText="Action">
            <ItemTemplate>
                <asp:Button ID="btnRequest" runat="server" Text="Request" CssClass="button"
                    OnClick="btnRequest_Click" CommandArgument='<%# Eval("RequestID") %>' />
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
</asp:GridView>
            <asp:Button ID="btnBackToPortal" runat="server" Text="Back to Student Portal" CssClass="button" OnClick="btnBackToPortal_Click" />




           
            <asp:Label ID="lblStatus" runat="server" ForeColor="Red"></asp:Label>
        </div>
    </form>
</body>
</html>