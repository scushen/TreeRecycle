<%@ Page Title="Club login" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="login.aspx.cs" Inherits="TreeRecycle.Web.Login" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="card narrow" style="margin:0 auto;max-width:440px">
        <h1>Club login</h1>
        <asp:Literal ID="litMsg" runat="server" />
        <asp:Panel ID="pnlLogin" runat="server" DefaultButton="btnLogin">
            <div class="field">
                <label for="<%= txtEmail.ClientID %>">Email</label>
                <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="200" autocomplete="username" />
            </div>
            <div class="field">
                <label for="<%= txtPassword.ClientID %>">Password</label>
                <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" MaxLength="100" autocomplete="current-password" />
            </div>
            <div class="actions">
                <asp:Button ID="btnLogin" runat="server" Text="Log in" CssClass="btn" OnClick="btnLogin_Click" />
                <a href="<%: ResolveUrl("~/forgot.aspx") %>" class="small">Forgot password?</a>
            </div>
        </asp:Panel>
        <p class="small muted mt">New here? <a href="<%: ResolveUrl("~/register.aspx") %>">Register your club</a>.</p>
    </div>
</asp:Content>
