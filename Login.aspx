<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="SeniorProjectHub3.Login" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Login - FCIT KAU</title>
  <style>
    body {
      font-family: Arial, sans-serif;
      background: url("https://smapse.com/storage/2018/09/converted/825_585_4671-king-abdulaziz-university-kau-health-sciences-center-and-university-campus-project-4674.jpg") no-repeat center center fixed;
      background-size: cover;
      margin: 0;
      min-height: 100vh;
    }
    .login-container {
      width: 350px;
      background-color: #fff;
      margin: 100px auto;
      padding: 30px;
      border-radius: 8px;
      box-shadow: 0 0 15px rgba(0,0,0,0.2);
      text-align: center;
    }
    .login-container img.logo {
      width: 150px;
      margin-bottom: 20px;
    }
    .login-container h2 {
      margin-bottom: 20px;
      color: #333;
    }
    .login-container input[type="text"],
    .login-container input[type="password"] {
      width: 100%;
      padding: 10px;
      margin: 10px 0;
      border: 1px solid #ccc;
      border-radius: 4px;
      font-size: 14px;
    }
    .login-container input[type="submit"],
    .login-container button {
      width: 100%;
      padding: 10px;
      background-color: #006400; 
      border: none;
      border-radius: 4px;
      color: #fff;
      font-size: 16px;
      cursor: pointer;
      margin-top: 10px;
    }
    .login-container input[type="submit"]:hover,
    .login-container button:hover {
      background-color: #004d00; 
    }
    .login-container .links {
      margin-top: 15px;
    }
    .login-container .links a {
      color: #006400; 
      text-decoration: none;
      margin: 0 5px;
    }
    .login-container .links a:hover {
      text-decoration: underline;
    }
    .error {
      color: red;
      font-weight: bold;
      margin-top: 10px;
    }
  </style>

</head>
<body>
  <form id="form1" runat="server">
    <div class="login-container">
      <img src="https://fcitweb.kau.edu.sa/fcitwebsite/images/white-logo.png" alt="FCIT Logo" class="logo" />
      <h2>Login</h2>
      <asp:Label ID="lblMessage" runat="server" Text="Please log in:" Visible="false"></asp:Label>
      <asp:TextBox ID="txtEmail" runat="server" Placeholder="Email"></asp:TextBox>
      <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" Placeholder="Password"></asp:TextBox>
      <asp:Button ID="btnLogin" runat="server" Text="Login" OnClick="btnSignIn_Click" />
      <div class="links">
        <a href="Signup.aspx">Register</a>
      </div>
      <asp:Label ID="lblError" runat="server" CssClass="error"></asp:Label>
      <asp:Label ID="lblSignInError" runat="server" CssClass="error"></asp:Label>
    </div>
  </form>
</body>
</html>