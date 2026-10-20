<%@ Page Title="Choose a new password" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="reset.aspx.cs" Inherits="TreeRecycle.Web.Reset" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="card narrow" style="margin:0 auto;max-width:440px">
        <h1>Choose a new password</h1>
        <asp:Literal ID="litMsg" runat="server" />
        <asp:Panel ID="pnlForm" runat="server" DefaultButton="btnSave">
            <div class="field">
                <label for="<%= txtPassword.ClientID %>">New password</label>
                <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" MaxLength="100" autocomplete="new-password" />
                <div class="hint">At least 10 characters.</div>
            </div>
            <div class="field">
                <label for="<%= txtPassword2.ClientID %>">Confirm</label>
                <asp:TextBox ID="txtPassword2" runat="server" TextMode="Password" MaxLength="100" autocomplete="new-password" />
            </div>
            <asp:Button ID="btnSave" runat="server" Text="Save password" CssClass="btn" OnClick="btnSave_Click" />
        </asp:Panel>
    </div>
</asp:Content>
