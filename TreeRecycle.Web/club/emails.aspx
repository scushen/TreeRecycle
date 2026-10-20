<%@ Page Title="Emails" Language="C#" MasterPageFile="~/club/Club.Master" AutoEventWireup="true" CodeBehind="emails.aspx.cs" Inherits="TreeRecycle.Web.ClubArea.Emails" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h1>Customer emails</h1>
    <p class="muted">Write these in your own words. Plain text only - line breaks are kept. Replies go to your club email.</p>
    <asp:Literal ID="litMsg" runat="server" />
    <div class="grid grid-2" style="align-items:start">
        <div class="card">
            <h2>Booking confirmation</h2>
            <p class="small muted">Sent automatically as soon as a customer pays.</p>
            <div class="field">
                <label for="<%= txtConfSubject.ClientID %>">Subject</label>
                <asp:TextBox ID="txtConfSubject" runat="server" ValidateRequestMode="Disabled" MaxLength="200" />
            </div>
            <div class="field">
                <label for="<%= txtConfBody.ClientID %>">Message</label>
                <asp:TextBox ID="txtConfBody" runat="server" ValidateRequestMode="Disabled" TextMode="MultiLine" Rows="18" style="min-height:360px" />
            </div>
            <div class="actions">
                <asp:Button ID="btnSaveConf" runat="server" Text="Save" CssClass="btn" CommandArgument="1" OnCommand="Save_Command" />
                <asp:Button ID="btnTestConf" runat="server" Text="Send me a test" CssClass="btn light" CommandArgument="1" OnCommand="Test_Command" />
                <asp:Button ID="btnResetConf" runat="server" Text="Reset to default" CssClass="btn danger small" CommandArgument="1" OnCommand="Reset_Command" OnClientClick="return confirm('Replace your text with the default?');" />
            </div>
        </div>
        <div class="card">
            <h2>Day-before reminder</h2>
            <p class="small muted">Sent when you press "Send reminder" on the Collection dates page.</p>
            <div class="field">
                <label for="<%= txtRemSubject.ClientID %>">Subject</label>
                <asp:TextBox ID="txtRemSubject" runat="server" ValidateRequestMode="Disabled" MaxLength="200" />
            </div>
            <div class="field">
                <label for="<%= txtRemBody.ClientID %>">Message</label>
                <asp:TextBox ID="txtRemBody" runat="server" ValidateRequestMode="Disabled" TextMode="MultiLine" Rows="18" style="min-height:360px" />
            </div>
            <div class="actions">
                <asp:Button ID="btnSaveRem" runat="server" Text="Save" CssClass="btn" CommandArgument="2" OnCommand="Save_Command" />
                <asp:Button ID="btnTestRem" runat="server" Text="Send me a test" CssClass="btn light" CommandArgument="2" OnCommand="Test_Command" />
                <asp:Button ID="btnResetRem" runat="server" Text="Reset to default" CssClass="btn danger small" CommandArgument="2" OnCommand="Reset_Command" OnClientClick="return confirm('Replace your text with the default?');" />
            </div>
        </div>
    </div>
    <div class="card">
        <h3>Placeholders</h3>
        <p class="small">These are swapped for the customer's details when the email is sent:</p>
        <p class="small"><asp:Literal ID="litTokens" runat="server" /></p>
    </div>
</asp:Content>
