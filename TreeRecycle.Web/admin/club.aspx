<%@ Page Title="Club" Language="C#" MasterPageFile="~/admin/Admin.Master" AutoEventWireup="true" CodeBehind="club.aspx.cs" Inherits="TreeRecycle.Web.Admin.ClubReview" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <p><a href="<%: ResolveUrl("~/admin/") %>">&larr; All clubs</a></p>
    <h1><asp:Literal ID="litName" runat="server" /> <asp:Literal ID="litStatus" runat="server" /></h1>
    <asp:Literal ID="litMsg" runat="server" />
    <div class="grid grid-2" style="align-items:start">
        <div class="card">
            <h2>Details</h2>
            <table class="data">
                <asp:Literal ID="litDetails" runat="server" />
            </table>
            <h3 class="mt">Checks</h3>
            <ul class="small">
                <li>Ring the contact number and confirm they're from the club.</li>
                <li>Does the website / Facebook page exist and match the name and address?</li>
                <li>Charities: <a href="https://www.charitiesregulator.ie/en/information-for-the-public/search-the-register-of-charities" target="_blank" rel="noopener">Register of Charities</a>. Companies: <a href="https://core.cro.ie/" target="_blank" rel="noopener">CRO</a>. Sports clubs: the governing body's club finder (GAA, IRFU, FAI...).</li>
                <li>Is the email on the club's own domain or a generic one? Generic is fine if everything else checks out.</li>
            </ul>
        </div>
        <div>
            <div class="card">
                <h2>Decision</h2>
                <div class="field">
                    <label for="<%= txtReason.ClientID %>">Reason (emailed to the club when rejecting or suspending)</label>
                    <asp:TextBox ID="txtReason" runat="server" TextMode="MultiLine" MaxLength="500" style="min-height:80px" />
                </div>
                <div class="actions">
                    <asp:Button ID="btnApprove" runat="server" Text="Approve" CssClass="btn" OnClick="btnApprove_Click" />
                    <asp:Button ID="btnReject" runat="server" Text="Reject" CssClass="btn danger" OnClick="btnReject_Click" OnClientClick="return confirm('Reject this club?');" />
                    <asp:Button ID="btnSuspend" runat="server" Text="Suspend" CssClass="btn danger" OnClick="btnSuspend_Click" OnClientClick="return confirm('Suspend this club? Their booking page goes offline immediately.');" />
                </div>
            </div>
            <div class="card">
                <h2>Activity</h2>
                <table class="data">
                    <asp:Literal ID="litActivity" runat="server" />
                </table>
                <p class="mt small">Booking page: <asp:HyperLink ID="lnkPublicUrl" runat="server" Target="_blank" /></p>
                <p><asp:HyperLink ID="lnkPublic" runat="server" Target="_blank" CssClass="btn light small">Open booking page</asp:HyperLink></p>
                <p class="small muted">Admins can preview the page even before the club is approved or live. Payment is switched off in preview.</p>
            </div>
            <div class="card">
                <h2>Emails sent</h2>
                <asp:Literal ID="litEmails" runat="server" />
            </div>
            <div class="card">
                <h2>Private notes</h2>
                <asp:TextBox ID="txtNotes" runat="server" TextMode="MultiLine" MaxLength="2000" />
                <asp:Button ID="btnNotes" runat="server" Text="Save notes" CssClass="btn light small mt" OnClick="btnNotes_Click" />
            </div>
        </div>
    </div>
</asp:Content>
