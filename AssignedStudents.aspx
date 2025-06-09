<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AssignedStudents.aspx.cs" Inherits="SeniorProjectHub3.AssignedStudents" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <title>Assigned Students</title>
    <link rel="stylesheet" type="text/css" href="styles.css" />
</head>
<body>
    <form id="form1" runat="server">
        <h1>Manage Assigned Students</h1>

        <asp:Label ID="lblAssignStatus" runat="server" CssClass="status-message"></asp:Label>

        <h3>Add a Student</h3>
        <table>
            <tr>
                <td>Student Name:</td>
                <td><asp:TextBox ID="txtStudentName" runat="server"></asp:TextBox></td>
            </tr>
            <tr>
                <td>Student Email:</td>
                <td><asp:TextBox ID="txtStudentEmail" runat="server"></asp:TextBox></td>
            </tr>
            <tr>
                <td colspan="2">
                    <asp:Button ID="btnAssignStudent" runat="server" Text="Assign Student" CssClass="button"
                        OnClick="btnAssignStudent_Click" />
                </td>
            </tr>
        </table>

        <h3>Assigned Students List</h3>
        <asp:GridView ID="gvAssignedStudents" runat="server" AutoGenerateColumns="False" CssClass="table">
            <Columns>
                <asp:BoundField DataField="FullName" HeaderText="Student Name" />
                <asp:BoundField DataField="StudentEmail" HeaderText="Email" />
                <asp:BoundField DataField="ProjectStatus" HeaderText="Project Status" />
            </Columns>
        </asp:GridView>
    </form>
</body>
</html>