<%@ Page Title="Admin login" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="login.aspx.cs" Inherits="TreeRecycle.Web.Admin.AdminLogin" %>
<asp:Content ContentPlaceHolderID="HeadContent" runat="server"><meta name="robots" content="noindex" /></asp:Content>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="card narrow" style="margin:0 auto;max-width:440px">
        <h1>Admin</h1>
        <asp:Literal ID="litMsg" runat="server" />
        <asp:Panel ID="pnlLogin" runat="server" DefaultButton="btnLogin">
            <div class="field">
                <label for="<%= txtEmail.ClientID %>">Email</label>
                <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" autocomplete="username" />
            </div>
            <div class="field">
                <label for="<%= txtPassword.ClientID %>">Password</label>
                <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" autocomplete="current-password" />
            </div>
            <asp:Button ID="btnLogin" runat="server" Text="Log in" CssClass="btn" OnClick="btnLogin_Click" />
        </asp:Panel>
    </div>
</asp:Content>
