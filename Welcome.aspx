<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Welcome.aspx.cs" Inherits="SeniorProjectHub3.Welcome" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Senior Project Hub - Welcome</title>
    <style>
        body {
            font-family: 'Arial', sans-serif;
            margin: 0;
            padding: 0;
            background: #f8f9fa;
            color: #2c3e50;
        }

        .header {
            background: #007bff;
            color: white;
            padding: 80px 0;
            text-align: center;
        }

        .container {
            max-width: 1200px;
            margin: 0 auto;
            padding: 40px 20px;
        }

        .project-info {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 30px;
            margin-top: 40px;
        }

        .card {
            background: white;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
            transition: transform 0.3s ease;
        }

        .card:hover {
            transform: translateY(-5px);
        }

        .cta-button {
            display: inline-block;
            background: #28a745;
            color: white;
            padding: 15px 40px;
            border-radius: 25px;
            text-decoration: none;
            margin-top: 30px;
            transition: background 0.3s ease;
        }

        .cta-button:hover {
            background: #218838;
        }

        .features {
            margin: 50px 0;
            text-align: center;
        }

        .feature-icon {
            font-size: 2.5rem;
            color: #007bff;
            margin-bottom: 20px;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="header">
            <div class="container">
                <h1>Senior Project Hub</h1>
                <p>Connecting FCIT Students and Professors for Better Project Collaboration</p>
                <asp:HyperLink ID="lnkLogin" runat="server" NavigateUrl="login.aspx" CssClass="cta-button">Get Started</asp:HyperLink>
            </div>
        </div>

        <div class="container">
            <div class="features">
                <h2>Why Choose Senior Project Hub?</h2>
                <div class="project-info">
                    <div class="card">
                        <div class="feature-icon">💡</div>
                        <h3>Discover Projects</h3>
                        <p>Browse and apply to professor-curated project ideas in your field of interest</p>
                    </div>
                    
                    <div class="card">
                        <div class="feature-icon">📚</div>
                        <h3>Submit Ideas</h3>
                        <p>Propose your own project ideas and find interested supervisors</p>
                    </div>

                    <div class="card">
                        <div class="feature-icon">🤝</div>
                        <h3>Collaborate Easily</h3>
                        <p>Integrated communication tools and progress tracking system</p>
                    </div>
                </div>
            </div>

            <div class="about-section">
                <h2>About the Project</h2>
                <div class="card">
                    <p>The Senior Project Hub revolutionizes how FCIT students and professors connect for graduation projects. Our platform eliminates the need for physical office visits by providing:</p>
                    <ul>
                        <li>Centralized project listings</li>
                        <li>Digital application system</li>
                        <li>Real-time communication</li>
                        <li>Progress tracking features</li>
                    </ul>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
