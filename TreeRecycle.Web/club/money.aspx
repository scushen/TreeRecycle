<%@ Page Title="Money" Language="C#" MasterPageFile="~/club/Club.Master" AutoEventWireup="true" CodeBehind="money.aspx.cs" Inherits="TreeRecycle.Web.ClubArea.MoneyPage" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h1>Money</h1>
    <asp:Literal ID="litMsg" runat="server" />
    <div class="grid grid-2" style="align-items:start">
        <div class="card">
            <h2>What you've raised</h2>
            <table class="data">
                <tr><td>Collections sold (<asp:Literal ID="litTrees" runat="server" /> trees)</td><td class="num"><asp:Literal ID="litGross" runat="server" /></td></tr>
                <tr><td>Stripe card fees</td><td class="num">- <asp:Literal ID="litStripe" runat="server" /></td></tr>
                <tr><td>Your costs (below)</td><td class="num">- <asp:Literal ID="litExpenses" runat="server" /></td></tr>
                <tr><td><strong>Raised for the club</strong></td><td class="num"><strong><asp:Literal ID="litNet" runat="server" /></strong></td></tr>
            </table>
            <p class="small muted mt">Customers also paid <asp:Literal ID="litPlatform" runat="server" /> in booking fees, which doesn't come out of your money. Refunded orders aren't counted. Payouts reach your bank from Stripe on its normal schedule - see your Stripe dashboard.</p>
        </div>
        <div class="card">
            <h2>Costs</h2>
            <p class="small muted">Skip or trailer hire, diesel, refreshments for the crew... anything you want taken off the total.</p>
            <div class="tablewrap">
                <table class="data">
                    <asp:Repeater ID="rptExpenses" runat="server" ItemType="TreeRecycle.DataAccessObjects.ClubExpense" OnItemCommand="rptExpenses_ItemCommand">
                        <ItemTemplate>
                            <tr>
                                <td><%#: Item.Description %></td>
                                <td class="num"><%#: Money(Item.Amount) %></td>
                                <td class="right"><asp:Button runat="server" CssClass="btn small danger" Text="Remove" CommandName="del" CommandArgument="<%# Item.ClubExpenseId %>" /></td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </table>
            </div>
            <div class="row2 mt">
                <div class="field">
                    <label for="<%= txtDesc.ClientID %>">Description</label>
                    <asp:TextBox ID="txtDesc" runat="server" MaxLength="200" />
                </div>
                <div class="field">
                    <label for="<%= txtAmount.ClientID %>">Amount (€)</label>
                    <asp:TextBox ID="txtAmount" runat="server" />
                </div>
            </div>
            <asp:Button ID="btnAdd" runat="server" Text="Add cost" CssClass="btn light" OnClick="btnAdd_Click" />
        </div>
    </div>
</asp:Content>
