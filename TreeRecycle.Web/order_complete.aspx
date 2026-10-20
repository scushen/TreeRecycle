<%@ Page Title="Booking confirmed" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="order_complete.aspx.cs" Inherits="TreeRecycle.Web.OrderComplete" %>
<asp:Content ContentPlaceHolderID="HeadContent" runat="server"><meta name="robots" content="noindex" /></asp:Content>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="card narrow" style="margin:0 auto">
        <asp:PlaceHolder ID="phPaid" runat="server" Visible="false">
            <h1>You're booked in!</h1>
            <p>Thanks for supporting <strong><asp:Literal ID="litClub" runat="server" /></strong>. We've emailed a confirmation to <strong><asp:Literal ID="litEmail" runat="server" /></strong>.</p>
            <table class="data">
                <tr><td>Reference</td><td><strong><asp:Literal ID="litRef" runat="server" /></strong></td></tr>
                <tr><td>Tree</td><td><asp:Literal ID="litTree" runat="server" /></td></tr>
                <tr><td>Collection</td><td><asp:Literal ID="litDate" runat="server" /></td></tr>
                <tr><td>Address</td><td><asp:Literal ID="litAddress" runat="server" /></td></tr>
                <tr><td>Paid</td><td><asp:Literal ID="litPaid" runat="server" /></td></tr>
            </table>
            <div class="msg warn mt"><strong>On the day:</strong> have your tree in your garden or driveway by 8am with all decorations and the stand removed. The crew won't be able to come back for it.</div>
        </asp:PlaceHolder>
        <asp:PlaceHolder ID="phPending" runat="server" Visible="false">
            <h1>Payment processing</h1>
            <p>Your payment is still being confirmed. You'll get an email as soon as it goes through - there's no need to pay again.</p>
        </asp:PlaceHolder>
        <asp:PlaceHolder ID="phUnknown" runat="server" Visible="false">
            <h1>We couldn't find that booking</h1>
            <p>If you've paid, check your email for a confirmation. Otherwise <a href="<%: ResolveUrl("~/") %>">start again</a>.</p>
        </asp:PlaceHolder>
    </div>
</asp:Content>
