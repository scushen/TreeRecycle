<%@ Page Title="Orders" Language="C#" MasterPageFile="~/club/Club.Master" AutoEventWireup="true" CodeBehind="orders.aspx.cs" Inherits="TreeRecycle.Web.ClubArea.Orders" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h1>Orders</h1>
    <asp:Literal ID="litMsg" runat="server" />
    <div class="filters no-print">
        <div class="field">
            <label for="<%= ddlDate.ClientID %>">Collection day</label>
            <asp:DropDownList ID="ddlDate" runat="server" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed" />
        </div>
        <div class="field">
            <label for="<%= ddlStatus.ClientID %>">Status</label>
            <asp:DropDownList ID="ddlStatus" runat="server" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
                <asp:ListItem Value="">All</asp:ListItem>
                <asp:ListItem Value="1">To collect</asp:ListItem>
                <asp:ListItem Value="2">Collected</asp:ListItem>
                <asp:ListItem Value="3">Not out</asp:ListItem>
                <asp:ListItem Value="5">Refunded</asp:ListItem>
            </asp:DropDownList>
        </div>
        <div class="field">
            <label for="<%= ddlSort.ClientID %>">Sort by</label>
            <asp:DropDownList ID="ddlSort" runat="server" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
                <asp:ListItem Value="route">Route (run, then stop)</asp:ListItem>
                <asp:ListItem Value="distance">Distance from club</asp:ListItem>
                <asp:ListItem Value="postcode">Postcode</asp:ListItem>
                <asp:ListItem Value="newest">Newest first</asp:ListItem>
            </asp:DropDownList>
        </div>
        <asp:Panel ID="pnlSearch" runat="server" DefaultButton="btnSearch" CssClass="field">
            <label for="<%= txtSearch.ClientID %>">Search</label>
            <asp:TextBox ID="txtSearch" runat="server" placeholder="Name, ref, postcode, phone" />
        </asp:Panel>
        <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn light" OnClick="Filter_Changed" />
        <asp:Button ID="btnCsv" runat="server" Text="Download CSV" CssClass="btn light" OnClick="btnCsv_Click" />
    </div>
    <p class="small muted"><asp:Literal ID="litSummary" runat="server" /></p>
    <div class="card" style="padding:0">
        <div class="tablewrap">
            <table class="data">
                <tr><th>Run</th><th>Ref</th><th>Customer</th><th>Address</th><th>Tree</th><th>Day</th><th class="num">Paid</th><th>Status</th><th class="no-print"></th></tr>
                <asp:Repeater ID="rptOrders" runat="server" ItemType="TreeRecycle.DataAccessObjects.TreeOrder" OnItemCommand="rptOrders_ItemCommand">
                    <ItemTemplate>
                        <tr>
                            <td class="nowrap"><%# Item.RunNumber == 0 ? "-" : Item.RunNumber + "." + Item.RunSequence %></td>
                            <td class="nowrap"><%#: Item.OrderRef %></td>
                            <td><%#: Item.CustomerName %><div class="small"><a href="tel:<%#: Item.CustomerPhone.Replace(" ", "") %>"><%#: Item.CustomerPhone %></a></div><div class="small muted"><%#: Item.CustomerEmail %></div></td>
                            <td><%#: Item.FullAddress %><div class="small"><a href="<%#: Item.MapsLink %>" target="_blank" rel="noopener">Map</a> &middot; <%#: Country.Distance((double)Item.DistanceKm) %></div><%# Item.LocationNotes == "" ? "" : "<div class=\"small muted\">" + Enc(Item.LocationNotes) + "</div>" %></td>
                            <td><%#: Item.Quantity + " x " + Item.SizeLabel %></td>
                            <td class="nowrap"><%#: DateName(Item.ClubCollectionDateId) %></td>
                            <td class="num"><%#: Money(Item.ClubAmount) %></td>
                            <td><span class="pill <%# PillClass(Item) %>"><%#: Item.StatusText %></span><%# Item.StatusNote == "" ? "" : "<div class=\"small muted\">" + Enc(Item.StatusNote) + "</div>" %></td>
                            <td class="actions no-print">
                                <asp:Button runat="server" CssClass="btn small" Text="Collected" CommandName="collected" CommandArgument="<%# Item.TreeOrderId %>" Visible="<%# Item.IsOutstanding %>" />
                                <asp:Button runat="server" CssClass="btn small light" Text="Not out" CommandName="notout" CommandArgument="<%# Item.TreeOrderId %>" Visible="<%# Item.IsOutstanding %>" />
                                <asp:Button runat="server" CssClass="btn small light" Text="Undo" CommandName="undo" CommandArgument="<%# Item.TreeOrderId %>" Visible="<%# Item.StatusEnum == TreeRecycle.DataAccessObjects.OrderStatus.Collected || Item.StatusEnum == TreeRecycle.DataAccessObjects.OrderStatus.NotOut %>" />
                                <asp:Button runat="server" CssClass="btn small light" Text="Resend email" CommandName="resend" CommandArgument="<%# Item.TreeOrderId %>" Visible="<%# Item.IsSale %>" />
                                <asp:Button runat="server" CssClass="btn small danger" Text="Refund" CommandName="refund" CommandArgument="<%# Item.TreeOrderId %>" Visible="<%# Item.IsSale %>" OnClientClick="return confirm('Refund this order in full through Stripe? This cannot be undone.');" />
                            </td>
                        </tr>
                    </ItemTemplate>
                </asp:Repeater>
            </table>
        </div>
    </div>
</asp:Content>
