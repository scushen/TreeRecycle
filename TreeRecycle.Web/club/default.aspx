<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/club/Club.Master" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="TreeRecycle.Web.ClubArea.Dashboard" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h1>Dashboard</h1>

    <asp:PlaceHolder ID="phSetup" runat="server">
        <div class="card">
            <h2>Before you can take bookings</h2>
            <ul class="checklist">
                <asp:Literal ID="litBlockers" runat="server" />
            </ul>
        </div>
    </asp:PlaceHolder>

    <asp:PlaceHolder ID="phLive" runat="server">
        <div class="msg ok"><strong>You're live.</strong> Share your booking page: <a href="<%: PublicUrl %>" target="_blank"><%: PublicUrl %></a>
            <button type="button" class="btn small light" onclick="navigator.clipboard.writeText('<%: PublicUrl %>');this.textContent='Copied';">Copy link</button></div>
    </asp:PlaceHolder>

    <div class="grid grid-4">
        <div class="stat"><div class="n"><asp:Literal ID="litTrees" runat="server" /></div><div class="l">Trees booked</div></div>
        <div class="stat"><div class="n"><asp:Literal ID="litToCollect" runat="server" /></div><div class="l">Still to collect</div></div>
        <div class="stat"><div class="n"><asp:Literal ID="litCollected" runat="server" /></div><div class="l">Collected</div></div>
        <div class="stat"><div class="n"><asp:Literal ID="litNet" runat="server" /></div><div class="l">Raised so far (after fees and costs)</div></div>
    </div>

    <div class="grid grid-2 mt">
        <div class="card">
            <h2>Collection days</h2>
            <asp:Literal ID="litDates" runat="server" />
            <a class="btn light small" href="dates.aspx">Manage dates and reminders</a>
        </div>
        <div class="card">
            <h2>Latest bookings</h2>
            <asp:Literal ID="litLatest" runat="server" />
            <a class="btn light small" href="orders.aspx">All orders</a>
        </div>
    </div>
    <p class="small muted">Your booking page: <a href="<%: PublicUrl %>" target="_blank"><%: PublicUrl %></a> (you can preview it while logged in, even before you're live).</p>
</asp:Content>
