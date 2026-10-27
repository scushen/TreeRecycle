<%@ Page Title="Clubs" Language="C#" MasterPageFile="~/admin/Admin.Master" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="TreeRecycle.Web.Admin.Clubs" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h1>Clubs</h1>
    <div class="grid grid-4">
        <div class="stat"><div class="n"><asp:Literal ID="litPending" runat="server" /></div><div class="l">Waiting for verification</div></div>
        <div class="stat"><div class="n"><asp:Literal ID="litLive" runat="server" /></div><div class="l">Live clubs</div></div>
        <div class="stat"><div class="n"><asp:Literal ID="litTrees" runat="server" /></div><div class="l">Trees booked</div></div>
        <div class="stat"><div class="n"><asp:Literal ID="litFees" runat="server" /></div><div class="l">Booking fees earned</div></div>
    </div>

    <div class="filters mt">
        <div class="field">
            <label for="<%= ddlStatus.ClientID %>">Show</label>
            <asp:DropDownList ID="ddlStatus" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlStatus_Changed">
                <asp:ListItem Value="0">Waiting for verification</asp:ListItem>
                <asp:ListItem Value="1">Approved</asp:ListItem>
                <asp:ListItem Value="3">Suspended</asp:ListItem>
                <asp:ListItem Value="2">Rejected</asp:ListItem>
                <asp:ListItem Value="">All</asp:ListItem>
            </asp:DropDownList>
        </div>
    </div>

    <div class="card" style="padding:0">
        <div class="tablewrap">
            <table class="data">
                <tr><th>Club</th><th>Contact</th><th>Registered</th><th>Status</th><th>Stripe</th><th class="num">Trees</th><th class="num">Fees</th><th></th></tr>
                <asp:Repeater ID="rptClubs" runat="server" ItemType="TreeRecycle.Web.Admin.ClubRow">
                    <ItemTemplate>
                        <tr>
                            <td><strong><%#: Item.Club.ClubName %></strong><div class="small muted"><%#: Item.Club.ClubType %> &middot; <%#: Item.Club.Town %>, <%#: Item.Club.Region %>, <%#: Item.Club.Country.Name %></div></td>
                            <td><%#: Item.Club.ContactName %><div class="small"><%#: Item.Club.ContactPhone %></div><div class="small muted"><%#: Item.Club.ContactEmail %></div></td>
                            <td class="nowrap"><%#: Item.Registered %></td>
                            <td><span class="pill <%# Item.StatusClass %>"><%#: Item.Club.StatusEnum %></span></td>
                            <td><%# Item.Club.StripeChargesEnabled ? "<span class=\"pill\">Ready</span>" : (Item.Club.StripeAccountId != "" ? "<span class=\"pill gold\">Started</span>" : "<span class=\"pill grey\">No</span>") %></td>
                            <td class="num"><%# Item.Trees %></td>
                            <td class="num"><%#: Item.Fees %></td>
                            <td class="actions"><a class="btn small" href="club.aspx?id=<%# Item.Club.ClubId %>"><%# Item.Club.StatusEnum == TreeRecycle.DataAccessObjects.ClubStatus.Pending ? "Review" : "Open" %></a>
                                <a class="btn small light" href="<%#: ResolveUrl("~/c/" + Item.Club.Slug) %>" target="_blank">Booking page</a></td>
                        </tr>
                    </ItemTemplate>
                </asp:Repeater>
            </table>
        </div>
        <asp:Literal ID="litEmpty" runat="server" />
    </div>
</asp:Content>
