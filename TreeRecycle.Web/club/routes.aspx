<%@ Page Title="Routes" Language="C#" MasterPageFile="~/club/Club.Master" AutoEventWireup="true" CodeBehind="routes.aspx.cs" Inherits="TreeRecycle.Web.ClubArea.Routes" %>
<asp:Content ContentPlaceHolderID="HeadContent" runat="server">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/leaflet.min.css" />
</asp:Content>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="no-print">
        <h1>Collection routes</h1>
        <p class="muted">We split the day into trailer-sized runs (your load size is <asp:Literal ID="litCapacity" runat="server" /> trees - change it on Club details), each covering one part of your area, and order the stops in each run. Re-plan after late bookings come in.</p>
        <asp:Literal ID="litMsg" runat="server" />
        <div class="filters">
            <div class="field">
                <label for="<%= ddlDate.ClientID %>">Collection day</label>
                <asp:DropDownList ID="ddlDate" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlDate_Changed" />
            </div>
            <asp:Button ID="btnPlan" runat="server" Text="Plan runs" CssClass="btn" OnClick="btnPlan_Click" />
            <button type="button" class="btn light" onclick="window.print()">Print collection sheets</button>
        </div>
        <div id="map" class="mt"></div>
        <p class="small muted">Club shown as the black marker. Each colour is one run.</p>
    </div>

    <asp:Literal ID="litEmpty" runat="server" />

    <asp:Repeater ID="rptRuns" runat="server" ItemType="TreeRecycle.Web.ClubArea.RunView" OnItemCommand="rptRuns_ItemCommand">
        <ItemTemplate>
            <div class="card run mt">
                <div class="print-only"><strong><%#: ClubName %></strong> &middot; <%#: DayName %></div>
                <div class="actions" style="justify-content:space-between">
                    <h2 class="mb0"><span class="runchip" style="background:<%# Item.Colour %>"></span>Run <%# Item.Number %> &middot; <%# Item.Trees %> trees, <%# Item.Orders.Count %> stops</h2>
                    <div class="actions no-print">
                        <%# Item.LinksHtml %>
                        <asp:Button runat="server" CssClass="btn small" Text="Mark whole run collected" CommandName="collectrun" CommandArgument="<%# Item.Number %>" Visible="<%# Item.Outstanding > 0 %>" OnClientClick="return confirm('Mark every outstanding stop in this run as collected?');" />
                    </div>
                </div>
                <div class="tablewrap mt">
                    <table class="data">
                        <tr><th>#</th><th>Customer</th><th>Address</th><th>Postcode</th><th>Tree</th><th>Notes</th><th>Status</th><th>Done</th></tr>
                        <asp:Repeater runat="server" DataSource="<%# Item.Orders %>" ItemType="TreeRecycle.DataAccessObjects.TreeOrder">
                            <ItemTemplate>
                                <tr>
                                    <td><strong><%# Item.RunSequence %></strong></td>
                                    <td><%#: Item.CustomerName %><div class="small"><%#: Item.CustomerPhone %></div></td>
                                    <td><a href="<%#: Item.MapsLink %>" target="_blank" rel="noopener"><%#: Item.AddressLine1 %></a><%#: Item.AddressLine2 == "" ? "" : ", " + Item.AddressLine2 %>, <%#: Item.Town %></td>
                                    <td class="nowrap"><%#: Item.Postcode %></td>
                                    <td class="nowrap"><%#: Item.Quantity + " x " + Item.SizeLabel %></td>
                                    <td class="small"><%#: Item.LocationNotes %></td>
                                    <td><%#: Item.StatusText %></td>
                                    <td style="width:40px">&#9744;</td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </table>
                </div>
            </div>
        </ItemTemplate>
    </asp:Repeater>
</asp:Content>
<asp:Content ContentPlaceHolderID="Scripts" runat="server">
    <script src="https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/leaflet.min.js"></script>
    <script>
    (function () {
        var data = <%= MapJson %>;
        if (!data || !window.L) return;
        var map = L.map('map');
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 19, attribution: '&copy; OpenStreetMap contributors' }).addTo(map);
        var bounds = [[data.club.lat, data.club.lng]];
        L.circleMarker([data.club.lat, data.club.lng], { radius: 9, color: '#000', fillColor: '#000', fillOpacity: 1 }).addTo(map).bindPopup(data.club.name);
        data.stops.forEach(function (s) {
            bounds.push([s.lat, s.lng]);
            L.circleMarker([s.lat, s.lng], { radius: 7, color: '#fff', weight: 1.5, fillColor: s.colour, fillOpacity: 0.95 })
              .addTo(map).bindPopup('Run ' + s.run + ' stop ' + s.seq + '<br>' + s.label);
        });
        map.fitBounds(bounds, { padding: [24, 24] });
    })();
    </script>
</asp:Content>
