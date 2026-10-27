<%@ Page Title="Prices" Language="C#" MasterPageFile="~/club/Club.Master" AutoEventWireup="true" CodeBehind="prices.aspx.cs" Inherits="TreeRecycle.Web.ClubArea.Prices" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h1>Tree sizes and prices</h1>
    <p class="muted">Customers pick one of these. On top of your price they pay a <asp:Literal ID="litFee" runat="server" /> booking fee per tree, which goes to us. Stripe's card fee comes out of your payout.</p>
    <asp:Literal ID="litMsg" runat="server" />
    <div class="card">
        <div class="tablewrap">
            <table class="data">
                <tr><th>Label shown to customers</th><th>Size (ft)</th><th>Your price (<%: Country.CurrencySymbol %>)</th><th>Customer pays</th><th>Order</th><th>Offered</th></tr>
                <asp:Repeater ID="rptPrices" runat="server" ItemType="TreeRecycle.DataAccessObjects.ClubPrice">
                    <ItemTemplate>
                        <tr>
                            <td><asp:HiddenField ID="hfId" runat="server" Value="<%# Item.ClubPriceId %>" />
                                <asp:TextBox ID="txtLabel" runat="server" Text="<%# Item.Label %>" MaxLength="60" /></td>
                            <td style="width:90px"><asp:TextBox ID="txtFeet" runat="server" Text="<%# Item.SizeFeet %>" TextMode="Number" min="1" max="30" /></td>
                            <td style="width:120px"><asp:TextBox ID="txtPrice" runat="server" Text='<%# Item.Price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) %>' /></td>
                            <td class="nowrap"><%#: CustomerPays(Item.Price) %></td>
                            <td style="width:80px"><asp:TextBox ID="txtSort" runat="server" Text="<%# Item.SortOrder %>" TextMode="Number" /></td>
                            <td><asp:CheckBox ID="chkActive" runat="server" Checked="<%# Item.Active %>" /></td>
                        </tr>
                    </ItemTemplate>
                </asp:Repeater>
            </table>
        </div>
        <div class="actions mt"><asp:Button ID="btnSave" runat="server" Text="Save prices" CssClass="btn" OnClick="btnSave_Click" /></div>
        <p class="hint">To stop offering a size, untick "Offered". Sizes are never deleted, so past orders keep their details.</p>
    </div>

    <div class="card narrow">
        <h2>Add a size</h2>
        <div class="row3">
            <div class="field">
                <label for="<%= txtNewLabel.ClientID %>">Label</label>
                <asp:TextBox ID="txtNewLabel" runat="server" MaxLength="60" placeholder="e.g. 9ft and over" />
            </div>
            <div class="field">
                <label for="<%= txtNewFeet.ClientID %>">Size (ft)</label>
                <asp:TextBox ID="txtNewFeet" runat="server" TextMode="Number" min="1" max="30" />
            </div>
            <div class="field">
                <label for="<%= txtNewPrice.ClientID %>">Price (<%: Country.CurrencySymbol %>)</label>
                <asp:TextBox ID="txtNewPrice" runat="server" />
            </div>
        </div>
        <asp:Button ID="btnAdd" runat="server" Text="Add size" CssClass="btn light" OnClick="btnAdd_Click" />
    </div>
</asp:Content>
