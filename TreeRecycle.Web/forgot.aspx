<%@ Page Title="Reset your password" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="forgot.aspx.cs" Inherits="TreeRecycle.Web.Forgot" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="card narrow" style="margin:0 auto;max-width:440px">
        <h1>Reset your password</h1>
        <asp:Literal ID="litMsg" runat="server" />
        <asp:Panel ID="pnlForm" runat="server" DefaultButton="btnSend">
            <div class="field">
                <label for="<%= txtEmail.ClientID %>">Club email</label>
                <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="200" />
            </div>
            <asp:Button ID="btnSend" runat="server" Text="Email me a reset link" CssClass="btn" OnClick="btnSend_Click" />
        </asp:Panel>
    </div>
</asp:Content>
