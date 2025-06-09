<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Signup.aspx.cs" Inherits="SeniorProjectHub3.Signup"  UnobtrusiveValidationMode="None"  %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Sign Up - FCIT KAU</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background: url("https://smapse.com/storage/2018/09/converted/825_585_4671-king-abdulaziz-university-kau-health-sciences-center-and-university-campus-project-4674.jpg") no-repeat center center fixed;
            background-size: cover;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
        }

        .container {
            background-color: #fff;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
            width: 400px;
            text-align: center;
        }

        h2 { margin-bottom: 20px; color: #333; }

        input, select {
            width: 100%;
            padding: 10px;
            margin: 10px 0;
            border: 1px solid #ccc;
            border-radius: 4px;
            font-size: 14px;
            display: block;
        }

        button {
            width: 100%;
            padding: 10px;
            background-color: #006400;
            color: #fff;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 16px;
        }

        button:hover { background-color: #004d00; }

        .login-link {
            margin-top: 10px;
            display: block;
            color: #006400;
            text-decoration: none;
        }
         .phone-input-wrapper {
    position: relative;
    margin: 10px 0;
  }
  .phone-prefix {
    position: absolute;
    top: 0; left: 0;
    height: 38px; line-height: 38px;
    padding: 0 12px;
    background: #eee;
    border: 1px solid #ccc;
    border-right: none;
    border-radius: 4px 0 0 4px;
    color: #555;
    font-size: 14px;
  }
  .phone-field {
    padding-left: 60px !important;
    width: calc(100% - 60px) !important;
    max-width: 200px;
    display: inline-block;
  }
        .error { color: red; font-size: 14px; margin-top: 10px; }
        .success { color: green; font-size: 14px; margin-top: 10px; }
        .logo { width: 150px; margin-bottom: 20px; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
          
            <img src="https://fcitweb.kau.edu.sa/fcitwebsite/images/white-logo.png" alt="FCIT Logo" class="logo" />
            <h2>Create an Account</h2>

            <asp:TextBox ID="txtFullName" runat="server" CssClass="input" Placeholder="Full Name"></asp:TextBox>
            <asp:TextBox ID="txtStudentID" runat="server" CssClass="input" Placeholder="Student ID"></asp:TextBox>
            <asp:TextBox ID="txtEmail" runat="server" CssClass="input" Placeholder="KAU Email"></asp:TextBox>
          <div class="phone-input-wrapper">
  <span class="phone-prefix">+966</span>
  <asp:TextBox 
    ID="txtPhone" 
    runat="server" 
    CssClass="input phone-field" 
    Placeholder="5xxxxxxxx" 
    MaxLength="9" />
  <asp:RegularExpressionValidator 
    ID="revPhone" 
    runat="server"
    ControlToValidate="txtPhone"
    ValidationExpression="^5\d{8}$"
    ErrorMessage="Enter 9 digits starting with 5"
    CssClass="error" 
    Display="Dynamic" />
</div>

          
            <asp:DropDownList ID="ddlMajor" runat="server" CssClass="input">
                <asp:ListItem Text="Select Major" Value="" />
                <asp:ListItem Text="Information Systems" Value="IS" />
                <asp:ListItem Text="Information Technology" Value="IT" />
                <asp:ListItem Text="Computer Science" Value="CS" />
            </asp:DropDownList>
            <asp:RequiredFieldValidator
    ID="rfvMajor"
    runat="server"
    ControlToValidate="ddlMajor"
    InitialValue=""
    ErrorMessage="* You must select a major"
    ForeColor="Red"
    Display="Dynamic" />


            <asp:DropDownList ID="ddlInterests" runat="server" CssClass="input">
    <asp:ListItem Text="Select Interest" Value="" />
    <asp:ListItem Text="Database" Value="Database" />
    <asp:ListItem Text="Machine Learning" Value="Machine Learning" />
    <asp:ListItem Text="Web Development" Value="Web Development" />
    <asp:ListItem Text="AI" Value="AI" />
    <asp:ListItem Text="Networking" Value="Networking" />
                <asp:ListItem Text="Other" Value="Other" />
</asp:DropDownList>


            <asp:TextBox ID="txtPassword" runat="server" CssClass="input" Placeholder="Password" TextMode="Password"></asp:TextBox>
            <asp:TextBox ID="txtConfirmPassword" runat="server" CssClass="input" Placeholder="Confirm Password" TextMode="Password"></asp:TextBox>

            <asp:Button ID="btnSignup" runat="server" Text="Sign Up" CssClass="button" OnClick="btnSignup_Click" />
            <asp:Label ID="lblMessage" runat="server" CssClass="error"></asp:Label>

            <asp:HyperLink ID="lnkLogin" runat="server" NavigateUrl="Login.aspx" CssClass="login-link">Already have an account? Log In</asp:HyperLink>
        </div>
    </form>
</body>
</html>