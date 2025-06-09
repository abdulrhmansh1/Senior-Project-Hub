<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="HODPortal.aspx.cs" Inherits="SeniorProjectHub3.HODPortal" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8" />
    <title>HOD Portal - Senior Project Hub</title>
    <style>
        body {
            font-family: 'Segoe UI', sans-serif;
             background: url("https://smapse.com/storage/2018/09/converted/825_585_4671-king-abdulaziz-university-kau-health-sciences-center-and-university-campus-project-4674.jpg") no-repeat center center fixed;
              background-size: cover;
            margin: 0;
            padding: 0;
        }

        .container {
            max-width: 1200px;
            margin: auto;
            padding: 30px;
            background: white;
            box-shadow: 0 0 15px rgba(0,0,0,0.1);
        }

        h2 {
            color: #1a3e1a;
            border-bottom: 2px solid #2d5a2d;
            padding-bottom: 10px;
        }

        .section {
            margin-top: 30px;
        }

        .section h3 {
            background-color: #2d5a2d;
            color: white;
            padding: 10px;
            margin: 0;
            border-radius: 5px 5px 0 0;
        }

        .grid-container {
            border: 1px solid #ccc;
            border-top: none;
            padding: 10px;
            border-radius: 0 0 5px 5px;
            background-color: #fafafa;
        }

        .auto-style-grid {
            width: 100%;
            border-collapse: collapse;
        }

        .auto-style-grid th {
            background-color: #3a723a;
            color: white;
            padding: 10px;
            border: 1px solid #ddd;
        }

        .auto-style-grid td {
            padding: 8px;
            border: 1px solid #ddd;
            text-align: center;
        }

        .menu-bar {
            background-color: #1a3e1a;
            color: white;
            padding: 15px;
            text-align: center;
            font-size: 20px;
            letter-spacing: 1px;
        }

        .status-yes {
            background-color: #dff5e1 !important;
            color: #27ae60;
            font-weight: bold;
        }

        .status-no {
            background-color: #fcebea !important;
            color: #e74c3c;
            font-weight: bold;
        }

        .expand-button {
            cursor: pointer;
            color: #2d5a2d;
            font-weight: bold;
            border: none;
            background: none;
        }

        .dropdown-row {
            display: none;
            background-color: #f9f9f9;
            font-size: 0.9em;
        }

        .dropdown-cell {
            padding: 10px;
            text-align: left;
            border: 1px solid #ddd;
        }
    </style>

    <script type="text/javascript">
        function toggleRow(uniqueId) {
            var row = document.getElementById('details-' + uniqueId);
            if (row) {
                row.style.display = row.style.display === 'table-row' ? 'none' : 'table-row';
            }
        }
        
        function generateUniqueId(prefix, value) {
            return prefix + '-' + value.replace(/\s+/g, '-');
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
                <center>
    <img src="https://fcitweb.kau.edu.sa/fcitwebsite/images/white-logo.png" alt="FCIT Logo" class="logo" width="1000" height="300"/>
</center>

        <div class="menu-bar">Senior Project Hub – Head of Department</div>
       
        <div class="container">
            <h2>Welcome, HOD</h2>

            <div class="section">
                <h3>📊 Group Functionality Roadmap</h3>
                <div class="grid-container">
                    <asp:GridView ID="gvRoadmap" runat="server"
    CssClass="auto-style-grid"
    AutoGenerateColumns="False"
    OnRowDataBound="gvRoadmap_RowDataBound">
    <Columns>
        <asp:TemplateField HeaderText="Group Code">
            <ItemTemplate>
                <button class="expand-button" onclick="toggleRow('<%# Eval("GroupCode") %>'); return false;">▼</button>
                <%# Eval("GroupCode") %>
            </ItemTemplate>
        </asp:TemplateField>

        <asp:BoundField DataField="Members" HeaderText="Members" />
        <asp:BoundField DataField="IdeaSubmitted" HeaderText="Idea Submitted" />
        <asp:BoundField DataField="IdeaApproved" HeaderText="Idea Approved" />
        <asp:BoundField DataField="SupervisorAssigned" HeaderText="Supervisor Assigned" />
        <asp:BoundField DataField="CPIS498Status" HeaderText="CPIS 498" />
        <asp:BoundField DataField="CPIS499Status" HeaderText="CPIS 499" />
    </Columns>
</asp:GridView>

                </div>

                <table id="extraRowsTable" class="auto-style-grid">
                    <asp:Literal ID="ltExtraRows" runat="server" />
                </table>
            </div>

            <div class="section">
                <h3>👥 All Student Groups</h3>
                <div class="grid-container">
                    <asp:GridView ID="gvGroups" runat="server" CssClass="auto-style-grid" AutoGenerateColumns="True" />
                </div>
            </div>

            <div class="section">
                <h3>📝 Final Grades Overview</h3>
                <div class="grid-container">
                    <asp:GridView ID="gvGrades" runat="server" CssClass="auto-style-grid" AutoGenerateColumns="True" />
                </div>
            </div>
        </div>
    </form>
</body>
</html>
