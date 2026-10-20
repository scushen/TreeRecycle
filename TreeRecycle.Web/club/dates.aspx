<%@ Page Title="Collection dates" Language="C#" MasterPageFile="~/club/Club.Master" AutoEventWireup="true" CodeBehind="dates.aspx.cs" Inherits="TreeRecycle.Web.ClubArea.Dates" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h1>Collection days and reminders</h1>
    <p class="muted">Customers choose one of your active days when they book. Send the reminder the day before - it tells customers to have the tree out by 8am, or forfeit the collection.</p>
    <asp:Literal ID="litMsg" runat="server" />
    <div class="card">
        <div class="tablewrap">
            <table class="data">
                <tr><th>Day</th><th class="num">Trees booked</th><th class="num">Limit</th><th>Taking bookings</th><th>Reminder</th><th></th></tr>
                <asp:Repeater ID="rptDates" runat="server" ItemType="TreeRecycle.Web.ClubArea.DateRow" OnItemCommand="rptDates_ItemCommand">
                    <ItemTemplate>
                        <tr>
                            <td><strong><%#: Item.Date.DisplayName %></strong></td>
                            <td class="num"><%# Item.Booked %></td>
                            <td class="num"><%# Item.Date.MaxTrees == 0 ? "No limit" : Item.Date.MaxTrees.ToString() %></td>
                            <td><%# Item.Date.Active ? "<span class=\"pill\">Yes</span>" : "<span class=\"pill grey\">No</span>" %></td>
                            <td class="small"><%#: Item.ReminderText %></td>
                            <td class="actions">
                                <asp:Button runat="server" CssClass="btn small berry" Text='<%# "Send reminder (" + Item.ToRemind + ")" %>' CommandName="remind" CommandArgument="<%# Item.Date.ClubCollectionDateId %>" Visible="<%# Item.ToRemind > 0 %>" OnClientClick='<%# "return confirm(\"Email " + Item.ToRemind + " customer(s) now?\");" %>' />
                                <a class="btn small light" href="routes.aspx?date=<%# Item.Date.ClubCollectionDateId %>">Routes</a>
                                <asp:Button runat="server" CssClass="btn small light" Text='<%# Item.Date.Active ? "Stop bookings" : "Reopen" %>' CommandName="toggle" CommandArgument="<%# Item.Date.ClubCollectionDateId %>" />
                            </td>
                        </tr>
                    </ItemTemplate>
                </asp:Repeater>
            </table>
        </div>
        <asp:Literal ID="litEmpty" runat="server" />
    </div>

    <div class="card narrow">
        <h2>Add a collection day</h2>
        <div class="row3">
            <div class="field">
                <label for="<%= txtDate.ClientID %>">Date</label>
                <asp:TextBox ID="txtDate" runat="server" TextMode="Date" />
            </div>
            <div class="field">
                <label for="<%= txtLabel.ClientID %>">Note (optional)</label>
                <asp:TextBox ID="txtLabel" runat="server" MaxLength="100" placeholder="e.g. North side" />
            </div>
            <div class="field">
                <label for="<%= txtMax.ClientID %>">Tree limit</label>
                <asp:TextBox ID="txtMax" runat="server" TextMode="Number" min="0" Text="0" />
                <div class="hint">0 = no limit</div>
            </div>
        </div>
        <asp:Button ID="btnAdd" runat="server" Text="Add day" CssClass="btn light" OnClick="btnAdd_Click" />
    </div>
</asp:Content>
